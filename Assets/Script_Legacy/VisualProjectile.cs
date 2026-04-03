using System.Collections;
using UnityEngine;

/// <summary>
/// 纯视觉层射线弹道
/// </summary>
public class VisualProjectile : MonoBehaviour
{
    [Header("视觉参数")]
    public float visualSpeed = 300f;                                    // 虚假弹道的视觉飞行速度
    private Vector3 startPos;                                           // 起点
    private Vector3 targetPos;                                          // 目标落点（射线算出来的确切位置）

    private bool isFlying = false;

    // 这里可以暴露一些自定特效所需的拖尾 Renderer，初始化时换颜色
    private TrailRenderer trail;
    // 如果有实体发光的子弹网格模型，拖给这个参数，击中后将其隐藏，仅留拖尾消散
    public MeshRenderer bulletMesh;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    void Start()
    {

    }

    void Update()
    {
        if (!isFlying)
            return;

        // 向目标点进行线性插值匀速运动
        float distanceThisFrame = visualSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, distanceThisFrame);

        // 如果距离极近，或者已经抵达预定死点，自身生命周期结束，回收！
        if (Vector3.SqrMagnitude(transform.position - targetPos) < 0.01f)
        {
            isFlying = false;
            StartCoroutine(DespawnRoutine()); // 开启协程，延迟回收，让子弹飞出一种“没入墙壁/怪物”的自然消散感
            //GameObjectPool.Instance.Release(gameObject);
        }
    }

    private IEnumerator DespawnRoutine()
    {
        // 到达终点后，立马隐藏可能存在的子弹头实体模型
        if (bulletMesh != null) bulletMesh.enabled = false;

        // 关闭拖尾发射器（旧的拖尾节点还会留在原地慢慢缩小）
        if (trail != null)
        {
            trail.emitting = false;
            // 等待拖尾时间耗尽，再进行彻底隐藏。这里加上一点冗余时间 0.05 秒确保平滑
            yield return new WaitForSeconds(trail.time + 0.05f);
        }
        else
        {
            // 如果你没有配拖尾，命中就瞬间回收即可
            yield return null;
        }

        // 将其安全的交还给对象池
        GameObjectPool.Instance.Release(gameObject);
    }

    /// <summary>
    /// 开火瞬间立刻由 WeaponController 调用，配置好飞行的必达点
    /// </summary>
    public void Launch(Vector3 _muzzlePosition, Vector3 _targetHitPosition, int _elementType)
    {
        startPos = _muzzlePosition;
        targetPos = _targetHitPosition;
        transform.position = startPos;
        transform.LookAt(targetPos);

        // 如果有子弹实体Mesh，重置其显示
        if (bulletMesh != null)
            bulletMesh.enabled = true;

        if (trail != null)
        {
            trail.emitting = true;   // 允许发射拖尾
            trail.Clear(); // 必须重置拖尾，否则复用时会连线
            // （如果以后有材质）可以在这里根据 elementType 设置子弹光的颜色

            // 根据元素类型可以动态改变拖尾颜色，方便测试（可以换成HDR发光材质色）
            switch (_elementType)
            {
                case 2: trail.startColor = Color.red; break;      // 火
                case 3: trail.startColor = Color.magenta; break;  // 雷
                case 4: trail.startColor = Color.cyan; break;     // 冰
                default: trail.startColor = Color.yellow; break;  // 物理/常规
            }
        }

        isFlying = true;
    }
}
