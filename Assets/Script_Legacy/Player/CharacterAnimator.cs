using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private Animator animator;

    #region 动画参数哈希值
    private static readonly int speedHash = Animator.StringToHash("Speed");             // 根据动画参数阈值和输入强度设置
    private static readonly int inputXHash = Animator.StringToHash("InputX");           // 根据输入 (归一化) 设置
    private static readonly int inputZHash = Animator.StringToHash("InputZ");           // 根据输入 (归一化) 设置
    private static readonly int velocityYHash = Animator.StringToHash("VelocityY");

    private static readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int isAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int isDeadHash = Animator.StringToHash("IsDead");

    private static readonly int jumpHash = Animator.StringToHash("Jump");
    private static readonly int fireHash = Animator.StringToHash("Fire");
    private static readonly int reloadHash = Animator.StringToHash("Reload");
    private static readonly int hitHash = Animator.StringToHash("Hit");
    #endregion

    [Header("动画参数阈值")]
    public float runValue = 5.0f;                                                   // 跑步速度阈值
    public float sprintValue = 10.0f;                                               // 冲刺速度阈值

    [Header("动画阻尼时间")]
    [Tooltip("移动混合的阻尼时间")]
    public float locomotionDampTime = 0.1f;
    [Tooltip("瞄准混合的阻尼时间")]
    public float aimingDampTime = 0.1f;
    [Tooltip("拔枪/收枪（半身权重）过渡有多丝滑")]
    public float layerWeightLerpSpeed = 10f;

    // 缓存上层半身的Layer Index (我们之前定的是索引为 1)
    private const int UpperBodyLayerIndex = 1;
    private float targetLayerWeight = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 更新角色的移动相关动画参数 (每帧调用)，包括输入方向、速度
    /// </summary>
    /// <param name="_inputMove">移动输入</param>
    /// <param name="_isSprinting">是否冲刺</param>
    /// <param name="_isAiming">是否瞄准</param>
    public void UpdateLocomotion(Vector2 _inputMove, bool _isSprinting, bool _isAiming)
    {
        animator.SetFloat(inputXHash, _inputMove.x, aimingDampTime, Time.deltaTime);
        animator.SetFloat(inputZHash, _inputMove.y, aimingDampTime, Time.deltaTime);

        float finalSpeed = 0f;
        if (_inputMove.sqrMagnitude > 0f)
        {
            finalSpeed = _isSprinting ? sprintValue : runValue;
        }
        // 根据输入的强度调整速度参数
        finalSpeed *= _inputMove.magnitude;
        animator.SetFloat(speedHash, finalSpeed, locomotionDampTime, Time.deltaTime);
    }

    /// <summary>
    /// 更新角色的物理状态相关动画参数 (每帧调用)，包括是否在地面上和垂直速度
    /// </summary>
    /// <param name="_isGrounded">是否在地面上</param>
    /// <param name="_velocityY">垂直速度</param>
    public void UpdatePhysicsState(bool _isGrounded, float _velocityY)
    {
        animator.SetBool(isGroundedHash, _isGrounded);
        animator.SetFloat(velocityYHash, _velocityY);
    }

    /// <summary>
    /// 更新角色的瞄准状态动画参数 (每帧调用)
    /// </summary>
    /// <param name="_isAiming">是否瞄准</param>
    public void UpdateAiming(bool _isAiming)
    {
        animator.SetBool(isAimingHash, _isAiming);

        // 上半身智能混合检测逻辑
        // 判断当前 UpperBody 这层（Layer 1）是不是正在播放除了 Empty 以外的动作（例如 Firing, Reloading）
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(UpperBodyLayerIndex);
        bool isPlayingAction = !stateInfo.IsName("Empty");

        // 只有当按了右键瞄准，或者系统正在强行播开火/换弹动画时，上半身的权重才走向 1
        targetLayerWeight = (_isAiming || isPlayingAction) ? 1f : 0f;

        // 让上半身权重发生极度顺滑的过渡（Lerp）
        float currentWeight = animator.GetLayerWeight(UpperBodyLayerIndex);
        float newWeight = Mathf.Lerp(currentWeight, targetLayerWeight, Time.deltaTime * layerWeightLerpSpeed);

        animator.SetLayerWeight(UpperBodyLayerIndex, newWeight);
    }

    public void TriggerJump() => animator.SetTrigger(jumpHash);
    public void TriggerFire() => animator.SetTrigger(fireHash);
    public void TriggerReload() => animator.SetTrigger(reloadHash);
}
