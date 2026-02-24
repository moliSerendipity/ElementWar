using UnityEngine;

public class CharacterMotor : MonoBehaviour
{
    private CharacterController cc;                                             // 角色控制器组件
    private Vector3 planarVelocity;                                             // 平面速度 (X, Z)
    private float verticalVelocity;                                             // 垂直速度 (Y)

    private float lastJumpTime;                                                 // 上次跳跃的时间
    private const float jumpCooldown = 0.2f;                                    // 跳跃冷却时间，防止连续跳跃

    [Header("物理设置")]
    public float gravity = -9.81f;                                              // 重力加速度
    [Tooltip("贴地力，防止下坡时角色悬空")]
    public float stickToGroundForce = -2f;

    public Vector3 Velocity => cc.velocity;                                     // 当前角色速度
    public bool IsGrounded => cc.isGrounded;                                    // 是否在地面上

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {

    }

    void Update()
    {
        ProcessGravity();
        ProcessMovement();
    }

    /// <summary>
    /// 处理重力和贴地力的应用
    /// </summary>
    private void ProcessGravity()
    {
        // 如果刚跳跃不久，强制不应用贴地力
        bool isJustJumped = Time.time < lastJumpTime + jumpCooldown;

        if (cc.isGrounded && !isJustJumped)
        {
            // 只有当垂直速度本来就是向下（<= 0）时，才强制设置为贴地力。
            // 如果 verticalVelocity > 0（说明刚按了跳跃），不能覆盖
            if (verticalVelocity <= 0)
                verticalVelocity = stickToGroundForce;

        }
        else
        {
            // 如果在空中，应用重力加速度
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    /// <summary>
    /// 处理角色的移动，结合平面速度和垂直速度，并使用角色控制器进行移动
    /// </summary>
    private void ProcessMovement()
    {
        // 合并平面速度和垂直速度，形成最终的角色移动速度
        Vector3 finalVelocity = planarVelocity;
        finalVelocity.y = verticalVelocity;

        // 使用角色控制器移动角色，乘以 Time.deltaTime 以确保帧率独立的移动
        cc.Move(finalVelocity * Time.deltaTime);

        // 移动后重置平面速度 (因为是瞬时速度，除非状态机一直设置它)
        // 如果要惯性，不要在这里重置，而是用 Lerp 设置 planarVelocity
        planarVelocity = Vector3.zero;
    }

    /// <summary>
    /// 设置角色的平面速度，保持垂直速度不变
    /// </summary>
    /// <param name="_planarVelocity">平面速度</param>
    public void SetPlanarVelocity(Vector3 _planarVelocity)
    {
        planarVelocity.x = _planarVelocity.x;
        planarVelocity.y = 0;
        planarVelocity.z = _planarVelocity.z;
    }

    /// <summary>
    /// 如果在地面上，根据指定的跳跃高度应用跳跃，计算需要的初始垂直速度
    /// </summary>
    /// <param name="_jumpHeight">跳跃高度</param>
    public void ApplyJumpHeight(float _jumpHeight)
    {
        // 根据跳跃高度计算需要的初始垂直速度，v = sqrt(-2 * g * h)
        verticalVelocity = Mathf.Sqrt(-2f * gravity * _jumpHeight);
        // 记录跳跃时间
        lastJumpTime = Time.time;
    }

    /// <summary>
    /// 瞬移角色到指定位置和旋转，清空水平动量，防止瞬移后角色因为之前的惯性飞出去
    /// </summary>
    /// <param name="_targetPosition">目标坐标</param>
    /// <param name="_targetRotation">目标旋转</param>
    public void Teleport(Vector3 _targetPosition, Quaternion _targetRotation)
    {
        // 必须先关闭 CharacterController，否则 Unity 物理底层会覆盖修改
        cc.enabled = false;
        // 赋值新坐标和旋转
        transform.position = _targetPosition;
        transform.rotation = _targetRotation;
        // 清空水平残留动量，防止瞬移后角色因为之前的惯性飞出去
        planarVelocity = Vector3.zero;
        verticalVelocity = stickToGroundForce;
        // 重启 CharacterController
        cc.enabled = true;
        // 使 cc.isGrounded 立即更新，防止瞬移后角色悬空
        cc.Move(Vector3.down * 0.05f);
    }
}
