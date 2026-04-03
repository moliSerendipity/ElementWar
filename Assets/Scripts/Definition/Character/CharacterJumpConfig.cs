using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Character
{
    [CreateAssetMenu(fileName = "CharacterJumpConfig", menuName = "Game/Configs/Character/Character Jump Config")]
    public sealed class CharacterJumpConfig : ConfigBase
    {
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float maxFallSpeed = -30f;

        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float MaxFallSpeed => maxFallSpeed;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            if (jumpHeight <= 0f)
            {
                _context.AddError(ConfigId, "jumpHeight 必须大于 0。");
            }
        }
    }
}
