using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Buff
{
    /// <summary>
    /// Buff 主定义表。
    /// 当前阶段先承载持续时间、叠层与周期效果等主链字段，后续再继续细化触发条件与修饰器集合。
    /// </summary>
    [CreateAssetMenu(fileName = "BuffDefinitionConfig", menuName = "Game/Configs/Buff/Buff Definition Config")]
    public sealed class BuffDefinitionConfig : ConfigBase
    {
        [SerializeField] private float duration = 5f;
        [SerializeField] private int maxStackCount = 1;
        [SerializeField] private float tickInterval;
        [SerializeField] private bool useSnapshot;

        public float Duration => duration;
        public int MaxStackCount => maxStackCount;
        public float TickInterval => tickInterval;
        public bool UseSnapshot => useSnapshot;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (duration < 0f)
            {
                _context.AddError(ConfigId, "duration 不能小于 0。");
            }

            if (maxStackCount <= 0)
            {
                _context.AddError(ConfigId, "maxStackCount 必须大于 0。");
            }

            if (tickInterval < 0f)
            {
                _context.AddError(ConfigId, "tickInterval 不能小于 0。");
            }

            if (tickInterval > 0f && duration > 0f && tickInterval > duration)
            {
                _context.AddWarning(ConfigId, "tickInterval 大于 duration，生命周期内可能不会产生有效 Tick。");
            }
        }
    }
}
