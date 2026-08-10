using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Character
{
    /// <summary>
    /// 角色基础面板配置
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterBaseStatConfig", menuName = "Game/Configs/Character/Character Base Stat Config")]
    public sealed class CharacterBaseStatConfig : ConfigBase
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float attackPower = 10f;
        [SerializeField] private float defense = 0f;
        [SerializeField] private float maxShield;
        [SerializeField] private float toughness = 100f;
        [SerializeField] private float damageTakenMultiplier = 1f;
        [SerializeField] private float healingTakenMultiplier = 1f;

        public float MaxHealth => maxHealth;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public float MaxShield => maxShield;
        public float Toughness => toughness;
        public float DamageTakenMultiplier => damageTakenMultiplier;
        public float HealingTakenMultiplier => healingTakenMultiplier;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (maxHealth <= 0f)
            {
                _context.AddError(ConfigId, "MaxHealth 必须大于 0。");
            }

            if (maxShield < 0f)
            {
                _context.AddError(ConfigId, "MaxShield 不能小于 0。");
            }

            if (toughness < 0f)
            {
                _context.AddError(ConfigId, "Toughness 不能小于 0。");
            }

            if (damageTakenMultiplier <= 0f)
            {
                _context.AddError(ConfigId, "DamageTakenMultiplier 必须大于 0。");
            }

            if (healingTakenMultiplier < 0f)
            {
                _context.AddError(ConfigId, "HealingTakenMultiplier 不能小于 0。");
            }
        }
    }
}
