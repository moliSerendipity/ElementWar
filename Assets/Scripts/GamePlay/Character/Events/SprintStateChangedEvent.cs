using UnityEngine;

namespace Game.Gameplay.Character.Events
{
    /// <summary>
    /// 角色冲刺事实状态变化事件。
    /// </summary>
    public readonly struct SprintStateChangedEvent
    {
        public SprintStateChangedEvent(GameObject _characterObject, bool _isSprinting)
        {
            CharacterObject = _characterObject;
            IsSprinting = _isSprinting;
        }

        public GameObject CharacterObject { get; }
        public bool IsSprinting { get; }
    }
}
