using UnityEngine;

namespace Game.Gameplay.Weapon.Events
{
    /// <summary>
    /// 已提交的换弹事实状态变化事件。
    /// </summary>
    public readonly struct ReloadStateChangedEvent
    {
        public ReloadStateChangedEvent(GameObject _weaponObject, string _weaponConfigId, bool _isReloading, bool _isEmptyReload)
        {
            WeaponObject = _weaponObject;
            WeaponConfigId = _weaponConfigId;
            IsReloading = _isReloading;
            IsEmptyReload = _isEmptyReload;
        }

        public GameObject WeaponObject { get; }
        public string WeaponConfigId { get; }
        public bool IsReloading { get; }
        public bool IsEmptyReload { get; }
    }
}
