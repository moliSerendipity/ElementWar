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
        /// <param name="_executionId">已成立攻击的运行时执行身份。</param>
        /// <param name="_instigatorCombatant">承担归属的活动战斗实体。</param>
        /// <param name="_sourceObject">产生伤害的武器运行时、配置或其他具体来源。</param>
        /// <param name="_targetCombatant">接收请求的活动权威目标。</param>
        /// <param name="_element">伤害携带的元素语义。</param>
        /// <param name="_delivery">伤害的传递形态。</param>
        /// <param name="_baseDamage">进入防守侧公式前的基础伤害。</param>
        /// <param name="_hitPartType">命中查询已解析的部位类型。</param>
        /// <param name="_headShotDamageMultiplier">头部命中的来源侧倍率。</param>
        /// <param name="_weakPointDamageMultiplier">弱点命中的来源侧倍率。</param>
        /// <param name="_attackOrigin">攻击起点的世界坐标。</param>
        /// <param name="_attackDirection">攻击的世界空间方向。</param>
        /// <param name="_hitPoint">命中点的世界坐标。</param>
        /// <param name="_hitNormal">命中表面的世界空间法线。</param>
        /// <param name="_requestTime">请求产生时的运行时时间戳。</param>
        public DamageRequest(
            AttackExecutionId _executionId,
            Combatant _instigatorCombatant,
            Object _sourceObject,
            Combatant _targetCombatant,
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
            ExecutionId = _executionId;
            InstigatorCombatant = _instigatorCombatant;
            InstigatorId = _instigatorCombatant != null ? _instigatorCombatant.Id : default;
            SourceObject = _sourceObject;
            TargetCombatant = _targetCombatant;
            TargetId = _targetCombatant != null ? _targetCombatant.Id : default;
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

        /// <summary>本次攻击执行的运行时身份。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>承担归属的权威战斗实体。</summary>
        public Combatant InstigatorCombatant { get; }

        /// <summary>请求创建时冻结的责任实体身份。</summary>
        public CombatantId InstigatorId { get; }

        /// <summary>承担伤害与后续击杀归属的责任实体。</summary>
        public GameObject Instigator => InstigatorCombatant != null ? InstigatorCombatant.gameObject : null;

        /// <summary>产生本次伤害的具体武器运行时、攻击配置或其他 Unity 对象。</summary>
        public Object SourceObject { get; }

        /// <summary>接收请求的权威战斗目标。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>请求创建时冻结的目标身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>接收已裁决伤害的唯一生命组件。</summary>
        public HealthComponent Target => TargetCombatant != null ? TargetCombatant.Health : null;

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
