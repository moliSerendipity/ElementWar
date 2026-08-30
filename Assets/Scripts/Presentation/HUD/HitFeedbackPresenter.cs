using Game.Foundation.Events;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 最小命中反馈 Presenter。
    /// 只消费已提交伤害结果事件，并把它转成短时可见的 HUD 命中标记；
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
            GameEventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        }

        private void OnDisable()
        {
            if (GameEventBus.Instance != null)
            {
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

        private void OnDamageApplied(DamageAppliedEvent _eventArgs)
        {
            DamageResult damageResult = _eventArgs.DamageResult;

            if (enableDebugLog)
            {
                string targetName = damageResult.TargetCombatant != null
                    ? damageResult.TargetCombatant.name
                    : "Unknown";
                Debug.Log(
                    $"[{nameof(HitFeedbackPresenter)}] Damage applied. Target={targetName}, Element={damageResult.Element}, Part={damageResult.HitPartType}, Point={damageResult.HitPoint}",
                    this);
            }

            if (hitMarkerGraphic == null)
            {
                return;
            }

            // 已提交伤害结果一次性决定普通、弱点或击杀反馈层级。
            hitMarkerGraphic.color = damageResult.DidDepleteHealth
                ? killHitMarkerColor
                : damageResult.HitPartType == HitPartType.WeakPoint
                    ? weakPointHitMarkerColor
                    : hitMarkerColor;
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
