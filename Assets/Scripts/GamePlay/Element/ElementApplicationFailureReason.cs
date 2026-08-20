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
        InvalidProfileData = 5,
        InvalidSourceId = 6,
        InvalidInstigator = 7,
        MissingSourceObject = 8,
        InvalidSourceSnapshot = 9,
        InvalidExecution = 10,
        InvalidTarget = 11,
        FactionNotAllowed = 12,
        InvalidApplicationTime = 13,
    }
}
