using Game.Foundation.Events;
using Game.Gameplay.Camera;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Weapon;
using Game.Gameplay.Weapon.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 准星展示器。
    ///
    /// 当前版本正式职责：
    /// 1. 只从 WeaponViewState 读取当前散布与准星最终展示参数；
    /// 2. 从 CameraAimPointContext 读取基础瞄点阻挡状态；
    /// 3. 从已提交事件读取开火/命中反馈；
    /// 4. 只负责 HUD 展示，不解释 Weapon 配置，不通过 ActionController 中转取状态。
    ///
    /// 约束：
    /// 1. 四向准星臂为正式必需装配，不再保留旧单图缩放降级路径；
    /// 2. 如果核心引用缺失，则直接隐藏并报错，不伪造第二套显示语义；
    /// 3. 所有颜色、间距、长度、脉冲参数统一来自 WeaponViewState。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrosshairPresenter : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [SerializeField] private MonoBehaviour cameraAimPointProviderBehaviour;
        [SerializeField] private WeaponViewState weaponViewState;
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private RectTransform topArm;
        [SerializeField] private RectTransform bottomArm;
        [SerializeField] private RectTransform leftArm;
        [SerializeField] private RectTransform rightArm;
        [SerializeField] private Graphic centerDotGraphic;

        [Header("Behaviour")]
        [SerializeField] private bool hideWhenProviderInvalid;
        [SerializeField] private float hitConfirmDuration = 0.08f;
        [SerializeField] private float fireKickDuration = 0.08f;
        [SerializeField] private float fireKickSpreadMultiplier = 8f;

        #endregion

        #region Runtime State

        private ICameraAimPointProvider cameraAimPointProvider;
        private Graphic topArmGraphic;
        private Graphic bottomArmGraphic;
        private Graphic leftArmGraphic;
        private Graphic rightArmGraphic;
        private float hitConfirmEndTime;
        private float fireKickEndTime;
        private float currentFireKickOffset;
        private Vector3 defaultScale = Vector3.one;
        private Color activeHitFeedbackColor = Color.white;
        private float activeHitPulseScale = 1f;
        private bool isBindingValid;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
            ValidateBindings();

            if (crosshairRoot != null)
            {
                defaultScale = crosshairRoot.localScale;
            }

            if (weaponViewState != null)
            {
                activeHitFeedbackColor = weaponViewState.CrosshairHitConfirmColor;
                activeHitPulseScale = weaponViewState.CrosshairHitPulseScale;
            }
        }

        private void OnEnable()
        {
            // Presenter 允许晚于事件总线装配；总线未就绪时不做硬崩。
            if (GameEventBus.Instance == null)
            {
                return;
            }

            GameEventBus.Instance.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            GameEventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        }

        private void OnDisable()
        {
            if (GameEventBus.Instance != null)
            {
                GameEventBus.Instance.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
                GameEventBus.Instance.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
            }
        }

        private void LateUpdate()
        {
            RefreshCrosshair();
        }

        #endregion

        #region Refresh

        public void RefreshCrosshair()
        {
            if (crosshairRoot == null)
            {
                return;
            }

            if (isBindingValid == false)
            {
                SetCrosshairVisible(false);
                return;
            }

            KeepCrosshairCentered();
            float spreadGap = ResolveCurrentGap();
            ApplyCrosshairGeometry(spreadGap);

            if (Time.unscaledTime < hitConfirmEndTime)
            {
                SetCrosshairVisible(true);
                ApplyCrosshairColor(activeHitFeedbackColor);
                crosshairRoot.localScale = defaultScale * activeHitPulseScale;
                return;
            }

            crosshairRoot.localScale = defaultScale;

            if (cameraAimPointProvider != null
                && cameraAimPointProvider.TryGetCameraAimPointContext(out CameraAimPointContext cameraAimPointContext))
            {
                SetCrosshairVisible(true);
                ApplyCrosshairColor(cameraAimPointContext.HasBlockingHit
                    ? weaponViewState.CrosshairBlockingHitColor
                    : weaponViewState.CrosshairDefaultColor);
                return;
            }

            SetCrosshairVisible(!hideWhenProviderInvalid);
            ApplyCrosshairColor(weaponViewState.CrosshairFallbackPointColor);
        }

        public void KeepCrosshairCentered()
        {
            if (crosshairRoot == null)
            {
                return;
            }

            crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchoredPosition = Vector2.zero;
        }

        public void SetCrosshairVisible(bool _visible)
        {
            if (crosshairRoot != null && crosshairRoot.gameObject.activeSelf != _visible)
            {
                crosshairRoot.gameObject.SetActive(_visible);
            }
        }

        public void ApplyCrosshairColor(Color _color)
        {
            ApplyGraphicColor(centerDotGraphic, _color);
            ApplyGraphicColor(topArmGraphic, _color);
            ApplyGraphicColor(bottomArmGraphic, _color);
            ApplyGraphicColor(leftArmGraphic, _color);
            ApplyGraphicColor(rightArmGraphic, _color);
        }

        #endregion

        #region Events

        private void OnWeaponFired(WeaponFiredEvent _eventArgs)
        {
            if (weaponViewState == null)
            {
                return;
            }

            currentFireKickOffset = Mathf.Max(currentFireKickOffset, Mathf.Max(0f, _eventArgs.CrosshairKick) * fireKickSpreadMultiplier);
            fireKickEndTime = Time.unscaledTime + fireKickDuration;
        }

        private void OnDamageApplied(DamageAppliedEvent _eventArgs)
        {
            if (weaponViewState == null)
            {
                return;
            }

            DamageResult damageResult = _eventArgs.DamageResult;

            if (damageResult.DidDepleteHealth)
            {
                activeHitFeedbackColor = weaponViewState.CrosshairKillHitConfirmColor;
                activeHitPulseScale = weaponViewState.CrosshairKillHitPulseScale;
            }
            else if (damageResult.HitPartType == HitPartType.WeakPoint)
            {
                activeHitFeedbackColor = weaponViewState.CrosshairWeakPointHitConfirmColor;
                activeHitPulseScale = weaponViewState.CrosshairWeakPointHitPulseScale;
            }
            else
            {
                activeHitFeedbackColor = weaponViewState.CrosshairHitConfirmColor;
                activeHitPulseScale = weaponViewState.CrosshairHitPulseScale;
            }

            hitConfirmEndTime = Time.unscaledTime + hitConfirmDuration;
        }

        #endregion

        #region Geometry

        private float ResolveCurrentGap()
        {
            float gap = Mathf.Lerp(
                weaponViewState.CrosshairBaseGap,
                weaponViewState.CrosshairMaxGap,
                weaponViewState.NormalizedSpread);

            if (Time.unscaledTime < fireKickEndTime)
            {
                gap += currentFireKickOffset;
            }
            else
            {
                currentFireKickOffset = Mathf.MoveTowards(currentFireKickOffset, 0f, 100f * Time.unscaledDeltaTime);
            }

            return gap;
        }

        private void ApplyCrosshairGeometry(float _gap)
        {
            SetGraphicVisible(centerDotGraphic, weaponViewState.CrosshairShowCenterDot);

            ApplyArm(topArm, new Vector2(0f, _gap), new Vector2(weaponViewState.CrosshairLineThickness, weaponViewState.CrosshairLineLength));
            ApplyArm(bottomArm, new Vector2(0f, -_gap), new Vector2(weaponViewState.CrosshairLineThickness, weaponViewState.CrosshairLineLength));
            ApplyArm(leftArm, new Vector2(-_gap, 0f), new Vector2(weaponViewState.CrosshairLineLength, weaponViewState.CrosshairLineThickness));
            ApplyArm(rightArm, new Vector2(_gap, 0f), new Vector2(weaponViewState.CrosshairLineLength, weaponViewState.CrosshairLineThickness));
        }

        private static void ApplyArm(RectTransform _arm, Vector2 _anchoredPosition, Vector2 _size)
        {
            if (_arm == null)
            {
                return;
            }

            _arm.anchorMin = new Vector2(0.5f, 0.5f);
            _arm.anchorMax = new Vector2(0.5f, 0.5f);
            _arm.pivot = new Vector2(0.5f, 0.5f);
            _arm.anchoredPosition = _anchoredPosition;
            _arm.sizeDelta = _size;
        }

        #endregion

        #region Helpers

        private static void ApplyGraphicColor(Graphic _graphic, Color _color)
        {
            if (_graphic != null)
            {
                _graphic.color = _color;
            }
        }

        private static void SetGraphicVisible(Graphic _graphic, bool _visible)
        {
            if (_graphic != null)
            {
                _graphic.enabled = _visible;
            }
        }

        private void ResolveReferences()
        {
            cameraAimPointProvider = cameraAimPointProviderBehaviour as ICameraAimPointProvider;

            if (crosshairRoot == null)
            {
                crosshairRoot = GetComponent<RectTransform>();
            }

            topArmGraphic = topArm != null ? topArm.GetComponent<Graphic>() : null;
            bottomArmGraphic = bottomArm != null ? bottomArm.GetComponent<Graphic>() : null;
            leftArmGraphic = leftArm != null ? leftArm.GetComponent<Graphic>() : null;
            rightArmGraphic = rightArm != null ? rightArm.GetComponent<Graphic>() : null;
        }

        /// <summary>
        /// 校验正式准星装配是否完整。
        /// 缺失即视为装配错误，不再降级到旧方案。
        /// </summary>
        private void ValidateBindings()
        {
            isBindingValid = crosshairRoot != null
                && weaponViewState != null
                && topArm != null
                && bottomArm != null
                && leftArm != null
                && rightArm != null;

            if (isBindingValid)
            {
                return;
            }

            Debug.LogError(
                $"[{nameof(CrosshairPresenter)}] 准星正式装配不完整：必须绑定 WeaponViewState、CrosshairRoot、TopArm、BottomArm、LeftArm、RightArm。Object={name}",
                this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            ValidateBindings();
            KeepCrosshairCentered();
        }
#endif

        #endregion
    }
}
