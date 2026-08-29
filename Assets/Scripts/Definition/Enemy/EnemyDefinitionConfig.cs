using System;
using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Combat;
using Game.Definition.Weapon;
using Game.Definition.Skill;

namespace Game.Definition.Enemy
{
    /// <summary>
    /// 敌人面对硬控制时采用的首版等级策略。
    /// </summary>
    public enum EnemyTier
    {
        /// <summary>接受来源提供的完整硬控制时长。</summary>
        Normal,
        /// <summary>接受来源提供的一半硬控制时长。</summary>
        Elite,
        /// <summary>不进入硬控制；转换削韧会与同次攻击的基础削韧相加后统一结算。</summary>
        Boss,
    }

    /// <summary>
    /// 敌人主定义表
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinitionConfig", menuName = "Game/Configs/Enemy/Enemy Definition Config")]
    public sealed class EnemyDefinitionConfig : ConfigBase
    {
        [SerializeField] private EnemyTier enemyTier = EnemyTier.Normal;
        [SerializeField] private EnemyBaseStatConfig enemyBaseStatConfig;
        [SerializeField] private EnemyMovementConfig enemyMovementConfig;
        [SerializeField] private ResistanceSetConfig enemyResistanceSetConfig;
        [SerializeField] private WeaponLoadoutConfig weaponLoadoutConfig;
        [SerializeField] private SkillLoadoutConfig skillLoadoutConfig;

        /// <summary>该敌人面对硬控制时采用的等级策略。</summary>
        public EnemyTier EnemyTier => enemyTier;
        public EnemyBaseStatConfig EnemyBaseStatConfig => enemyBaseStatConfig;
        public EnemyMovementConfig EnemyMovementConfig => enemyMovementConfig;
        public ResistanceSetConfig EnemyResistanceSetConfig => enemyResistanceSetConfig;
        public WeaponLoadoutConfig WeaponLoadoutConfig => weaponLoadoutConfig;
        public SkillLoadoutConfig SkillLoadoutConfig => skillLoadoutConfig;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (Enum.IsDefined(typeof(EnemyTier), enemyTier) == false)
            {
                _context.AddError(ConfigId, $"字段 {nameof(enemyTier)} 不是有效的敌人等级。");
            }

            if (enemyBaseStatConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(enemyBaseStatConfig)} 不能为空。");
            }

            if (enemyMovementConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(enemyMovementConfig)} 不能为空。");
            }

            if (enemyResistanceSetConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(enemyResistanceSetConfig)} 不能为空。");
            }

            if (weaponLoadoutConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(weaponLoadoutConfig)} 不能为空。");
            }
        }
    }
}
