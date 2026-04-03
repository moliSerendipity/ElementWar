using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Combat
{
    /// <summary>
    /// 通用伤害规则基线表。
    /// 当前阶段只保留最小闭环需要的规则开关，不提前铺开高级伤害公式枚举。
    /// </summary>
    [CreateAssetMenu(fileName = "DamageRuleConfig", menuName = "Game/Configs/Combat/Damage Rule Config")]
    public sealed class DamageRuleConfig : ConfigBase
    {
        [SerializeField] private bool canCritical = true;
        [SerializeField] private bool canHeadShot = true;
        [SerializeField] private bool canWeakPoint = true;

        public bool CanCritical => canCritical;
        public bool CanHeadShot => canHeadShot;
        public bool CanWeakPoint => canWeakPoint;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
        }
    }
}
