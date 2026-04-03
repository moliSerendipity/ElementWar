using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// Weapon 域只读表现态。
    /// HUD / Animation / SFX 统一读取这里，而不是直接读取 Runtime 或执行器内部临时字段。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponViewState : MonoBehaviour
    {
        [SerializeField] private string weaponDefinitionConfigId;
        [SerializeField] private int currentMagazineAmmo;
        [SerializeField] private int currentReserveAmmo;
        [SerializeField] private int maxMagazineAmmo;
        [SerializeField] private bool isReloading;
        [SerializeField] private bool isEmptyReload;
        [SerializeField] private float actualReloadDuration;
        [SerializeField] private bool fireTriggeredThisFrame;
        [SerializeField] private bool isFiring;
        [SerializeField] private float nextAllowedFireTime;
        [SerializeField] private float shotDistance;
        [SerializeField] private LayerMask hitLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction hitTriggerInteraction = QueryTriggerInteraction.Ignore;

        public string WeaponDefinitionConfigId => weaponDefinitionConfigId;
        public int CurrentMagazineAmmo => currentMagazineAmmo;
        public int CurrentReserveAmmo => currentReserveAmmo;
        public int MaxMagazineAmmo => maxMagazineAmmo;
        public bool IsReloading => isReloading;
        public bool IsEmptyReload => isEmptyReload;
        public float ActualReloadDuration => actualReloadDuration;
        public bool FireTriggeredThisFrame => fireTriggeredThisFrame;
        public bool IsFiring => isFiring;
        public float NextAllowedFireTime => nextAllowedFireTime;
        public float ShotDistance => shotDistance;
        public LayerMask HitLayerMask => hitLayerMask;
        public QueryTriggerInteraction HitTriggerInteraction => hitTriggerInteraction;

        /// <summary>
        /// 由 WeaponRuntime 在合法时机统一刷新只读表现态。
        /// </summary>
        public void Sync(WeaponRuntime _weaponRuntime)
        {
            // Runtime 为空时，直接拒绝同步，避免把旧状态误写成无效数据。
            if (_weaponRuntime == null)
            {
                return;
            }

            // 同步当前武器定义 id，供 HUD / Debug 识别当前武器来源。
            weaponDefinitionConfigId = _weaponRuntime.WeaponDefinitionConfigId;
            // 同步弹匣弹药数，供 HUD 与 Reload 表现读取。
            currentMagazineAmmo = _weaponRuntime.CurrentMagazineAmmo;
            // 同步备弹数，供 HUD 与换弹表现读取。
            currentReserveAmmo = _weaponRuntime.CurrentReserveAmmo;
            // 同步弹匣容量，供 HUD 进行上限显示。
            maxMagazineAmmo = _weaponRuntime.MaxMagazineAmmo;
            // 同步换弹事实，供 Animation / HUD 做只读表现。
            isReloading = _weaponRuntime.IsReloading;
            // 同步是否为空仓换弹，供表现层区分不同换弹表现。
            isEmptyReload = _weaponRuntime.IsEmptyReload;
            // 同步本次换弹时长，供 Reload 条与动画速度映射读取。
            actualReloadDuration = _weaponRuntime.ActualReloadDuration;
            // 同步本帧是否刚触发开火，供表现层做单帧触发反馈。
            fireTriggeredThisFrame = _weaponRuntime.FireTriggeredThisFrame;
            // 同步当前是否处于持续开火表现窗口。
            isFiring = _weaponRuntime.IsFiring;
            // 同步下一次允许开火时间，供 Debug / 表现层只读查看。
            nextAllowedFireTime = _weaponRuntime.NextAllowedFireTime;
            // 同步当前武器射程，统一相机逻辑瞄点、命中查询与视觉拖尾的距离口径。
            shotDistance = _weaponRuntime.Range;
            // 同步当前武器命中层级，统一相机逻辑瞄点与真实命中查询的过滤口径。
            hitLayerMask = _weaponRuntime.HitLayerMask;
            // 同步当前武器 Trigger 查询策略，避免相机与真实射线对 Trigger 的处理不一致。
            hitTriggerInteraction = _weaponRuntime.HitTriggerInteraction;
        }
    }
}
