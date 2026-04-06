using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Weapon
{
    /// <summary>
    /// 武器散布配置。
    /// 当前版本只描述规则与默认值，不保存运行时散布结果。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponSpreadConfig", menuName = "Game/Configs/Weapon/Weapon Spread Config")]
    public sealed class WeaponSpreadConfig : ConfigBase
    {
        [Min(0f)][SerializeField] private float baseSpread = 1f;
        [Min(0f)][SerializeField] private float aimSpread = 0.35f;
        [Min(0f)][SerializeField] private float maxSpread = 4.2f;
        [Min(0f)][SerializeField] private float spreadIncreasePerShot = 0.45f;
        [Min(0f)][SerializeField] private float spreadRecoverSpeed = 15f;
        [Min(0f)][SerializeField] private float movingSpreadPenalty = 1.4f;
        [Min(0f)][SerializeField] private float airborneSpreadPenalty = 2.2f;
        [Range(0f, 1f)][SerializeField] private float firstShotAccuracy = 0.7f;

        public float BaseSpread => baseSpread;
        public float AimSpread => aimSpread;
        public float MaxSpread => maxSpread;
        public float SpreadIncreasePerShot => spreadIncreasePerShot;
        public float SpreadRecoverSpeed => spreadRecoverSpeed;
        public float MovingSpreadPenalty => movingSpreadPenalty;
        public float AirborneSpreadPenalty => airborneSpreadPenalty;
        public float FirstShotAccuracy => firstShotAccuracy;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (maxSpread < baseSpread)
            {
                _context.AddError(ConfigId, "MaxSpread 不能小于 BaseSpread。");
            }

            if (maxSpread < aimSpread)
            {
                _context.AddError(ConfigId, "MaxSpread 不能小于 AimSpread。");
            }
        }
    }
}
