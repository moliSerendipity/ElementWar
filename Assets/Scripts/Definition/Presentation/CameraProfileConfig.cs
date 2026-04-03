using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Presentation
{
    [CreateAssetMenu(fileName = "CameraProfileConfig", menuName = "Game/Configs/Presentation/Camera Profile Config")]
    public sealed class CameraProfileConfig : ConfigBase
    {
        [SerializeField] private float defaultFov = 55f;
        [SerializeField] private float aimFov = 47f;

        public float DefaultFov => defaultFov;
        public float AimFov => aimFov;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
            if (defaultFov <= 1f || defaultFov >= 179f)
            {
                _context.AddError(ConfigId, "defaultFov 必须处于 (1, 179) 范围内。");
            }
            if (aimFov <= 1f || aimFov >= 179f)
            {
                _context.AddError(ConfigId, "aimFov 必须处于 (1, 179) 范围内。");
            }
        }
    }
}
