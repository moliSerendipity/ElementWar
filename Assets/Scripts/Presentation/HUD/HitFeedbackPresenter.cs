using Game.Foundation.Events;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 最小命中反馈 Presenter。
    /// 只消费真实命中与伤害结果事件，并把它们转成短时可见的 HUD 命中标记；
    /// 不在表现层补做命中判定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Graphic hitMarkerGraphic;
        [SerializeField] private Color hitMarkerColor = Color.white;
        [SerializeField] private Color weakPointHitMarkerColor = new(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color killHitMarkerColor = new(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private float displayDuration = 0.08f;
        [SerializeField] private bool enableDebugLog;

        private float hideTime;

        private void Awake()
        {
            // 初始时先隐藏命中标记，避免场景启动时出现残留显示。
            ApplyVisible(false);
        }

        private void OnEnable()
        {
            GameEventBus.Instance.Subscribe<HitConfirmedEvent>(OnHitConfirmed);
            GameEventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        }

        private void OnDisable()
        {
            if (GameEventBus.Instance != null)
            {
                GameEventBus.Instance.Unsubscribe<HitConfirmedEvent>(OnHitConfirmed);
                GameEventBus.Instance.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
            }
        }

        private void LateUpdate()
        {
            if (hitMarkerGraphic == null)
            {
                return;
            }

            // 超过展示时长后自动隐藏，避免命中标记长期滞留在屏幕上。
            if (Time.unscaledTime >= hideTime && hitMarkerGraphic.enabled)
            {
                ApplyVisible(false);
            }
        }

        private void OnHitConfirmed(HitConfirmedEvent _eventArgs)
        {
            if (enableDebugLog)
            {
                string targetName = _eventArgs.Target != null ? _eventArgs.Target.name : "Unknown";
                Debug.Log($"[{nameof(HitFeedbackPresenter)}] Hit confirmed. Target={targetName}, Part={_eventArgs.HitPartType}, Point={_eventArgs.HitPoint}", this);
            }

            if (hitMarkerGraphic == null)
            {
                return;
            }

            // 命中确认先给基础反馈；弱点命中在这一步直接提高颜色层级。
            hitMarkerGraphic.color = _eventArgs.HitPartType == HitPartType.WeakPoint
                ? weakPointHitMarkerColor
                : hitMarkerColor;
            hideTime = Time.unscaledTime + displayDuration;
            ApplyVisible(true);
        }

        private void OnDamageApplied(DamageAppliedEvent _eventArgs)
        {
            if (hitMarkerGraphic == null)
            {
                return;
            }

            DamageResult damageResult = _eventArgs.DamageResult;

            // 生命耗尽结果会比命中确认更完整，所以在这里覆盖最终反馈层级。
            if (damageResult.DidDepleteHealth)
            {
                hitMarkerGraphic.color = killHitMarkerColor;
            }

            // 重新推进显示截止时间，保证最终反馈不会被基础命中标记立即盖掉。
            hideTime = Time.unscaledTime + displayDuration;
            ApplyVisible(true);
        }

        private void ApplyVisible(bool _visible)
        {
            if (hitMarkerGraphic != null)
            {
                // 这里只控制 Graphic 可见性，不在 HUD 层对命中标记做额外动画裁决。
                hitMarkerGraphic.enabled = _visible;
            }
        }
    }
}
