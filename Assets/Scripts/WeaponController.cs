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
    public GameObject bulletPrefab;

    public void FireProjectile(int _ammoConfigID)
    {
        if (muzzlePoint == null || bulletPrefab == null)
        {
            Debug.LogError("[WeaponController] 枪口点或子弹预制体未配置！");
            return;
        }

        // 生成子弹
        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // 初始化弹道
        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
        {
            // 这里的发射方向应该是相机的准星方向。
            // 对于 Demo 而言，我们暂时使用 MuzzlePoint 的正前方。
            projectile.Init(muzzlePoint.forward);
        }
    }
}
