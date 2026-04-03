using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.Enemy
{
    /// <summary>
    /// 敌人基础面板配置
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBaseStatConfig", menuName = "Game/Configs/Enemy/Enemy Base Stat Config")]
    public sealed class EnemyBaseStatConfig : ConfigBase
    {
        [SerializeField] private float maxHealth = 120f;
        [SerializeField] private float attackPower = 12f;
        [SerializeField] private float defense = 2f;
        [SerializeField] private float toughness = 120f;
        [SerializeField] private float damageTakenMultiplier = 1f;
        [SerializeField] private float weakPointDamageMultiplier = 1.5f;

        public float MaxHealth => maxHealth;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public float Toughness => toughness;
        public float DamageTakenMultiplier => damageTakenMultiplier;
        public float WeakPointDamageMultiplier => weakPointDamageMultiplier;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (maxHealth <= 0f)
            {
                _context.AddError(ConfigId, "MaxHealth 必须大于 0。");
            }

            if (damageTakenMultiplier <= 0f)
            {
                _context.AddError(ConfigId, "DamageTakenMultiplier 必须大于 0。");
            }

            if (weakPointDamageMultiplier < 1f)
            {
                _context.AddWarning(ConfigId, "WeakPointDamageMultiplier 小于 1，通常不符合敌人弱点预期。");
            }
        }
    }
}
