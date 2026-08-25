using System.Collections.Generic;
using Game.Definition.Combat;
using Game.Foundation.Events;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 敌方战斗目标的唯一元素附着事实所有者；首版只启用一个主要槽。
    /// 生命周期由 Combatant 绑定，时间由 EnemyRoot 或等价权威驱动显式推进。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Combatant))]
    public sealed class ElementAttachmentRuntime : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private Combatant combatant;
        [SerializeField] private HealthComponent healthComponent;

        /// <summary>
        /// 当前目标生命周期内，按来源身份记录下一次允许成功施加的时间。
        /// 目标身份由本 Runtime 的生命周期边界保证，无需再复制进字典键。
        /// </summary>
        private readonly Dictionary<ElementApplicationSourceId, float> nextAllowedTimesBySource = new();

        /// <summary>
        /// 清理过期间隔时复用的临时键列表。Dictionary 遍历期间不能直接删除元素，
        /// 因此先收集键、遍历结束后再删除，并复用列表容量以避免每帧分配。
        /// </summary>
        private readonly List<ElementApplicationSourceId> expiredIntervalSourceIds = new();

        /// <summary>
        /// 当前目标生命周期内已经成功触发反应的攻击执行。
        /// 与伤害去重分离，因为伤害和元素是同一攻击的并列输出。
        /// </summary>
        private readonly HashSet<AttackExecutionId> reactedExecutionIds = new();

        /// <summary>首版唯一主要槽中的当前附着；默认值表示没有附着。</summary>
        private ElementAttachmentSnapshot primaryAttachment;

        /// <summary>
        /// 本组件已经确认绑定的 Combatant 生命周期身份。
        /// Combatant.Id 仍是权威身份；这里保存绑定快照，用于发现旧状态未随对象复用正确重置。
        /// </summary>
        private CombatantId boundTargetId;

        /// <summary>当前目标生命周期内最近分配的附着版本；每次成功附着或刷新前递增。</summary>
        private long nextAttachmentVersion;

        /// <summary>
        /// 当前目标生命周期是否已经接收过合法且非倒退的时间戳。
        /// 单独记录该状态，是为了区分“时间轴尚未建立”和“已经合法处理到 0 秒”。
        /// </summary>
        private bool hasEstablishedRuntimeTimeline;

        /// <summary>
        /// 当前目标生命周期已经处理到的最晚运行时时间。
        /// Tick、元素请求和附着消费都可能推进它；后续输入不得早于该时间。
        /// </summary>
        private float latestProcessedRuntimeTime;

        /// <summary>当前绑定的权威目标；未完成装配时为 null。</summary>
        public Combatant Owner => combatant;

        /// <summary>当前绑定的目标生命周期身份；禁用或未绑定时无效。</summary>
        public CombatantId BoundTargetId => boundTargetId;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (combatant != null && combatant.Id.IsValid)
            {
                // Runtime 可能单独重新启用，此时主动接入已有生命周期。
                // 正常整体启用时 Combatant 也会发起绑定；BeginTargetLifecycle 会幂等忽略重复调用。
                BeginTargetLifecycle(combatant, combatant.Id, Time.time);
            }
        }

        private void OnDisable()
        {
            // 支持 Runtime 被单独禁用；若 Combatant 同时结束生命周期，重复结束也保持幂等。
            EndTargetLifecycle(Time.time, ElementAttachmentChangeKind.TargetDisabled);
        }

        /// <summary>尝试读取首版主要附着槽。</summary>
        /// <param name="_attachment">当前存在时返回只读快照。</param>
        /// <returns>当前主要槽存在附着时返回 <see langword="true"/>。</returns>
        public bool TryGetPrimaryAttachment(out ElementAttachmentSnapshot _attachment)
        {
            _attachment = primaryAttachment;
            return primaryAttachment.IsValid;
        }

        /// <summary>
        /// 推进附着时间并提交到期、生命耗尽或生命重置清理。早于已处理时间的调用保持状态不变。
        /// </summary>
        /// <param name="_currentTime">与元素请求相同的运行时时间轴。</param>
        public void Tick(float _currentTime)
        {
            TryAdvanceTime(_currentTime, out _);
        }

        /// <summary>查询当前目标生命周期是否已经接受该执行的一次反应。</summary>
        internal bool HasCommittedReaction(
            AttackExecutionId _executionId,
            CombatantId _expectedTargetId)
        {
            return _executionId.IsValid
                && _expectedTargetId.IsValid
                && boundTargetId == _expectedTargetId
                && reactedExecutionIds.Contains(_executionId);
        }

        /// <summary>
        /// 原子提交一次已经匹配成功的反应：重验待反应版本与间隔，登记执行去重，
        /// 再记录触发来源间隔并消费已有附着。任一检查失败时不产生部分反应状态。
        /// </summary>
        internal bool TryCommitReaction(
            in ElementApplicationRequest _triggerRequest,
            in ElementAttachmentSnapshot _expectedAttachment,
            out ElementAttachmentSnapshot _consumedAttachment)
        {
            _consumedAttachment = default;

            // 请求在同一同步调用中刚完成目标、时间和接收资格裁决；这里只重验提交依赖的附着事实。
            if (_expectedAttachment.IsValid == false ||
                _expectedAttachment.TargetId != boundTargetId ||
                primaryAttachment.IsValid == false ||
                primaryAttachment.Version != _expectedAttachment.Version ||
                primaryAttachment.Element != _expectedAttachment.Element ||
                primaryAttachment.Element == _triggerRequest.Source.Element)
            {
                return false;
            }

            // 同一执行在当前 TargetId 生命周期只能成功反应一次，重复 Collider 或回调直接拒绝。
            if (reactedExecutionIds.Contains(_triggerRequest.ExecutionId))
            {
                return false;
            }

            // 触发元素仍受自己的来源—目标间隔约束；失败时不能提前登记执行或消费附着。
            if (IsSourceTargetIntervalAllowed(_triggerRequest) == false ||
                TryCalculateNextAllowedTime(_triggerRequest, out float nextAllowedTime) == false)
            {
                return false;
            }

            // 所有可能失败的检查必须早于以下写回；先登记去重，避免消费事件回调重入同一执行。
            reactedExecutionIds.Add(_triggerRequest.ExecutionId);
            RecordSourceTargetInterval(_triggerRequest.Source.SourceId, nextAllowedTime);
            _consumedAttachment = primaryAttachment;
            ClearPrimaryAttachment(
                ElementAttachmentChangeKind.Consumed,
                _triggerRequest.ApplicationTime);
            return true;
        }

        /// <summary>
        /// 把附着状态绑定到 Combatant 当前这一次活动生命周期。
        /// 相同 TargetId 的重复绑定保持不变；不同生命周期会先清除旧状态和间隔。
        /// </summary>
        internal void BeginTargetLifecycle(
            Combatant _combatant,
            CombatantId _targetId,
            float _currentTime)
        {
            ResolveReferences();
            if (_combatant == null ||
                _combatant != combatant ||
                _targetId.IsValid == false ||
                _combatant.Id != _targetId)
            {
                return;
            }

            if (boundTargetId == _targetId)
            {
                // Combatant.OnEnable 与本组件 OnEnable 都可能发起绑定，必须允许安全重复调用。
                return;
            }

            if (primaryAttachment.IsValid)
            {
                ClearPrimaryAttachment(
                    ElementAttachmentChangeKind.TargetDisabled,
                    ResolveLifecycleTime(_currentTime));
            }

            ResetRuntimeStorage();
            boundTargetId = _targetId;
        }

        /// <summary>
        /// 结束当前绑定，发布必要的最后一次清理事件，并清空附着、间隔、版本和时间轴。
        /// </summary>
        internal void EndTargetLifecycle(
            float _currentTime,
            ElementAttachmentChangeKind _changeKind)
        {
            if (boundTargetId.IsValid == false)
            {
                ResetRuntimeStorage();
                return;
            }

            if (primaryAttachment.IsValid)
            {
                ClearPrimaryAttachment(_changeKind, ResolveLifecycleTime(_currentTime));
            }

            ResetRuntimeStorage();
            boundTargetId = default;
        }

        /// <summary>
        /// 目标侧元素请求的唯一提交入口。依次完成目标生命周期核对、时间同步、接收资格、
        /// 完全重复识别、来源—目标间隔、异元素反应交接和最终附着写回。
        /// </summary>
        internal ElementApplicationResult ResolveAndApply(in ElementApplicationRequest _request)
        {
            // 旧请求不能推进或清理对象复用后的新生命周期，因此必须在时间同步前拦截。
            if (MatchesCurrentTarget(_request) == false)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.InvalidTarget,
                    primaryAttachment);
            }

            // 在读取当前槽前同步到请求时间，保证过期、死亡或重置状态已经被清理。
            ElementApplicationRejectionReason rejectionReason;
            if (TryAdvanceTime(_request.ApplicationTime, out rejectionReason) == false)
            {
                return ElementApplicationResult.Rejected(
                    rejectionReason,
                    primaryAttachment);
            }

            // 时间可以成功推进，但未初始化或生命耗尽的目标仍不能接收新附着。
            if (CanReceiveAttachment() == false)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.TargetCannotReceiveAttachment,
                    primaryAttachment);
            }

            // 提前验证两个时间加法，避免 Infinity 或浮点精度导致不可到期的运行时状态。
            float expiresAt = _request.ApplicationTime + _request.Source.AttachmentDurationSeconds;
            if (IsFinite(expiresAt) == false || expiresAt <= _request.ApplicationTime)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.InvalidAttachmentDuration,
                    primaryAttachment);
            }

            if (TryCalculateNextAllowedTime(_request, out float nextAllowedTime) == false)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.InvalidRequest,
                    primaryAttachment);
            }

            // 完全相同的请求可能因重复回调或重试再次到达；它已经体现在当前状态中。
            // 此判断必须早于间隔检查，使重复提交返回 Unchanged 而不是冷却拒绝。
            if (primaryAttachment.RepresentsSameApplication(_request, expiresAt))
            {
                return ElementApplicationResult.Unchanged(primaryAttachment);
            }

            // 来源—目标间隔只约束新的施加尝试，不是全局元素或反应冷却。
            if (IsSourceTargetIntervalAllowed(_request) == false)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.SourceTargetIntervalActive,
                    primaryAttachment);
            }

            if (primaryAttachment.IsValid && primaryAttachment.Element != _request.Source.Element)
            {
                // 不同元素只交出“已有附着 + 触发请求”。ELM-030 会按版本原子判定和消费，
                // 本阶段不提前写入间隔或改变槽，否则反应失败时无法恢复原状态。
                return ElementApplicationResult.ReactionRequired(primaryAttachment);
            }

            // 空槽提交 Attached；已有同元素提交 Refreshed，并用最新合法请求替换来源快照。
            ElementAttachmentSnapshot previousAttachment = primaryAttachment;
            ElementApplicationResolutionStatus status = previousAttachment.IsValid
                ? ElementApplicationResolutionStatus.Refreshed
                : ElementApplicationResolutionStatus.Attached;
            ElementAttachmentChangeKind changeKind = previousAttachment.IsValid
                ? ElementAttachmentChangeKind.Refreshed
                : ElementAttachmentChangeKind.Attached;

            nextAttachmentVersion++;
            primaryAttachment = new ElementAttachmentSnapshot(
                nextAttachmentVersion,
                _request,
                expiresAt);
            RecordSourceTargetInterval(_request.Source.SourceId, nextAllowedTime);
            PublishChange(
                changeKind,
                previousAttachment,
                primaryAttachment,
                _request.ApplicationTime);

            return ElementApplicationResult.Committed(
                status,
                primaryAttachment);
        }

        /// <summary>判断请求冻结的目标引用和身份是否仍属于本 Runtime 当前生命周期。</summary>
        /// <param name="_request">已经由元素请求工厂建立并交给目标处理的请求。</param>
        /// <returns>目标引用和 TargetId 都匹配当前绑定时返回 <see langword="true"/>。</returns>
        private bool MatchesCurrentTarget(
            in ElementApplicationRequest _request)
        {
            return _request.TargetCombatant == combatant
                && _request.TargetId == boundTargetId;
        }

        /// <summary>
        /// 把运行时同步到指定的非倒退时间：清理过期间隔，并根据 Health 或到期时间
        /// 清除无效附着。它只负责“时间与现有状态同步”，新附着资格由调用方随后判断。
        /// </summary>
        /// <returns>目标绑定与时间都合法、时间轴成功推进时返回 <see langword="true"/>。</returns>
        private bool TryAdvanceTime(
            float _currentTime,
            out ElementApplicationRejectionReason _rejectionReason)
        {
            // 只有已启用且仍绑定到 Combatant 当前身份的 Runtime 才能推进自己的时间轴。
            if (isActiveAndEnabled == false ||
                combatant == null ||
                boundTargetId.IsValid == false ||
                combatant.MatchesCurrentIdentity(boundTargetId) == false)
            {
                _rejectionReason = ElementApplicationRejectionReason.AttachmentOwnerNotReady;
                return false;
            }

            // NaN、Infinity 和负数不能进入任何运行时计时状态。
            if (IsFinite(_currentTime) == false || _currentTime < 0f)
            {
                _rejectionReason = ElementApplicationRejectionReason.InvalidApplicationTime;
                return false;
            }

            // 所有入口共享一条单调时间轴，防止迟到请求让附着或间隔回到过去。
            if (hasEstablishedRuntimeTimeline && _currentTime < latestProcessedRuntimeTime)
            {
                _rejectionReason = ElementApplicationRejectionReason.StaleApplicationTime;
                return false;
            }

            hasEstablishedRuntimeTimeline = true;
            latestProcessedRuntimeTime = _currentTime;
            PruneExpiredIntervals(_currentTime);

            // Health 重置和生命耗尽都会终止本生命周期内现有的附着与应用间隔。
            if (healthComponent == null || healthComponent.IsInitialized == false)
            {
                if (primaryAttachment.IsValid)
                {
                    ClearPrimaryAttachment(ElementAttachmentChangeKind.TargetReset, _currentTime);
                }

                nextAllowedTimesBySource.Clear();
            }
            else if (healthComponent.IsHealthDepleted)
            {
                if (primaryAttachment.IsValid)
                {
                    ClearPrimaryAttachment(ElementAttachmentChangeKind.TargetDepleted, _currentTime);
                }

                nextAllowedTimesBySource.Clear();
            }
            else if (primaryAttachment.IsValid && _currentTime >= primaryAttachment.ExpiresAt)
            {
                // 自然到期只清除附着；来源间隔有自己的结束时间，不能在这里一并取消。
                ClearPrimaryAttachment(ElementAttachmentChangeKind.Expired, _currentTime);
            }

            _rejectionReason = ElementApplicationRejectionReason.None;
            return true;
        }

        private bool CanReceiveAttachment()
        {
            return combatant != null
                && combatant.Faction == CombatFaction.Enemy
                && healthComponent != null
                && healthComponent.IsInitialized
                && healthComponent.IsHealthDepleted == false;
        }

        private bool IsSourceTargetIntervalAllowed(in ElementApplicationRequest _request)
        {
            // 等于边界时间时允许再次施加，只有严格早于下一允许时间才拒绝。
            return nextAllowedTimesBySource.TryGetValue(
                    _request.Source.SourceId,
                    out float nextAllowedTime) == false
                || _request.ApplicationTime >= nextAllowedTime;
        }

        private static bool TryCalculateNextAllowedTime(
            in ElementApplicationRequest _request,
            out float _nextAllowedTime)
        {
            _nextAllowedTime =
                _request.ApplicationTime + _request.Source.SourceTargetIntervalSeconds;
            return IsFinite(_nextAllowedTime);
        }

        private void RecordSourceTargetInterval(
            ElementApplicationSourceId _sourceId,
            float _nextAllowedTime)
        {
            if (_nextAllowedTime <= latestProcessedRuntimeTime)
            {
                // 零间隔来源不保留无意义字典项。
                nextAllowedTimesBySource.Remove(_sourceId);
                return;
            }

            nextAllowedTimesBySource[_sourceId] = _nextAllowedTime;
        }

        private void PruneExpiredIntervals(float _currentTime)
        {
            if (nextAllowedTimesBySource.Count == 0)
            {
                return;
            }

            // 先收集再删除，避免在 foreach 枚举 Dictionary 时修改集合。
            expiredIntervalSourceIds.Clear();
            foreach (KeyValuePair<ElementApplicationSourceId, float> entry in nextAllowedTimesBySource)
            {
                if (_currentTime >= entry.Value)
                {
                    expiredIntervalSourceIds.Add(entry.Key);
                }
            }

            for (int i = 0; i < expiredIntervalSourceIds.Count; i++)
            {
                nextAllowedTimesBySource.Remove(expiredIntervalSourceIds[i]);
            }

            expiredIntervalSourceIds.Clear();
        }

        private void ClearPrimaryAttachment(
            ElementAttachmentChangeKind _changeKind,
            float _changeTime)
        {
            if (primaryAttachment.IsValid == false)
            {
                return;
            }

            // 先写回事实，再发布包含前后快照的已提交事件。
            ElementAttachmentSnapshot previousAttachment = primaryAttachment;
            primaryAttachment = default;
            PublishChange(
                _changeKind,
                previousAttachment,
                primaryAttachment,
                _changeTime);
        }

        private void PublishChange(
            ElementAttachmentChangeKind _changeKind,
            in ElementAttachmentSnapshot _previousAttachment,
            in ElementAttachmentSnapshot _currentAttachment,
            float _changeTime)
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (eventBus == null)
            {
                return;
            }

            CombatantId eventTargetId = _currentAttachment.IsValid
                ? _currentAttachment.TargetId
                : _previousAttachment.TargetId;
            eventBus.Publish(new ElementAttachmentChangedEvent(
                _changeKind,
                combatant,
                eventTargetId,
                _previousAttachment,
                _currentAttachment,
                _changeTime));
        }

        private void ResetRuntimeStorage()
        {
            // 这些状态全部只属于一个 CombatantId 生命周期，绝不能泄漏到对象池复用后的新身份。
            primaryAttachment = default;
            nextAllowedTimesBySource.Clear();
            expiredIntervalSourceIds.Clear();
            reactedExecutionIds.Clear();
            nextAttachmentVersion = 0L;
            hasEstablishedRuntimeTimeline = false;
            latestProcessedRuntimeTime = 0f;
        }

        private float ResolveLifecycleTime(float _requestedTime)
        {
            // 生命周期回调可能携带非法或略早的时间；事件时间不能早于已经处理过的状态。
            float resolvedTime = IsFinite(_requestedTime) && _requestedTime >= 0f
                ? _requestedTime
                : 0f;
            return hasEstablishedRuntimeTimeline
                ? Mathf.Max(latestProcessedRuntimeTime, resolvedTime)
                : resolvedTime;
        }

        private void ResolveReferences()
        {
            if (combatant == null)
            {
                combatant = GetComponent<Combatant>();
            }

            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }
        }

        private static bool IsFinite(float _value)
        {
            return float.IsNaN(_value) == false && float.IsInfinity(_value) == false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
