using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Buff;

namespace Game.Definition.AreaEffect
{
    /// <summary>
    /// 区域效果主定义表。
    /// 当前阶段先承载范围、持续时间、结算频率与 Buff 挂接入口。
    /// </summary>
    [CreateAssetMenu(fileName = "AreaEffectConfig", menuName = "Game/Configs/AreaEffect/Area Effect Config")]
    public sealed class AreaEffectConfig : ConfigBase
    {
        [SerializeField] private float radius = 1f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private string appliedBuffConfigId;
        [SerializeField] private bool followCaster;
        [SerializeField] private bool stickToGround = true;

        public float Radius => radius;
        public float Duration => duration;
        public float TickInterval => tickInterval;
        public string AppliedBuffConfigId => ConfigIdUtility.Normalize(appliedBuffConfigId);
        public bool FollowCaster => followCaster;
        public bool StickToGround => stickToGround;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (radius <= 0f)
            {
                _context.AddError(ConfigId, "radius 必须大于 0。");
            }

            if (duration < 0f)
            {
                _context.AddError(ConfigId, "duration 不能小于 0。");
            }

            if (tickInterval <= 0f)
            {
                _context.AddError(ConfigId, "tickInterval 必须大于 0。");
            }

            if (ConfigIdUtility.IsValid(appliedBuffConfigId))
            {
                _configService.ValidateRequiredReference<BuffDefinitionConfig>(_context, ConfigId, appliedBuffConfigId, nameof(appliedBuffConfigId));
            }
        }
    }
}
