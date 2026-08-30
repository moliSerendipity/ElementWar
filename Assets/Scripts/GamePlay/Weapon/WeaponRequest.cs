namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// Weapon 域唯一正式请求。
    /// Character 只允许把与武器直接相关的动作意图转成这个对象，
    /// 不再把 Switch / Skill 等非 Weapon 语义混入武器裁决链。
    /// </summary>
    public readonly struct WeaponRequest
    {
        public WeaponRequest(
            bool _firePressed,
            bool _fireHeld,
            bool _reloadTriggered,
            bool _switchAmmoTriggered)
        {
            FirePressed = _firePressed;
            FireHeld = _fireHeld;
            ReloadTriggered = _reloadTriggered;
            SwitchAmmoTriggered = _switchAmmoTriggered;
        }

        public bool FirePressed { get; }
        public bool FireHeld { get; }
        public bool ReloadTriggered { get; }
        /// <summary>本帧是否请求切换当前武器实例的元素弹药来源。</summary>
        public bool SwitchAmmoTriggered { get; }
        public bool HasFireIntent => FirePressed || FireHeld;

        public static WeaponRequest Empty => new(false, false, false, false);
    }
}
