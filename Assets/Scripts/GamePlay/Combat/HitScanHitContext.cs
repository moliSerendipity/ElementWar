using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 一次逻辑射线命中的最小上下文。
    /// Weapon 侧只关心是否命中和命中了什么；真正的伤害结算由 Combat 域继续处理。
    /// </summary>
    public readonly struct HitScanHitContext
    {
        public HitScanHitContext(
            bool _hasHit,
            Collider _hitCollider,
            HealthComponent _damageReceiver,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _hitDistance,
            CombatHitPartType _hitPartType)
        {
            HasHit = _hasHit;
            HitCollider = _hitCollider;
            DamageReceiver = _damageReceiver;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
            HitDistance = _hitDistance;
            HitPartType = _hitPartType;
        }

        public bool HasHit { get; }
        public Collider HitCollider { get; }
        public HealthComponent DamageReceiver { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public float HitDistance { get; }
        public CombatHitPartType HitPartType { get; }

        public static HitScanHitContext None => new(
            false,
            null,
            null,
            Vector3.zero,
            Vector3.up,
            0f,
            CombatHitPartType.Default);
    }
}
