namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 命中部位分类。
    /// 当前阶段先保留 Default / Head / WeakPoint 三档，后续可按 HurtBox 扩展更多类型。
    /// </summary>
    public enum CombatHitPartType
    {
        Default = 0,
        Head = 1,
        WeakPoint = 2,
    }
}
