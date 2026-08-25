namespace Game.Gameplay.Element
{
    /// <summary>
    /// 创建元素来源快照或施加请求失败时的确定原因；成功时为 <see cref="None"/>。
    /// </summary>
    public enum ElementApplicationFailureReason
    {
        None = 0,
        ConfigServiceUnavailable = 1,
        InvalidProfileId = 2,
        ProfileNotFound = 3,
        ProfileDisabled = 4,
        InvalidSourceId = 5,
        InvalidInstigator = 6,
        MissingSourceObject = 7,
        InvalidSourceSnapshot = 8,
        InvalidExecution = 9,
        InvalidTarget = 10,
        FactionNotAllowed = 11,
        InvalidApplicationTime = 12,
    }
}
