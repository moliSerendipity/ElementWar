using UnityEngine;

public class CharacterIdleState : CharacterState
{
    public CharacterIdleState(TPSCharacterController _tpsCC) : base(_tpsCC)
    {
    }

    public override void Enter()
    {
        // 进入 Idle 状态时，确保水平速度为零
        tpsCC.Motor.SetPlanarVelocity(Vector3.zero);
    }

    public override void Update(InputFrame _inputFrame)
    {
        if (_inputFrame.move.sqrMagnitude > 0.01f)
        {
            tpsCC.SwitchState(tpsCC.MoveState);
            return;
        }

        if (_inputFrame.jumpButton.wasPressedThisFrame && tpsCC.Motor.IsGrounded)
        {
            tpsCC.SwitchState(tpsCC.JumpState);
            return;
        }

        // 待机时也时刻保持面向摄像机，除非按住 Alt
        bool isFreeLooking = _inputFrame.freeLookButton.isHeld;
        tpsCC.CalculateMoveDirectionAndRotation(Vector2.zero, isFreeLooking);
    }
}
