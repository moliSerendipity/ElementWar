using System;
using Game.Gameplay.Camera;
using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Combat 域唯一 Hitscan 查询入口
    ///
    /// 说明：
    /// 1. 只负责命中查询，不负责伤害裁决
    /// 2. 热路径只读取 WeaponRuntime 已解析好的射程与查询规则，不再去读 HitScanConfig
    /// 3. 命中层级、Trigger 策略等规则已并回 WeaponStatConfig，并在 WeaponRuntime 初始化时缓存
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitScanService : MonoBehaviour
    {
        [Serializable]
        private struct ShotDebugSnapshot
        {
            public bool hadValidAimPoint;
            public bool hadObstruction;
            public bool hadHit;
            public Vector3 cameraRayOrigin;
            public Vector3 cameraRayDirection;
            public Vector3 aimPoint;
            public Vector3 shotOrigin;
            public Vector3 shotDirection;
            public Vector3 obstructionPoint;
            public Vector3 finalHitPoint;
            public float obstructionDistance;
            public float finalHitDistance;
            public string obstructionColliderName;
            public string finalHitColliderName;
        }

        [Header("References")]
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private MonoBehaviour cameraAimPointProviderBehaviour;

        [Header("Shot Rules")]
        [SerializeField] private bool enableNearMuzzleObstruction = true;
        [SerializeField][Min(0f)] private float muzzleObstructionCheckDistance = 1.25f;
        [SerializeField][Min(0f)] private float muzzleObstructionSphereRadius = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool enableShotDebugGizmos = true;
        [SerializeField] private ShotDebugSnapshot lastShotDebugSnapshot;

        private ICameraAimPointProvider cameraAimPointProvider;
        private bool hasLoggedMissingAimPointProvider;

        /// <summary>
        /// 统一执行一次 Hitscan 查询。
        /// 只有当武器配置、相机瞄点、Physics 查询都完整成功时才返回 true。
        /// </summary>
        public bool TryHit(
            WeaponRuntime _weaponRuntime,
            out HitScanHitContext _hitContext,
            out Ray _shotRay,
            out CameraAimPointContext _cameraAimPointContext)
        {
            _hitContext = HitScanHitContext.None;
            _shotRay = default;
            _cameraAimPointContext = CameraAimPointContext.Default;
            lastShotDebugSnapshot = default;

            if (_weaponRuntime == null || _weaponRuntime.IsInitialized == false)
            {
                return false;
            }

            if (TryBuildShotRay(_weaponRuntime, out _shotRay, out _cameraAimPointContext, out RaycastHit obstructionHit, out bool hadObstruction) == false)
            {
                return false;
            }

            RaycastHit finalHitInfo;
            bool hadFinalHit;
            if (hadObstruction)
            {
                finalHitInfo = obstructionHit;
                hadFinalHit = true;
            }
            else
            {
                hadFinalHit = Physics.Raycast(
                    _shotRay,
                    out finalHitInfo,
                    _weaponRuntime.Range,
                    _weaponRuntime.HitLayerMask,
                    _weaponRuntime.HitTriggerInteraction);
            }

            RecordShotDebug(_cameraAimPointContext, _shotRay, hadObstruction ? obstructionHit : (RaycastHit?)null, hadFinalHit ? finalHitInfo : (RaycastHit?)null);

            if (hadFinalHit == false)
            {
                return false;
            }

            HealthComponent damageReceiver = finalHitInfo.collider.GetComponentInParent<HealthComponent>();
            CombatHitPartType hitPartType = ResolveHitPartType(finalHitInfo.collider);

            _hitContext = new HitScanHitContext(
                true,
                finalHitInfo.collider,
                damageReceiver,
                finalHitInfo.point,
                finalHitInfo.normal,
                finalHitInfo.distance,
                hitPartType);
            return true;
        }

        private bool TryBuildShotRay(
            WeaponRuntime _weaponRuntime,
            out Ray _shotRay,
            out CameraAimPointContext _cameraAimPointContext,
            out RaycastHit _obstructionHit,
            out bool _hadObstruction)
        {
            ResolveAimPointProvider();
            _obstructionHit = default;
            _hadObstruction = false;
             
            if (cameraAimPointProvider == null)
            {
                LogMissingAimPointProvider();
                _shotRay = default;
                _cameraAimPointContext = CameraAimPointContext.Default;
                return false;
            }

            if (cameraAimPointProvider.TryGetCameraAimPointContext(out _cameraAimPointContext) == false)
            {
                Debug.LogWarning($"[{nameof(HitScanService)}] 当前帧未取得有效的 CameraAimPointContext，已拒绝本次逻辑射线构建。Object={name}", this);
                _shotRay = default;
                return false;
            }

            Vector3 shotOrigin = muzzleTransform != null ? muzzleTransform.position : _cameraAimPointContext.RayOrigin;
            Vector3 shotVector = _cameraAimPointContext.AimPoint - shotOrigin;
            Vector3 shotDirection = shotVector.sqrMagnitude > 0.0001f
                ? shotVector.normalized
                : _cameraAimPointContext.RayDirection.normalized;

            _shotRay = new Ray(shotOrigin, shotDirection);

            if (enableNearMuzzleObstruction == false)
            {
                return true;
            }

            float obstructionCheckDistance = Mathf.Min(
                Mathf.Max(0f, muzzleObstructionCheckDistance),
                Mathf.Max(0.01f, _weaponRuntime.Range),
                Mathf.Max(0.01f, _cameraAimPointContext.Distance));

            if (obstructionCheckDistance <= 0f)
            {
                return true;
            }

            if (Physics.SphereCast(
                    shotOrigin,
                    muzzleObstructionSphereRadius,
                    shotDirection,
                    out _obstructionHit,
                    obstructionCheckDistance,
                    _weaponRuntime.HitLayerMask,
                    _weaponRuntime.HitTriggerInteraction))
            {
                _hadObstruction = true;
                _shotRay = new Ray(shotOrigin, (_obstructionHit.point - shotOrigin).normalized);
            }

            return true;
        }

        private void ResolveAimPointProvider()
        {
            if (cameraAimPointProviderBehaviour != null)
            {
                cameraAimPointProvider = cameraAimPointProviderBehaviour as ICameraAimPointProvider;
            }

            if (cameraAimPointProvider == null)
            {
                MonoBehaviour[] candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] is ICameraAimPointProvider provider)
                    {
                        cameraAimPointProviderBehaviour = candidates[i];
                        cameraAimPointProvider = provider;
                        break;
                    }
                }
            }
        }

        private void RecordShotDebug(
            in CameraAimPointContext _cameraAimPointContext,
            in Ray _shotRay,
            RaycastHit? _obstructionHit,
            RaycastHit? _finalHit)
        {
            lastShotDebugSnapshot = new ShotDebugSnapshot
            {
                hadValidAimPoint = true,
                hadObstruction = _obstructionHit.HasValue,
                hadHit = _finalHit.HasValue,
                cameraRayOrigin = _cameraAimPointContext.RayOrigin,
                cameraRayDirection = _cameraAimPointContext.RayDirection,
                aimPoint = _cameraAimPointContext.AimPoint,
                shotOrigin = _shotRay.origin,
                shotDirection = _shotRay.direction
            };

            if (_obstructionHit.HasValue)
            {
                RaycastHit obstructionHit = _obstructionHit.Value;
                lastShotDebugSnapshot.obstructionPoint = obstructionHit.point;
                lastShotDebugSnapshot.obstructionDistance = obstructionHit.distance;
                lastShotDebugSnapshot.obstructionColliderName = obstructionHit.collider != null ? obstructionHit.collider.name : string.Empty;
            }

            if (_finalHit.HasValue)
            {
                RaycastHit finalHit = _finalHit.Value;
                lastShotDebugSnapshot.finalHitPoint = finalHit.point;
                lastShotDebugSnapshot.finalHitDistance = finalHit.distance;
                lastShotDebugSnapshot.finalHitColliderName = finalHit.collider != null ? finalHit.collider.name : string.Empty;
            }
        }

        private CombatHitPartType ResolveHitPartType(Collider _collider)
        {
            if (_collider == null)
            {
                return CombatHitPartType.Default;
            }

            WeakSpotComponent weakSpot = _collider.GetComponent<WeakSpotComponent>() ?? _collider.GetComponentInParent<WeakSpotComponent>();
            if (weakSpot != null && weakSpot.IsEnabled)
            {
                return CombatHitPartType.WeakPoint;
            }

            CombatHitBoxComponent hitBox = _collider.GetComponent<CombatHitBoxComponent>() ?? _collider.GetComponentInParent<CombatHitBoxComponent>();
            return hitBox != null ? hitBox.HitPartType : CombatHitPartType.Default;
        }

        private void LogMissingAimPointProvider()
        {
            if (hasLoggedMissingAimPointProvider)
            {
                return;
            }

            hasLoggedMissingAimPointProvider = true;
            Debug.LogWarning($"[{nameof(HitScanService)}] 缺少 {nameof(ICameraAimPointProvider)}，已阻止本次射击逻辑射线构建。请确认场景中存在并正确绑定 CameraAimPointResolver。Object={name}", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (enableShotDebugGizmos == false || lastShotDebugSnapshot.hadValidAimPoint == false)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(lastShotDebugSnapshot.cameraRayOrigin, lastShotDebugSnapshot.aimPoint);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lastShotDebugSnapshot.shotOrigin, 
                lastShotDebugSnapshot.shotOrigin + lastShotDebugSnapshot.shotDirection * Mathf.Max(0.5f, lastShotDebugSnapshot.finalHitDistance > 0f ? lastShotDebugSnapshot.finalHitDistance : 2f));

            if (lastShotDebugSnapshot.hadObstruction)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(lastShotDebugSnapshot.obstructionPoint, 0.05f);
            }

            if (lastShotDebugSnapshot.hadHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(lastShotDebugSnapshot.finalHitPoint, 0.06f);
            }
        }

        private void OnValidate()
        {
            ResolveAimPointProvider();
        }
#endif
    }
}
