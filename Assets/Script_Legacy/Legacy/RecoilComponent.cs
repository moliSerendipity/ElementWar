using UnityEngine;

/// <summary>
/// 纯数学驱动的物理后坐力组件（修复滑枪穿模版）
/// 原理：入侵 Animator 的渲染流水线，在它定型后，对关键脊椎节点叠加一层高能物理力矩！
/// </summary>
public class RecoilComponent : MonoBehaviour
{
    [Header("半身骨骼物理联动")]
    [Tooltip("核心部位：拖入你的角色大纲里的脊椎骨(Spine / UpperChest)")]
    public Transform spineBone;

    public float bodyRecoilSpeed = 40f;             // 爆发力：向后震的速度
    public float bodyReturnSpeed = 15f;             // 肌肉复位：回复原状的速度[Tooltip("极其关键：因为各个体型的骨架坐标轴不一样！如果填 -4 的时候身子没有向后仰，而是往左右拐了，请你把这 -4 换到 Y 或者 Z 槽位上去试！直到能完美后仰即可。")]
    public Vector3 bodyRotKick = new Vector3(-4f, 1f, 1f); [Header("摄像机准星后坐力上跳")]
    public float cameraRecoilSpeed = 30f;
    public float cameraReturnSpeed = 8f;
    public Vector2 cameraKickAmount = new Vector2(0.2f, -1.0f); // X是随机准星水平抖动，Y是上跳

    // 弹簧变量池
    private Vector3 currentBodyRot;
    private Vector3 targetBodyRot;
    private Vector2 currentCameraOffset;
    private Vector2 targetCameraOffset;

    void Update()
    {
        // 核心归零回流引擎 (肌肉卸力)
        targetBodyRot = Vector3.Lerp(targetBodyRot, Vector3.zero, bodyReturnSpeed * Time.deltaTime);
        targetCameraOffset = Vector2.Lerp(targetCameraOffset, Vector2.zero, cameraReturnSpeed * Time.deltaTime);

        // 高速咬合引擎 (视觉瞬冲)
        currentBodyRot = Vector3.Slerp(currentBodyRot, targetBodyRot, bodyRecoilSpeed * Time.deltaTime);
        currentCameraOffset = Vector2.Lerp(currentCameraOffset, targetCameraOffset, cameraRecoilSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 最伟大的魔法通道：在系统算完瞄准动画的最后一刻，加料！
    /// </summary>
    private void LateUpdate()
    {
        // 我们利用纯粹的四元数(Quaternion)相乘，这就等价于在这个骨头的本来姿势上“加上一笔旋转偏移”
        if (spineBone != null && currentBodyRot != Vector3.zero)
        {
            spineBone.localRotation *= Quaternion.Euler(currentBodyRot);
        }
    }

    /// <summary>
    /// 开火瞬间立刻调用！爆发向后的寸劲和上跳的阻力！
    /// </summary>
    public void GenerateRecoilPulse()
    {
        // 对脊椎施加后仰扭力
        targetBodyRot += new Vector3(
            bodyRotKick.x,
            Random.Range(-bodyRotKick.y, bodyRotKick.y),
            Random.Range(-bodyRotKick.z, bodyRotKick.z));

        // 对镜头施加上跳扭力
        targetCameraOffset += new Vector2(
            Random.Range(-cameraKickAmount.x, cameraKickAmount.x),
            cameraKickAmount.y);
    }

    public void ConsumeCameraRecoil(out Vector2 outOffset)
    {
        outOffset = currentCameraOffset;
        currentCameraOffset = Vector2.zero;
    }
}