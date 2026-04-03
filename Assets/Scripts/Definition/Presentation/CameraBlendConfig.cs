using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Presentation
{
    [CreateAssetMenu(fileName = "CameraBlendConfig", menuName = "Game/Configs/Presentation/Camera Blend Config")]
    public sealed class CameraBlendConfig : ConfigBase
    {
        [SerializeField] private float blendDuration = 0.2f;

        public float BlendDuration => blendDuration;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            if (blendDuration < 0f)
            {
                _context.AddError(ConfigId, "blendDuration 不能小于 0。");
            }
        }
    }
}
