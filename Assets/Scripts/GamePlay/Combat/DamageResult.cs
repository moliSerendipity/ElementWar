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
        public DamageResult(
            bool _isApplied,
            GameObject _instigator,
            Object _sourceObject,
            HealthComponent _target,
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
            Instigator = _instigator;
            SourceObject = _sourceObject;
            Target = _target;
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

        /// <summary>承担本次伤害归属的责任实体。</summary>
        public GameObject Instigator { get; }

        /// <summary>产生本次伤害的具体来源对象。</summary>
        public Object SourceObject { get; }

        /// <summary>已被写入生命事实的目标。</summary>
        public HealthComponent Target { get; }

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

        /// <summary>表示请求未被目标接受的空结果。</summary>
        public static DamageResult None => new(
            false,
            null,
            null,
            null,
            ElementType.None,
            DamageDeliveryType.Direct,
            HitPartType.Default,
            0f,
            0f,
            false,
            Vector3.zero,
            Vector3.up,
            0f);
    }
}
