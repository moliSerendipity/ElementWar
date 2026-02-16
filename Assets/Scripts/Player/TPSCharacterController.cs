using UnityEngine;

public class TPSCharacterController : MonoBehaviour
{
    public CharacterMotor Motor { get; private set; }
    public CharacterAnimator Animator { get; private set; }
    public Transform MainCameraTransform { get; set; }

    [Header("移动设置")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float sprintSpeed = 10.0f;
    public float aimMoveSpeed = 2.5f;
    public float rotationSmoothTime = 0.1f;

    [Header("跳跃设置")]
    public float jumpHeight = 1.2f;
    public float gravity = -15.0f;

    #region 状态机
    private CharacterState currentState;
    public CharacterIdleState IdleState { get; private set; }
    public CharacterMoveState MoveState { get; private set; }
    public CharacterJumpState JumpState { get; private set; }
    #endregion

    #region 输入源
    private IInputSource inputSource;
    private PlayerInputSource playerInputSource;
    private AICharacterInputSource aiInputSource;
    #endregion

    // 辅助变量：用于平滑旋转
    private float targetRotationY;
    private float rotationVelocity;

    private void Awake()
    {
        Motor = GetComponent<CharacterMotor>();
        Animator = GetComponent<CharacterAnimator>();
        Motor.gravity = this.gravity;

        // 初始化输入源
        playerInputSource = new PlayerInputSource();
        aiInputSource = GetComponent<AICharacterInputSource>();

        // 初始化角色默认控制权
        SetPlayerControl(true);

        // 获取主相机 Transform (如果是 AI，不需要 MainCameraTransform，或者需要赋值为 LookTarget)
        if (inputSource == playerInputSource && Camera.main != null)
            MainCameraTransform = Camera.main.transform;

        // 初始化状态实例
        IdleState = new CharacterIdleState(this);
        MoveState = new CharacterMoveState(this);
        JumpState = new CharacterJumpState(this);
    }

    void Start()
    {
        SwitchState(IdleState);
    }

    void Update()
    {
        // 获取输入帧
        InputFrame inputFrame = inputSource.GetInputFrame();

        // 更新当前状态
        currentState?.Update(inputFrame);

        // 是否冲刺：按住冲刺键，未按住瞄准键，并且有移动输入
        bool allowSprint = inputFrame.sprintButton.isHeld && !inputFrame.aimButton.isHeld && inputFrame.move.magnitude > 0.1f;

        // 更新动画参数
        Animator.UpdateLocomotion(inputFrame.move, allowSprint, inputFrame.aimButton.isHeld);
        Animator.UpdatePhysicsState(Motor.IsGrounded, Motor.Velocity.y);
        Animator.UpdateAiming(inputFrame.aimButton.isHeld);

        Debug.Log(currentState.GetType().Name);
    }

    /// <summary>
    /// 切换角色控制权，玩家控制或AI控制
    /// </summary>
    /// <param name="_isPlayerControlled">是否是玩家控制</param>
    public void SetPlayerControl(bool _isPlayerControlled)
    {
        if (_isPlayerControlled)
        {
            inputSource = playerInputSource;
            // 禁用AI输入源
            if (aiInputSource != null)
                aiInputSource.enabled = false;

            // 这里可以添加开启摄像机跟随的代码
        }
        else
        {
            if (aiInputSource == null)
                return;
            inputSource = aiInputSource;
            aiInputSource.enabled = true;
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="_nextState">下一个状态</param>
    public void SwitchState(CharacterState _nextState)
    {
        currentState?.Exit();
        currentState = _nextState;
        currentState?.Enter();
    }

    /// <summary>
    /// 根据输入和相机计算角色的移动方向，并根据是否瞄准调整角色朝向
    /// </summary>
    /// <param name="_inputMove">移动输入</param>
    /// <param name="_isAiming">是否瞄准</param>
    /// <returns>角色移动方向</returns>
    public Vector3 CalculateMoveDirectionAndRotation(Vector2 _inputMove, bool _isAiming)
    {
        if (MainCameraTransform == null)
            return transform.forward * _inputMove.y + transform.right * _inputMove.x;

        // 计算相机的前向量和右向量，并将它们投影到水平面上 (归一化)
        Vector3 cameraForward = MainCameraTransform.forward;
        Vector3 cameraRight = MainCameraTransform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 根据输入和相机方向计算角色的移动方向 (归一化)
        Vector3 moveDir = (cameraForward * _inputMove.y + cameraRight * _inputMove.x).normalized;

        // 如果没有输入，并且不在瞄准状态，保持当前朝向，不需要旋转
        if (moveDir == Vector3.zero && !_isAiming)
            return Vector3.zero;

        // 根据是否瞄准来决定角色的目标朝向
        // 瞄准时角色朝向相机前方；非瞄准时角色朝向移动方向
        if (_isAiming)
        {
            targetRotationY = MainCameraTransform.eulerAngles.y;
        }
        else
        {
            if (moveDir.sqrMagnitude > 0.01f)
            {
                targetRotationY = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            }
        }

        // 平滑旋转角色朝向
        float smoothRotationY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotationY, ref rotationVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0, smoothRotationY, 0);

        return moveDir;
    }

    /// <summary>
    /// 根据移动输入、瞄准状态和冲刺状态计算角色的目标移动速度
    /// </summary>
    /// <param name="_inputMove">移动输入</param>
    /// <param name="_isAiming">是否瞄准</param>
    /// <param name="_isSprinting">是否冲刺</param>
    /// <returns>目标移动速度</returns>
    public float GetTargetSpeed(Vector2 _inputMove, bool _isAiming, bool _isSprinting)
    {
        if (_inputMove == Vector2.zero)
            return 0f;
        if (_isAiming)
            return aimMoveSpeed;
        if (_isSprinting)
            return sprintSpeed;

        return _inputMove.magnitude > 0.5f ? runSpeed : walkSpeed;
    }
}
