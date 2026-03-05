using UnityEngine;

/// <summary>
/// 实体投射物 (防穿墙弹道算法)
/// 职责：处理自身的高速飞行、重力下坠（抛物线）、以及每一帧的射线碰撞预判
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("弹道参数")]
    public float initialSpeed = 200f;                                   // 初始速度 (米/秒)
    public float gravityMultiplier = 1f;                                // 重力下坠倍率 (影响抛物线弧度)
    public float maxLifeTime = 5f;                                      // 最大存活时间，防止飞出地图边界内存泄漏
    private float lifeTimer = 0f;                                       // 存活计时器

    [Header("碰撞设置")]
    [Tooltip("子弹能击中哪些层")]
    public LayerMask hitMask;

    private Vector3 currentVelocity;                                    // 当前速度向量
    private bool isInitialized = false;                                 // 是否已初始化

    void Update()
    {
        if (!isInitialized)
            return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer > maxLifeTime)
        {
            isInitialized = false;
            GameObjectPool.Instance.Release(gameObject);
            return;
        }

        // 施加重力影响 (v = v0 + gt)
        currentVelocity += Physics.gravity * gravityMultiplier * Time.deltaTime;

        // 计算这一帧即将移动的距离和方向
        Vector3 nextPosition = transform.position + currentVelocity * Time.deltaTime;
        Vector3 direction = nextPosition - transform.position;
        float distanceThisFrame = direction.magnitude;

        // 在子弹真正移动过去之前，先发射一根长度等于此帧移动距离的射线
        if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, distanceThisFrame, hitMask))
        {
            transform.position = hit.point;
            HandleHit(hit);
        }
        else
        {
            transform.position = nextPosition;
            // 保持子弹头部朝向飞行方向
            transform.rotation = Quaternion.LookRotation(currentVelocity.normalized);
        }
    }

    /// <summary>
    /// 初始化子弹（由武器控制器调用）
    /// </summary>
    /// <param name="_direction">开火方向</param>
    public void Init(Vector3 _direction)
    {
        if (!isInitialized)
        {
            currentVelocity = _direction.normalized * initialSpeed;
            isInitialized = true;
            // 每次从池子里拿出来，重置计时器
            lifeTimer = 0f;
            // TrailRenderer 重新激活时可能会有一条飞过去的线，需要清除它
            TrailRenderer tr = GetComponent<TrailRenderer>();
            if (tr != null)
                tr.Clear();
        }
    }

    /// <summary>
    /// 处理命中逻辑
    /// </summary>
    private void HandleHit(RaycastHit _hit)
    {
        Debug.Log($"[Projectile] 命中物体: {_hit.collider.name}，坐标: {_hit.point}");

        // TODO: Phase 4 将在这里反向调用 Lua 进行元素附着与伤害计算

        // 命中后回收
        isInitialized = false;
        GameObjectPool.Instance.Release(gameObject);
    }
}
