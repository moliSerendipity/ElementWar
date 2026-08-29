using System.Collections.Generic;
using Game.Gameplay.Element;
using Game.Gameplay.Enemy;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 战斗目标根及阵营事实所有者，并分别去重生命伤害与敌人控制执行。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class Combatant : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private CombatFaction faction = CombatFaction.Unassigned;

        [Header("References")]
        [SerializeField] private HealthComponent healthComponent;
        [SerializeField] private ElementAttachmentRuntime elementAttachmentRuntime;

        private readonly HashSet<AttackExecutionId> acceptedDamageExecutionIds = new();
        private readonly HashSet<AttackExecutionId> acceptedControlExecutionIds = new();
        private EnemyRoot enemyRoot;
        private CombatantId id;

        /// <summary>当前活动生命周期的运行时身份；禁用时为无效值。</summary>
        public CombatantId Id => id;

        /// <summary>当前战斗阵营。</summary>
        public CombatFaction Faction => faction;

        /// <summary>该目标唯一的生命事实组件。</summary>
        public HealthComponent Health => healthComponent;

        /// <summary>
        /// 该目标的元素附着事实所有者；首版玩家根和未迁移目标可以为空。
        /// </summary>
        public ElementAttachmentRuntime ElementAttachments
        {
            get
            {
                if (elementAttachmentRuntime == null)
                {
                    elementAttachmentRuntime = GetComponent<ElementAttachmentRuntime>();
                }

                return elementAttachmentRuntime;
            }
        }

        /// <summary>同一目标上的敌人装配根；非敌方目标或装配缺失时为空。</summary>
        internal EnemyRoot Enemy
        {
            get
            {
                if (enemyRoot == null && faction == CombatFaction.Enemy)
                {
                    enemyRoot = GetComponent<EnemyRoot>();
                }

                return enemyRoot;
            }
        }

        /// <summary>组件已启用、身份有效且生命引用存在时为真。</summary>
        public bool IsRuntimeActive => isActiveAndEnabled && id.IsValid && healthComponent != null;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            // 新 TargetId 建立前先清空上一轮去重；附着随后绑定同一份新身份。
            acceptedDamageExecutionIds.Clear();
            acceptedControlExecutionIds.Clear();
            id = CombatantId.Create();
            ElementAttachments?.BeginTargetLifecycle(this, id, Time.time);
        }

        private void OnDisable()
        {
            // 附着清理仍需观察旧身份，因此先结束附着，再使 TargetId 失效。
            ElementAttachments?.EndTargetLifecycle(
                Time.time,
                ElementAttachmentChangeKind.TargetDisabled);
            id = default;
            acceptedDamageExecutionIds.Clear();
            acceptedControlExecutionIds.Clear();
        }

        /// <summary>确认调用方冻结的身份仍属于当前活动生命周期。</summary>
        /// <param name="_expectedId">调用方在请求创建时保存的目标身份。</param>
        /// <returns>组件活动且身份有效、相等时返回 <see langword="true"/>。</returns>
        internal bool MatchesCurrentIdentity(CombatantId _expectedId)
        {
            return IsRuntimeActive && _expectedId.IsValid && id == _expectedId;
        }

        /// <summary>
        /// 在目标权威层登记一次生命伤害执行；重复、无效或旧生命周期执行保持状态不变。
        /// </summary>
        /// <param name="_executionId">本次攻击执行身份。</param>
        /// <param name="_expectedTargetId">请求创建时冻结的目标身份。</param>
        /// <returns>本生命周期第一次登记该伤害执行时返回 <see langword="true"/>。</returns>
        internal bool TryAcceptDamageExecution(
            AttackExecutionId _executionId,
            CombatantId _expectedTargetId)
        {
            if (_executionId.IsValid == false || MatchesCurrentIdentity(_expectedTargetId) == false)
            {
                return false;
            }

            return acceptedDamageExecutionIds.Add(_executionId);
        }

        /// <summary>
        /// 登记一次合并削韧与硬控制执行；与生命伤害分开去重，使同一攻击可以提交两个领域的结果。
        /// </summary>
        /// <param name="_executionId">本次攻击执行身份。</param>
        /// <param name="_expectedTargetId">请求创建时冻结的目标身份。</param>
        /// <returns>本生命周期第一次登记该控制执行时返回 <see langword="true"/>。</returns>
        internal bool TryAcceptControlExecution(
            AttackExecutionId _executionId,
            CombatantId _expectedTargetId)
        {
            if (_executionId.IsValid == false || MatchesCurrentIdentity(_expectedTargetId) == false)
            {
                return false;
            }

            return acceptedControlExecutionIds.Add(_executionId);
        }

        private void ResolveReferences()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }

            if (elementAttachmentRuntime == null)
            {
                elementAttachmentRuntime = GetComponent<ElementAttachmentRuntime>();
            }

            // 敌人装配根在目标生命周期内稳定，初始化时缓存以免每次控制申请重复查组件。
            if (enemyRoot == null && faction == CombatFaction.Enemy)
            {
                enemyRoot = GetComponent<EnemyRoot>();
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
