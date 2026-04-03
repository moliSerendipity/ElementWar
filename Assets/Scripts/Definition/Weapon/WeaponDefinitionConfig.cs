using UnityEngine;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Presentation;

namespace Game.Definition.Weapon
{
    public enum WeaponFireMode
    {
        Single = 0,
        Burst = 1,
        Auto = 2,
    }

    public enum WeaponAmmoType
    {
        None = 0,
        Rifle = 1,
        Pistol = 2,
        Shotgun = 3,
        Sniper = 4,
        Energy = 5,
        Special = 6,
    }

    /// <summary>
    /// 武器主定义表。
    ///
    /// 当前版本改为直接持有正式子配置引用：
    /// 1. 避免 WeaponRuntime / Presentation 在运行时继续走多层字符串 Id 查询；
    /// 2. 让主表承担“装配关系”职责，子表承担规则与资源职责；
    /// 3. 减少初始化阶段样板代码和错链风险。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDefinitionConfig", menuName = "Game/Configs/Weapon/Weapon Definition Config")]
    public sealed class WeaponDefinitionConfig : ConfigBase
    {
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Single;
        [SerializeField] private WeaponAmmoType ammoType = WeaponAmmoType.None;
        [SerializeField] private WeaponStatConfig weaponStatConfig;
        [SerializeField] private WeaponReloadConfig weaponReloadConfig;
        [SerializeField] private WeaponPresentationConfig weaponPresentationConfig;
        [SerializeField] private WeaponSpreadConfig weaponSpreadConfig;
        [SerializeField] private WeaponRecoilConfig weaponRecoilConfig;
        [SerializeField] private bool canAim = true;
        [SerializeField] private bool canSprintFire;
        [SerializeField] private bool canFireInAir = true;

        public WeaponFireMode FireMode => fireMode;
        public WeaponAmmoType AmmoType => ammoType;
        public WeaponStatConfig WeaponStatConfig => weaponStatConfig;
        public WeaponReloadConfig WeaponReloadConfig => weaponReloadConfig;
        public WeaponPresentationConfig WeaponPresentationConfig => weaponPresentationConfig;
        public WeaponSpreadConfig WeaponSpreadConfig => weaponSpreadConfig;
        public WeaponRecoilConfig WeaponRecoilConfig => weaponRecoilConfig;
        public bool CanAim => canAim;
        public bool CanSprintFire => canSprintFire;
        public bool CanFireInAir => canFireInAir;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (weaponStatConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(weaponStatConfig)} 不能为空。");
            }

            if (weaponReloadConfig == null)
            {
                _context.AddError(ConfigId, $"字段 {nameof(weaponReloadConfig)} 不能为空。");
            }

            if (weaponSpreadConfig == null)
            {
                _context.AddWarning(ConfigId, "SpreadConfig 为空。当前可以运行，但准星扩张与散布规则缺正式配置入口。");
            }

            if (weaponRecoilConfig == null)
            {
                _context.AddWarning(ConfigId, "RecoilConfig 为空。当前可以运行，但后坐力规则缺正式配置入口。");
            }

            if (weaponPresentationConfig == null)
            {
                _context.AddWarning(ConfigId, "PresentationConfig 为空。当前 HUD 仍可退化显示，但后续正式表现层会缺武器展示入口。");
            }

            if (ammoType == WeaponAmmoType.None)
            {
                _context.AddWarning(ConfigId, "AmmoType 仍为 None。当前阶段虽然还能运行，但后续共享备弹/库存映射会缺正式来源。");
            }
        }
    }
}
