namespace Game.Gameplay.Element
{
    /// <summary>元素附着事实发生变化的确定原因。</summary>
    public enum ElementAttachmentChangeKind
    {
        None = 0,
        Attached = 1,
        Refreshed = 2,
        Expired = 3,
        Consumed = 4,
        TargetDepleted = 5,
        TargetReset = 6,
        TargetDisabled = 7,
    }
}
