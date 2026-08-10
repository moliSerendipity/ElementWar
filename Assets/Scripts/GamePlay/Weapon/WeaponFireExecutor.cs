using Game.Definition.Weapon;
using Game.Definition.Combat;
using Game.Foundation.Events;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Weapon.Events;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    /// <summary>
    /// 开火执行器。
    /// 只消费 WeaponFramePlan 与 WeaponRuntime 已解析好的运行时数据，
    /// 不在这里直接播放表现层音频或特效。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponFireExecutor : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [SerializeField] private CharacterStat characterStat;
        [SerializeField] private WeaponRuntime weaponRuntime;
        [SerializeField] private WeaponAmmoComponent weaponAmmoComponent;
        [SerializeField] private HitScanService hitScanService;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        #endregion

        #region Public API

        /// <summary>
        /// 执行已经裁决完成的武器计划。
        /// 返回值表示本帧是否真正完成了一次扣弹与逻辑开火。
        /// </summary>
        public bool Execute(WeaponFramePlan _plan, CharacterFacts _characterFacts, in CharacterFramePlan _characterPlan, float _currentTime)
        {
            if (_plan.DryFireTriggered)
            {
                // 空仓触发属于一条已提交事实，但它不是“真正开火成立”。
                PublishWeaponDryFireEvent();
                return false;
            }

            if (_plan.FireTriggered == false || weaponRuntime == null || weaponAmmoComponent == null)
            {
                // 裁决未通过或核心依赖缺失时，本帧不提交任何开火事实。
                return false;
            }

            // 真正开火成立后，先扣弹，再把本次射击写回武器长期状态。
            weaponAmmoComponent.TryConsumeMagazineAmmo(1);
            weaponRuntime.CommitFire(_currentTime, weaponRuntime.FireInterval);

            // 真实后坐力不做自动恢复；当前帧只提交一次单发增量，后续由 CharacterRoot 按固定顺序消费。
            CommitSingleShotRecoil();

            // 扣弹与运行时事实提交完成后，立即进入命中查询与 Combat 主链。
            ResolveHitAndDamage(_characterFacts, _characterPlan, _currentTime);
            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 根据当前武器后坐力配置提交一次单发真实后坐力增量。
        /// 这一步只产生本次开火的 pitch / yaw 增量，不保存自动恢复状态。
        /// </summary>
        private void CommitSingleShotRecoil()
        {
            if (weaponRuntime == null || weaponRuntime.WeaponRecoilConfig == null)
            {
                return;
            }

            WeaponRecoilConfig recoilConfig = weaponRuntime.WeaponRecoilConfig;

            // 每发真实后坐力都从配置区间采样一次，直接提交给 Character 域按顺序消费。
            float recoilPitch = Random.Range(
                Mathf.Min(recoilConfig.RecoilPitchPerShotMin, recoilConfig.RecoilPitchPerShotMax),
                Mathf.Max(recoilConfig.RecoilPitchPerShotMin, recoilConfig.RecoilPitchPerShotMax));
            float recoilYaw = Random.Range(
                Mathf.Min(recoilConfig.RecoilYawPerShotMin, recoilConfig.RecoilYawPerShotMax),
                Mathf.Max(recoilConfig.RecoilYawPerShotMin, recoilConfig.RecoilYawPerShotMax));

            weaponRuntime.SetPendingRecoil(recoilPitch, recoilYaw);
        }

        /// <summary>
        /// 成功开火后，立即走逻辑射线与 Combat 主链。
        /// 当前阶段不再让 Weapon 域自己直写目标生命，而是统一把请求交给 DamageResolver。
        /// </summary>
        private void ResolveHitAndDamage(CharacterFacts _characterFacts, in CharacterFramePlan _characterPlan, float _currentTime)
        {
            if (hitScanService == null || weaponRuntime == null)
            {
                return;
            }

            float spreadAngle = weaponRuntime.GetCurrentShotSpreadAngle(_characterPlan, _characterFacts);

            // 先完成命中查询，并拿到最终射线与命中上下文。
            bool hadHit = hitScanService.TryHit(weaponRuntime, spreadAngle, out HitScanHitContext hitContext, out Ray shotRay, out _);
            Vector3 resolvedImpactPoint = hadHit
                ? hitContext.HitPoint
                : shotRay.origin + shotRay.direction * weaponRuntime.Range;
            Vector3 resolvedImpactNormal = hadHit
                ? hitContext.HitNormal
                : -shotRay.direction;

            // 先把“已提交的一枪”广播给表现层，视觉链不参与 Combat 裁决。
            PublishWeaponFiredEvent(shotRay, hadHit, hitContext, resolvedImpactPoint, resolvedImpactNormal);

            if (hadHit == false || hitContext.HealthComponent == null)
            {
                return;
            }

            GameObject instigator = characterStat != null
                ? characterStat.gameObject
                : transform.root.gameObject;

            DamageRequest damageRequest = new(
                instigator,
                weaponRuntime,
                hitContext.HealthComponent,
                ElementType.None,
                DamageDeliveryType.Direct,
                weaponRuntime.Damage,
                hitContext.HitPartType,
                weaponRuntime.HeadShotDamageMultiplier,
                weaponRuntime.WeakPointDamageMultiplier,
                shotRay.origin,
                shotRay.direction,
                hitContext.HitPoint,
                hitContext.HitNormal,
                _currentTime);

            DamageResolver.ResolveAndApply(damageRequest);
        }

        private void PublishWeaponFiredEvent(
            in Ray _shotRay,
            bool _hadHit,
            in HitScanHitContext _hitContext,
            Vector3 _resolvedImpactPoint,
            Vector3 _resolvedImpactNormal)
        {
            if (GameEventBus.Instance == null || weaponRuntime == null)
            {
                return;
            }

            GameEventBus.Instance.Publish(new WeaponFiredEvent(
                gameObject,
                weaponRuntime.WeaponDefinitionConfigId,
                weaponRuntime.CurrentMagazineAmmo,
                _shotRay.origin,
                _shotRay.direction,
                weaponRuntime.Range,
                _hadHit,
                _hitContext.HealthComponent != null,
                _hitContext.HitPartType,
                _resolvedImpactPoint,
                _resolvedImpactNormal,
                weaponRuntime.WeaponRecoilConfig != null ? weaponRuntime.WeaponRecoilConfig.CameraKickPitch : 0f,
                weaponRuntime.WeaponRecoilConfig != null ? weaponRuntime.WeaponRecoilConfig.CameraKickYaw : 0f,
                weaponRuntime.WeaponRecoilConfig != null ? weaponRuntime.WeaponRecoilConfig.CrosshairKick : 0f));
        }

        private void PublishWeaponDryFireEvent()
        {
            if (GameEventBus.Instance == null || weaponRuntime == null)
            {
                return;
            }

            GameEventBus.Instance.Publish(new WeaponDryFireEvent(gameObject, weaponRuntime.WeaponDefinitionConfigId));
        }

        private void ResolveReferences()
        {
            if (characterStat == null)
            {
                characterStat = GetComponentInParent<CharacterStat>();
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponent<WeaponRuntime>();
            }

            if (weaponAmmoComponent == null)
            {
                weaponAmmoComponent = GetComponent<WeaponAmmoComponent>();
            }

            if (hitScanService == null)
            {
                hitScanService = GetComponent<HitScanService>();
            }
        }

        #endregion
    }
}
