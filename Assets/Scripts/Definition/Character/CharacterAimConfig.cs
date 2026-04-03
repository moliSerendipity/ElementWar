using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Presentation;

namespace Game.Definition.Character
{
    [CreateAssetMenu(fileName = "CharacterAimConfig", menuName = "Game/Configs/Character/Character Aim Config")]
    public sealed class CharacterAimConfig : ConfigBase
    {
        [SerializeField] private string cameraProfileConfigId;
        [SerializeField] private float aimMoveSpeedMultiplier = 0.7f;

        public string CameraProfileConfigId => ConfigIdUtility.Normalize(cameraProfileConfigId);
        public float AimMoveSpeedMultiplier => aimMoveSpeedMultiplier;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            _configService.ValidateRequiredReference<CameraProfileConfig>(_context, ConfigId, cameraProfileConfigId, nameof(cameraProfileConfigId));
            if (aimMoveSpeedMultiplier <= 0f)
            {
                _context.AddError(ConfigId, "aimMoveSpeedMultiplier 必须大于 0。");
            }
        }
    }
}
