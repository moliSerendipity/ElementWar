using Game.Definition.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 进入 Combat 域的一次标准化伤害请求。
    ///
    /// 设计要点：
    /// 与具体伤害来源无关。无论是枪械 Hitscan、敌人近战、技能、区域效果，
    /// 都通过同一个结构进入 DamageResolver，保证伤害管线统一。
    ///
    /// 该结构只描述本次攻击事实，不保存最终伤害结果。
    /// </summary>
    public readonly struct CombatDamageRequestContext
    {
        public CombatDamageRequestContext(
            GameObject _attacker,
            CombatDamageKind _damageKind,
            float _baseDamage,
            float _critChance,
            float _critDamageMultiplier,
            float _headShotDamageMultiplier,
            float _weakPointDamageMultiplier,
            Vector3 _attackOrigin,
            Vector3 _attackDirection,
            in HitScanHitContext _hitContext,
            float _requestTime)
        {
            Attacker = _attacker;
            DamageKind = _damageKind;
            BaseDamage = _baseDamage;
            CritChance = _critChance;
            CritDamageMultiplier = _critDamageMultiplier;
            HeadShotDamageMultiplier = _headShotDamageMultiplier;
            WeakPointDamageMultiplier = _weakPointDamageMultiplier;
            AttackOrigin = _attackOrigin;
            AttackDirection = _attackDirection;
            HitContext = _hitContext;
            RequestTime = _requestTime;
        }

        /// <summary>发起攻击的 GameObject。</summary>
        public GameObject Attacker { get; }

        /// <summary>伤害类型（物理、火、雷、冰、爆炸等）。</summary>
        public CombatDamageKind DamageKind { get; }

        /// <summary>基础伤害值。枪械取武器面板，敌人取 AttackPower。</summary>
        public float BaseDamage { get; }

        /// <summary>暴击率（百分比，0~100）。</summary>
        public float CritChance { get; }

        /// <summary>暴击伤害倍率（≥1）。</summary>
        public float CritDamageMultiplier { get; }

        /// <summary>爆头伤害倍率（≥1）。</summary>
        public float HeadShotDamageMultiplier { get; }

        /// <summary>弱点伤害倍率（≥1）。</summary>
        public float WeakPointDamageMultiplier { get; }

        /// <summary>攻击起点。枪械为枪口位置，近战为敌人位置。</summary>
        public Vector3 AttackOrigin { get; }

        /// <summary>攻击方向。枪械为射线方向，近战为面朝方向。</summary>
        public Vector3 AttackDirection { get; }

        /// <summary>命中上下文。包含碰撞体、命中点、部位类型、目标 HealthComponent。</summary>
        public HitScanHitContext HitContext { get; }

        /// <summary>请求产生的时间戳。</summary>
        public float RequestTime { get; }
    }
}
