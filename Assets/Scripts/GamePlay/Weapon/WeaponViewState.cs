using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// Weapon 域只读表现态。
    ///
    /// 职责：
    /// 1. 供 HUD / Animation / SFX / Camera 统一读取武器当前表现状态；
    /// 2. 管理“开火表现窗口”（isFiring / firePoseHold），决定角色视觉上是否处于开火姿态；
    /// 3. 对 HUD 暴露当前散布与准星最终展示参数，避免 HUD 自己再去解释 Weapon 配置。
    ///
    /// 约束：
    /// 1. 不回写 WeaponRuntime 任何运行时事实；
    /// 2. 只由 WeaponRuntime.SyncViewState 和 CharacterActionController.PreTickCurrentWeapon 驱动刷新。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponViewState : MonoBehaviour
    {
        #region Synced State (from WeaponRuntime)

        [Header("Weapon Identity")]
        [SerializeField] private string weaponDefinitionConfigId;

        [Header("Ammo")]
        [SerializeField] private int currentMagazineAmmo;
        [SerializeField] private int currentReserveAmmo;
        [SerializeField] private int maxMagazineAmmo;

        [Header("Reload")]
        [SerializeField] private bool isReloading;
        [SerializeField] private bool isEmptyReload;
        [SerializeField] private float actualReloadDuration;

        [Header("Fire")]
        [SerializeField] private bool fireTriggeredThisFrame;
        [SerializeField] private float nextAllowedFireTime;
        [SerializeField] private float currentSpread;
        [SerializeField] private float normalizedSpread;

        [Header("Shot")]
        [SerializeField] private float shotDistance;
        [SerializeField] private LayerMask hitLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction hitTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Crosshair")]
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

        #region Fire Pose Presentation (owned by ViewState)

        private const float DefaultMinFirePoseHoldDuration = 0.08f;
        private const float DefaultMaxFirePoseHoldDuration = 0.16f;

        [Header("Fire Pose (Presentation)")]
        [SerializeField] private float firePoseHoldUntil;
        [SerializeField] private bool isFiring;

        #endregion

        #region Public Accessors

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
        public float CurrentSpread => currentSpread;
        public float NormalizedSpread => normalizedSpread;
        public float ShotDistance => shotDistance;
        public LayerMask HitLayerMask => hitLayerMask;
        public QueryTriggerInteraction HitTriggerInteraction => hitTriggerInteraction;
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

        #region Pre-Tick

        public void PreTick(float _currentTime)
        {
            EvaluateFiringState(_currentTime);
        }

        #endregion

        #region Sync

        public void Sync(WeaponRuntime _weaponRuntime, float _currentTime)
        {
            if (_weaponRuntime == null)
            {
                return;
            }

            weaponDefinitionConfigId = _weaponRuntime.WeaponDefinitionConfigId;

            currentMagazineAmmo = _weaponRuntime.CurrentMagazineAmmo;
            currentReserveAmmo = _weaponRuntime.CurrentReserveAmmo;
            maxMagazineAmmo = _weaponRuntime.MaxMagazineAmmo;

            isReloading = _weaponRuntime.IsReloading;
            isEmptyReload = _weaponRuntime.IsEmptyReload;
            actualReloadDuration = _weaponRuntime.ActualReloadDuration;

            fireTriggeredThisFrame = _weaponRuntime.FireTriggeredThisFrame;
            nextAllowedFireTime = _weaponRuntime.NextAllowedFireTime;
            currentSpread = _weaponRuntime.CurrentSpread;

            float maxSpread = _weaponRuntime.WeaponSpreadConfig != null
                ? Mathf.Max(0.0001f, _weaponRuntime.WeaponSpreadConfig.MaxSpread)
                : 1f;
            normalizedSpread = Mathf.Clamp01(currentSpread / maxSpread);

            shotDistance = _weaponRuntime.Range;
            hitLayerMask = _weaponRuntime.HitLayerMask;
            hitTriggerInteraction = _weaponRuntime.HitTriggerInteraction;

            crosshairBaseGap = _weaponRuntime.CrosshairBaseGap;
            crosshairMaxGap = _weaponRuntime.CrosshairMaxGap;
            crosshairLineThickness = _weaponRuntime.CrosshairLineThickness;
            crosshairLineLength = _weaponRuntime.CrosshairLineLength;
            crosshairShowCenterDot = _weaponRuntime.CrosshairShowCenterDot;
            crosshairDefaultColor = _weaponRuntime.CrosshairDefaultColor;
            crosshairBlockingHitColor = _weaponRuntime.CrosshairBlockingHitColor;
            crosshairFallbackPointColor = _weaponRuntime.CrosshairFallbackPointColor;
            crosshairHitConfirmColor = _weaponRuntime.CrosshairHitConfirmColor;
            crosshairWeakPointHitConfirmColor = _weaponRuntime.CrosshairWeakPointHitConfirmColor;
            crosshairCriticalHitConfirmColor = _weaponRuntime.CrosshairCriticalHitConfirmColor;
            crosshairKillHitConfirmColor = _weaponRuntime.CrosshairKillHitConfirmColor;
            crosshairHitPulseScale = _weaponRuntime.CrosshairHitPulseScale;
            crosshairCriticalHitPulseScale = _weaponRuntime.CrosshairCriticalHitPulseScale;
            crosshairKillHitPulseScale = _weaponRuntime.CrosshairKillHitPulseScale;

            if (fireTriggeredThisFrame)
            {
                float holdDuration = Mathf.Clamp(
                    _weaponRuntime.FireInterval,
                    DefaultMinFirePoseHoldDuration,
                    DefaultMaxFirePoseHoldDuration);
                firePoseHoldUntil = _currentTime + holdDuration;
            }

            EvaluateFiringState(_currentTime);
        }

        #endregion

        #region Presentation Internal

        private void EvaluateFiringState(float _currentTime)
        {
            isFiring = !isReloading && _currentTime < firePoseHoldUntil;
        }

        #endregion
    }
}
