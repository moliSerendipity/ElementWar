using UnityEngine;

namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// 武器弹药运行时组件。
    /// 当前阶段先采用“武器持有弹匣 + 本地备弹”的最小闭环方案，
    /// 后续如果接入共享背包，reserveAmmo 的真实来源会迁移到共享库存域。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponAmmoComponent : MonoBehaviour
    {
        [Header("Debug / ReadOnly")]
        [SerializeField] private int currentMagazineAmmo;
        [SerializeField] private int currentReserveAmmo;
        [SerializeField] private int maxMagazineAmmo;
        [SerializeField] private int maxReserveAmmo;
        [SerializeField] private bool isInitialized;

        /// <summary>
        /// 当前弹匣子弹数。
        /// </summary>
        public int CurrentMagazineAmmo => currentMagazineAmmo;

        /// <summary>
        /// 当前备弹数。
        /// 当前阶段先作为武器本地备弹使用，后续共享背包接入后会替换为库存映射值。
        /// </summary>
        public int CurrentReserveAmmo => currentReserveAmmo;

        /// <summary>
        /// 弹匣上限。
        /// </summary>
        public int MaxMagazineAmmo => maxMagazineAmmo;

        /// <summary>
        /// 备弹上限。
        /// </summary>
        public int MaxReserveAmmo => maxReserveAmmo;

        /// <summary>
        /// 是否已经完成初始化。
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 使用“满弹匣 + 满备弹”规则初始化当前武器弹药状态。
        /// </summary>
        public void InitializeFromCapacity(int _magazineCapacity, int _reserveAmmoCapacity)
        {
            maxMagazineAmmo = Mathf.Max(0, _magazineCapacity);
            maxReserveAmmo = Mathf.Max(0, _reserveAmmoCapacity);
            currentMagazineAmmo = maxMagazineAmmo;
            currentReserveAmmo = maxReserveAmmo;
            isInitialized = true;
        }

        /// <summary>
        /// 清空当前运行时弹药状态。
        /// </summary>
        public void ResetRuntimeState()
        {
            currentMagazineAmmo = 0;
            currentReserveAmmo = 0;
            maxMagazineAmmo = 0;
            maxReserveAmmo = 0;
            isInitialized = false;
        }

        /// <summary>
        /// 当前弹匣是否还有子弹。
        /// </summary>
        public bool HasAmmoInMagazine()
        {
            return currentMagazineAmmo > 0;
        }

        /// <summary>
        /// 当前是否存在备弹。
        /// </summary>
        public bool HasReserveAmmo()
        {
            return currentReserveAmmo > 0;
        }

        /// <summary>
        /// 弹匣是否未满。
        /// </summary>
        public bool NeedsReload()
        {
            return isInitialized && currentMagazineAmmo < maxMagazineAmmo;
        }

        /// <summary>
        /// 弹匣是否为空。
        /// </summary>
        public bool IsMagazineEmpty()
        {
            return currentMagazineAmmo <= 0;
        }

        /// <summary>
        /// 弹匣是否已满。
        /// </summary>
        public bool IsMagazineFull()
        {
            return isInitialized && currentMagazineAmmo >= maxMagazineAmmo;
        }

        /// <summary>
        /// 当前弹匣还缺多少发子弹。
        /// </summary>
        public int GetMissingMagazineAmmoCount()
        {
            if (isInitialized == false)
            {
                return 0;
            }

            return Mathf.Max(0, maxMagazineAmmo - currentMagazineAmmo);
        }

        /// <summary>
        /// 计算当前最多还能装填多少发。
        /// </summary>
        public int GetReloadableAmmoCount()
        {
            if (isInitialized == false)
            {
                return 0;
            }

            int missingAmmoCount = GetMissingMagazineAmmoCount();
            return Mathf.Clamp(currentReserveAmmo, 0, missingAmmoCount);
        }

        /// <summary>
        /// 尝试消耗指定数量的弹匣子弹。
        /// 这里只负责武器本地弹匣，不判断开火时序与其他武器状态。
        /// </summary>
        public bool TryConsumeMagazineAmmo(int _consumeCount)
        {
            if (isInitialized == false)
            {
                return false;
            }

            if (_consumeCount <= 0)
            {
                return false;
            }

            if (currentMagazineAmmo < _consumeCount)
            {
                return false;
            }

            currentMagazineAmmo -= _consumeCount;
            return true;
        }

        /// <summary>
        /// 执行一次整匣补弹。
        /// 返回本次实际装填进弹匣的数量。
        /// 当前阶段补弹来源仍然是本地备弹；共享背包接入后这里会改为库存消耗入口。
        /// </summary>
        public int ReloadMagazineFromReserve()
        {
            if (isInitialized == false)
            {
                return 0;
            }

            int reloadableAmmoCount = GetReloadableAmmoCount();
            if (reloadableAmmoCount <= 0)
            {
                return 0;
            }

            currentMagazineAmmo += reloadableAmmoCount;
            currentReserveAmmo -= reloadableAmmoCount;
            return reloadableAmmoCount;
        }
    }
}
