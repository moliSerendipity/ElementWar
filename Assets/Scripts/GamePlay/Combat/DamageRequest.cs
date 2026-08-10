using Game.Definition.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 进入伤害域的一次标准化请求，不依赖 Hitscan、近战或其他具体命中实现。
    /// </summary>
    public readonly struct DamageRequest
    {
        /// <summary>
        /// 创建一次已经完成目标与命中部位解析的伤害请求。
        /// </summary>
        public DamageRequest(
            GameObject _instigator,
            Object _sourceObject,
            HealthComponent _target,
            ElementType _element,
            DamageDeliveryType _delivery,
            float _baseDamage,
            HitPartType _hitPartType,
            float _headShotDamageMultiplier,
            float _weakPointDamageMultiplier,
            Vector3 _attackOrigin,
            Vector3 _attackDirection,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _requestTime)
        {
            Instigator = _instigator;
            SourceObject = _sourceObject;
            Target = _target;
            Element = _element;
            Delivery = _delivery;
            BaseDamage = _baseDamage;
            HitPartType = _hitPartType;
            HeadShotDamageMultiplier = _headShotDamageMultiplier;
            WeakPointDamageMultiplier = _weakPointDamageMultiplier;
            AttackOrigin = _attackOrigin;
            AttackDirection = _attackDirection;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
            RequestTime = _requestTime;
        }

        /// <summary>承担伤害与后续击杀归属的责任实体。</summary>
        public GameObject Instigator { get; }

        /// <summary>产生本次伤害的具体武器运行时、攻击配置或其他 Unity 对象。</summary>
        public Object SourceObject { get; }

        /// <summary>接收已裁决伤害的权威生命组件。</summary>
        public HealthComponent Target { get; }

        /// <summary>本次伤害携带的元素；不负责元素附着或反应。</summary>
        public ElementType Element { get; }

        /// <summary>本次伤害的传递形态。</summary>
        public DamageDeliveryType Delivery { get; }

        /// <summary>进入防守侧计算前的基础伤害。</summary>
        public float BaseDamage { get; }

        /// <summary>已经由命中查询解析出的部位类型。</summary>
        public HitPartType HitPartType { get; }

        /// <summary>头部命中的确定性倍率。</summary>
        public float HeadShotDamageMultiplier { get; }

        /// <summary>弱点命中的来源侧确定性倍率。</summary>
        public float WeakPointDamageMultiplier { get; }

        /// <summary>攻击起点。</summary>
        public Vector3 AttackOrigin { get; }

        /// <summary>攻击方向。</summary>
        public Vector3 AttackDirection { get; }

        /// <summary>命中世界坐标。</summary>
        public Vector3 HitPoint { get; }

        /// <summary>命中表面法线。</summary>
        public Vector3 HitNormal { get; }

        /// <summary>请求产生的时间戳。</summary>
        public float RequestTime { get; }
    }
}
