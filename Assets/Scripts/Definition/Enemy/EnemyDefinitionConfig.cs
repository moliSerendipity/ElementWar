using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Combat;
using Game.Definition.AI;
using Game.Definition.Weapon;
using Game.Definition.Skill;

namespace Game.Definition.Enemy
{
    /// <summary>
    /// 敌人主定义表
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinitionConfig", menuName = "Game/Configs/Enemy/Enemy Definition Config")]
    public sealed class EnemyDefinitionConfig : ConfigBase
    {
        [SerializeField] private EnemyBaseStatConfig enemyBaseStatConfig;
        [SerializeField] private EnemyMovementConfig enemyMovementConfig;
        [SerializeField] private ResistanceSetConfig enemyResistanceSetConfig;
        [SerializeField] private ActorAIProfileConfig aiProfileConfig;
        [SerializeField] private WeaponLoadoutConfig weaponLoadoutConfig;
        [SerializeField] private SkillLoadoutConfig skillLoadoutConfig;

        public EnemyBaseStatConfig EnemyBaseStatConfig => enemyBaseStatConfig;
        public EnemyMovementConfig EnemyMovementConfig => enemyMovementConfig;
        public ResistanceSetConfig EnemyResistanceSetConfig => enemyResistanceSetConfig;
        public ActorAIProfileConfig AiProfileConfig => aiProfileConfig;
        public WeaponLoadoutConfig WeaponLoadoutConfig => weaponLoadoutConfig;
        public SkillLoadoutConfig SkillLoadoutConfig => skillLoadoutConfig;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

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

            if (aiProfileConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(aiProfileConfig)} 不能为空。");
            }

            if (weaponLoadoutConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(weaponLoadoutConfig)} 不能为空。");
            }
        }
    }
}
