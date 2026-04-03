using UnityEngine;

namespace Game.Gameplay.Weapon.Events
{
    /// <summary>
    /// 已提交的空仓触发事件。
    /// 只在本帧存在 FirePressed，且武器因弹匣为空而未能真正开火时发布一次。
    /// </summary>
    public readonly struct WeaponDryFireEvent
    {
        /// <summary>
        /// 构造空仓触发事件。
        /// </summary>
        public WeaponDryFireEvent(GameObject _weaponObject, string _weaponConfigId)
        {
            WeaponObject = _weaponObject;
            WeaponConfigId = _weaponConfigId;
        }

        /// <summary>
        /// 触发空仓反馈的武器对象。
        /// </summary>
        public GameObject WeaponObject { get; }

        /// <summary>
        /// 触发空仓反馈的武器配置 Id。
        /// </summary>
        public string WeaponConfigId { get; }
    }
}
