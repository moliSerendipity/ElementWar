using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 一次逻辑射线命中的最小上下文。
    /// Weapon 侧只关心是否命中和命中了什么；真正的伤害结算由 Combat 域继续处理。
    /// </summary>
    public readonly struct HitScanHitContext
    {
        /// <summary>创建一次完成物理查询和目标根解析的只读命中上下文。</summary>
        /// <param name="_hasHit">射线是否命中任意 Collider。</param>
        /// <param name="_hitCollider">物理查询直接命中的 Collider。</param>
        /// <param name="_targetCombatant">从 Collider 解析出的活动权威目标；非战斗对象时为空。</param>
        /// <param name="_hitPoint">命中点的世界坐标。</param>
        /// <param name="_hitNormal">命中表面的世界空间法线。</param>
        /// <param name="_hitDistance">从射线起点到命中点的距离。</param>
        /// <param name="_hitPartType">从命中 Collider 解析出的部位类型。</param>
        public HitScanHitContext(
            bool _hasHit,
            Collider _hitCollider,
            Combatant _targetCombatant,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _hitDistance,
            HitPartType _hitPartType)
        {
            HasHit = _hasHit;
            HitCollider = _hitCollider;
            TargetCombatant = _targetCombatant;
            TargetId = _targetCombatant != null ? _targetCombatant.Id : default;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
            HitDistance = _hitDistance;
            HitPartType = _hitPartType;
        }

        /// <summary>射线是否命中任意 Collider。</summary>
        public bool HasHit { get; }

        /// <summary>物理查询直接命中的 Collider。</summary>
        public Collider HitCollider { get; }

        /// <summary>从命中 Collider 解析出的权威战斗目标。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>命中查询时冻结的权威目标身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>权威目标的生命组件；未命中战斗目标时为空。</summary>
        public HealthComponent HealthComponent => TargetCombatant != null ? TargetCombatant.Health : null;

        /// <summary>命中点的世界坐标。</summary>
        public Vector3 HitPoint { get; }

        /// <summary>命中表面的世界空间法线。</summary>
        public Vector3 HitNormal { get; }

        /// <summary>从射线起点到命中点的距离。</summary>
        public float HitDistance { get; }

        /// <summary>命中 Collider 对应的部位类型。</summary>
        public HitPartType HitPartType { get; }

        /// <summary>没有发生物理命中的默认上下文。</summary>
        public static HitScanHitContext None => new(
            false,
            null,
            null,
            Vector3.zero,
            Vector3.up,
            0f,
            HitPartType.Default);
    }
}
