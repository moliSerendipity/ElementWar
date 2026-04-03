using Game.Foundation.Events;
using Game.Gameplay.Camera;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Weapon.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 最小准星展示器。
    /// 当前阶段负责三件事：
    /// 1）维持中心准星基础显示；
    /// 2）根据相机逻辑瞄点结果切换基础颜色；
    /// 3）收到真实命中确认与伤害结果后给出短时反馈。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrosshairPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonoBehaviour cameraAimPointProviderBehaviour;
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private Graphic crosshairGraphic;

        [Header("Display")]
        [SerializeField] private bool hideWhenProviderInvalid = false;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color blockingHitColor = Color.red;
        [SerializeField] private Color fallbackPointColor = Color.blue;
        [SerializeField] private Color hitConfirmColor = Color.yellow;
        [SerializeField] private Color weakPointHitConfirmColor = new(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color criticalHitConfirmColor = new(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color killHitConfirmColor = new(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private float hitConfirmDuration = 0.08f;
        [SerializeField] private float hitPulseScale = 1.15f;
        [SerializeField] private float criticalHitPulseScale = 1.22f;
        [SerializeField] private float killHitPulseScale = 1.3f;

        private ICameraAimPointProvider cameraAimPointProvider;
        private float hitConfirmEndTime;
        private float fireKickEndTime;
        private Vector3 defaultScale = Vector3.one;
        private Color activeHitFeedbackColor;
        private float activeHitPulseScale = 1f;
        private float fireKickScale = 1f;

        /// <summary>
        /// 在 Awake 阶段自动解析准星展示所需的最小依赖。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            if (crosshairRoot != null)
            {
                // 记录默认缩放，用于命中脉冲结束后恢复准星原始尺寸。
                defaultScale = crosshairRoot.localScale;
            }

            // 默认命中反馈颜色从普通命中开始，后续由事件覆盖。
            activeHitFeedbackColor = hitConfirmColor;
            activeHitPulseScale = hitPulseScale;
        }

        private void OnEnable()
        {
            GameEventBus.Instance.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            GameEventBus.Instance.Subscribe<HitConfirmedEvent>(OnHitConfirmed);
            GameEventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        }

        private void OnDisable()
        {
            if (GameEventBus.Instance != null)
            {
                GameEventBus.Instance.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
                GameEventBus.Instance.Unsubscribe<HitConfirmedEvent>(OnHitConfirmed);
                GameEventBus.Instance.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
            }
        }

        /// <summary>
        /// 在 LateUpdate 阶段刷新准星可见性与基础颜色。
        /// 使用 LateUpdate 是为了读取当帧已经完成解析的相机逻辑瞄点结果，避免 UI 抢在相机链之前刷新。
        /// </summary>
        private void LateUpdate()
        {
            RefreshCrosshair();
        }

        /// <summary>
        /// 刷新当前帧准星展示状态。
        /// 当前阶段准星固定停留在屏幕中心，只通过逻辑瞄点有效性与命中反馈切换显示。
        /// </summary>
        public void RefreshCrosshair()
        {
            if (crosshairRoot == null)
            {
                return;
            }

            // 当前版本准星始终停在屏幕中心，不在 HUD 层直接表达真实散布偏移。
            KeepCrosshairCentered();

            if (Time.unscaledTime < hitConfirmEndTime)
            {
                // 命中反馈窗口内优先显示命中颜色与脉冲，不被普通瞄点颜色覆盖。
                SetCrosshairVisible(true);
                ApplyCrosshairColor(activeHitFeedbackColor);
                crosshairRoot.localScale = defaultScale * activeHitPulseScale;
                Debug.Log("Crosshair showing hit feedback color: " + activeHitFeedbackColor);
                return;
            }

            // 命中反馈结束后，仍然允许保留一小段开火脉冲缩放。
            if (Time.unscaledTime < fireKickEndTime)
            {
                crosshairRoot.localScale = defaultScale * fireKickScale;
            }
            else
            {
                crosshairRoot.localScale = defaultScale;
            }

            if (cameraAimPointProvider != null
                && cameraAimPointProvider.TryGetCameraAimPointContext(out CameraAimPointContext cameraAimPointContext))
            {
                // 相机逻辑瞄点有效时，根据阻挡状态切换基础颜色。
                SetCrosshairVisible(true);
                ApplyCrosshairColor(cameraAimPointContext.HasBlockingHit ? blockingHitColor : fallbackPointColor);
                return;
            }

            // 相机提供者失效时，只按当前显示策略保留或隐藏准星。
            SetCrosshairVisible(!hideWhenProviderInvalid);
            ApplyCrosshairColor(defaultColor);
        }

        /// <summary>
        /// 维持准星锚点位于屏幕中心。
        /// 当前阶段不做扩散偏移与动态散布动画，避免 UI 逻辑先于武器链路提前膨胀。
        /// </summary>
        public void KeepCrosshairCentered()
        {
            if (crosshairRoot == null)
            {
                return;
            }

            // 始终把锚点、枢轴和偏移收回到屏幕中心，避免其他 UI 修改残留位置漂移。
            crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 设置准星显示状态。
        /// </summary>
        /// <param name="_visible">是否显示准星。</param>
        public void SetCrosshairVisible(bool _visible)
        {
            if (crosshairRoot != null && crosshairRoot.gameObject.activeSelf != _visible)
            {
                crosshairRoot.gameObject.SetActive(_visible);
            }
        }

        /// <summary>
        /// 设置准星图形颜色。
        /// </summary>
        /// <param name="_color">目标颜色。</param>
        public void ApplyCrosshairColor(Color _color)
        {
            if (crosshairGraphic != null)
            {
                crosshairGraphic.color = _color;
            }
        }


        private void OnWeaponFired(WeaponFiredEvent _eventArgs)
        {
            // 开火脉冲只消费已提交事件里的 crosshairKick，不在 HUD 自己发明另一套武器参数。
            fireKickScale = 1f + Mathf.Max(0f, _eventArgs.CrosshairKick);
            fireKickEndTime = Time.unscaledTime + hitConfirmDuration;
        }

        private void OnHitConfirmed(HitConfirmedEvent _eventArgs)
        {
            // 命中确认先提供基础反馈；弱点命中在这一步直接提升颜色层级。
            activeHitFeedbackColor = _eventArgs.HitPartType == CombatHitPartType.WeakPoint
                ? weakPointHitConfirmColor
                : hitConfirmColor;
            activeHitPulseScale = _eventArgs.HitPartType == CombatHitPartType.WeakPoint
                ? criticalHitPulseScale
                : hitPulseScale;
            hitConfirmEndTime = Time.unscaledTime + hitConfirmDuration;
        }

        private void OnDamageApplied(DamageAppliedEvent _eventArgs)
        {
            CombatDamageResult damageResult = _eventArgs.DamageResult;

            // 击杀反馈优先级最高，其次是暴击；普通伤害不覆盖已有弱点颜色。
            if (damageResult.WasKilled)
            {
                activeHitFeedbackColor = killHitConfirmColor;
                activeHitPulseScale = killHitPulseScale;
            }
            else if (damageResult.IsCritical)
            {
                activeHitFeedbackColor = criticalHitConfirmColor;
                activeHitPulseScale = criticalHitPulseScale;
            }

            // 伤害结果事件比命中确认更靠后，用它把反馈窗口重新向后推一小段，避免弱点/暴击感受过短。
            hitConfirmEndTime = Time.unscaledTime + hitConfirmDuration;
        }

        /// <summary>
        /// 自动解析相机瞄点提供者与当前节点上的 UI 引用。
        /// </summary>
        private void ResolveReferences()
        {
            cameraAimPointProvider = cameraAimPointProviderBehaviour as ICameraAimPointProvider;

            if (crosshairRoot == null)
            {
                crosshairRoot = GetComponent<RectTransform>();
            }

            if (crosshairGraphic == null)
            {
                crosshairGraphic = GetComponent<Graphic>();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中参数变动后自动重新解析引用，减少装配遗漏。
        /// </summary>
        private void OnValidate()
        {
            ResolveReferences();
            KeepCrosshairCentered();
        }
#endif
    }
}
