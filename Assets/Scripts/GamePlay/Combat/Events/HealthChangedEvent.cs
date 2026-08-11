namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 生命值事实写回后的事件。
    /// 该事件只描述提交后的最新生命状态，不承载任何伤害裁决逻辑。
    /// </summary>
    public readonly struct HealthChangedEvent
    {
        /// <summary>创建一条生命事实已完成写回的通知。</summary>
        /// <param name="_executionId">造成变化的攻击执行身份。</param>
        /// <param name="_targetId">写回时冻结的目标身份。</param>
        /// <param name="_target">被写回的生命组件。</param>
        /// <param name="_currentHealth">写回后的当前生命值。</param>
        /// <param name="_maxHealth">本次初始化采用的生命上限。</param>
        public HealthChangedEvent(
            AttackExecutionId _executionId,
            CombatantId _targetId,
            HealthComponent _target,
            float _currentHealth,
            float _maxHealth)
        {
            ExecutionId = _executionId;
            TargetId = _targetId;
            Target = _target;
            CurrentHealth = _currentHealth;
            MaxHealth = _maxHealth;
        }

        /// <summary>造成变化的攻击执行身份。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>写回时冻结的目标身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>被写回的生命组件。</summary>
        public HealthComponent Target { get; }

        /// <summary>写回后的当前生命值。</summary>
        public float CurrentHealth { get; }

        /// <summary>本次初始化采用的生命上限。</summary>
        public float MaxHealth { get; }
    }
}
