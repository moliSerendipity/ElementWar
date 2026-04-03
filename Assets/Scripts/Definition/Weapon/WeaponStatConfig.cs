using UnityEngine;
using UnityEngine.Rendering;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Weapon
{
    /// <summary>
    /// 武器数值表
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponStatConfig", menuName = "Game/Configs/Weapon/Weapon Stat Config")]
    public sealed class WeaponStatConfig : ConfigBase
    {
        [Header("Damage")]
        [Min(0f)][SerializeField] private float damage = 20f;
        [Range(1f, 10f)][SerializeField] private float headShotDamageMultiplier = 2f;
        [Range(1f, 10f)][SerializeField] private float weakPointDamageMultiplier = 1.5f;

        [Header("Fire")]
        [Min(0.01f)][SerializeField] private float fireInterval = 0.1f;
        [Min(1)][SerializeField] private int burstCount = 1;
        [Min(0.01f)][SerializeField] private float burstInterval = 0.08f;
        [Min(0)][SerializeField] private int magazineSize = 30;
        [Min(0)][SerializeField] private int reserveAmmoCapacity = 180;

        [Header("Hit Scan")]
        [Min(0.01f)][SerializeField] private float range = 1000f;
        [SerializeField] private LayerMask hitLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction hitTriggerInteraction = QueryTriggerInteraction.Ignore;
        [Min(0)][SerializeField] private int penetrationCount;
        [Min(0f)][SerializeField] private float penetrationDamageDecay;

        public float Damage => damage;
        public float HeadShotDamageMultiplier => headShotDamageMultiplier;
        public float WeakPointDamageMultiplier => weakPointDamageMultiplier;
        public float FireInterval => fireInterval;
        public int BurstCount => burstCount;
        public float BurstInterval => burstInterval;
        public int MagazineSize => magazineSize;
        public int ReserveAmmoCapacity => reserveAmmoCapacity;
        public float Range => range;
        public LayerMask HitLayerMask => hitLayerMask;
        public QueryTriggerInteraction HitTriggerInteraction => hitTriggerInteraction;
        public int PenetrationCount => penetrationCount;
        public float PenetrationDamageDecay => penetrationDamageDecay;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (burstCount > 1 && burstInterval <= 0f)
            {
                _context.AddError(ConfigId, "BurstInterval 必须大于 0 当 BurstCount 大于 1。");
            }

            if (range <= 0f)
            {
                _context.AddError(ConfigId, "Range 必须大于 0。");
            }

            if (penetrationCount == 0 && penetrationDamageDecay > 0f)
            {
                _context.AddWarning(ConfigId, "PenetrationCount 为 0 时，PenetrationDamageDecay 不会生效。");
            }
        }
    }
}
