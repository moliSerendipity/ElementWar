using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.HUD
{
    [CreateAssetMenu(fileName = "CrosshairConfig", menuName = "Game/Configs/HUD/Crosshair Config")]
    public class CrosshairConfig : ConfigBase
    {
        [SerializeField, Min(0f)] private float baseGap = 30f;
        [SerializeField, Min(0f)] private float maxGap = 100f;
        [SerializeField, Min(1f)] private float lineThickness = 3f;
        [SerializeField, Min(1f)] private float lineLength = 50f;
        [SerializeField] private bool showCenterDot = true;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color blockingHitColor = Color.red;
        [SerializeField] private Color fallbackPointColor = Color.blue;
        [SerializeField] private Color hitConfirmColor = Color.yellow;
        [SerializeField] private Color weakPointHitConfirmColor = new(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color killHitConfirmColor = new(1f, 0.25f, 0.25f, 1f);

        [Header("Pulse")]
        [SerializeField, Min(1f)] private float hitPulseScale = 1.15f;
        [SerializeField, Min(1f)] private float weakPointHitPulseScale = 1.22f;
        [SerializeField, Min(1f)] private float killHitPulseScale = 1.3f;

        public float BaseGap => baseGap;
        public float MaxGap => maxGap;
        public float LineThickness => lineThickness;
        public float LineLength => lineLength;
        public bool ShowCenterDot => showCenterDot;
        public Color DefaultColor => defaultColor;
        public Color BlockingHitColor => blockingHitColor;
        public Color FallbackPointColor => fallbackPointColor;
        public Color HitConfirmColor => hitConfirmColor;
        public Color WeakPointHitConfirmColor => weakPointHitConfirmColor;
        public Color KillHitConfirmColor => killHitConfirmColor;
        public float HitPulseScale => hitPulseScale;
        public float WeakPointHitPulseScale => weakPointHitPulseScale;
        public float KillHitPulseScale => killHitPulseScale;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (maxGap < baseGap)
            {
                _context.AddError(ConfigId, "maxGap 不得小于 baseGap。");
            }

            if (lineThickness <= 0f)
            {
                _context.AddError(ConfigId, "lineThickness 必须大于 0。");
            }

            if (lineLength <= 0f)
            {
                _context.AddError(ConfigId, "lineLength 必须大于 0。");
            }
        }
    }
}
