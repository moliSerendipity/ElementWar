namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// Weapon 域当前帧计划。
    /// 由 WeaponCommandResolver 统一产出，执行阶段只消费这里的已裁决结果。
    /// </summary>
    public readonly struct WeaponFramePlan
    {
        public WeaponFramePlan(
            bool _fireTriggered,
            bool _dryFireTriggered,
            bool _reloadTriggered,
            bool _autoReloadAfterFire,
            bool _isEmptyReload,
            float _reloadDuration,
            WeaponFireFailureReason _fireFailureReason,
            WeaponReloadFailureReason _reloadFailureReason)
        {
            FireTriggered = _fireTriggered;
            DryFireTriggered = _dryFireTriggered;
            ReloadTriggered = _reloadTriggered;
            AutoReloadAfterFire = _autoReloadAfterFire;
            IsEmptyReload = _isEmptyReload;
            ReloadDuration = _reloadDuration;
            FireFailureReason = _fireFailureReason;
            ReloadFailureReason = _reloadFailureReason;
        }

        public bool FireTriggered { get; }
        public bool DryFireTriggered { get; }
        public bool ReloadTriggered { get; }
        public bool AutoReloadAfterFire { get; }
        public bool IsEmptyReload { get; }
        public float ReloadDuration { get; }
        public WeaponFireFailureReason FireFailureReason { get; }
        public WeaponReloadFailureReason ReloadFailureReason { get; }
        public bool HasAnyExecution => FireTriggered || DryFireTriggered || ReloadTriggered;

        public static WeaponFramePlan Empty => new(
            false,
            false,
            false,
            false,
            false,
            0f,
            WeaponFireFailureReason.None,
            WeaponReloadFailureReason.None);

        public static WeaponFramePlan CreateInvalid(
            WeaponFireFailureReason _fireFailureReason,
            WeaponReloadFailureReason _reloadFailureReason)
        {
            return new WeaponFramePlan(
                false,
                false,
                false,
                false,
                false,
                0f,
                _fireFailureReason,
                _reloadFailureReason);
        }

        public static WeaponFramePlan CreateResolved(
            bool _fireTriggered,
            bool _dryFireTriggered,
            bool _reloadTriggered,
            bool _autoReloadAfterFire,
            bool _isEmptyReload,
            float _reloadDuration,
            WeaponFireFailureReason _fireFailureReason,
            WeaponReloadFailureReason _reloadFailureReason)
        {
            return new WeaponFramePlan(
                _fireTriggered,
                _dryFireTriggered,
                _reloadTriggered,
                _autoReloadAfterFire,
                _isEmptyReload,
                _reloadDuration,
                _fireFailureReason,
                _reloadFailureReason);
        }
    }
}
