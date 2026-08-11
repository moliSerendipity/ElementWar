namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 首版战斗阵营。未分配阵营不能成为合法伤害来源或目标。
    /// </summary>
    public enum CombatFaction
    {
        Unassigned = 0,
        PlayerParty = 1,
        Enemy = 2,
    }
}
