namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 冻结一次攻击对敌人产生的削韧与硬控制输出；Boss 转换量不包含同次攻击的基础削韧。
    /// </summary>
    public readonly struct EnemyControlApplicationRequest
    {
        /// <summary>创建一次以同一攻击执行为单位的敌人控制请求。</summary>
        /// <param name="_executionId">已经成立的攻击执行身份。</param>
        /// <param name="_instigatorCombatant">承担本次效果归属的活动战斗实体。</param>
        /// <param name="_targetCombatant">接收本次效果的活动敌人目标。</param>
        /// <param name="_baseToughnessDamage">攻击本身提供的基础削韧。</param>
        /// <param name="_hardControlDuration">普通敌人采用的硬控制时长，单位秒。</param>
        /// <param name="_bossToughnessDamage">硬控制被 Boss 拒绝时额外转换出的削韧。</param>
        public EnemyControlApplicationRequest(
            AttackExecutionId _executionId,
            Combatant _instigatorCombatant,
            Combatant _targetCombatant,
            float _baseToughnessDamage,
            float _hardControlDuration,
            float _bossToughnessDamage)
        {
            ExecutionId = _executionId;
            InstigatorCombatant = _instigatorCombatant;
            InstigatorId = _instigatorCombatant != null ? _instigatorCombatant.Id : default;
            TargetCombatant = _targetCombatant;
            TargetId = _targetCombatant != null ? _targetCombatant.Id : default;
            BaseToughnessDamage = _baseToughnessDamage;
            HardControlDuration = _hardControlDuration;
            BossToughnessDamage = _bossToughnessDamage;
        }

        /// <summary>本次攻击执行的运行时身份。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>承担效果归属的战斗实体。</summary>
        public Combatant InstigatorCombatant { get; }

        /// <summary>请求创建时冻结的责任实体身份。</summary>
        public CombatantId InstigatorId { get; }

        /// <summary>接收效果的敌人目标。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>请求创建时冻结的目标身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>攻击本身提供、尚未经过最低阈值的基础削韧。</summary>
        public float BaseToughnessDamage { get; }

        /// <summary>普通敌人采用的来源硬控制时长，单位秒。</summary>
        public float HardControlDuration { get; }

        /// <summary>Boss 拒绝硬控制时额外转换出的削韧，不包含基础削韧。</summary>
        public float BossToughnessDamage { get; }
    }
}
