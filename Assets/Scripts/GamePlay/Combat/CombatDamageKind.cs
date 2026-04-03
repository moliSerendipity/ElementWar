namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 当前阶段最小伤害类型枚举。
    /// Step 4B 先覆盖 Hitscan 直接伤害真正会走到的通道，后续元素/Buff 再扩展。
    /// </summary>
    public enum CombatDamageKind
    {
        Physical = 0,
        Fire = 1,
        Electric = 2,
        Ice = 3,
        Explosion = 4,
    }
}
