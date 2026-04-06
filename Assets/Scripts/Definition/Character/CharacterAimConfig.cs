using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Presentation;

namespace Game.Definition.Character
{
    [CreateAssetMenu(fileName = "CharacterAimConfig", menuName = "Game/Configs/Character/Character Aim Config")]
    public sealed class CharacterAimConfig : ConfigBase
    {
        [SerializeField] private float aimMoveSpeedMultiplier = 0.7f;
        [SerializeField] private float normalYawSensitivity = 0.18f;
        [SerializeField] private float normalPitchSensitivity = 0.12f;
        [SerializeField] private float aimYawSensitivity = 0.12f;
        [SerializeField] private float aimPitchSensitivity = 0.08f;

        public float AimMoveSpeedMultiplier => aimMoveSpeedMultiplier;
        public float NormalYawSensitivity => normalYawSensitivity;
        public float NormalPitchSensitivity => normalPitchSensitivity;
        public float AimYawSensitivity => aimYawSensitivity;
        public float AimPitchSensitivity => aimPitchSensitivity;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            if (aimMoveSpeedMultiplier <= 0f)
            {
                _context.AddError(ConfigId, "aimMoveSpeedMultiplier 必须大于 0。");
            }

            if (normalYawSensitivity <= 0f)
            {
                _context.AddError(ConfigId, "normalYawSensitivity 必须大于 0。");
            }

            if (normalPitchSensitivity <= 0f)
            {
                _context.AddError(ConfigId, "normalPitchSensitivity 必须大于 0。");
            }

            if (aimYawSensitivity <= 0f)
            {
                _context.AddError(ConfigId, "aimYawSensitivity 必须大于 0。");
            }

            if (aimPitchSensitivity <= 0f)
            {
                _context.AddError(ConfigId, "aimPitchSensitivity 必须大于 0。");
            }
        }
    }
}
