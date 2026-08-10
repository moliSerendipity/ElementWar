namespace Game.Definition.Combat
{
    /// <summary>
    /// 一次伤害携带的元素语义。<see cref="None"/> 表示不携带元素，当前按物理抗性结算。
    /// </summary>
    public enum ElementType
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Electric = 3,
        Ice = 4,
    }
}
