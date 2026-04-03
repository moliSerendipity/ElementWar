using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色统一消费的输入语义。
    /// 与具体设备解耦，后续玩家与 AI 都必须产出同构数据。
    /// FireHeld 与 FirePressed 同时存在，避免把“持续开火”和“单帧触发”混成一套语义。
    /// </summary>
    public readonly struct InputContext
    {
        public InputContext(
            Vector2 _moveVector,
            Vector2 _lookDelta,
            bool _sprintHeld,
            bool _jumpPressed,
            bool _firePressed,
            bool _fireHeld,
            bool _aimHeld,
            bool _reloadPressed,
            bool _switchPressed,
            bool _skillPressed)
        {
            MoveVector = _moveVector;
            LookDelta = _lookDelta;
            SprintHeld = _sprintHeld;
            JumpPressed = _jumpPressed;
            FirePressed = _firePressed;
            FireHeld = _fireHeld;
            AimHeld = _aimHeld;
            ReloadPressed = _reloadPressed;
            SwitchPressed = _switchPressed;
            SkillPressed = _skillPressed;
        }

        public Vector2 MoveVector { get; }

        public Vector2 LookDelta { get; }

        public bool SprintHeld { get; }

        public bool JumpPressed { get; }

        public bool FirePressed { get; }
        
        public bool FireHeld { get; }
        
        public bool AimHeld { get; }

        public bool ReloadPressed { get; }

        public bool SwitchPressed { get; }

        public bool SkillPressed { get; }

        public bool HasMovement => MoveVector.sqrMagnitude > 0f;

        public bool HasLookDelta => LookDelta.sqrMagnitude > 0f;

        public static InputContext Empty => new(
            Vector2.zero,
            Vector2.zero,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false);
    }
}
