using Game.Definition.ConfigSystem.Core;
using Game.Definition.Presentation;
using Game.Definition.HUD;
using Game.Definition.Weapon;
using Game.Gameplay.Character;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// 武器域唯一长期状态。
    ///
    /// 设计原则：
    /// 1. 唯一长期真相源：弹药、换弹、射击节奏、散布、后坐力等运行时事实统一由本组件保管；
    /// 2. 配置只读不副本：对 WeaponStatConfig / WeaponReloadConfig / WeaponDefinitionConfig
    ///    的只读常量直接通过缓存的 SO 引用访问，不再逐字段复制到本地 SerializeField；
    /// 3. 可变值独立持有：只有 Buff 确实会修改的数值（damage、fireInterval、reloadDuration 等）
    ///    才作为本地 SerializeField 持有，供 Buff 系统后续改写；
    /// 4. 表现态归 ViewState：isFiring / firePoseHold 等纯表现判定已移入 WeaponViewState，
    ///    本组件只提交 fireTriggeredThisFrame 事实；
    /// 5. 散布正式收口到 WeaponRuntime：恢复、姿态罚值、单发增长、首发精度判定都不允许散落到 HUD / Hitscan / Character；
    /// 6. 准星表现参数在初始化阶段一次性缓存，运行时不再让 HUD 通过 id 二次查表。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponRuntime : MonoBehaviour
    {
        #region Config References

        [Header("Config")]
        [SerializeField] private string weaponDefinitionConfigId;
        [SerializeField] private WeaponDefinitionConfig weaponDefinitionConfig;
        [SerializeField] private WeaponStatConfig weaponStatConfig;
        [SerializeField] private WeaponSpreadConfig weaponSpreadConfig;
        [SerializeField] private WeaponRecoilConfig weaponRecoilConfig;
        [SerializeField] private WeaponReloadConfig weaponReloadConfig;
        [SerializeField] private WeaponPresentationConfig weaponPresentationConfig;

        #endregion

        #region Component References

        [Header("References")]
        [SerializeField] private WeaponAmmoComponent weaponAmmoComponent;
        [SerializeField] private WeaponCommandResolver weaponCommandResolver;
        [SerializeField] private WeaponFireExecutor weaponFireExecutor;
        [SerializeField] private WeaponViewState weaponViewState;

        #endregion

        #region Buffable Stats

        /// <summary>
        /// 可被 Buff 修改的运行时数值。
        /// 初始化时从配置读取，运行时允许 Buff 系统改写。
        /// </summary>
        [Header("Buffable Stats")]
        [SerializeField] private float damage;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private float tacticalReloadDuration = 1.5f;

        #endregion

        #region Crosshair Cached Visuals

        [Header("Crosshair Cached Visuals")]
        [SerializeField] private float crosshairBaseGap = 10f;
        [SerializeField] private float crosshairMaxGap = 28f;
        [SerializeField] private float crosshairLineThickness = 3f;
        [SerializeField] private float crosshairLineLength = 14f;
        [SerializeField] private bool crosshairShowCenterDot = true;
        [SerializeField] private Color crosshairDefaultColor = Color.white;
        [SerializeField] private Color crosshairBlockingHitColor = Color.red;
        [SerializeField] private Color crosshairFallbackPointColor = Color.blue;
        [SerializeField] private Color crosshairHitConfirmColor = Color.yellow;
        [SerializeField] private Color crosshairWeakPointHitConfirmColor = new(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color crosshairCriticalHitConfirmColor = new(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color crosshairKillHitConfirmColor = new(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private float crosshairHitPulseScale = 1.15f;
        [SerializeField] private float crosshairCriticalHitPulseScale = 1.22f;
        [SerializeField] private float crosshairKillHitPulseScale = 1.3f;

        #endregion

        #region Spread & Recoil State

        [Header("Spread & Recoil")]
        [SerializeField] private float currentSpread;
        [SerializeField] private float pendingRecoilPitch;
        [SerializeField] private float pendingRecoilYaw;

        #endregion

        #region Reload Runtime State

        [Header("Reload State")]
        [SerializeField] private bool isReloading;
        [SerializeField] private bool isEmptyReload;
        [SerializeField] private bool hasCommittedReloadAmmoThisCycle;
        [SerializeField] private float actualReloadDuration;
        [SerializeField] private float reservedReloadTime;

        #endregion

        #region Fire Runtime State

        [Header("Fire State")]
        [SerializeField] private float nextAllowedFireTime;
        [SerializeField] private bool fireTriggeredThisFrame;
        [SerializeField] private bool isInitialized;

        #endregion

        #region Config-Forwarding Properties (只读常量，直接从缓存 SO 读取)

        public string WeaponDefinitionConfigId => weaponDefinitionConfigId;
        public WeaponDefinitionConfig WeaponDefinitionConfig => weaponDefinitionConfig;
        public WeaponStatConfig WeaponStatConfig => weaponStatConfig;
        public WeaponSpreadConfig WeaponSpreadConfig => weaponSpreadConfig;
        public WeaponRecoilConfig WeaponRecoilConfig => weaponRecoilConfig;
        public WeaponReloadConfig WeaponReloadConfig => weaponReloadConfig;
        public WeaponPresentationConfig WeaponPresentationConfig => weaponPresentationConfig;
        public WeaponAmmoComponent WeaponAmmoComponent => weaponAmmoComponent;
        public WeaponViewState WeaponViewState => weaponViewState;

        /// <summary>武器是否支持瞄准。来自 WeaponDefinitionConfig，运行时不变。</summary>
        public bool CanAim => weaponDefinitionConfig != null && weaponDefinitionConfig.CanAim;

        /// <summary>冲刺中是否允许开火。来自 WeaponDefinitionConfig，运行时不变。</summary>
        public bool CanSprintFire => weaponDefinitionConfig != null && weaponDefinitionConfig.CanSprintFire;

        /// <summary>空中是否允许开火。来自 WeaponDefinitionConfig，缺失时默认允许。</summary>
        public bool CanFireInAir => weaponDefinitionConfig == null || weaponDefinitionConfig.CanFireInAir;

        /// <summary>爆头伤害倍率。来自 WeaponStatConfig，运行时不变。</summary>
        public float HeadShotDamageMultiplier => weaponStatConfig != null ? Mathf.Max(1f, weaponStatConfig.HeadShotDamageMultiplier) : 2f;

        /// <summary>弱点伤害倍率。来自 WeaponStatConfig，运行时不变。</summary>
        public float WeakPointDamageMultiplier => weaponStatConfig != null ? Mathf.Max(1f, weaponStatConfig.WeakPointDamageMultiplier) : 1.5f;

        /// <summary>连射发数。来自 WeaponStatConfig，运行时不变。</summary>
        public int BurstCount => weaponStatConfig != null ? Mathf.Max(1, weaponStatConfig.BurstCount) : 1;

        /// <summary>连射间隔。来自 WeaponStatConfig，运行时不变。</summary>
        public float BurstInterval => weaponStatConfig != null ? Mathf.Max(0.01f, weaponStatConfig.BurstInterval) : 0.08f;

        /// <summary>射程。来自 WeaponStatConfig，运行时不变。</summary>
        public float Range => weaponStatConfig != null ? Mathf.Max(0.01f, weaponStatConfig.Range) : 1000f;

        /// <summary>命中检测层级掩码。来自 WeaponStatConfig，运行时不变。</summary>
        public LayerMask HitLayerMask => weaponStatConfig != null ? weaponStatConfig.HitLayerMask : (LayerMask)(~0);

        /// <summary>命中 Trigger 查询策略。来自 WeaponStatConfig，运行时不变。</summary>
        public QueryTriggerInteraction HitTriggerInteraction => weaponStatConfig != null
            ? weaponStatConfig.HitTriggerInteraction : QueryTriggerInteraction.Ignore;

        /// <summary>穿透次数。来自 WeaponStatConfig，运行时不变。</summary>
        public int PenetrationCount => weaponStatConfig != null ? Mathf.Max(0, weaponStatConfig.PenetrationCount) : 0;

        /// <summary>穿透伤害衰减。来自 WeaponStatConfig，运行时不变。</summary>
        public float PenetrationDamageDecay => weaponStatConfig != null ? Mathf.Max(0f, weaponStatConfig.PenetrationDamageDecay) : 0f;

        /// <summary>换弹类型。来自 WeaponReloadConfig，运行时不变。</summary>
        public WeaponReloadType ReloadType => weaponReloadConfig != null ? weaponReloadConfig.ReloadType : WeaponReloadType.Magazine;

        /// <summary>逐发换弹时长。来自 WeaponReloadConfig，运行时不变。</summary>
        public float PerBulletReloadDuration => weaponReloadConfig != null ? Mathf.Max(0f, weaponReloadConfig.PerBulletReloadDuration) : 0f;

        /// <summary>换弹是否允许被打断。来自 WeaponReloadConfig，运行时不变。</summary>
        public bool AllowInterruptReload => weaponReloadConfig != null && weaponReloadConfig.AllowInterruptReload;

        /// <summary>开火是否可打断换弹。来自 WeaponReloadConfig，运行时不变。</summary>
        public bool AllowFireBreakReload => weaponReloadConfig != null && weaponReloadConfig.AllowFireBreakReload;

        /// <summary>切枪是否可打断换弹。来自 WeaponReloadConfig，运行时不变。</summary>
        public bool AllowSwitchBreakReload => weaponReloadConfig != null && weaponReloadConfig.AllowSwitchBreakReload;

        #endregion

        #region Buffable Properties

        public float Damage => damage;
        public float FireInterval => fireInterval;
        public float ReloadDuration => reloadDuration;
        public float TacticalReloadDuration => tacticalReloadDuration;

        #endregion

        #region Cached Crosshair Properties

        public float CrosshairBaseGap => crosshairBaseGap;
        public float CrosshairMaxGap => crosshairMaxGap;
        public float CrosshairLineThickness => crosshairLineThickness;
        public float CrosshairLineLength => crosshairLineLength;
        public bool CrosshairShowCenterDot => crosshairShowCenterDot;
        public Color CrosshairDefaultColor => crosshairDefaultColor;
        public Color CrosshairBlockingHitColor => crosshairBlockingHitColor;
        public Color CrosshairFallbackPointColor => crosshairFallbackPointColor;
        public Color CrosshairHitConfirmColor => crosshairHitConfirmColor;
        public Color CrosshairWeakPointHitConfirmColor => crosshairWeakPointHitConfirmColor;
        public Color CrosshairCriticalHitConfirmColor => crosshairCriticalHitConfirmColor;
        public Color CrosshairKillHitConfirmColor => crosshairKillHitConfirmColor;
        public float CrosshairHitPulseScale => crosshairHitPulseScale;
        public float CrosshairCriticalHitPulseScale => crosshairCriticalHitPulseScale;
        public float CrosshairKillHitPulseScale => crosshairKillHitPulseScale;

        #endregion

        #region Runtime State Properties

        public int CurrentMagazineAmmo => weaponAmmoComponent != null ? weaponAmmoComponent.CurrentMagazineAmmo : 0;
        public int CurrentReserveAmmo => weaponAmmoComponent != null ? weaponAmmoComponent.CurrentReserveAmmo : 0;
        public int MaxMagazineAmmo => weaponAmmoComponent != null ? weaponAmmoComponent.MaxMagazineAmmo : 0;
        public float CurrentSpread => currentSpread;
        public bool IsReloading => isReloading;
        public bool IsEmptyReload => isEmptyReload;
        public float ActualReloadDuration => actualReloadDuration;
        public float NextAllowedFireTime => nextAllowedFireTime;
        public bool FireTriggeredThisFrame => fireTriggeredThisFrame;
        public bool IsInitialized => isInitialized;

        #endregion

        #region Recoil

        /// <summary>
        /// 提交本次开火成立后的一次性真实后坐力增量。
        /// 该增量在 CharacterRoot 固定顺序中被消费一次，不做运行时累计与自动恢复。
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

        #endregion

        #region Spread

        /// <summary>
        /// 计算当前这一枪应该使用的角度散布值。
        /// 这一步会把基础散布与角色姿态罚值收口到 WeaponRuntime，避免 HUD / Hitscan 各算各的。
        /// </summary>
        public float GetCurrentShotSpreadAngle(in CharacterFramePlan _characterPlan, CharacterFacts _characterFacts)
        {
            if (weaponSpreadConfig == null)
            {
                return 0f;
            }

            float targetSpread = ResolveTargetSpread(_characterPlan, _characterFacts);
            float shotSpread = Mathf.Max(currentSpread, targetSpread);

            // 首发精度只在“稳定状态下的第一枪”生效，用来把首发弹着进一步收紧。
            if (IsFirstShotAccuracyQualified(_characterPlan, _characterFacts, targetSpread))
            {
                float accuracyFactor = Mathf.Clamp01(1f - weaponSpreadConfig.FirstShotAccuracy);
                shotSpread *= accuracyFactor;
            }

            return Mathf.Clamp(shotSpread, 0f, weaponSpreadConfig.MaxSpread);
        }

        /// <summary>
        /// 在本帧武器执行完成后推进散布状态。
        /// 未开火时向当前姿态目标散布恢复；开火成立时立刻叠加单发增长。
        /// </summary>
        public void UpdateSpreadState(in CharacterFramePlan _characterPlan, CharacterFacts _characterFacts, bool _fireTriggeredThisFrame, float _deltaTime)
        {
            if (weaponSpreadConfig == null)
            {
                currentSpread = 0f;
                return;
            }

            float targetSpread = ResolveTargetSpread(_characterPlan, _characterFacts);
            float recoverSpeed = Mathf.Max(0f, weaponSpreadConfig.SpreadRecoverSpeed);

            currentSpread = Mathf.MoveTowards(currentSpread, targetSpread, recoverSpeed * Mathf.Max(0f, _deltaTime));

            if (_fireTriggeredThisFrame)
            {
                currentSpread = Mathf.Clamp(
                    currentSpread + Mathf.Max(0f, weaponSpreadConfig.SpreadIncreasePerShot),
                    0f,
                    Mathf.Max(targetSpread, weaponSpreadConfig.MaxSpread));
            }
        }

        /// <summary>
        /// 解析当前姿态下的目标散布下限。
        /// </summary>
        private float ResolveTargetSpread(in CharacterFramePlan _characterPlan, CharacterFacts _characterFacts)
        {
            if (weaponSpreadConfig == null)
            {
                return 0f;
            }

            float spread = _characterPlan.AimActive
                ? weaponSpreadConfig.AimSpread
                : weaponSpreadConfig.BaseSpread;

            bool isMoving = _characterPlan.HasMoveInput || (_characterFacts != null && _characterFacts.IsMoving);
            bool isGrounded = _characterFacts == null || _characterFacts.IsGrounded;

            if (isMoving)
            {
                spread += weaponSpreadConfig.MovingSpreadPenalty;
            }

            if (isGrounded == false)
            {
                spread += weaponSpreadConfig.AirborneSpreadPenalty;
            }

            return Mathf.Clamp(spread, 0f, weaponSpreadConfig.MaxSpread);
        }

        /// <summary>
        /// 判定当前这一枪是否满足首发精度条件。
        /// </summary>
        private bool IsFirstShotAccuracyQualified(in CharacterFramePlan _characterPlan, CharacterFacts _characterFacts, float _targetSpread)
        {
            if (weaponSpreadConfig == null || weaponSpreadConfig.FirstShotAccuracy <= 0f)
            {
                return false;
            }

            bool isMoving = _characterPlan.HasMoveInput || (_characterFacts != null && _characterFacts.IsMoving);
            bool isGrounded = _characterFacts == null || _characterFacts.IsGrounded;
            if (isMoving || isGrounded == false)
            {
                return false;
            }

            return currentSpread <= _targetSpread + 0.0001f;
        }

        #endregion

        #region Per-Frame Tick

        /// <summary>
        /// 在 Character 裁决前推进武器域已提交事实。
        /// 包括重置单帧标记、推进换弹倒计时。
        /// </summary>
        /// <param name="_currentTime">当前帧时间（Time.time）。</param>
        /// <param name="_deltaTime">帧间隔（Time.deltaTime），用于换弹倒计时。</param>
        public void PreTickRuntimeFacts(float _currentTime, float _deltaTime)
        {
            // 重置单帧触发标记，避免上一帧的开火事实延续到本帧。
            fireTriggeredThisFrame = false;

            // 推进换弹倒计时。
            if (isReloading)
            {
                reservedReloadTime -= _deltaTime;
                if (reservedReloadTime <= 0f)
                {
                    CompleteReload();
                }
            }
        }

        /// <summary>
        /// 武器域完整主链 Tick。
        /// 由 CharacterActionController.Execute 在角色执行阶段调用。
        /// </summary>
        public WeaponFramePlan TickWeaponRuntime(WeaponRequest _request, CharacterFacts _characterFacts, in CharacterFramePlan _characterPlan, float _currentTime)
        {
            if (isInitialized == false)
            {
                return WeaponFramePlan.CreateInvalid(
                    WeaponFireFailureReason.NotInitialized,
                    WeaponReloadFailureReason.NotInitialized);
            }

            // 裁决本帧武器计划。
            WeaponFramePlan framePlan = weaponCommandResolver.Resolve(_request, _characterFacts, _currentTime);

            // 执行已裁决计划。
            ExecutePlan(framePlan, _characterFacts, _characterPlan, _currentTime);

            // 开火执行完成后再推进散布，保证这一枪使用的是“开火前散布”。
            UpdateSpreadState(_characterPlan, _characterFacts, fireTriggeredThisFrame, Time.deltaTime);

            // 同步只读表现态，传入当前时间和射速供 ViewState 管理表现窗口。
            SyncViewState(_currentTime);

            return framePlan;
        }

        #endregion

        #region Reload

        /// <summary>
        /// 根据是否空仓换弹，返回实际换弹时长。
        /// </summary>
        public float ResolveReloadDuration(bool _isEmptyReload)
        {
            if (ReloadType == WeaponReloadType.Magazine)
            {
                return _isEmptyReload ? reloadDuration : tacticalReloadDuration;
            }

            return PerBulletReloadDuration;
        }

        /// <summary>
        /// Reload 动画关键帧回调：弹匣插回武器。
        /// 补弹在插入弹匣这一刻成立；是否允许补弹由 WeaponRuntime 作为唯一真相源判断。
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

        #endregion

        #region Fire Commit

        /// <summary>
        /// 由 WeaponFireExecutor 在开火成立后调用，写回射击冷却。
        /// </summary>
        internal void CommitFire(float _currentTime, float _fireInterval)
        {
            nextAllowedFireTime = _currentTime + Mathf.Max(0.01f, _fireInterval);
        }

        #endregion

        #region Plan Execution

        /// <summary>
        /// 执行已裁决的武器帧计划。
        /// </summary>
        private void ExecutePlan(in WeaponFramePlan _framePlan, CharacterFacts _characterFacts, in CharacterFramePlan _characterPlan, float _currentTime)
        {
            // 换弹优先于开火。
            if (_framePlan.ReloadTriggered)
            {
                actualReloadDuration = _framePlan.ReloadDuration;
                BeginReload(_framePlan.ReloadDuration, _framePlan.IsEmptyReload);
                return;
            }

            // 尝试执行开火。
            fireTriggeredThisFrame = weaponFireExecutor.Execute(_framePlan, _characterFacts, _characterPlan, _currentTime);

            // 最后一发打完后自动换弹。
            if (fireTriggeredThisFrame && _framePlan.AutoReloadAfterFire)
            {
                actualReloadDuration = _framePlan.ReloadDuration;
                BeginReload(actualReloadDuration, true);
            }
        }

        #endregion

        #region Reload Internal

        private void BeginReload(float _reloadDuration, bool _isEmptyReload)
        {
            isReloading = true;
            reservedReloadTime = Mathf.Max(0f, _reloadDuration);
            isEmptyReload = _isEmptyReload;
            hasCommittedReloadAmmoThisCycle = false;
        }

        private void CompleteReload()
        {
            isReloading = false;
            isEmptyReload = false;
            reservedReloadTime = 0f;
        }

        #endregion

        #region ViewState Sync

        /// <summary>
        /// 同步只读表现态。传入当前时间供 ViewState 管理开火表现窗口。
        /// </summary>
        private void SyncViewState(float _currentTime)
        {
            if (weaponViewState != null)
            {
                weaponViewState.Sync(this, _currentTime);
            }
        }

        #endregion

        #region Initialization

        private void Awake()
        {
            ResolveReferences();

            ConfigService configService = ConfigService.Active;
            if (configService == null || configService.IsInitialized == false)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 自动初始化失败：当前没有可用的共享 ConfigService。Object={name}", this);
                ResetRuntimeState();
                SyncViewState(0f);
                return;
            }

            if (TryInitialize(weaponDefinitionConfigId, configService) == false)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 自动初始化失败：WeaponDefinitionConfigId={weaponDefinitionConfigId}。Object={name}", this);
            }
        }

        /// <summary>
        /// 从武器主定义配置初始化全部运行时状态。
        /// </summary>
        private bool TryInitialize(string _weaponDefinitionConfigId, ConfigService _configService)
        {
            ResetRuntimeState();

            // 解析武器主定义配置。
            if (TryResolveDefinitionConfig(_weaponDefinitionConfigId, _configService,
                    out WeaponDefinitionConfig resolvedConfig) == false)
            {
                return false;
            }

            if (ValidateCoreReferences() == false)
            {
                return false;
            }

            // 缓存子配置 SO 引用。
            CacheResolvedConfigs(resolvedConfig);

            // 从配置初始化可变运行时数值。
            if (InitializeBuffableStats() == false)
            {
                return false;
            }

            // 缓存准星最终展示参数，后续运行时只读取缓存字段。
            CacheCrosshairVisuals();

            // 初始化散布。
            currentSpread = weaponSpreadConfig != null ? Mathf.Max(0f, weaponSpreadConfig.BaseSpread) : 0f;
            pendingRecoilPitch = 0f;
            pendingRecoilYaw = 0f;

            // 初始化弹药组件。
            int magSize = weaponStatConfig != null ? Mathf.Max(0, weaponStatConfig.MagazineSize) : 0;
            int reserveCap = weaponStatConfig != null ? Mathf.Max(0, weaponStatConfig.ReserveAmmoCapacity) : 0;
            weaponAmmoComponent.InitializeFromCapacity(magSize, reserveCap);

            isInitialized = true;
            SyncViewState(0f);
            return true;
        }

        private bool TryResolveDefinitionConfig(
            string _id, ConfigService _configService, out WeaponDefinitionConfig _resolved)
        {
            _resolved = null;

            string normalizedId = ConfigIdUtility.Normalize(_id);
            if (ConfigIdUtility.IsValid(normalizedId) == false)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 初始化失败：WeaponDefinitionConfigId 非法。RawId={_id}，Object={name}", this);
                return false;
            }

            if (_configService.TryGetConfig(normalizedId, out _resolved) == false)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 初始化失败：找不到 WeaponDefinitionConfig，Id={normalizedId}，Object={name}", this);
                return false;
            }

            weaponDefinitionConfigId = normalizedId;
            return true;
        }

        private bool ValidateCoreReferences()
        {
            if (weaponAmmoComponent == null || weaponCommandResolver == null
                || weaponFireExecutor == null || weaponViewState == null)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 初始化失败：Weapon 域核心引用不完整。Object={name}", this);
                return false;
            }

            return true;
        }

        private void CacheResolvedConfigs(WeaponDefinitionConfig _resolved)
        {
            weaponDefinitionConfig = _resolved;
            weaponStatConfig = _resolved.WeaponStatConfig;
            weaponSpreadConfig = _resolved.WeaponSpreadConfig;
            weaponRecoilConfig = _resolved.WeaponRecoilConfig;
            weaponReloadConfig = _resolved.WeaponReloadConfig;
            weaponPresentationConfig = _resolved.WeaponPresentationConfig;
        }

        /// <summary>
        /// 从配置初始化 Buff 可修改的运行时数值。
        /// </summary>
        private bool InitializeBuffableStats()
        {
            if (weaponStatConfig == null)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 初始化失败：WeaponStatConfig 缺失。WeaponId={weaponDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (weaponReloadConfig == null)
            {
                Debug.LogError(
                    $"[{nameof(WeaponRuntime)}] 初始化失败：WeaponReloadConfig 缺失。WeaponId={weaponDefinitionConfigId}，Object={name}", this);
                return false;
            }

            damage = Mathf.Max(0f, weaponStatConfig.Damage);
            fireInterval = Mathf.Max(0.01f, weaponStatConfig.FireInterval);
            reloadDuration = Mathf.Max(0f, weaponReloadConfig.ReloadDuration);
            tacticalReloadDuration = Mathf.Max(0f, weaponReloadConfig.TacticalReloadDuration);
            return true;
        }

        /// <summary>
        /// 在初始化阶段把准星表现参数一次性缓存到运行时字段。
        /// 这样 HUD 只读 WeaponViewState，同步链上不再做 styleId 解释。
        /// </summary>
        private void CacheCrosshairVisuals()
        {
            CrosshairConfig crosshairConfig = weaponPresentationConfig != null
                ? weaponPresentationConfig.CrosshairConfig
                : null;

            if (crosshairConfig == null)
            {
                crosshairBaseGap = 10f;
                crosshairMaxGap = 28f;
                crosshairLineThickness = 3f;
                crosshairLineLength = 14f;
                crosshairShowCenterDot = true;
                crosshairDefaultColor = Color.white;
                crosshairBlockingHitColor = Color.red;
                crosshairFallbackPointColor = Color.blue;
                crosshairHitConfirmColor = Color.yellow;
                crosshairWeakPointHitConfirmColor = new Color(1f, 0.55f, 0.1f, 1f);
                crosshairCriticalHitConfirmColor = new Color(1f, 0.8f, 0.2f, 1f);
                crosshairKillHitConfirmColor = new Color(1f, 0.25f, 0.25f, 1f);
                crosshairHitPulseScale = 1.15f;
                crosshairCriticalHitPulseScale = 1.22f;
                crosshairKillHitPulseScale = 1.3f;
                return;
            }

            crosshairBaseGap = Mathf.Max(0f, crosshairConfig.BaseGap);
            crosshairMaxGap = Mathf.Max(crosshairBaseGap, crosshairConfig.MaxGap);
            crosshairLineThickness = Mathf.Max(1f, crosshairConfig.LineThickness);
            crosshairLineLength = Mathf.Max(1f, crosshairConfig.LineLength);
            crosshairShowCenterDot = crosshairConfig.ShowCenterDot;
            crosshairDefaultColor = crosshairConfig.DefaultColor;
            crosshairBlockingHitColor = crosshairConfig.BlockingHitColor;
            crosshairFallbackPointColor = crosshairConfig.FallbackPointColor;
            crosshairHitConfirmColor = crosshairConfig.HitConfirmColor;
            crosshairWeakPointHitConfirmColor = crosshairConfig.WeakPointHitConfirmColor;
            crosshairCriticalHitConfirmColor = crosshairConfig.CriticalHitConfirmColor;
            crosshairKillHitConfirmColor = crosshairConfig.KillHitConfirmColor;
            crosshairHitPulseScale = Mathf.Max(1f, crosshairConfig.HitPulseScale);
            crosshairCriticalHitPulseScale = Mathf.Max(1f, crosshairConfig.CriticalHitPulseScale);
            crosshairKillHitPulseScale = Mathf.Max(1f, crosshairConfig.KillHitPulseScale);
        }

        #endregion

        #region Reset

        /// <summary>
        /// 清空全部运行时状态，回到未初始化。
        /// </summary>
        public void ResetRuntimeState()
        {
            // 清空配置引用。
            weaponDefinitionConfigId = string.Empty;
            weaponDefinitionConfig = null;
            weaponStatConfig = null;
            weaponSpreadConfig = null;
            weaponRecoilConfig = null;
            weaponReloadConfig = null;
            weaponPresentationConfig = null;

            // 重置可变数值。
            damage = 0f;
            fireInterval = 0.1f;
            reloadDuration = 0f;
            tacticalReloadDuration = 0f;

            // 重置准星缓存参数。
            crosshairBaseGap = 10f;
            crosshairMaxGap = 28f;
            crosshairLineThickness = 3f;
            crosshairLineLength = 14f;
            crosshairShowCenterDot = true;
            crosshairDefaultColor = Color.white;
            crosshairBlockingHitColor = Color.red;
            crosshairFallbackPointColor = Color.blue;
            crosshairHitConfirmColor = Color.yellow;
            crosshairWeakPointHitConfirmColor = new Color(1f, 0.55f, 0.1f, 1f);
            crosshairCriticalHitConfirmColor = new Color(1f, 0.8f, 0.2f, 1f);
            crosshairKillHitConfirmColor = new Color(1f, 0.25f, 0.25f, 1f);
            crosshairHitPulseScale = 1.15f;
            crosshairCriticalHitPulseScale = 1.22f;
            crosshairKillHitPulseScale = 1.3f;

            // 重置散布与后坐力。
            currentSpread = 0f;
            pendingRecoilPitch = 0f;
            pendingRecoilYaw = 0f;

            // 重置换弹状态。
            isReloading = false;
            isEmptyReload = false;
            hasCommittedReloadAmmoThisCycle = false;
            reservedReloadTime = 0f;
            actualReloadDuration = 0f;

            // 重置开火状态。
            nextAllowedFireTime = 0f;
            fireTriggeredThisFrame = false;
            isInitialized = false;

            weaponAmmoComponent?.ResetRuntimeState();
            SyncViewState(0f);
        }

        #endregion

        #region Reference Resolution

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

        #endregion
    }
}
