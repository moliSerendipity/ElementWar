using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 战斗目标根及阵营事实所有者，并在当前活动生命周期内拒绝同一攻击执行的重复提交。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class Combatant : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private CombatFaction faction = CombatFaction.Unassigned;

        [Header("References")]
        [SerializeField] private HealthComponent healthComponent;

        private readonly HashSet<AttackExecutionId> acceptedExecutionIds = new();
        private CombatantId id;

        /// <summary>当前活动生命周期的运行时身份；禁用时为无效值。</summary>
        public CombatantId Id => id;

        /// <summary>当前战斗阵营。</summary>
        public CombatFaction Faction => faction;

        /// <summary>该目标唯一的生命事实组件。</summary>
        public HealthComponent Health => healthComponent;

        /// <summary>组件已启用、身份有效且生命引用存在时为真。</summary>
        public bool IsRuntimeActive => isActiveAndEnabled && id.IsValid && healthComponent != null;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            acceptedExecutionIds.Clear();
            id = CombatantId.Create();
        }

        private void OnDisable()
        {
            id = default;
            acceptedExecutionIds.Clear();
        }

        internal bool MatchesCurrentIdentity(CombatantId _expectedId)
        {
            return IsRuntimeActive && _expectedId.IsValid && id == _expectedId;
        }

        /// <summary>
        /// 在目标权威层登记一次执行；重复、无效或属于旧生命周期的执行保持状态不变。
        /// </summary>
        internal bool TryAcceptExecution(AttackExecutionId _executionId, CombatantId _expectedTargetId)
        {
            if (_executionId.IsValid == false || MatchesCurrentIdentity(_expectedTargetId) == false)
            {
                return false;
            }

            return acceptedExecutionIds.Add(_executionId);
        }

        private void ResolveReferences()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
