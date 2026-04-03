using UnityEngine;

/// <summary>
/// 武器表现层控制器
/// 职责：管理枪口位置，生成实体子弹
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("挂点与骨骼IK对接")]
    [Tooltip("角色骨骼系统里的真实右手武器挂点 (Socket)")]
    public Transform rightHandWeaponSocket;
    [Tooltip("副/闲置武器背后的腰间或背部挂点")]
    public Transform idleWeaponSocket;
    [Tooltip("Animation Rigging的左手 IK Target，换枪时需要移动它的位置去吸附枪械模型")]
    public Transform leftHandIKSolverTarget;

    [Header("视觉弹道系统")]
    [Tooltip("VisualProjectile的预制体，不带任何碰撞体积，纯飞行动画")]
    public GameObject visualProjectilePrefab;

    [Header("动态散布系统 (Bullet Spread)")]
    [Tooltip("准星的最大偏移率 (例如0.05代表偏离屏幕中心5%的距离)")]
    public float maxSpread = 0.05f;
    [Tooltip("连续开火时，每一发增加的散布值")]
    public float spreadPerShot = 0.008f; [Tooltip("停止开火时，准星散布的恢复速度")]
    public float spreadRecovery = 0.04f;

    // 当前积累的散布惩罚值
    private float currentSpread = 0f;

    // 当前正在手中的武器实例
    [SerializeField] private GameObject currentWeaponInstance;
    private RecoilComponent proceduralRecoil;
    [SerializeField] private Transform muzzlePoint;

    private Cinemachine.CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        proceduralRecoil = GetComponent<RecoilComponent>();
        impulseSource = GetComponent<Cinemachine.CinemachineImpulseSource>();
    }

    private void Update()
    {

        // 2. 枪械后坐力/散布 的随时间恢复机制 (只要不在开枪，准星就在收缩)
        if (currentSpread > 0)
        {
            currentSpread -= spreadRecovery * Time.deltaTime;
            currentSpread = Mathf.Max(0, currentSpread);
        }
    }

    /// <summary>
    /// 动态换枪模组。生成真实枪械并对齐IK（解决穿模、浮空手问题）
    /// </summary>
    public void EquipWeaponModel(GameObject weaponPrefab)
    {
        // 销毁旧武器表现
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }

        if (weaponPrefab == null)
            return;

        // 生成新武器并挂载到右手持握点
        currentWeaponInstance = Instantiate(weaponPrefab, rightHandWeaponSocket);
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;

        WeaponView weaponView = currentWeaponInstance.GetComponent<WeaponView>();
        if (weaponView != null)
        {
            muzzlePoint = weaponView.muzzlePoint;

            // IK 吸附处理
            if (weaponView.leftHandIKTarget != null && leftHandIKSolverTarget != null)
            {
                // 将全身系统的左手IK极点设置在新枪指定的挂点上
                // 使得切换大小不一的武器时，左手总能精准握在护木的位置
                leftHandIKSolverTarget.SetParent(weaponView.leftHandIKTarget);
                leftHandIKSolverTarget.localPosition = Vector3.zero;
                leftHandIKSolverTarget.localRotation = Quaternion.identity;
            }
        }
        else
        {
            Debug.LogError($"[WeaponController] 武器 {weaponPrefab.name} 缺少 WeaponView 脚本，无法绑定节点！");
        }
    }

    /// <summary>
    /// 即时命中型（Hitscan）射线开火算法
    /// </summary>
    public void FireHitscan(int _ammoConfigID, int _instigatorID, int _elementType, float _weaponRange = 100f)
    {
        if (muzzlePoint == null)
            return;

        // 在真正开枪发子弹的同一次帧流中，引发程序的反坐冲击震动！
        if (proceduralRecoil != null)
        {
            proceduralRecoil.GenerateRecoilPulse();
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        // 加入数学随机散布 (不再是绝对的正中心 0.5f, 0.5f)
        float offsetX = Random.Range(-currentSpread, currentSpread);
        float offsetY = Random.Range(-currentSpread, currentSpread);

        // 射线索敌 (获取摄像机正中央指向)
        Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f + offsetX, 0.5f + offsetY, 0));
        // 开火后，立刻积累惩罚值（如果一直按着不放，很快就堆叠到 maxSpread）
        currentSpread = Mathf.Min(currentSpread + spreadPerShot, maxSpread);
        Vector3 aimTargetPoint = cameraRay.GetPoint(_weaponRange); // 默认射向最远端

        // 避开玩家和忽略光线投射层
        int playerLayer = LayerMask.NameToLayer("Player");
        int layerMask = ~(1 << playerLayer | 1 << LayerMask.NameToLayer("Ignore Raycast"));

        // 查看视线落点究竟瞄准了什么
        if (Physics.Raycast(cameraRay, out RaycastHit aimHit, _weaponRange, layerMask))
        {
            // 确保准星打到的点位于枪口的前方（防止背后穿模瞄准）
            Vector3 toAimHit = aimHit.point - muzzlePoint.position;
            if (Vector3.Dot(cameraRay.direction, toAimHit) > 0)
                aimTargetPoint = aimHit.point;
        }

        // 真正的枪口实体判定射线
        // 因为人在掩体后，可能准星看得到怪，但是枪管被墙挡住了，所以必须从枪口再发一次射线！
        Vector3 realFireDirection = (aimTargetPoint - muzzlePoint.position).normalized;
        Vector3 finalHitPoint = aimTargetPoint;

        // 枪口到目标点，如果中间撞了物体，这就是实际被打中的东西
        float distanceToAimTarget = Vector3.Distance(muzzlePoint.position, aimTargetPoint);

        if (Physics.Raycast(muzzlePoint.position, realFireDirection, out RaycastHit bulletHit, distanceToAimTarget, layerMask))
        {
            finalHitPoint = bulletHit.point;

            // 向 Lua 发送展平的伤害信息，0 性能开销
            Hitbox hitbox = bulletHit.collider.GetComponent<Hitbox>();
            if (hitbox != null && hitbox.owner != null)
            {
                LuaManager.Instance.SendHitMessageFlat(
                    _instigatorID,
                    hitbox.owner.uid,
                    hitbox.owner.configID,
                    hitbox.owner.level,
                    _ammoConfigID,
                    hitbox.damageMultiplier
                );
            }
        }

        // 播放纯表现层的空壳飞行拖尾
        if (visualProjectilePrefab != null)
        {
            GameObject vfxObj = GameObjectPool.Instance.Get(visualProjectilePrefab, muzzlePoint.position, Quaternion.LookRotation(realFireDirection));
            VisualProjectile visProj = vfxObj.GetComponent<VisualProjectile>();
            if (visProj != null)
            {
                visProj.Launch(muzzlePoint.position, finalHitPoint, _elementType);
            }
        }

        // TO DO: 可以在此添加后坐力触发、屏幕抖动逻辑
    }

    //public void FireProjectile(int _ammoConfigID, int _instigatorID)
    //{
    //    if (muzzlePoint == null || projectilePrefab == null)
    //    {
    //        Debug.LogError("[WeaponController] 枪口点或子弹预制体未配置！");
    //        return;
    //    }

    //    // 获取屏幕正中心 (准星位置) 发出的射线
    //    Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    //    // 默认打向 1000 米外的远方
    //    Vector3 targetPoint = cameraRay.GetPoint(1000);

    //    int playerLayer = LayerMask.NameToLayer("Player");
    //    int layerMask = ~(1 << playerLayer);

    //    // 检测射线是否打中了东西 (1000米射程，排除Player层)
    //    if (Physics.Raycast(cameraRay, out RaycastHit hit, 1000f, layerMask))
    //    {
    //        // 通过计算“枪口点 -> 击中点”与“摄像机射线方向”的点乘，确保击中点在射线前方
    //        Vector3 toHit = hit.point - muzzlePoint.position;
    //        if (Vector3.Dot(cameraRay.direction, toHit) > 0)
    //            targetPoint = hit.point;
    //    }

    //    // 计算真实的射击方向：从枪口指向瞄准点
    //    Vector3 realFireDirection = (targetPoint - muzzlePoint.position).normalized;

    //    // 生成子弹
    //    GameObject projectileObj = GameObjectPool.Instance.Get(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(realFireDirection));

    //    // 初始化弹道
    //    Projectile projectile = projectileObj.GetComponent<Projectile>();
    //    if (projectile != null)
    //    {
    //        // 将子弹 ID 和 攻击者 ID 一并传给子弹
    //        projectile.Init(realFireDirection, _ammoConfigID, _instigatorID);
    //    }
    //}
}
