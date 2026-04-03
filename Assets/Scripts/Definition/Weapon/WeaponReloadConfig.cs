using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Weapon
{
    public enum WeaponReloadType
    {
        Magazine = 0,
        PerBullet = 1,
    }

    /// <summary>
    /// 换弹流程专用配置。
    /// 和 WeaponStatConfig 分离，是为了避免“面板数值”和“流程阶段时序”混在一起。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponReloadConfig", menuName = "Game/Configs/Weapon/Weapon Reload Config")]
    public sealed class WeaponReloadConfig : ConfigBase
    {
        [SerializeField] private WeaponReloadType reloadType = WeaponReloadType.Magazine;
        [Min(0f)][SerializeField] private float reloadDuration = 1.8f;
        [Min(0f)][SerializeField] private float tacticalReloadDuration = 1.5f;
        [Min(0f)][SerializeField] private float perBulletReloadDuration;
        [SerializeField] private bool allowInterruptReload = true;
        [SerializeField] private bool allowFireBreakReload = false;
        [SerializeField] private bool allowSwitchBreakReload = true;

        public WeaponReloadType ReloadType => reloadType;
        public float ReloadDuration => reloadDuration;
        public float TacticalReloadDuration => tacticalReloadDuration;
        public float PerBulletReloadDuration => perBulletReloadDuration;
        public bool AllowInterruptReload => allowInterruptReload;
        public bool AllowFireBreakReload => allowFireBreakReload;
        public bool AllowSwitchBreakReload => allowSwitchBreakReload;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (reloadType == WeaponReloadType.Magazine && reloadDuration <= 0f)
            {
                _context.AddError(ConfigId, "Magazine ReloadType 下，ReloadDuration 必须大于 0。");
            }

            if (reloadType == WeaponReloadType.PerBullet && perBulletReloadDuration <= 0f)
            {
                _context.AddError(ConfigId, "PerBullet ReloadType 下，PerBulletReloadDuration 必须大于 0。");
            }
        }
    }
}
