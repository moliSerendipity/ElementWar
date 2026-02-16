using UnityEngine;

public class CharacterMoveState : CharacterState
{
    public CharacterMoveState(TPSCharacterController _tpsCC) : base(_tpsCC)
    {
    }

    public override void Update(InputFrame _inputFrame)
    {
        bool isAiming = _inputFrame.aimButton.isHeld;
        bool isSprinting = _inputFrame.sprintButton.isHeld && !isAiming;

        Vector3 moveDir = tpsCC.CalculateMoveDirectionAndRotation(_inputFrame.move, isAiming);

        float targetSpeed = tpsCC.GetTargetSpeed(_inputFrame.move, isAiming, isSprinting);

        tpsCC.Motor.SetPlanarVelocity(moveDir * targetSpeed);

        if (_inputFrame.move.sqrMagnitude < 0.01f)
        {
            tpsCC.SwitchState(tpsCC.IdleState);
        }

        if (_inputFrame.jumpButton.wasPressedThisFrame && tpsCC.Motor.IsGrounded)
        {
            tpsCC.SwitchState(tpsCC.JumpState);
        }
    }
}
