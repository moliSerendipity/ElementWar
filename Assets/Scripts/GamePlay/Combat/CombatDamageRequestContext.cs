using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 从 Weapon 域进入 Combat 域的一次标准化伤害请求。
    /// 该结构只描述本次攻击事实，不直接保存最终伤害结果。
    /// </summary>
    public readonly struct CombatDamageRequestContext
    {
        public CombatDamageRequestContext(
            GameObject _attacker,
            WeaponRuntime _sourceWeaponRuntime,
            CombatDamageKind _damageKind,
            float _baseDamage,
            float _critChance,
            float _critDamageMultiplier,
            float _headShotDamageMultiplier,
            float _weakPointDamageMultiplier,
            Vector3 _shotOrigin,
            Vector3 _shotDirection,
            in HitScanHitContext _hitContext,
            float _requestTime)
        {
            Attacker = _attacker;
            SourceWeaponRuntime = _sourceWeaponRuntime;
            DamageKind = _damageKind;
            BaseDamage = _baseDamage;
            CritChance = _critChance;
            CritDamageMultiplier = _critDamageMultiplier;
            HeadShotDamageMultiplier = _headShotDamageMultiplier;
            WeakPointDamageMultiplier = _weakPointDamageMultiplier;
            ShotOrigin = _shotOrigin;
            ShotDirection = _shotDirection;
            HitContext = _hitContext;
            RequestTime = _requestTime;
        }

        public GameObject Attacker { get; }
        public WeaponRuntime SourceWeaponRuntime { get; }
        public CombatDamageKind DamageKind { get; }
        public float BaseDamage { get; }
        public float CritChance { get; }
        public float CritDamageMultiplier { get; }
        public float HeadShotDamageMultiplier { get; }
        public float WeakPointDamageMultiplier { get; }
        public Vector3 ShotOrigin { get; }
        public Vector3 ShotDirection { get; }
        public HitScanHitContext HitContext { get; }
        public float RequestTime { get; }
    }
}
