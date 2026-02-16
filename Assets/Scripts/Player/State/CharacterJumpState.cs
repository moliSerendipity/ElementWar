using UnityEngine;

public class CharacterJumpState : CharacterState
{
    // 用于记录起跳时的水平速度（惯性）
    private Vector3 momentumVelocity;
    // 空中控制力：决定在空中能多快地改变方向 (数值越小惯性越大，数值越大越灵活)
    private float airControl = 2.0f;

    public CharacterJumpState(TPSCharacterController _tpsCC) : base(_tpsCC)
    {
    }

    public override void Enter()
    {
        tpsCC.Motor.ApplyJumpHeight(tpsCC.jumpHeight);
        tpsCC.Animator.TriggerJump();

        // 计算起跳瞬间的水平速度
        Vector3 currentVelocity = tpsCC.Motor.Velocity;
        currentVelocity.y = 0;

        // 记录惯性速度
        momentumVelocity = currentVelocity;
    }

    public override void Update(InputFrame _inputFrame)
    {
        bool isAiming = _inputFrame.aimButton.isHeld;

        Vector3 moveDir = tpsCC.CalculateMoveDirectionAndRotation(_inputFrame.move, isAiming);

        // 目标速度：玩家想要达到的速度
        float targetSpeed = isAiming ? tpsCC.aimMoveSpeed : momentumVelocity.magnitude;

        // 如果起跳时是静止的，空中移动最大给个 runSpeed
        if (targetSpeed < tpsCC.walkSpeed)
            targetSpeed = tpsCC.runSpeed;

        Vector3 targetVelocity = moveDir * targetSpeed;
        momentumVelocity = Vector3.MoveTowards(momentumVelocity, targetVelocity, tpsCC.runSpeed * airControl * Time.deltaTime);

        tpsCC.Motor.SetPlanarVelocity(momentumVelocity);

        if (tpsCC.Motor.IsGrounded)
        {
            if (_inputFrame.move.sqrMagnitude > 0.01f)
            {
                tpsCC.SwitchState(tpsCC.MoveState);
            }
            else
            {
                tpsCC.SwitchState(tpsCC.IdleState);
            }
        }
    }
}
