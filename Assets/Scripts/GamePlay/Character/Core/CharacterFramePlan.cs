using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色当前帧执行计划。
    /// 只在当前帧内有效，由 CharacterDecisionResolver 统一产出。
    /// </summary>
    public readonly struct CharacterFramePlan
    {
        public CharacterFramePlan(
            Vector2 _moveVector,
            Vector2 _lookDelta,
            bool _aimActive,
            bool _sprintActive,
            bool _jumpTriggered,
            bool _firePressed,
            bool _fireHeld,
            bool _reloadTriggered,
            bool _switchTriggered,
            bool _skillTriggered)
        {
            MoveVector = _moveVector;
            LookDelta = _lookDelta;
            AimActive = _aimActive;
            SprintActive = _sprintActive;
            JumpTriggered = _jumpTriggered;
            FirePressed = _firePressed;
            FireHeld = _fireHeld;
            ReloadTriggered = _reloadTriggered;
            SwitchTriggered = _switchTriggered;
            SkillTriggered = _skillTriggered;
        }

        public Vector2 MoveVector { get; }
        public Vector2 LookDelta { get; }
        public bool AimActive { get; }
        public bool SprintActive { get; }
        public bool JumpTriggered { get; }
        public bool FirePressed { get; }
        public bool FireHeld { get; }
        public bool ReloadTriggered { get; }
        public bool SwitchTriggered { get; }
        public bool SkillTriggered { get; }
        public bool FireRequested => FirePressed || FireHeld;
        public bool HasMoveInput => MoveVector.sqrMagnitude > 0.0001f;
        public bool HasLookInput => LookDelta.sqrMagnitude > 0.0001f;

        public static CharacterFramePlan Empty => new(
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
