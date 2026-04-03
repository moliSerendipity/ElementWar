namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 最终伤害结果已经提交到生命事实组件后的事件。
    /// Presentation / Debug 只能读取结果，不允许反向修改 Combat 逻辑。
    /// </summary>
    public readonly struct DamageAppliedEvent
    {
        public DamageAppliedEvent(in CombatDamageResult _damageResult)
        {
            DamageResult = _damageResult;
        }

        public CombatDamageResult DamageResult { get; }
    }
}
