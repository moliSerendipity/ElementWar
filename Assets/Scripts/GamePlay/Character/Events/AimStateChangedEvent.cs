using UnityEngine;

namespace Game.Gameplay.Character.Events
{
    /// <summary>
    /// 角色瞄准事实状态变化事件。
    /// </summary>
    public readonly struct AimStateChangedEvent
    {
        public AimStateChangedEvent(GameObject _characterObject, bool _isAiming)
        {
            CharacterObject = _characterObject;
            IsAiming = _isAiming;
        }

        public GameObject CharacterObject { get; }
        public bool IsAiming { get; }
    }
}
