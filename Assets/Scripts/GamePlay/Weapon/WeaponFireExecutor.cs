using Game.Definition.Weapon;
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

        /// <summary>
        /// 解析当前执行器依赖的运行时引用。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器下同步自动补齐引用。
        /// </summary>
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
        public bool Execute(WeaponFramePlan _plan, float _currentTime)
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
            ResolveHitAndDamage(_currentTime);
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
        private void ResolveHitAndDamage(float _currentTime)
        {
            if (hitScanService == null || weaponRuntime == null)
            {
                return;
            }

            // 先完成命中查询，并拿到最终射线与命中上下文。
            bool hadHit = hitScanService.TryHit(weaponRuntime, out HitScanHitContext hitContext, out Ray shotRay, out _);
            Vector3 resolvedImpactPoint = hadHit
                ? hitContext.HitPoint
                : shotRay.origin + shotRay.direction * weaponRuntime.Range;
            Vector3 resolvedImpactNormal = hadHit
                ? hitContext.HitNormal
                : -shotRay.direction;

            // 先把“已提交的一枪”广播给表现层，视觉链不参与 Combat 裁决。
            PublishWeaponFiredEvent(shotRay, hadHit, hitContext, resolvedImpactPoint, resolvedImpactNormal);

            if (hadHit == false || hitContext.DamageReceiver == null)
            {
                // 命中世界遮挡或完全未命中时，到此为止，不进入伤害链。
                return;
            }

            // 命中合法受击目标后，统一把伤害请求交给 Combat 域裁决。
            CombatDamageRequestContext damageRequestContext = new(
                gameObject,
                weaponRuntime,
                CombatDamageKind.Physical,
                weaponRuntime.Damage,
                characterStat.CritChance,
                characterStat.CritDamageMultiplier,
                weaponRuntime.HeadShotDamageMultiplier,
                weaponRuntime.WeakPointDamageMultiplier,
                shotRay.origin,
                shotRay.direction,
                hitContext,
                _currentTime);

            DamageResolver.ResolveAndApply(damageRequestContext);
        }

        /// <summary>
        /// 发布已提交的开火事件。
        /// 该事件只代表真正完成扣弹与命中查询的一枪。
        /// </summary>
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

            // 开火事件只描述已成立事实，同时把世界命中和受击目标命中区分清楚，供表现层直接消费。
            GameEventBus.Instance.Publish(new WeaponFiredEvent(
                gameObject,
                weaponRuntime.WeaponDefinitionConfigId,
                weaponRuntime.CurrentMagazineAmmo,
                _shotRay.origin,
                _shotRay.direction,
                weaponRuntime.Range,
                _hadHit,
                _hitContext.DamageReceiver != null,
                _hitContext.HitPartType,
                _resolvedImpactPoint,
                _resolvedImpactNormal,
                weaponRuntime.WeaponRecoilConfig != null ? weaponRuntime.WeaponRecoilConfig.CameraKickPitch : 0f,
                weaponRuntime.WeaponRecoilConfig != null ? weaponRuntime.WeaponRecoilConfig.CameraKickYaw : 0f,
                weaponRuntime.WeaponRecoilConfig != null ? weaponRuntime.WeaponRecoilConfig.CrosshairKick : 0f));
        }

        /// <summary>
        /// 发布已提交的空仓触发事件。
        /// 当前规则下，它只对应 FirePressed 的那一次触发，不对持续按住重复生效。
        /// </summary>
        private void PublishWeaponDryFireEvent()
        {
            if (GameEventBus.Instance == null || weaponRuntime == null)
            {
                return;
            }

            // 空仓反馈属于事实事件，但它不代表真正开火成功。
            GameEventBus.Instance.Publish(new WeaponDryFireEvent(gameObject, weaponRuntime.WeaponDefinitionConfigId));
        }

        /// <summary>
        /// 解析执行器需要的运行时组件引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (characterStat == null)
            {
                // 角色面板属性由角色根对象提供，武器执行器只读它的结果。
                characterStat = GetComponentInParent<CharacterStat>();
            }

            if (weaponRuntime == null)
            {
                // 武器长期状态和配置引用都从 WeaponRuntime 统一读取。
                weaponRuntime = GetComponent<WeaponRuntime>();
            }

            if (weaponAmmoComponent == null)
            {
                // 弹药组件仍然是当前版本的弹匣与备弹事实持有者。
                weaponAmmoComponent = GetComponent<WeaponAmmoComponent>();
            }

            if (hitScanService == null)
            {
                // Hitscan 服务负责命中查询，不在执行器里自行拼射线逻辑。
                hitScanService = GetComponent<HitScanService>();
            }
        }

        #endregion
    }
}
