using Cinemachine;
using UnityEngine;

public class CameraController : SingleMonoBase<CameraController>
{
    [Header("核心摄像机组件")]
    public CinemachineFreeLook freeLookCamera;

    [Header("回弹设置")]
    [Tooltip("自由视角松开后，镜头回弹到正后方的平滑时间")]
    public float recenterSmoothTime = 0.15f;

    public bool IsRecentering { get; private set; } = false;                    // 是否正在回弹
    private float recenterVelocity;                                             // SmoothDamp 需要的引用速度

    private float lockedXAxisValue;                                             // 按下 Alt 瞬间记录的完美无视差轨道值
    private bool wasFreeLookingLastFrame;                                       // 状态机前置判断

    protected override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (freeLookCamera == null || freeLookCamera.Follow == null)
            return;

        InputFrame inputFrame = InputManager.Instance.Frame;
        bool isFreeLooking = inputFrame.freeLookButton.isHeld;

        // 拍下快照：当玩家刚刚按下Alt 键时
        if (isFreeLooking && !wasFreeLookingLastFrame)
        {
            // 记录下此刻完美对准背后的轨道值
            lockedXAxisValue = freeLookCamera.m_XAxis.Value;
            IsRecentering = false; // 如果还在回弹，瞬间打断
        }

        // 触发回弹的条件：玩家刚松开 Alt 键
        if (inputFrame.freeLookButton.wasReleasedThisFrame)
        {
            IsRecentering = true;
            // 清空初速度，保证每次回弹都很柔和
            recenterVelocity = 0f;
        }

        // 打断回弹 (任何玩家的主动干预都会终止回弹)
        if (inputFrame.freeLookButton.isHeld || inputFrame.look.sqrMagnitude > 100f ||
            inputFrame.fireButton.isHeld || inputFrame.aimButton.isHeld)
        {
            IsRecentering = false;
        }

        // 执行平滑阻尼回弹
        if (IsRecentering)
        {
            float currentAngle = freeLookCamera.m_XAxis.Value;
            float targetAngle = lockedXAxisValue;

            // 使用 SmoothDampAngle 完美处理 360 度循环的平滑插值
            float newAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref recenterVelocity, recenterSmoothTime);
            freeLookCamera.m_XAxis.Value = newAngle;

            // 终止条件：如果角度已经极其接近，结束回弹状态，对齐目标
            if (Mathf.Abs(Mathf.DeltaAngle(newAngle, targetAngle)) < 0.1f)
            {
                freeLookCamera.m_XAxis.Value = targetAngle;
                IsRecentering = false;
            }
        }

        wasFreeLookingLastFrame = isFreeLooking;
    }

    /// <summary>
    /// 供外部系统调用，切换摄像机的焦点
    /// </summary>
    public void SetTarget(Transform _target)
    {
        if (freeLookCamera != null && _target != null)
        {
            freeLookCamera.Follow = _target;
            freeLookCamera.LookAt = _target;
        }
    }
}
