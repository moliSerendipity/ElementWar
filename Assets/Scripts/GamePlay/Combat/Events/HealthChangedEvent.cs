namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 生命值事实写回后的事件。
    /// 该事件只描述提交后的最新生命状态，不承载任何伤害裁决逻辑。
    /// </summary>
    public readonly struct HealthChangedEvent
    {
        public HealthChangedEvent(HealthComponent _target, float _currentHealth, float _maxHealth)
        {
            Target = _target;
            CurrentHealth = _currentHealth;
            MaxHealth = _maxHealth;
        }

        public HealthComponent Target { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
    }
}
