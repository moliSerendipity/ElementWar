using UnityEngine;

/// <summary>
/// 武器表现层控制器
/// 职责：管理枪口位置，生成实体子弹
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("武器表现配置")]
    [Tooltip("枪口位置，用于生成子弹的起始坐标")]
    public Transform muzzlePoint;
    [Tooltip("子弹预制体")]
    public GameObject projectilePrefab;

    public void FireProjectile(int _ammoConfigID)
    {
        if (muzzlePoint == null || projectilePrefab == null)
        {
            Debug.LogError("[WeaponController] 枪口点或子弹预制体未配置！");
            return;
        }

        // 获取屏幕正中心 (准星位置) 发出的射线
        Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        int playerLayer = LayerMask.NameToLayer("Player");
        int layerMask = ~(1 << playerLayer);

        // 2. 检测射线是否打中了东西 (1000米射程，排除Player层)
        // 注意：你需要定义一个 layerMask 排除玩家自己，这里简写
        if (Physics.Raycast(cameraRay, out RaycastHit hit, 1000f, layerMask))
        {
            targetPoint = hit.point; // 瞄准到了墙壁/怪物
        }
        else
        {
            targetPoint = cameraRay.GetPoint(1000f); // 瞄准到了天空
        }

        // 3. 计算真实的射击方向：从枪口指向瞄准点
        Vector3 realFireDirection = (targetPoint - muzzlePoint.position).normalized;

        // 生成子弹
        GameObject projectileObj = GameObjectPool.Instance.Get(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(realFireDirection));

        // 初始化弹道
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Init(realFireDirection);
        }
    }
}
