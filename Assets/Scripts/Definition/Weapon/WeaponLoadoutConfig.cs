using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Weapon
{
    [CreateAssetMenu(fileName = "WeaponLoadoutConfig", menuName = "Game/Configs/Weapon/Weapon Loadout Config")]
    public sealed class WeaponLoadoutConfig : ConfigBase
    {
        [SerializeField] private string primaryWeaponId;
        [SerializeField] private string secondaryWeaponId;
        [SerializeField] private string meleeWeaponId;

        public string PrimaryWeaponId => ConfigIdUtility.Normalize(primaryWeaponId);
        public string SecondaryWeaponId => ConfigIdUtility.Normalize(secondaryWeaponId);
        public string MeleeWeaponId => ConfigIdUtility.Normalize(meleeWeaponId);

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            if (ConfigIdUtility.IsValid(primaryWeaponId))
            {
                _configService.ValidateRequiredReference<WeaponDefinitionConfig>(_context, ConfigId, primaryWeaponId, nameof(primaryWeaponId));
            }
            if (ConfigIdUtility.IsValid(secondaryWeaponId))
            {
                _configService.ValidateRequiredReference<WeaponDefinitionConfig>(_context, ConfigId, secondaryWeaponId, nameof(secondaryWeaponId));
            }
            if (ConfigIdUtility.IsValid(meleeWeaponId))
            {
                _configService.ValidateRequiredReference<WeaponDefinitionConfig>(_context, ConfigId, meleeWeaponId, nameof(meleeWeaponId));
            }
        }
    }
}
