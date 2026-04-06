using Game.Definition.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Combat 域处理完成后输出的最小结果结构。
    /// Presentation 只能读这个结果，不允许反向修改逻辑。
    /// </summary>
    public readonly struct CombatDamageResult
    {
        public CombatDamageResult(
            bool _isApplied,
            GameObject _attacker,
            HealthComponent _target,
            CombatDamageKind _damageKind,
            CombatHitPartType _hitPartType,
            float _finalDamage,
            bool _isCritical,
            float _remainingHealth,
            bool _wasKilled,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _appliedTime)
        {
            IsApplied = _isApplied;
            Attacker = _attacker;
            Target = _target;
            DamageKind = _damageKind;
            HitPartType = _hitPartType;
            FinalDamage = _finalDamage;
            IsCritical = _isCritical;
            RemainingHealth = _remainingHealth;
            WasKilled = _wasKilled;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
            AppliedTime = _appliedTime;
        }

        public bool IsApplied { get; }
        public GameObject Attacker { get; }
        public HealthComponent Target { get; }
        public CombatDamageKind DamageKind { get; }
        public CombatHitPartType HitPartType { get; }
        public float FinalDamage { get; }
        public bool IsCritical { get; }
        public float RemainingHealth { get; }
        public bool WasKilled { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public float AppliedTime { get; }

        public static CombatDamageResult None => new(
            false,
            null,
            null,
            CombatDamageKind.Physical,
            CombatHitPartType.Default,
            0f,
            false,
            0f,
            false,
            Vector3.zero,
            Vector3.up,
            0f);
    }
}
