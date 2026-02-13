using UnityEngine;

public class CharacterMotor : MonoBehaviour
{
    [Header("物理设置")]
    public float gravity = -9.81f;                                              // 重力加速度
    [Tooltip("贴地力，防止下坡时角色悬空")]
    public float stickToGroundForce = -2f;

    private CharacterController cc;                                             // 角色控制器组件
    private Vector3 planarVelocity;                                             // 平面速度 (X, Z)
    private float verticalVelocity;                                             // 垂直速度 (Y)

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
        if (cc.isGrounded)
        {
            // 如果在地面上，重置垂直速度并施加贴地力
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
        // 这一步很重要，这决定了是否有惯性。
        // 如果想要惯性，不要在这里重置，而是用 Lerp 设置 planarVelocity
        planarVelocity = Vector3.zero;
    }

    /// <summary>
    /// 设置角色的平面速度，保持垂直速度不变
    /// </summary>
    /// <param name="_planarVelocity"></param>
    public void SetPlanarVelocity(Vector3 _planarVelocity)
    {
        planarVelocity.x = _planarVelocity.x;
        planarVelocity.y = 0;
        planarVelocity.z = _planarVelocity.z;
    }

    /// <summary>
    /// 如果在地面上，根据指定的跳跃高度应用跳跃，计算需要的初始垂直速度
    /// </summary>
    /// <param name="_jumpHeight"></param>
    public void ApplyJumpHeight(float _jumpHeight)
    {
        if (cc.isGrounded)
        {
            // 根据跳跃高度计算需要的初始垂直速度，v = sqrt(-2 * g * h)
            verticalVelocity = Mathf.Sqrt(-2f * gravity * _jumpHeight);
        }
    }
}
