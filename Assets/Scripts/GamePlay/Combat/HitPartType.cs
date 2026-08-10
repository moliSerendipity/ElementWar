namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 命中部位分类。当前只表达普通、头部与显式弱点。
    /// </summary>
    public enum HitPartType
    {
        Default = 0,
        Head = 1,
        WeakPoint = 2,
    }
}
