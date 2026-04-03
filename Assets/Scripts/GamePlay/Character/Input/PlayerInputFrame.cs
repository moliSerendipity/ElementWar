using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 原始玩家设备输入帧。
    /// 该结构只表达“这一帧设备给了什么”，不负责角色行为合法性判断。
    /// FireHeld 与 FirePressed 必须同时保留：
    /// 1）FirePressed 负责单发武器、起始边沿和表现触发；
    /// 2）FireHeld 负责自动武器持续开火。
    /// </summary>
    public readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(
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

        public static PlayerInputFrame Empty => new(
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
