namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 伤害请求未提交到生命事实时的确定原因。
    /// </summary>
    public enum DamageRejectionReason
    {
        None = 0,
        InvalidExecution = 1,
        InvalidInstigator = 2,
        InvalidTarget = 3,
        FactionNotAllowed = 4,
        TargetCannotReceiveDamage = 5,
        DuplicateExecution = 6,
    }
}
