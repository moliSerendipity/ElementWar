using Game.Definition.ConfigSystem.Core;
using Game.Definition.HUD;
using UnityEngine;

namespace Game.Definition.Presentation
{
    /// <summary>
    /// 武器表现资源配置。
    /// 当前版本只保留对象池 key、寿命、音频与准星表现配置。
    /// 预制体唯一来源交给对象池服务维护。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponPresentationConfig", menuName = "Game/Configs/Presentation/Weapon Presentation Config")]
    public sealed class WeaponPresentationConfig : ConfigBase
    {
        #region Inspector

        [Header("Muzzle Flash")]
        [SerializeField] private string muzzleFlashPoolKey;
        [SerializeField, Min(0.01f)] private float muzzleFlashLifeTime = 0.4f;

        [Header("Bullet Projectile")]
        [SerializeField] private string bulletProjectilePoolKey;
        [SerializeField, Min(0.01f)] private float bulletProjectileLifeTime = 0.3f;

        [Header("Impact")]
        [SerializeField] private string worldImpactPoolKey;
        [SerializeField, Min(0.01f)] private float worldImpactLifeTime = 1.5f;
        [SerializeField] private string actorImpactPoolKey;
        [SerializeField, Min(0.01f)] private float actorImpactLifeTime = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioEventConfig fireAudio;
        [SerializeField] private AudioEventConfig dryFireAudio;
        [SerializeField] private AudioEventConfig worldHitAudio;
        [SerializeField] private AudioEventConfig actorHitAudio;
        [SerializeField] private AudioEventConfig weakPointHitAudio;
        [SerializeField] private AudioEventConfig criticalHitAudio;
        [SerializeField] private AudioEventConfig killHitAudio;
        [SerializeField] private AudioEventConfig reloadAudio;
        [SerializeField] private AudioEventConfig equipAudio;

        [Header("UI")]
        [SerializeField] private CrosshairConfig crosshairConfig;
        [SerializeField] private Sprite uiIcon;

        #endregion

        #region Properties

        public string MuzzleFlashPoolKey => muzzleFlashPoolKey;
        public float MuzzleFlashLifeTime => muzzleFlashLifeTime;
        public string BulletProjectilePoolKey => bulletProjectilePoolKey;
        public float BulletProjectileLifeTime => bulletProjectileLifeTime;
        public string WorldImpactPoolKey => worldImpactPoolKey;
        public float WorldImpactLifeTime => worldImpactLifeTime;
        public string ActorImpactPoolKey => actorImpactPoolKey;
        public float ActorImpactLifeTime => actorImpactLifeTime;
        public AudioEventConfig FireAudio => fireAudio;
        public AudioEventConfig DryFireAudio => dryFireAudio;
        public AudioEventConfig WorldHitAudio => worldHitAudio;
        public AudioEventConfig ActorHitAudio => actorHitAudio;
        public AudioEventConfig WeakPointHitAudio => weakPointHitAudio;
        public AudioEventConfig CriticalHitAudio => criticalHitAudio;
        public AudioEventConfig KillHitAudio => killHitAudio;
        public AudioEventConfig ReloadAudio => reloadAudio;
        public AudioEventConfig EquipAudio => equipAudio;
        public CrosshairConfig CrosshairConfig => crosshairConfig;
        public Sprite UiIcon => uiIcon;

        #endregion

        #region Validation

        /// <summary>
        /// 校验武器表现配置是否合法。
        /// </summary>
        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            // 枪口火花寿命必须大于 0，避免借出后立刻回池。
            if (muzzleFlashLifeTime <= 0f)
            {
                _context.AddError(ConfigId, "muzzleFlashLifeTime 必须大于 0。");
            }

            // 视觉拖尾寿命必须大于 0，避免飞行协程没有有效时间窗口。
            if (bulletProjectileLifeTime <= 0f)
            {
                _context.AddError(ConfigId, "bulletProjectileLifeTime 必须大于 0。");
            }

            // 世界命中特效寿命必须大于 0，避免火花立即消失。
            if (worldImpactLifeTime <= 0f)
            {
                _context.AddError(ConfigId, "worldImpactLifeTime 必须大于 0。");
            }

            // 目标命中特效寿命必须大于 0，避免受击反馈不可见。
            if (actorImpactLifeTime <= 0f)
            {
                _context.AddError(ConfigId, "actorImpactLifeTime 必须大于 0。");
            }
        }

        #endregion
    }
}
