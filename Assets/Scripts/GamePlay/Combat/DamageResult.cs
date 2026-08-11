using Game.Definition.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 伤害域完成裁决并写回生命事实后的只读结果。
    /// </summary>
    public readonly struct DamageResult
    {
        /// <summary>
        /// 创建一次已提交或被拒绝的伤害结果。
        /// </summary>
        internal DamageResult(
            bool _isApplied,
            DamageRejectionReason _rejectionReason,
            AttackExecutionId _executionId,
            Combatant _instigatorCombatant,
            CombatantId _instigatorId,
            Object _sourceObject,
            Combatant _targetCombatant,
            CombatantId _targetId,
            ElementType _element,
            DamageDeliveryType _delivery,
            HitPartType _hitPartType,
            float _finalDamage,
            float _remainingHealth,
            bool _didDepleteHealth,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _appliedTime)
        {
            IsApplied = _isApplied;
            RejectionReason = _rejectionReason;
            ExecutionId = _executionId;
            InstigatorCombatant = _instigatorCombatant;
            InstigatorId = _instigatorId;
            SourceObject = _sourceObject;
            TargetCombatant = _targetCombatant;
            TargetId = _targetId;
            Element = _element;
            Delivery = _delivery;
            HitPartType = _hitPartType;
            FinalDamage = _finalDamage;
            RemainingHealth = _remainingHealth;
            DidDepleteHealth = _didDepleteHealth;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
            AppliedTime = _appliedTime;
        }

        /// <summary>伤害是否已经写回目标生命事实。</summary>
        public bool IsApplied { get; }

        /// <summary>未提交时的确定原因；成功提交时为 None。</summary>
        public DamageRejectionReason RejectionReason { get; }

        /// <summary>本次攻击执行的运行时身份。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>承担归属的权威战斗实体引用。</summary>
        public Combatant InstigatorCombatant { get; }

        /// <summary>请求创建时冻结的责任实体身份。</summary>
        public CombatantId InstigatorId { get; }

        /// <summary>承担本次伤害归属的责任实体。</summary>
        public GameObject Instigator => InstigatorCombatant != null ? InstigatorCombatant.gameObject : null;

        /// <summary>产生本次伤害的具体来源对象。</summary>
        public Object SourceObject { get; }

        /// <summary>请求创建时的权威目标引用。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>请求创建时冻结的目标身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>已被写入或尝试写入生命事实的目标。</summary>
        public HealthComponent Target => TargetCombatant != null ? TargetCombatant.Health : null;

        /// <summary>本次伤害的元素语义。</summary>
        public ElementType Element { get; }

        /// <summary>本次伤害的传递形态。</summary>
        public DamageDeliveryType Delivery { get; }

        /// <summary>本次命中的部位类型。</summary>
        public HitPartType HitPartType { get; }

        /// <summary>提交给目标的最终裁决伤害值；过量伤害可大于写回前的剩余生命。</summary>
        public float FinalDamage { get; }

        /// <summary>写回后的剩余生命值。</summary>
        public float RemainingHealth { get; }

        /// <summary>本次写回是否首次使生命值从正数降至零。</summary>
        public bool DidDepleteHealth { get; }

        /// <summary>命中世界坐标。</summary>
        public Vector3 HitPoint { get; }

        /// <summary>命中表面法线。</summary>
        public Vector3 HitNormal { get; }

        /// <summary>生命事实写回的时间戳。</summary>
        public float AppliedTime { get; }

        /// <summary>表示尚未经过伤害裁决的默认空结果。</summary>
        public static DamageResult None => default;

        internal static DamageResult Rejected(in DamageRequest _request, DamageRejectionReason _reason)
        {
            HealthComponent targetHealth = _request.Target;
            return new DamageResult(
                false,
                _reason,
                _request.ExecutionId,
                _request.InstigatorCombatant,
                _request.InstigatorId,
                _request.SourceObject,
                _request.TargetCombatant,
                _request.TargetId,
                _request.Element,
                _request.Delivery,
                _request.HitPartType,
                0f,
                targetHealth != null ? targetHealth.CurrentHealth : 0f,
                false,
                _request.HitPoint,
                _request.HitNormal,
                0f);
        }
    }
}
