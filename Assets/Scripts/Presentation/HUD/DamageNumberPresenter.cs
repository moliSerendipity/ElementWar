using Game.Foundation.Events;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using TMPro;
using UnityEngine;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 最小伤害数字 Presenter。
    /// 当前阶段先把 Combat 结果接到单个文本出口，
    /// 后续如果需要正式飘字池，再从这里拆到独立实例系统。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumberPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text debugDamageText;
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color weakPointDamageColor = new(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color killDamageColor = Color.red;
        [SerializeField] private float displayDuration = 0.5f;
        [SerializeField] private string weakPointPrefix = "WEAK ";
        [SerializeField] private string killSuffix = " KILL";

        private float hideTime;

        private void Awake()
        {
            // 初始先关闭调试飘字，避免启动时展示脏数据。
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
            if (debugDamageText == null)
            {
                return;
            }

            // 到时间后自动隐藏，保持调试文本只在最近一次伤害窗口内可见。
            if (Time.unscaledTime >= hideTime && debugDamageText.enabled)
            {
                ApplyVisible(false);
            }
        }

        private void OnDamageApplied(DamageAppliedEvent _eventArgs)
        {
            if (debugDamageText == null)
            {
                return;
            }

            DamageResult damageResult = _eventArgs.DamageResult;
            string text = $"{damageResult.FinalDamage:0}";

            // 弱点命中使用独立前缀；生命耗尽在结果阶段追加最终反馈。
            if (damageResult.HitPartType == HitPartType.WeakPoint)
            {
                text = weakPointPrefix + text;
            }

            if (damageResult.DidDepleteHealth)
            {
                text += killSuffix;
            }

            // 伤害文本只消费最终裁决结果，不在 HUD 层自行推导生命事实。
            debugDamageText.text = text;
            debugDamageText.color = ResolveColor(damageResult);
            hideTime = Time.unscaledTime + displayDuration;
            ApplyVisible(true);
        }

        private Color ResolveColor(DamageResult _damageResult)
        {
            if (_damageResult.DidDepleteHealth)
            {
                return killDamageColor;
            }

            if (_damageResult.HitPartType == HitPartType.WeakPoint)
            {
                return weakPointDamageColor;
            }

            return normalDamageColor;
        }

        private void ApplyVisible(bool _visible)
        {
            if (debugDamageText != null)
            {
                // 当前阶段仍然只控制单个调试文本，不在这里扩成实例化飘字系统。
                debugDamageText.enabled = _visible;
            }
        }
    }
}
