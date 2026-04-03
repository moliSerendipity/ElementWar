using Game.Definition.ConfigSystem.Core;
using Game.Definition.Presentation;
using Game.Definition.Weapon;
using Game.Gameplay.Character;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// 武器域唯一长期状态。
    ///
    /// 约束：
    /// 1. 仍然保持 WeaponRuntime 为唯一长期真相源，不拆第二个平级 Runtime；
    /// 2. 但把配置引用、运行时事实和初始化流程显式分区，避免单个 Initialize 膨胀；
    /// 3. 初始化阶段一次性解析正式子配置，热路径不再继续散读配置。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string weaponDefinitionConfigId;
        [SerializeField] private WeaponDefinitionConfig weaponDefinitionConfig;
        [SerializeField] private WeaponStatConfig weaponStatConfig;
        [SerializeField] private WeaponSpreadConfig weaponSpreadConfig;
        [SerializeField] private WeaponRecoilConfig weaponRecoilConfig;
        [SerializeField] private WeaponReloadConfig weaponReloadConfig;
        [SerializeField] private WeaponPresentationConfig weaponPresentationConfig;

        [Header("References")]
        [SerializeField] private WeaponAmmoComponent weaponAmmoComponent;
        [SerializeField] private WeaponCommandResolver weaponCommandResolver;
        [SerializeField] private WeaponFireExecutor weaponFireExecutor;
        [SerializeField] private WeaponViewState weaponViewState;

        [Header("Definition")]
        [SerializeField] private bool canAim = true;
        [SerializeField] private bool canSprintFire;
        [SerializeField] private bool canFireInAir = true;

        [Header("Stat")]
        [SerializeField] private float damage;
        [SerializeField] private float headShotDamageMultiplier = 2f;
        [SerializeField] private float weakPointDamageMultiplier = 1.5f;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private int burstCount = 1;
        [SerializeField] private float burstInterval = 0.08f;
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int reserveAmmoCapacity = 180;
        [SerializeField] private float range = 1000f;
        [SerializeField] private LayerMask hitLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction hitTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private int penetrationCount;
        [SerializeField] private float penetrationDamageDecay;

        [Header("Handling")]
        [SerializeField] private float currentSpread;

        [Header("Pending Recoil")]
        [SerializeField] private float pendingRecoilPitch;
        [SerializeField] private float pendingRecoilYaw;

        [Header("Reload")]
        [SerializeField] private WeaponReloadType reloadType = WeaponReloadType.Magazine;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private float tacticalReloadDuration = 1.5f;
        [SerializeField] private float perBulletReloadDuration;
        [SerializeField] private bool allowInterruptReload = true;
        [SerializeField] private bool allowFireBreakReload;
        [SerializeField] private bool allowSwitchBreakReload = true;

        [Header("Fire Presentation")]
        [SerializeField] private float minFirePoseHoldDuration = 0.08f;
        [SerializeField] private float maxFirePoseHoldDuration = 0.16f;
        [SerializeField] private float firePoseHoldUntil;

        [Header("Runtime Facts")]
        [SerializeField] private bool isReloading;
        [SerializeField] private bool isEmptyReload;
        [SerializeField] private bool hasCommittedReloadAmmoThisCycle;
        [SerializeField] private float actualReloadDuration;
        [SerializeField] private float reservedReloadTime;
        [SerializeField] private float nextAllowedFireTime;
        [SerializeField] private bool fireTriggeredThisFrame;
        [SerializeField] private bool isFiring;
        [SerializeField] private bool isInitialized;

        #region 属性

        public string WeaponDefinitionConfigId => weaponDefinitionConfigId;
        public WeaponDefinitionConfig WeaponDefinitionConfig => weaponDefinitionConfig;
        public WeaponStatConfig WeaponStatConfig => weaponStatConfig;
        public WeaponSpreadConfig WeaponSpreadConfig => weaponSpreadConfig;
        public WeaponRecoilConfig WeaponRecoilConfig => weaponRecoilConfig;
        public WeaponReloadConfig WeaponReloadConfig => weaponReloadConfig;
        public WeaponPresentationConfig WeaponPresentationConfig => weaponPresentationConfig;
        public WeaponAmmoComponent WeaponAmmoComponent => weaponAmmoComponent;
        public WeaponViewState WeaponViewState => weaponViewState;
        public bool CanAim => canAim;
        public bool CanSprintFire => canSprintFire;
        public bool CanFireInAir => canFireInAir;
        public float Damage => damage;
        public float HeadShotDamageMultiplier => headShotDamageMultiplier;
        public float WeakPointDamageMultiplier => weakPointDamageMultiplier;
        public float FireInterval => fireInterval;
        public int BurstCount => burstCount;
        public float BurstInterval => burstInterval;
        public int MagazineSize => magazineSize;
        public int ReserveAmmoCapacity => reserveAmmoCapacity;
        public float Range => range;
        public LayerMask HitLayerMask => hitLayerMask;
        public QueryTriggerInteraction HitTriggerInteraction => hitTriggerInteraction;
        public int PenetrationCount => penetrationCount;
        public float PenetrationDamageDecay => penetrationDamageDecay;
        public float CurrentSpread => currentSpread;
        public WeaponReloadType ReloadType => reloadType;
        public float ReloadDuration => reloadDuration;
        public float TacticalReloadDuration => tacticalReloadDuration;
        public float PerBulletReloadDuration => perBulletReloadDuration;
        public bool AllowInterruptReload => allowInterruptReload;
        public bool AllowFireBreakReload => allowFireBreakReload;
        public bool AllowSwitchBreakReload => allowSwitchBreakReload;
        public int CurrentMagazineAmmo => weaponAmmoComponent != null ? weaponAmmoComponent.CurrentMagazineAmmo : 0;
        public int CurrentReserveAmmo => weaponAmmoComponent != null ? weaponAmmoComponent.CurrentReserveAmmo : 0;
        public int MaxMagazineAmmo => weaponAmmoComponent != null ? weaponAmmoComponent.MaxMagazineAmmo : 0;
        public bool IsReloading => isReloading;
        public bool IsEmptyReload => isEmptyReload;
        public float ActualReloadDuration => actualReloadDuration;
        public float NextAllowedFireTime => nextAllowedFireTime;
        public bool FireTriggeredThisFrame => fireTriggeredThisFrame;
        public bool IsFiring => isFiring;
        public bool IsInitialized => isInitialized;

        #endregion


        /// <summary>
        /// 提交本次开火成立后的一次性真实后坐力增量。
        /// 该增量会在 CharacterRoot 固定顺序中被消费一次，不做运行时累计与自动恢复。
        /// </summary>
        public void SetPendingRecoil(float _pitchDelta, float _yawDelta)
        {
            pendingRecoilPitch = _pitchDelta;
            pendingRecoilYaw = _yawDelta;
        }

        /// <summary>
        /// 取走当前帧待提交给 CharacterFacingController 的真实后坐力增量。
        /// 读取后立即清零，避免下一帧重复生效。
        /// </summary>
        public bool ConsumePendingRecoil(out float _pitchDelta, out float _yawDelta)
        {
            _pitchDelta = pendingRecoilPitch;
            _yawDelta = pendingRecoilYaw;

            if (Mathf.Abs(_pitchDelta) <= 0.0001f && Mathf.Abs(_yawDelta) <= 0.0001f)
            {
                return false;
            }

            pendingRecoilPitch = 0f;
            pendingRecoilYaw = 0f;
            return true;
        }

        private void Awake()
        {
            ResolveReferences();

            ConfigService configService = ConfigService.Active;
            if (configService == null || configService.IsInitialized == false)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 自动初始化失败：当前没有可用的共享 ConfigService。请先完成配置初始化。Object={name}", this);
                ResetRuntimeState();
                SyncViewState();
                return;
            }

            if (TryInitialize(weaponDefinitionConfigId, configService) == false)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 自动初始化失败：WeaponDefinitionConfigId={weaponDefinitionConfigId}。Object={name}", this);
            }
        }

        public void PreTickWeaponRuntimeFacts(float _currentTime)
        {
            fireTriggeredThisFrame = false;
            isFiring = EvaluateFiringPresentationState(_currentTime);

            if (isReloading)
            {
                reservedReloadTime -= Time.deltaTime;
                if (reservedReloadTime <= 0f)
                {
                    ReloadOver();
                }
            }
        }

        public WeaponFramePlan TickWeaponRuntime(WeaponRequest _request, CharacterFacts _characterFacts, float _currentTime)
        {
            if (isInitialized == false)
            {
                return WeaponFramePlan.CreateInvalid(
                    WeaponFireFailureReason.NotInitialized,
                    WeaponReloadFailureReason.NotInitialized);
            }

            WeaponFramePlan framePlan = weaponCommandResolver.Resolve(_request, _characterFacts, _currentTime);
            ExecutePlan(framePlan, _currentTime);
            UpdateFiringPresentationState(_currentTime);
            SyncViewState();
            return framePlan;
        }

        public float ResolveReloadDuration(bool _isEmptyReload)
        {
            if (reloadType == WeaponReloadType.Magazine)
            {
                return _isEmptyReload ? reloadDuration : tacticalReloadDuration;
            }

            return perBulletReloadDuration;
        }

        /// <summary>
        /// Reload 动画关键帧：弹匣插回武器。
        /// 当前正式规则下，补弹在插入弹匣这一刻成立；
        /// 但是否允许补弹、是否已经补过弹，统一由 WeaponRuntime 作为唯一真相源判断。
        /// </summary>
        public void NotifyReloadInsertMagazineKeyframe()
        {
            if (isInitialized == false || isReloading == false || hasCommittedReloadAmmoThisCycle)
            {
                return;
            }

            weaponAmmoComponent.ReloadMagazineFromReserve();
            hasCommittedReloadAmmoThisCycle = true;
        }

        private void ExecutePlan(in WeaponFramePlan _framePlan, float _currentTime)
        {
            if (_framePlan.ReloadTriggered)
            {
                actualReloadDuration = _framePlan.ReloadDuration;
                BeginReload(_framePlan.ReloadDuration, _framePlan.IsEmptyReload);
                return;
            }

            fireTriggeredThisFrame = weaponFireExecutor.Execute(_framePlan, _currentTime);

            if (fireTriggeredThisFrame && _framePlan.AutoReloadAfterFire)
            {
                actualReloadDuration = _framePlan.ReloadDuration;
                BeginReload(actualReloadDuration, true);
            }
        }

        private void UpdateFiringPresentationState(float _currentTime)
        {
            if (fireTriggeredThisFrame)
            {
                float holdDuration = Mathf.Clamp(fireInterval, minFirePoseHoldDuration, maxFirePoseHoldDuration);
                firePoseHoldUntil = _currentTime + holdDuration;
            }

            isFiring = EvaluateFiringPresentationState(_currentTime);
        }

        private bool EvaluateFiringPresentationState(float _currentTime)
        {
            if (isInitialized == false || isReloading)
            {
                return false;
            }

            return _currentTime < firePoseHoldUntil;
        }

        private void BeginReload(float _reloadDuration, bool _isEmptyReload)
        {
            isReloading = true;
            reservedReloadTime = Mathf.Max(0f, _reloadDuration);
            isEmptyReload = _isEmptyReload;
            hasCommittedReloadAmmoThisCycle = false;
            isFiring = false;
            firePoseHoldUntil = 0f;
        }

        private void ReloadOver()
        {
            isReloading = false;
            isEmptyReload = false;
            reservedReloadTime = 0f;
        }

        internal void CommitFire(float _currentTime, float _fireInterval)
        {
            nextAllowedFireTime = _currentTime + Mathf.Max(0.01f, _fireInterval);
        }

        public void ResetRuntimeState()
        {
            weaponDefinitionConfigId = string.Empty;
            weaponDefinitionConfig = null;
            weaponStatConfig = null;
            weaponSpreadConfig = null;
            weaponRecoilConfig = null;
            weaponReloadConfig = null;
            weaponPresentationConfig = null;

            canAim = true;
            canSprintFire = false;
            canFireInAir = true;

            damage = 0f;
            headShotDamageMultiplier = 2f;
            weakPointDamageMultiplier = 1.5f;
            fireInterval = 0.1f;
            burstCount = 1;
            burstInterval = 0.08f;
            magazineSize = 0;
            reserveAmmoCapacity = 0;
            range = 0.1f;
            hitLayerMask = ~0;
            hitTriggerInteraction = QueryTriggerInteraction.Ignore;
            penetrationCount = 0;
            penetrationDamageDecay = 0f;

            currentSpread = 0f;
            pendingRecoilPitch = 0f;
            pendingRecoilYaw = 0f;

            reloadType = WeaponReloadType.Magazine;
            reloadDuration = 0f;
            tacticalReloadDuration = 0f;
            perBulletReloadDuration = 0f;
            allowInterruptReload = true;
            allowFireBreakReload = false;
            allowSwitchBreakReload = true;

            firePoseHoldUntil = 0f;
            isReloading = false;
            isEmptyReload = false;
            hasCommittedReloadAmmoThisCycle = false;
            reservedReloadTime = 0f;
            actualReloadDuration = 0f;
            nextAllowedFireTime = 0f;
            fireTriggeredThisFrame = false;
            isFiring = false;
            isInitialized = false;

            weaponAmmoComponent?.ResetRuntimeState();
            SyncViewState();
        }

        private void SyncViewState()
        {
            if (weaponViewState != null)
            {
                weaponViewState.Sync(this);
            }
        }

        #region 初始化

        private bool TryInitialize(string _weaponDefinitionConfigId, ConfigService _configService)
        {
            ResetRuntimeState();

            if (TryResolveDefinitionConfig(_weaponDefinitionConfigId, _configService, out WeaponDefinitionConfig resolvedWeaponDefinitionConfig) == false)
            {
                return false;
            }

            if (ValidateCoreReferences() == false)
            {
                return false;
            }

            CacheResolvedConfigs(resolvedWeaponDefinitionConfig);
            if (InitializeResolvedConfigState() == false)
            {
                return false;
            }

            InitializeHandlingState();
            weaponAmmoComponent.InitializeFromCapacity(magazineSize, reserveAmmoCapacity);

            isInitialized = true;
            SyncViewState();
            return true;
        }

        private bool TryResolveDefinitionConfig(string _weaponDefinitionConfigId, ConfigService _configService, out WeaponDefinitionConfig _resolvedDefinitionConfig)
        {
            _resolvedDefinitionConfig = null;

            string normalizedWeaponDefinitionConfigId = ConfigIdUtility.Normalize(_weaponDefinitionConfigId);
            if (ConfigIdUtility.IsValid(normalizedWeaponDefinitionConfigId) == false)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 初始化失败：WeaponDefinitionConfigId 非法。RawId={_weaponDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (_configService.TryGetConfig(normalizedWeaponDefinitionConfigId, out _resolvedDefinitionConfig) == false)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 初始化失败：找不到 WeaponDefinitionConfig，Id={normalizedWeaponDefinitionConfigId}，Object={name}", this);
                return false;
            }

            weaponDefinitionConfigId = normalizedWeaponDefinitionConfigId;
            return true;
        }

        private bool ValidateCoreReferences()
        {
            if (weaponAmmoComponent == null || weaponCommandResolver == null || weaponFireExecutor == null || weaponViewState == null)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 初始化失败：Weapon 域核心引用不完整。Object={name}", this);
                return false;
            }

            return true;
        }

        private void CacheResolvedConfigs(WeaponDefinitionConfig _resolvedWeaponDefinitionConfig)
        {
            weaponDefinitionConfig = _resolvedWeaponDefinitionConfig;
            weaponStatConfig = _resolvedWeaponDefinitionConfig.WeaponStatConfig;
            weaponSpreadConfig = _resolvedWeaponDefinitionConfig.WeaponSpreadConfig;
            weaponRecoilConfig = _resolvedWeaponDefinitionConfig.WeaponRecoilConfig;
            weaponReloadConfig = _resolvedWeaponDefinitionConfig.WeaponReloadConfig;
            weaponPresentationConfig = _resolvedWeaponDefinitionConfig.WeaponPresentationConfig;
        }

        private bool InitializeResolvedConfigState()
        {
            InitializeDefinitionState();

            if (InitializeStatState() == false)
            {
                return false;
            }

            if (InitializeReloadState() == false)
            {
                return false;
            }

            return true;
        }

        private void InitializeDefinitionState()
        {
            canAim = weaponDefinitionConfig != null && weaponDefinitionConfig.CanAim;
            canSprintFire = weaponDefinitionConfig != null && weaponDefinitionConfig.CanSprintFire;
            canFireInAir = weaponDefinitionConfig == null || weaponDefinitionConfig.CanFireInAir;
        }

        private bool InitializeStatState()
        {
            if (weaponStatConfig == null)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 初始化失败：WeaponStatConfig 缺失。WeaponId={weaponDefinitionConfigId}，Object={name}", this);
                return false;
            }

            damage = Mathf.Max(0f, weaponStatConfig.Damage);
            headShotDamageMultiplier = Mathf.Max(1f, weaponStatConfig.HeadShotDamageMultiplier);
            weakPointDamageMultiplier = Mathf.Max(1f, weaponStatConfig.WeakPointDamageMultiplier);
            fireInterval = Mathf.Max(0.01f, weaponStatConfig.FireInterval);
            burstCount = Mathf.Max(1, weaponStatConfig.BurstCount);
            burstInterval = Mathf.Max(0.01f, weaponStatConfig.BurstInterval);
            magazineSize = Mathf.Max(0, weaponStatConfig.MagazineSize);
            reserveAmmoCapacity = Mathf.Max(0, weaponStatConfig.ReserveAmmoCapacity);
            range = Mathf.Max(0.01f, weaponStatConfig.Range);
            hitLayerMask = weaponStatConfig.HitLayerMask;
            hitTriggerInteraction = weaponStatConfig.HitTriggerInteraction;
            penetrationCount = Mathf.Max(0, weaponStatConfig.PenetrationCount);
            penetrationDamageDecay = Mathf.Max(0f, weaponStatConfig.PenetrationDamageDecay);
            return true;
        }

        private bool InitializeReloadState()
        {
            if (weaponReloadConfig == null)
            {
                Debug.LogError($"[{nameof(WeaponRuntime)}] 初始化失败：WeaponReloadConfig 缺失。WeaponId={weaponDefinitionConfigId}，Object={name}", this);
                return false;
            }

            reloadType = weaponReloadConfig.ReloadType;
            reloadDuration = Mathf.Max(0f, weaponReloadConfig.ReloadDuration);
            tacticalReloadDuration = Mathf.Max(0f, weaponReloadConfig.TacticalReloadDuration);
            perBulletReloadDuration = Mathf.Max(0f, weaponReloadConfig.PerBulletReloadDuration);
            allowInterruptReload = weaponReloadConfig.AllowInterruptReload;
            allowFireBreakReload = weaponReloadConfig.AllowFireBreakReload;
            allowSwitchBreakReload = weaponReloadConfig.AllowSwitchBreakReload;
            return true;
        }

        private void InitializeHandlingState()
        {
            currentSpread = weaponSpreadConfig != null ? Mathf.Max(0f, weaponSpreadConfig.BaseSpread) : 0f;
            pendingRecoilPitch = 0f;
            pendingRecoilYaw = 0f;
        }

        #endregion

        private void ResolveReferences()
        {
            if (weaponAmmoComponent == null)
            {
                weaponAmmoComponent = GetComponent<WeaponAmmoComponent>();
            }

            if (weaponCommandResolver == null)
            {
                weaponCommandResolver = GetComponent<WeaponCommandResolver>();
            }

            if (weaponFireExecutor == null)
            {
                weaponFireExecutor = GetComponent<WeaponFireExecutor>();
            }

            if (weaponViewState == null)
            {
                weaponViewState = GetComponent<WeaponViewState>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
