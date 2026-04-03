using System.Collections;
using Game.Definition.Presentation;
using Game.Foundation.Events;
using Game.Foundation.Pooling;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Weapon;
using Game.Gameplay.Weapon.Events;
using UnityEngine;

namespace Game.Presentation.VFX
{
    /// <summary>
    /// 武器开火表现桥。
    /// 只在初始化阶段读取 WeaponPresentationConfig，运行时只消费已缓存资源与已提交事件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponPresentationBridge : MonoBehaviour
    {
        #region Inspector

        [Header("Scene References")]
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private AudioSource fireAudioSource;
        [SerializeField] private AudioSource hitAudioSource;

        [Header("Bullet Projectile")]
        [SerializeField, Min(0.01f)] private float bulletProjectileSpeed = 120f;
        [SerializeField] private bool orientBulletProjectileToVelocity = true;
        [SerializeField] private bool disableBulletProjectilePhysicsComponents = true;

        #endregion

        #region Runtime Fields

        private WeaponRuntime weaponRuntime;
        private WeaponPresentationConfig weaponPresentationConfig;

        private string muzzleFlashPoolKey;
        private float muzzleFlashLifeTime;

        private string bulletProjectilePoolKey;
        private float bulletProjectileLifeTime;

        private string worldImpactPoolKey;
        private float worldImpactLifeTime;

        private string actorImpactPoolKey;
        private float actorImpactLifeTime;

        private AudioClip fireClip;
        private float fireVolume = 1f;
        private AudioClip dryFireClip;
        private float dryFireVolume = 1f;

        private AudioEventConfig worldHitAudio;
        private AudioEventConfig actorHitAudio;
        private AudioEventConfig weakPointHitAudio;
        private AudioEventConfig criticalHitAudio;
        private AudioEventConfig killHitAudio;

        private bool isSubscribed;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 解析当前桥接层依赖，并缓存表现配置。
        /// </summary>
        private void Awake()
        {
            // 只在初始化阶段解析依赖，运行时不再重复找组件或查配置。
            weaponRuntime = GetComponent<WeaponRuntime>();

            if (fireAudioSource == null)
            {
                // 开火与空仓音频默认共用当前武器上的主 AudioSource。
                fireAudioSource = GetComponent<AudioSource>();
            }

            if (hitAudioSource == null)
            {
                // 命中音频默认和开火音频共用一个出口，只有单独拖了命中 AudioSource 才会分离。
                hitAudioSource = fireAudioSource;
            }

            InitializeFromConfig();
        }

        /// <summary>
        /// Presenter 启用时尝试订阅已提交事实事件。
        /// </summary>
        private void OnEnable()
        {
            TrySubscribe();
        }

        /// <summary>
        /// 当事件总线晚于当前 Presenter 装配时，延迟补订阅。
        /// </summary>
        private void Update()
        {
            if (isSubscribed == false)
            {
                // 总线可能晚于 Presenter 启用，这里只负责补订阅，不做任何业务逻辑。
                TrySubscribe();
            }
        }

        /// <summary>
        /// Presenter 关闭时取消事件订阅。
        /// </summary>
        private void OnDisable()
        {
            if (isSubscribed == false || GameEventBus.Instance == null)
            {
                isSubscribed = false;
                return;
            }

            // 取消全部事实事件订阅，避免重复装配导致多播。
            GameEventBus.Instance.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
            GameEventBus.Instance.Unsubscribe<WeaponDryFireEvent>(OnWeaponDryFire);
            GameEventBus.Instance.Unsubscribe<HitConfirmedEvent>(OnHitConfirmed);
            GameEventBus.Instance.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
            isSubscribed = false;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 订阅当前桥接层真正需要消费的事实事件。
        /// </summary>
        private void TrySubscribe()
        {
            if (isSubscribed || GameEventBus.Instance == null)
            {
                return;
            }

            // 开火、空仓、命中确认和伤害结果分别驱动不同表现层，不在桥接层重做裁决。
            GameEventBus.Instance.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            GameEventBus.Instance.Subscribe<WeaponDryFireEvent>(OnWeaponDryFire);
            GameEventBus.Instance.Subscribe<HitConfirmedEvent>(OnHitConfirmed);
            GameEventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
            isSubscribed = true;
        }

        /// <summary>
        /// 从 WeaponPresentationConfig 初始化当前桥接层要使用的资源引用。
        /// </summary>
        private void InitializeFromConfig()
        {
            if (weaponRuntime == null)
            {
                return;
            }

            weaponPresentationConfig = weaponRuntime.WeaponPresentationConfig;
            if (weaponPresentationConfig == null)
            {
                return;
            }

            // 所有表现资源都在初始化阶段一次性缓存，运行时只用字段，不再查配置。
            muzzleFlashPoolKey = weaponPresentationConfig.MuzzleFlashPoolKey;
            muzzleFlashLifeTime = weaponPresentationConfig.MuzzleFlashLifeTime;

            bulletProjectilePoolKey = weaponPresentationConfig.BulletProjectilePoolKey;
            bulletProjectileLifeTime = weaponPresentationConfig.BulletProjectileLifeTime;

            worldImpactPoolKey = weaponPresentationConfig.WorldImpactPoolKey;
            worldImpactLifeTime = weaponPresentationConfig.WorldImpactLifeTime;

            actorImpactPoolKey = weaponPresentationConfig.ActorImpactPoolKey;
            actorImpactLifeTime = weaponPresentationConfig.ActorImpactLifeTime;

            worldHitAudio = weaponPresentationConfig.WorldHitAudio;
            actorHitAudio = weaponPresentationConfig.ActorHitAudio;
            weakPointHitAudio = weaponPresentationConfig.WeakPointHitAudio;
            criticalHitAudio = weaponPresentationConfig.CriticalHitAudio;
            killHitAudio = weaponPresentationConfig.KillHitAudio;

            ApplyFireAudioConfig(weaponPresentationConfig.FireAudio);
            ApplyDryFireAudioConfig(weaponPresentationConfig.DryFireAudio);
        }

        /// <summary>
        /// 解析开火音频配置。
        /// </summary>
        private void ApplyFireAudioConfig(AudioEventConfig audioConfig)
        {
            if (audioConfig == null)
            {
                return;
            }

            // 开火音频只缓存播放所需数据，运行时不再解释配置。
            fireClip = audioConfig.AudioClip;
            fireVolume = audioConfig.Volume;

            if (fireAudioSource != null)
            {
                // AudioSource 的基础空间参数只在初始化阶段同步一次。
                fireAudioSource.spatialBlend = audioConfig.SpatialBlend;
                fireAudioSource.loop = audioConfig.Loop;
            }
        }

        /// <summary>
        /// 解析空仓音频配置。
        /// </summary>
        private void ApplyDryFireAudioConfig(AudioEventConfig audioConfig)
        {
            if (audioConfig == null)
            {
                return;
            }

            // 空仓音频和开火音频共用同一个音频出口，只缓存真正的播放资源。
            dryFireClip = audioConfig.AudioClip;
            dryFireVolume = audioConfig.Volume;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 处理真正开火成立后的表现播放。
        /// </summary>
        private void OnWeaponFired(WeaponFiredEvent eventData)
        {
            if (eventData.WeaponObject != gameObject)
            {
                return;
            }

            // 枪口挂点优先于逻辑射线起点；只有挂点缺失时才回退到逻辑开火原点。
            Vector3 muzzlePosition = muzzleTransform != null ? muzzleTransform.position : eventData.ShotOrigin;
            Quaternion muzzleRotation = muzzleTransform != null
                ? muzzleTransform.rotation
                : Quaternion.LookRotation(eventData.ShotDirection, Vector3.up);

            // 枪口火花和开火音效只绑定“开火成立”这件事，不关心后续是否命中受击目标。
            SpawnTemporaryVisual(muzzleFlashPoolKey, muzzlePosition, muzzleRotation, muzzleFlashLifeTime);
            PlayConfiguredAudio(fireAudioSource, fireClip, fireVolume);

            Vector3 bulletProjectileTargetPoint = eventData.HadHit
                ? eventData.ResolvedImpactPoint
                : eventData.ShotOrigin + eventData.ShotDirection * eventData.ShotDistance;

            // 视觉拖尾始终从枪口出发，但它只做表现，不参与命中与伤害判定。
            SpawnBulletProjectile(muzzlePosition, bulletProjectileTargetPoint);

            if (eventData.HadHit == false)
            {
                return;
            }

            if (eventData.HitDamageableTarget)
            {
                // 命中受击目标时，不在这里生成世界火花，避免肉体命中和墙面火花重叠。
                return;
            }

            Quaternion impactRotation = Quaternion.LookRotation(eventData.ResolvedImpactNormal, Vector3.up);
            SpawnTemporaryVisual(worldImpactPoolKey, eventData.ResolvedImpactPoint, impactRotation, worldImpactLifeTime);
            PlayConfiguredAudio(hitAudioSource, worldHitAudio);
        }

        /// <summary>
        /// 处理空仓触发后的表现播放。
        /// </summary>
        private void OnWeaponDryFire(WeaponDryFireEvent eventData)
        {
            if (eventData.WeaponObject != gameObject)
            {
                return;
            }

            // 空仓反馈只在 Weapon 域已提交 dry fire 事实后播放一次，不对持续按住重复生效。
            PlayConfiguredAudio(fireAudioSource, dryFireClip, dryFireVolume);
        }

        /// <summary>
        /// 处理命中合法受击目标后的受击特效与命中音频。
        /// </summary>
        private void OnHitConfirmed(HitConfirmedEvent eventData)
        {
            if (eventData.Attacker != gameObject || string.IsNullOrWhiteSpace(actorImpactPoolKey))
            {
                return;
            }

            // 命中合法受击目标后，只在这里生成目标受击特效，不重复生成世界火花。
            Quaternion impactRotation = Quaternion.LookRotation(eventData.HitNormal, Vector3.up);
            SpawnTemporaryVisual(actorImpactPoolKey, eventData.HitPoint, impactRotation, actorImpactLifeTime);

            if (eventData.HitPartType == CombatHitPartType.WeakPoint)
            {
                // 弱点命中优先播放弱点音效，没有配置时再退回普通目标命中音效。
                PlayConfiguredAudio(hitAudioSource, weakPointHitAudio ?? actorHitAudio);
                return;
            }

            // 普通部位命中只播放基础目标命中音效。
            PlayConfiguredAudio(hitAudioSource, actorHitAudio);
        }

        /// <summary>
        /// 处理伤害结果成立后的更高层命中音频反馈。
        /// </summary>
        private void OnDamageApplied(DamageAppliedEvent eventData)
        {
            CombatDamageResult damageResult = eventData.DamageResult;
            if (damageResult.Attacker != gameObject)
            {
                return;
            }

            if (damageResult.WasKilled)
            {
                // 击杀反馈优先级最高，命中音频在这里提升一层。
                PlayConfiguredAudio(hitAudioSource, killHitAudio);
                return;
            }

            if (damageResult.IsCritical)
            {
                // 暴击成立后，再叠一层更高优先级的命中音效。
                PlayConfiguredAudio(hitAudioSource, criticalHitAudio);
            }
        }

        #endregion

        #region Visual Playback

        /// <summary>
        /// 生成并驱动视觉拖尾。
        /// </summary>
        private void SpawnBulletProjectile(Vector3 startPoint, Vector3 endPoint)
        {
            if (string.IsNullOrWhiteSpace(bulletProjectilePoolKey))
            {
                return;
            }

            Vector3 shotVector = endPoint - startPoint;
            if (shotVector.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // 视觉拖尾的朝向默认跟随当前飞行速度方向，而不是依赖 prefab 初始朝向。
            Vector3 shotDirection = shotVector.normalized;
            Quaternion spawnRotation = orientBulletProjectileToVelocity
                ? Quaternion.LookRotation(shotDirection, Vector3.up)
                : Quaternion.identity;

            SpawnedVisual bulletProjectile = SpawnVisual(bulletProjectilePoolKey, startPoint, spawnRotation);
            if (bulletProjectile.GameObject == null)
            {
                return;
            }

            if (disableBulletProjectilePhysicsComponents)
            {
                // 逻辑命中已经完成，这里要关闭视觉弹体自带的碰撞体，避免再次触发残留脚本。
                DisablePhysicsComponents(bulletProjectile.GameObject);
            }

            StartCoroutine(PlayBulletProjectileRoutine(bulletProjectile, startPoint, endPoint));
        }

        /// <summary>
        /// 推进视觉拖尾从枪口飞向最终表现落点。
        /// </summary>
        private IEnumerator PlayBulletProjectileRoutine(SpawnedVisual spawnedVisual, Vector3 startPoint, Vector3 endPoint)
        {
            float totalDistance = Vector3.Distance(startPoint, endPoint);
            float durationBySpeed = totalDistance / Mathf.Max(0.01f, bulletProjectileSpeed);
            float travelDuration = Mathf.Min(bulletProjectileLifeTime, durationBySpeed);
            travelDuration = Mathf.Max(0.01f, travelDuration);

            float elapsedTime = 0f;
            Transform bulletProjectileTransform = spawnedVisual.GameObject.transform;
            Vector3 previousPosition = startPoint;

            while (elapsedTime < travelDuration && bulletProjectileTransform != null)
            {
                // 每帧按线性插值推进拖尾位置，保持表现与逻辑 impact 点对齐。
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / travelDuration);
                Vector3 currentPosition = Vector3.Lerp(startPoint, endPoint, normalizedTime);
                bulletProjectileTransform.position = currentPosition;

                if (orientBulletProjectileToVelocity)
                {
                    // 跟随当前帧位移方向更新朝向，避免拖尾长时间保持初始旋转。
                    Vector3 frameDirection = currentPosition - previousPosition;
                    if (frameDirection.sqrMagnitude > 0.000001f)
                    {
                        bulletProjectileTransform.rotation = Quaternion.LookRotation(frameDirection.normalized, Vector3.up);
                    }
                }

                previousPosition = currentPosition;
                yield return null;
            }

            if (bulletProjectileTransform != null)
            {
                // 协程结束时把拖尾钉到最终命中点，避免末帧因为插值误差停在半路。
                bulletProjectileTransform.position = endPoint;
            }

            DespawnOrDestroy(spawnedVisual);
        }

        /// <summary>
        /// 播放一段配置化音频。
        /// </summary>
        private void PlayConfiguredAudio(AudioSource audioSource, AudioEventConfig audioConfig)
        {
            if (audioConfig == null)
            {
                return;
            }

            // AudioEventConfig 已经完成资源映射，这里只消费最终结果。
            PlayConfiguredAudio(audioSource, audioConfig.AudioClip, audioConfig.Volume, audioConfig.SpatialBlend, audioConfig.Loop);
        }

        /// <summary>
        /// 以最小参数播放一段 one-shot 音频。
        /// </summary>
        private void PlayConfiguredAudio(AudioSource audioSource, AudioClip audioClip, float volume)
        {
            PlayConfiguredAudio(audioSource, audioClip, volume, null, null);
        }

        /// <summary>
        /// 以给定参数播放音频，并按需覆盖 AudioSource 的表现参数。
        /// </summary>
        private void PlayConfiguredAudio(AudioSource audioSource, AudioClip audioClip, float volume, float? spatialBlend, bool? loop)
        {
            if (audioSource == null || audioClip == null)
            {
                return;
            }

            if (spatialBlend.HasValue)
            {
                // 命中或开火音频只有在真正播放前才覆盖 AudioSource 参数，避免常驻改写别的状态。
                audioSource.spatialBlend = spatialBlend.Value;
            }

            if (loop.HasValue)
            {
                // Loop 参数只在播放当下按配置覆盖，不让桥接层长期持有额外状态。
                audioSource.loop = loop.Value;
            }

            // 当前阶段命中、开火与空仓音频统一按 one-shot 播放，不在桥接层维护复杂实例管理。
            audioSource.PlayOneShot(audioClip, volume);
        }

        /// <summary>
        /// 生成一个临时表现对象，并在寿命结束后自动回池或销毁。
        /// </summary>
        private void SpawnTemporaryVisual(string poolKey, Vector3 position, Quaternion rotation, float lifeTime)
        {
            SpawnedVisual visual = SpawnVisual(poolKey, position, rotation);
            if (visual.GameObject == null)
            {
                return;
            }

            // 临时表现对象一律按寿命窗口自动回收或销毁，桥接层不持有长期引用。
            StartCoroutine(ReleaseVisualAfterDelay(visual, lifeTime));
        }

        /// <summary>
        /// 优先从对象池借出表现对象，失败时再退化为实例化。
        /// </summary>
        private SpawnedVisual SpawnVisual(string poolKey, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(poolKey) || GameObjectPoolService.Instance == null || GameObjectPoolService.Instance.HasPool(poolKey) == false)
            {
                // 当前正式方案下，武器表现对象的唯一来源就是对象池；没有合法池入口时直接放弃。
                return default;
            }

            PoolHandle handle = GameObjectPoolService.Instance.Spawn(poolKey, position, rotation, null);
            return handle.IsValid ? new SpawnedVisual(handle.GameObject, true) : default;
        }

        /// <summary>
        /// 在寿命窗口结束后释放一个临时表现对象。
        /// </summary>
        private IEnumerator ReleaseVisualAfterDelay(SpawnedVisual spawnedVisual, float lifeTime)
        {
            yield return new WaitForSeconds(lifeTime);
            DespawnOrDestroy(spawnedVisual);
        }

        /// <summary>
        /// 回收或销毁一个由桥接层生成的临时表现对象。
        /// </summary>
        private void DespawnOrDestroy(SpawnedVisual spawnedVisual)
        {
            if (spawnedVisual.GameObject == null)
            {
                return;
            }

            if (spawnedVisual.UsesPool && GameObjectPoolService.Instance != null)
            {
                // 当前武器表现对象统一来自对象池，生命周期结束后直接回池。
                GameObjectPoolService.Instance.Despawn(spawnedVisual.GameObject);
            }
        }

        /// <summary>
        /// 关闭视觉弹体自身的碰撞与刚体模拟。
        /// </summary>
        private static void DisablePhysicsComponents(GameObject visualObject)
        {
            Collider[] colliders = visualObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                // 关闭视觉弹体全部碰撞体，防止 prefab 自己再次触发命中或残留逻辑。
                colliders[i].enabled = false;
            }

            Rigidbody[] rigidbodies = visualObject.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                // 强制设为运动学刚体并关闭碰撞检测，确保拖尾完全由桥接层驱动。
                rigidbodies[i].isKinematic = true;
                rigidbodies[i].detectCollisions = false;
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// 记录一次表现对象借出结果。
        /// </summary>
        private readonly struct SpawnedVisual
        {
            /// <summary>
            /// 构造一个表现对象借出结果。
            /// </summary>
            public SpawnedVisual(GameObject gameObject, bool usesPool)
            {
                GameObject = gameObject;
                UsesPool = usesPool;
            }

            /// <summary>
            /// 借出的表现对象实例。
            /// </summary>
            public GameObject GameObject { get; }

            /// <summary>
            /// 当前对象是否来自对象池。
            /// </summary>
            public bool UsesPool { get; }
        }

        #endregion
    }
}
