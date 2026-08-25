namespace Game.Gameplay.Element
{
    /// <summary>元素请求经过目标附着运行时处理后的结果类别。</summary>
    public enum ElementApplicationResolutionStatus
    {
        None = 0,
        Rejected = 1,
        Attached = 2,
        Refreshed = 3,
        Unchanged = 4,
        ReactionRequired = 5,
    }
}
