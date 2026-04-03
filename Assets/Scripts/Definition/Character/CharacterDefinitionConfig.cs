using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Weapon;
using Game.Definition.Combat;
using Game.Definition.AI;
using Game.Definition.Skill;

namespace Game.Definition.Character
{
    /// <summary>
    /// 角色主定义表。只负责挂载关系，不直接承载具体战斗或移动数值。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDefinitionConfig", menuName = "Game/Configs/Character/Character Definition Config")]
    public sealed class CharacterDefinitionConfig : ConfigBase
    {
        [SerializeField] private CharacterBaseStatConfig characterBaseStatConfig;
        [SerializeField] private CharacterMovementConfig characterMovementConfig;
        [SerializeField] private CharacterJumpConfig characterJumpConfig;
        [SerializeField] private CharacterAimConfig characterAimConfig;
        [SerializeField] private CharacterSwitchConfig characterSwitchConfig;
        [SerializeField] private WeaponLoadoutConfig weaponLoadoutConfig;
        [SerializeField] private SkillLoadoutConfig skillLoadoutConfig;
        [SerializeField] private ResistanceSetConfig characterResistanceSetConfig;
        [SerializeField] private ActorAIProfileConfig aiProfileConfig;
        [SerializeField] private bool isPlayerControllable = true;
        [SerializeField] private bool isAiControllable;

        public CharacterBaseStatConfig CharacterBaseStatConfig => characterBaseStatConfig;
        public CharacterMovementConfig CharacterMovementConfig => characterMovementConfig;
        public CharacterJumpConfig CharacterJumpConfig => characterJumpConfig;
        public CharacterAimConfig CharacterAimConfig => characterAimConfig;
        public CharacterSwitchConfig CharacterSwitchConfig => characterSwitchConfig;
        public WeaponLoadoutConfig WeaponLoadoutConfig => weaponLoadoutConfig;
        public SkillLoadoutConfig SkillLoadoutConfig => skillLoadoutConfig;
        public ResistanceSetConfig CharacterResistanceSetConfig => characterResistanceSetConfig;
        public ActorAIProfileConfig AiProfileConfig => aiProfileConfig;
        public bool IsPlayerControllable => isPlayerControllable;
        public bool IsAiControllable => isAiControllable;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (characterBaseStatConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(characterBaseStatConfig)} 不能为空。");
            }

            if (characterMovementConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(characterMovementConfig)} 不能为空。");
            }

            if (characterJumpConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(characterJumpConfig)} 不能为空。");
            }

            if (characterAimConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(characterAimConfig)} 不能为空。");
            }

            if (characterSwitchConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(characterSwitchConfig)} 不能为空。");
            }

            if (weaponLoadoutConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(weaponLoadoutConfig)} 不能为空。");
            }

            if (characterResistanceSetConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(characterResistanceSetConfig)} 不能为空。");
            }

            if (isAiControllable && aiProfileConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(aiProfileConfig)} 不能为空。");
            }
        }
    }
}