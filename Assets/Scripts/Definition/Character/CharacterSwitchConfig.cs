using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Presentation;

namespace Game.Definition.Character
{
    [CreateAssetMenu(fileName = "CharacterSwitchConfig", menuName = "Game/Configs/Character/Character Switch Config")]
    public sealed class CharacterSwitchConfig : ConfigBase
    {
        [SerializeField] private float switchCooldown = 0.2f;
        [SerializeField] private string switchCameraBlendConfigId;

        public float SwitchCooldown => switchCooldown;
        public string SwitchCameraBlendConfigId => ConfigIdUtility.Normalize(switchCameraBlendConfigId);

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            if (switchCooldown < 0f)
            {
                _context.AddError(ConfigId, "switchCooldown 不能小于 0。");
            }
            if (ConfigIdUtility.IsValid(switchCameraBlendConfigId))
            {
                _configService.ValidateRequiredReference<CameraBlendConfig>(_context, ConfigId, switchCameraBlendConfigId, nameof(switchCameraBlendConfigId));
            }
        }
    }
}
