namespace Game.Gameplay.Element
{
    /// <summary>合法元素请求未能写入目标附着运行时时的确定原因。</summary>
    public enum ElementApplicationRejectionReason
    {
        None = 0,
        InvalidRequest = 1,
        InvalidTarget = 2,
        MissingAttachmentOwner = 3,
        AttachmentOwnerNotReady = 4,
        TargetCannotReceiveAttachment = 5,
        InvalidApplicationTime = 6,
        StaleApplicationTime = 7,
        SourceTargetIntervalActive = 8,
        InvalidAttachmentDuration = 9,
    }
}
