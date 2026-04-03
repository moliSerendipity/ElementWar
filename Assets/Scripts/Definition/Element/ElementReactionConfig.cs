using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.AreaEffect;
using Game.Definition.Buff;

namespace Game.Definition.Element
{
    /// <summary>
    /// 元素反应规则表。
    /// 当前阶段先承载主倍率、范围与附带 Buff / 区域入口。
    /// </summary>
    [CreateAssetMenu(fileName = "ElementReactionConfig", menuName = "Game/Configs/Element/Element Reaction Config")]
    public sealed class ElementReactionConfig : ConfigBase
    {
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float toughnessDamageMultiplier = 1f;
        [SerializeField] private float areaRadius;
        [SerializeField] private string appliedBuffConfigId;
        [SerializeField] private string spawnedAreaEffectConfigId;
        [SerializeField] private bool canChainReaction;

        public float DamageMultiplier => damageMultiplier;
        public float ToughnessDamageMultiplier => toughnessDamageMultiplier;
        public float AreaRadius => areaRadius;
        public string AppliedBuffConfigId => ConfigIdUtility.Normalize(appliedBuffConfigId);
        public string SpawnedAreaEffectConfigId => ConfigIdUtility.Normalize(spawnedAreaEffectConfigId);
        public bool CanChainReaction => canChainReaction;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (damageMultiplier < 0f)
            {
                _context.AddError(ConfigId, "damageMultiplier 不能小于 0。");
            }

            if (toughnessDamageMultiplier < 0f)
            {
                _context.AddError(ConfigId, "toughnessDamageMultiplier 不能小于 0。");
            }

            if (areaRadius < 0f)
            {
                _context.AddError(ConfigId, "areaRadius 不能小于 0。");
            }

            if (ConfigIdUtility.IsValid(appliedBuffConfigId))
            {
                _configService.ValidateRequiredReference<BuffDefinitionConfig>(_context, ConfigId, appliedBuffConfigId, nameof(appliedBuffConfigId));
            }

            if (ConfigIdUtility.IsValid(spawnedAreaEffectConfigId))
            {
                _configService.ValidateRequiredReference<AreaEffectConfig>(_context, ConfigId, spawnedAreaEffectConfigId, nameof(spawnedAreaEffectConfigId));
            }
        }
    }
}
