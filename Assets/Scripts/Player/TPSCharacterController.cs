using UnityEngine;
using XLua;

[LuaCallCSharp]
public class TPSCharacterController : MonoBehaviour
{
    public CharacterMotor Motor { get; private set; }
    public CharacterAnimator Animator { get; private set; }
    public Transform MainCameraTransform { get; set; }
    public WeaponController WeaponController { get; private set; }

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
    // 记录上一帧是否处于无视摄像机的状态
    private bool wasIgnoringCameraLastFrame = false;

    private void Awake()
    {
        Motor = GetComponent<CharacterMotor>();
        Animator = GetComponent<CharacterAnimator>();
        WeaponController = GetComponent<WeaponController>();
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
    /// 移动与转向计算
    /// </summary>
    /// <param name="_inputMove">移动输入</param>
    /// <param name="_isFreeLooking">是否按住了 Alt 键</param>
    /// <returns>角色移动方向</returns>
    public Vector3 CalculateMoveDirectionAndRotation(Vector2 _inputMove, bool _isFreeLooking)
    {
        Vector3 referenceForward;
        Vector3 referenceRight;

        // 获取摄像机是否正在处于回弹追赶状态
        bool isCameraRecentering = CameraController.Instance != null && CameraController.Instance.IsRecentering;

        // 只要按住 Alt，或者摄像机正在弹回来，角色的身体逻辑必须与摄像机彻底断开
        bool shouldIgnoreCamera = _isFreeLooking || isCameraRecentering;

        // 确定移动参照系
        if (!shouldIgnoreCamera && MainCameraTransform != null)
        {
            // 常规战斗状态：以摄像机为准
            referenceForward = MainCameraTransform.forward;
            referenceRight = MainCameraTransform.right;
        }
        else
        {
            // 自由观察或回弹状态中：以角色自己当前的身体朝向为准
            referenceForward = transform.forward;
            referenceRight = transform.right;
        }

        // 计算参照系的前向量和右向量，并将它们投影到水平面上 (归一化)
        referenceForward.y = 0;
        referenceRight.y = 0;
        referenceForward.Normalize();
        referenceRight.Normalize();

        // 根据输入和参照系计算角色的移动方向 (归一化)
        Vector3 moveDir = (referenceForward * _inputMove.y + referenceRight * _inputMove.x).normalized;

        // 当从“锁定状态”恢复到“跟随状态”的那一帧
        if (wasIgnoringCameraLastFrame && !shouldIgnoreCamera)
        {
            // 强行把被冻结的“旋转弹簧”速度清零，斩断过冲导致的抽搐现象
            rotationVelocity = 0f;
        }

        // 更新历史状态
        wasIgnoringCameraLastFrame = shouldIgnoreCamera;

        // 处理角色身体的旋转
        if (!shouldIgnoreCamera)
        {
            if (MainCameraTransform != null)
                targetRotationY = MainCameraTransform.eulerAngles.y;

            // 平滑旋转角色朝向
            float smoothRotationY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotationY, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0, smoothRotationY, 0);
        }

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

    #region 供 Lua 调用的接口
    /// <summary>
    /// 强制触发换弹，供 Lua 调用
    /// </summary>
    public void ForceReload()
    {
        Animator.TriggerReload();
        Debug.Log("[C#] 收到 Lua 指令，开始播放换弹动画！");
    }

    /// <summary>
    /// 触发开火表现，供 Lua 调用
    /// </summary>
    /// <param name="_ammoConfigID">使用的子弹ID</param>
    public void ForceFire(int _ammoConfigID)
    {
        if (WeaponController != null)
        {
            WeaponController.FireProjectile(_ammoConfigID);
        }
        else
        {
            Debug.LogWarning("[C#] 当前角色没有挂载 WeaponController！");
        }
    }
    #endregion
}
