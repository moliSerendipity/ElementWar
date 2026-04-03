using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Debug
{
    /// <summary>
    /// 调试开关配置。
    /// 只负责调试显示和日志，不参与任何正式业务判定。
    /// </summary>
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "Game/Configs/UI/Debug Config")]
    public sealed class DebugConfig : ConfigBase
    {
        [SerializeField] private bool enableDamageLog = true;
        [SerializeField] private bool enableBuffLog = true;
        [SerializeField] private bool enableElementLog = true;
        [SerializeField] private bool enableAiLog = true;
        [SerializeField] private bool enableConfigValidationLog = true;
        [Min(0f)]
        [SerializeField] private float gizmoDrawDistance = 50f;

        public bool EnableDamageLog => enableDamageLog;
        public bool EnableBuffLog => enableBuffLog;
        public bool EnableElementLog => enableElementLog;
        public bool EnableAiLog => enableAiLog;
        public bool EnableConfigValidationLog => enableConfigValidationLog;
        public float GizmoDrawDistance => gizmoDrawDistance;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (gizmoDrawDistance < 0f)
            {
                _context.AddError(ConfigId, "GizmoDrawDistance 不能小于 0。");
            }
        }
    }
}
