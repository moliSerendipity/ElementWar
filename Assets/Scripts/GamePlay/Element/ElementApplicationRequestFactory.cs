using Game.Definition.ConfigSystem.Core;
using Game.Definition.Element;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 负责从正式配置建立来源快照，并为已解析目标创建与伤害无关的元素施加请求。
    /// </summary>
    public static class ElementApplicationRequestFactory
    {
        /// <summary>
        /// 解析并冻结一个运行时元素来源；配置或归属非法时不产生部分有效快照。
        /// </summary>
        /// <param name="_configService">已经初始化的正式配置服务。</param>
        /// <param name="_profileId">要解析的元素应用 Profile 逻辑键。</param>
        /// <param name="_sourceId">由来源运行时所有者为本生命周期创建的身份。</param>
        /// <param name="_instigatorCombatant">承担后续附着与反应归属的活动战斗实体。</param>
        /// <param name="_sourceObject">具体武器、技能运行时、持续区域或配置对象。</param>
        /// <param name="_snapshot">成功时返回完全冻结的来源快照。</param>
        /// <param name="_failureReason">失败时返回确定原因；成功时为 None。</param>
        /// <returns>来源快照完整建立时返回 <see langword="true"/>。</returns>
        public static bool TryCreateSourceSnapshot(
            ConfigService _configService,
            string _profileId,
            ElementApplicationSourceId _sourceId,
            Combatant _instigatorCombatant,
            Object _sourceObject,
            out ElementApplicationSourceSnapshot _snapshot,
            out ElementApplicationFailureReason _failureReason)
        {
            _snapshot = default;

            if (_configService == null || _configService.IsInitialized == false)
            {
                _failureReason = ElementApplicationFailureReason.ConfigServiceUnavailable;
                return false;
            }

            string normalizedProfileId = ConfigIdUtility.Normalize(_profileId);
            if (ConfigIdUtility.IsValid(normalizedProfileId) == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidProfileId;
                return false;
            }

            if (_configService.TryGetConfig(
                    normalizedProfileId,
                    out ElementApplicationProfileConfig profile) == false)
            {
                _failureReason = ElementApplicationFailureReason.ProfileNotFound;
                return false;
            }

            if (profile.IsEnabled == false)
            {
                _failureReason = ElementApplicationFailureReason.ProfileDisabled;
                return false;
            }

            if (profile.HasValidApplicationData == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidProfileData;
                return false;
            }

            if (_sourceId.IsValid == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidSourceId;
                return false;
            }

            if (_instigatorCombatant == null || _instigatorCombatant.IsRuntimeActive == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidInstigator;
                return false;
            }

            if (_sourceObject == null)
            {
                _failureReason = ElementApplicationFailureReason.MissingSourceObject;
                return false;
            }

            _snapshot = new ElementApplicationSourceSnapshot(
                _sourceId,
                normalizedProfileId,
                profile.Element,
                profile.SourceTargetIntervalSeconds,
                profile.AttachmentDurationSeconds,
                _instigatorCombatant,
                _instigatorCombatant.Id,
                _instigatorCombatant.Faction,
                _sourceObject);
            _failureReason = ElementApplicationFailureReason.None;
            return true;
        }

        /// <summary>
        /// 为一个已解析到权威目标的攻击或技能执行创建元素施加尝试。
        /// 不读取或要求 DamageRequest、DamageResult、HealthComponent 或最终伤害值。
        /// </summary>
        /// <param name="_source">来源生命周期建立时冻结的快照。</param>
        /// <param name="_executionId">产生本次尝试的攻击或技能执行身份。</param>
        /// <param name="_targetCombatant">当前已解析的权威战斗目标。</param>
        /// <param name="_applicationTime">本次尝试成立的运行时时间戳。</param>
        /// <param name="_request">成功时返回完整元素施加请求。</param>
        /// <param name="_failureReason">失败时返回确定原因；成功时为 None。</param>
        /// <returns>请求结构、身份与首版阵营方向合法时返回 <see langword="true"/>。</returns>
        public static bool TryCreateRequest(
            in ElementApplicationSourceSnapshot _source,
            AttackExecutionId _executionId,
            Combatant _targetCombatant,
            float _applicationTime,
            out ElementApplicationRequest _request,
            out ElementApplicationFailureReason _failureReason)
        {
            _request = default;

            if (_source.IsCreated == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidSourceSnapshot;
                return false;
            }

            if (_executionId.IsValid == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidExecution;
                return false;
            }

            if (_targetCombatant == null || _targetCombatant.IsRuntimeActive == false)
            {
                _failureReason = ElementApplicationFailureReason.InvalidTarget;
                return false;
            }

            if (CanApplyElement(_source.InstigatorFaction, _targetCombatant.Faction) == false)
            {
                _failureReason = ElementApplicationFailureReason.FactionNotAllowed;
                return false;
            }

            if (IsFinite(_applicationTime) == false || _applicationTime < 0f)
            {
                _failureReason = ElementApplicationFailureReason.InvalidApplicationTime;
                return false;
            }

            _request = new ElementApplicationRequest(
                _source,
                _executionId,
                _targetCombatant,
                _targetCombatant.Id,
                _applicationTime);
            _failureReason = ElementApplicationFailureReason.None;
            return true;
        }

        private static bool CanApplyElement(CombatFaction _sourceFaction, CombatFaction _targetFaction)
        {
            // 首版只有玩家生产元素，且角色不是元素反应目标。
            return _sourceFaction == CombatFaction.PlayerParty
                && _targetFaction == CombatFaction.Enemy;
        }

        private static bool IsFinite(float _value)
        {
            return float.IsNaN(_value) == false && float.IsInfinity(_value) == false;
        }
    }
}
