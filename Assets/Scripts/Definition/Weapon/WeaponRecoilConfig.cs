using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.Weapon
{
    /// <summary>
    /// 武器后坐力配置。
    /// 只描述真实后坐力与表现层短时 Kick 所需的最小参数。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponRecoilConfig", menuName = "Game/Configs/Weapon/Weapon Recoil Config")]
    public sealed class WeaponRecoilConfig : ConfigBase
    {
        #region Inspector

        [Header("Control Recoil")]
        [SerializeField] private float recoilPitchPerShotMin = -1.25f;
        [SerializeField] private float recoilPitchPerShotMax = -0.75f;
        [SerializeField] private float recoilYawPerShotMin = -0.35f;
        [SerializeField] private float recoilYawPerShotMax = 0.35f;
        [Header("Presentation Kick")]
        [SerializeField] private float cameraKickPitch = -0.25f;
        [SerializeField] private float cameraKickYaw = 0.12f;
        [Min(0f)][SerializeField] private float crosshairKick = 0.12f;

        #endregion

        #region Properties

        public float RecoilPitchPerShotMin => recoilPitchPerShotMin;
        public float RecoilPitchPerShotMax => recoilPitchPerShotMax;
        public float RecoilYawPerShotMin => recoilYawPerShotMin;
        public float RecoilYawPerShotMax => recoilYawPerShotMax;
        public float CameraKickPitch => cameraKickPitch;
        public float CameraKickYaw => cameraKickYaw;
        public float CrosshairKick => crosshairKick;

        #endregion

        #region Validation

        /// <summary>
        /// 校验后坐力配置是否合法。
        /// </summary>
        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            // 竖直后坐力区间必须满足最小值不大于最大值。
            if (recoilPitchPerShotMin > recoilPitchPerShotMax)
            {
                _context.AddError(ConfigId, "recoilPitchPerShotMin 不能大于 recoilPitchPerShotMax。");
            }

            // 水平后坐力区间必须满足最小值不大于最大值。
            if (recoilYawPerShotMin > recoilYawPerShotMax)
            {
                _context.AddError(ConfigId, "recoilYawPerShotMin 不能大于 recoilYawPerShotMax。");
            }
        }

        #endregion
    }
}
