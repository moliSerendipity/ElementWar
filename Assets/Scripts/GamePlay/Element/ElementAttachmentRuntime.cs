using System;
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
        /// 按“来源生命周期 + 目标生命周期”记录下一次允许成功施加的时间。
        /// 值只在附着或同元素刷新真正提交后写入，不作为全局元素冷却。
        /// </summary>
        private readonly Dictionary<ElementApplicationIntervalKey, float> nextAllowedTimesByKey = new();

        /// <summary>
        /// 清理过期间隔时复用的临时键列表。Dictionary 遍历期间不能直接删除元素，
        /// 因此先收集键、遍历结束后再删除，并复用列表容量以避免每帧分配。
        /// </summary>
        private readonly List<ElementApplicationIntervalKey> expiredIntervalKeys = new();

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

        /// <summary>当前已启用的附着槽数量；首版只能为零或一。</summary>
        public int AttachmentCount => primaryAttachment.IsValid ? 1 : 0;

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

        /// <summary>
        /// 按只读索引查询当前附着。首版仅索引 0 有效，以便未来扩展为集合而不暴露可变容器。
        /// </summary>
        /// <param name="_index">要读取的槽索引。</param>
        /// <param name="_attachment">成功时返回对应附着快照。</param>
        /// <returns>索引存在且当前槽有效时返回 <see langword="true"/>。</returns>
        public bool TryGetAttachment(int _index, out ElementAttachmentSnapshot _attachment)
        {
            if (_index == 0 && primaryAttachment.IsValid)
            {
                _attachment = primaryAttachment;
                return true;
            }

            _attachment = default;
            return false;
        }

        /// <summary>尝试读取首版主要附着槽。</summary>
        /// <param name="_attachment">当前存在时返回只读快照。</param>
        /// <returns>当前主要槽存在附着时返回 <see langword="true"/>。</returns>
        public bool TryGetPrimaryAttachment(out ElementAttachmentSnapshot _attachment)
        {
            return TryGetAttachment(0, out _attachment);
        }

        /// <summary>
        /// 推进附着时间并提交到期、生命耗尽或生命重置清理。早于已处理时间的调用保持状态不变。
        /// </summary>
        /// <param name="_currentTime">与元素请求相同的运行时时间轴。</param>
        public void Tick(float _currentTime)
        {
            TryAdvanceTime(_currentTime, out _);
        }

        /// <summary>
        /// 仅在版本仍匹配时消费当前主要附着；迟到或重复消费者不能清除更新后的状态。
        /// </summary>
        /// <param name="_expectedVersion">调用方读取到的当前附着版本，必须大于零。</param>
        /// <param name="_currentTime">消费发生的运行时时间。</param>
        /// <param name="_consumedAttachment">成功时返回被清除的附着快照。</param>
        /// <returns>当前槽存在、尚未到期且版本匹配时返回 <see langword="true"/>。</returns>
        public bool TryConsumePrimary(
            long _expectedVersion,
            float _currentTime,
            out ElementAttachmentSnapshot _consumedAttachment)
        {
            _consumedAttachment = default;
            if (_expectedVersion <= 0L ||
                TryAdvanceTime(_currentTime, out _) == false ||
                primaryAttachment.IsValid == false ||
                primaryAttachment.Version != _expectedVersion)
            {
                return false;
            }

            _consumedAttachment = primaryAttachment;
            ClearPrimaryAttachment(ElementAttachmentChangeKind.Consumed, _currentTime);
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
        /// 目标侧元素请求的唯一提交入口。依次完成结构校验、时间同步、接收资格、
        /// 完全重复识别、来源—目标间隔、异元素反应交接和最终附着写回。
        /// </summary>
        internal ElementApplicationResult ResolveAndApply(in ElementApplicationRequest _request)
        {
            // 先验证不会改变运行时状态的结构、身份和配置字段。
            ElementApplicationRejectionReason rejectionReason = ValidateRequestStructure(_request);
            if (rejectionReason != ElementApplicationRejectionReason.None)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    rejectionReason,
                    primaryAttachment);
            }

            // 在读取当前槽前同步到请求时间，保证过期、死亡或重置状态已经被清理。
            if (TryAdvanceTime(_request.ApplicationTime, out rejectionReason) == false)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    rejectionReason,
                    primaryAttachment);
            }

            // 时间可以成功推进，但未初始化或生命耗尽的目标仍不能接收新附着。
            if (CanReceiveAttachment() == false)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    ElementApplicationRejectionReason.TargetCannotReceiveAttachment,
                    primaryAttachment);
            }

            // 提前验证两个时间加法，避免 Infinity 或浮点精度导致不可到期的运行时状态。
            float expiresAt = _request.ApplicationTime + _request.Source.AttachmentDurationSeconds;
            if (IsFinite(expiresAt) == false || expiresAt <= _request.ApplicationTime)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    ElementApplicationRejectionReason.InvalidAttachmentDuration,
                    primaryAttachment);
            }

            float nextAllowedTime =
                _request.ApplicationTime + _request.Source.SourceTargetIntervalSeconds;
            if (IsFinite(nextAllowedTime) == false)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    ElementApplicationRejectionReason.InvalidRequest,
                    primaryAttachment);
            }

            // 完全相同的请求可能因重复回调或重试再次到达；它已经体现在当前状态中。
            // 此判断必须早于间隔检查，使重复提交返回 Unchanged 而不是冷却拒绝。
            if (primaryAttachment.RepresentsSameApplication(_request, expiresAt))
            {
                return ElementApplicationResult.Unchanged(_request, primaryAttachment);
            }

            // 来源—目标间隔只约束新的施加尝试，不是全局元素或反应冷却。
            if (IsSourceTargetIntervalAllowed(_request) == false)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    ElementApplicationRejectionReason.SourceTargetIntervalActive,
                    primaryAttachment);
            }

            if (primaryAttachment.IsValid && primaryAttachment.Element != _request.Source.Element)
            {
                // 不同元素只交出“已有附着 + 触发请求”。ELM-030 会按版本原子判定和消费，
                // 本阶段不提前写入间隔或改变槽，否则反应失败时无法恢复原状态。
                return ElementApplicationResult.ReactionRequired(_request, primaryAttachment);
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
            RecordSourceTargetInterval(_request);
            PublishChange(
                changeKind,
                previousAttachment,
                primaryAttachment,
                _request.ApplicationTime);

            return ElementApplicationResult.Committed(
                status,
                _request,
                previousAttachment,
                primaryAttachment);
        }

        private ElementApplicationRejectionReason ValidateRequestStructure(
            in ElementApplicationRequest _request)
        {
            if (isActiveAndEnabled == false ||
                combatant == null ||
                boundTargetId.IsValid == false ||
                combatant.MatchesCurrentIdentity(boundTargetId) == false)
            {
                return ElementApplicationRejectionReason.AttachmentOwnerNotReady;
            }

            if (_request.Source.IsCreated == false ||
                _request.Source.SourceId.IsValid == false ||
                Enum.IsDefined(typeof(ElementType), _request.Source.Element) == false ||
                _request.Source.Element == ElementType.None ||
                _request.ExecutionId.IsValid == false ||
                _request.IntervalKey.IsValid == false)
            {
                return ElementApplicationRejectionReason.InvalidRequest;
            }

            if (_request.TargetCombatant != combatant ||
                _request.TargetId != boundTargetId ||
                _request.IntervalKey.TargetId != boundTargetId ||
                _request.IntervalKey.SourceId != _request.Source.SourceId ||
                combatant.Faction != CombatFaction.Enemy)
            {
                return ElementApplicationRejectionReason.InvalidTarget;
            }

            if (IsFinite(_request.ApplicationTime) == false || _request.ApplicationTime < 0f)
            {
                return ElementApplicationRejectionReason.InvalidApplicationTime;
            }

            if (IsFinite(_request.Source.SourceTargetIntervalSeconds) == false ||
                _request.Source.SourceTargetIntervalSeconds < 0f ||
                IsFinite(_request.Source.AttachmentDurationSeconds) == false ||
                _request.Source.AttachmentDurationSeconds <= 0f)
            {
                return ElementApplicationRejectionReason.InvalidRequest;
            }

            return ElementApplicationRejectionReason.None;
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

                nextAllowedTimesByKey.Clear();
            }
            else if (healthComponent.IsHealthDepleted)
            {
                if (primaryAttachment.IsValid)
                {
                    ClearPrimaryAttachment(ElementAttachmentChangeKind.TargetDepleted, _currentTime);
                }

                nextAllowedTimesByKey.Clear();
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
            return nextAllowedTimesByKey.TryGetValue(
                    _request.IntervalKey,
                    out float nextAllowedTime) == false
                || _request.ApplicationTime >= nextAllowedTime;
        }

        private void RecordSourceTargetInterval(in ElementApplicationRequest _request)
        {
            float intervalSeconds = _request.Source.SourceTargetIntervalSeconds;
            if (intervalSeconds <= 0f)
            {
                // 零间隔来源不保留无意义字典项。
                nextAllowedTimesByKey.Remove(_request.IntervalKey);
                return;
            }

            nextAllowedTimesByKey[_request.IntervalKey] = _request.ApplicationTime + intervalSeconds;
        }

        private void PruneExpiredIntervals(float _currentTime)
        {
            if (nextAllowedTimesByKey.Count == 0)
            {
                return;
            }

            // 先收集再删除，避免在 foreach 枚举 Dictionary 时修改集合。
            expiredIntervalKeys.Clear();
            foreach (KeyValuePair<ElementApplicationIntervalKey, float> entry in nextAllowedTimesByKey)
            {
                if (_currentTime >= entry.Value)
                {
                    expiredIntervalKeys.Add(entry.Key);
                }
            }

            for (int i = 0; i < expiredIntervalKeys.Count; i++)
            {
                nextAllowedTimesByKey.Remove(expiredIntervalKeys[i]);
            }

            expiredIntervalKeys.Clear();
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
            nextAllowedTimesByKey.Clear();
            expiredIntervalKeys.Clear();
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
