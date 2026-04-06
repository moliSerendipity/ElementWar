using System;
using Game.Gameplay.Camera;
using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Combat 域唯一 Hitscan 查询入口。
    ///
    /// 标准 TPS 射击逻辑：
    /// 1. 相机屏幕中心射线决定"准心瞄的是什么"（逻辑瞄点）
    /// 2. 枪口是子弹的物理出发点
    /// 3. 枪口收敛判定：枪口 forward 与理想射击方向的夹角超过阈值时，
    ///    说明 IK 还没对准，此时用枪口 forward 作为射击方向（诚实射击）
    /// 4. 遮挡自然处理：从枪口到瞄准点方向的全程 Raycast 自然命中中间的墙壁
    ///
    /// 约束：
    /// 1. 只负责命中查询，不负责伤害裁决
    /// 2. 热路径只读取 WeaponRuntime 已解析好的射程与查询规则
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitScanService : MonoBehaviour
    {
        #region Debug Snapshot

        /// <summary>
        /// 每次射击的调试快照，仅用于 Inspector 查看和 Gizmos 绘制。
        /// </summary>
        [Serializable]
        private struct ShotDebugSnapshot
        {
            public bool isValid;
            public Vector3 cameraRayOrigin;
            public Vector3 cameraRayDirection;
            public Vector3 aimPoint;
            public Vector3 muzzlePosition;
            public Vector3 muzzleForward;
            public Vector3 shotOrigin;
            public Vector3 shotDirection;
            public float convergenceAngle;
            public bool isConverged;
            public bool isMuzzleBuried;
            public bool hadHit;
            public Vector3 hitPoint;
            public float hitDistance;
            public string hitColliderName;
        }

        #endregion

        #region Inspector

        [Header("References")]
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private MonoBehaviour cameraAimPointProviderBehaviour;

        [Header("Muzzle Convergence")]
        [Tooltip("枪口 forward 与理想射击方向（枪口→瞄准点）的最大允许夹角。\n" +
                 "超过此角度时说明 IK 还没对准，改用枪口 forward 射击。\n" +
                 "一般 20~35 度比较合适。")]
        [SerializeField, Range(5f, 60f)] private float convergenceAngleThreshold = 25f;

        [Header("Muzzle Buried Detection")]
        [Tooltip("用于检测枪口是否埋入几何体的球检测半径。\n" +
                 "枪口在墙内时拒绝射击，避免穿墙打人。")]
        [SerializeField, Min(0.01f)] private float muzzleBuriedCheckRadius = 0.06f;

        [Header("Debug")]
        [SerializeField] private bool enableShotDebugGizmos = true;
        [SerializeField] private ShotDebugSnapshot lastShotDebugSnapshot;

        #endregion

        #region Runtime Fields

        private ICameraAimPointProvider cameraAimPointProvider;
        private bool hasLoggedMissingAimPointProvider;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 在初始化阶段完成 AimPointProvider 解析，不在射击热路径中全场扫描。
            ResolveAimPointProvider();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 执行一次完整的 TPS Hitscan 查询。
        ///
        /// 流程：
        /// ① 从 CameraAimPointResolver 获取逻辑瞄点（准心瞄的是什么）
        /// ② 计算枪口收敛角，决定射击方向：
        ///    - 收敛（IK 对准了）→ 用"枪口→瞄准点"方向，全程 Raycast 自然命中中间的墙
        ///    - 未收敛（IK 过渡中）→ 用枪口实际 forward，子弹从枪口朝向打出
        /// ③ 叠加散布偏移
        /// ④ 全射程 Raycast
        /// </summary>
        public bool TryHit(
            WeaponRuntime _weaponRuntime,
            float _spreadAngle,
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

            #region Step 1: 获取相机逻辑瞄点

            if (TryResolveCameraAimPoint(out _cameraAimPointContext) == false)
            {
                return false;
            }

            #endregion

            #region Step 2: 确定枪口位置与朝向

            // 枪口缺失时降级为相机射线原点（不会发生遮挡问题，退化为相机射击）。
            Vector3 muzzlePosition = muzzleTransform != null
                ? muzzleTransform.position
                : _cameraAimPointContext.RayOrigin;
            Vector3 muzzleForward = muzzleTransform != null
                ? muzzleTransform.forward
                : _cameraAimPointContext.RayDirection.normalized;

            #endregion

            #region Step 3: 枪口收敛角判定 → 决定射击方向

            // "理想方向"：从枪口指向相机逻辑瞄点（标准 TPS：准心所见即命中目标）。
            Vector3 toAimPoint = _cameraAimPointContext.AimPoint - muzzlePosition;
            Vector3 idealDirection = toAimPoint.sqrMagnitude > 0.0001f
                ? toAimPoint.normalized
                : _cameraAimPointContext.RayDirection.normalized;

            float convergenceAngle = Vector3.Angle(muzzleForward, idealDirection);
            bool isConverged = convergenceAngle <= convergenceAngleThreshold;

            // 收敛：射击方向指向瞄准点（Raycast 全程自然命中中间的墙壁）。
            // 未收敛：射击方向为枪口 forward（IK 没对准时从枪口实际朝向射出）。
            Vector3 shotDirection = isConverged ? idealDirection : muzzleForward;

            #endregion

            #region Step 4: 叠加散布偏移

            if (_spreadAngle > 0f)
            {
                shotDirection = ApplySpreadToDirection(shotDirection, _spreadAngle);
            }

            #endregion

            #region Step 5: 全射程 Raycast

            _shotRay = new Ray(muzzlePosition, shotDirection);

            bool hadHit = Physics.Raycast(
                _shotRay,
                out RaycastHit hitInfo,
                _weaponRuntime.Range,
                _weaponRuntime.HitLayerMask,
                _weaponRuntime.HitTriggerInteraction);

            RecordDebugSnapshot(
                _cameraAimPointContext, muzzlePosition, muzzleForward,
                _shotRay, convergenceAngle, isConverged, false,
                hadHit, hadHit ? hitInfo : default);

            if (hadHit == false)
            {
                return false;
            }

            #endregion

            #region Step 6: 构建命中上下文

            HealthComponent healthComponent = hitInfo.collider.GetComponentInParent<HealthComponent>();
            CombatHitPartType hitPartType = ResolveHitPartType(hitInfo.collider);

            _hitContext = new HitScanHitContext(
                true,
                hitInfo.collider,
                healthComponent,
                hitInfo.point,
                hitInfo.normal,
                hitInfo.distance,
                hitPartType);

            return true;

            #endregion
        }

        #endregion

        #region Camera Aim Point

        /// <summary>
        /// 从 CameraAimPointResolver 获取本帧逻辑瞄点。
        /// </summary>
        private bool TryResolveCameraAimPoint(out CameraAimPointContext _context)
        {
            _context = CameraAimPointContext.Default;

            // 如果 Awake 阶段没有找到 provider，尝试补救一次（应对动态创建场景）。
            if (cameraAimPointProvider == null)
            {
                ResolveAimPointProvider();
            }

            if (cameraAimPointProvider == null)
            {
                LogMissingAimPointProvider();
                return false;
            }

            if (cameraAimPointProvider.TryGetCameraAimPointContext(out _context) == false)
            {
                Debug.LogWarning(
                    $"[{nameof(HitScanService)}] 当前帧未取得有效 CameraAimPointContext，拒绝本次射击。Object={name}", this);
                return false;
            }

            return true;
        }

        #endregion

        #region Spread

        /// <summary>
        /// 在射击方向上叠加散布偏移。
        /// 在锥体内均匀采样一个偏移角度，旋转射击方向。
        /// </summary>
        private static Vector3 ApplySpreadToDirection(Vector3 _baseDirection, float _spreadAngleDegrees)
        {
            if (_spreadAngleDegrees <= 0f)
            {
                return _baseDirection;
            }

            // 在锥体内均匀采样偏移角度。
            float halfSpreadRad = _spreadAngleDegrees * 0.5f * Mathf.Deg2Rad;
            float randomAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float randomRadius = Mathf.Sin(UnityEngine.Random.Range(0f, halfSpreadRad));

            // 构建垂直于射击方向的局部坐标系。
            Vector3 perpendicular = Vector3.Cross(_baseDirection, Vector3.up).sqrMagnitude > 0.0001f
                ? Vector3.Cross(_baseDirection, Vector3.up).normalized
                : Vector3.Cross(_baseDirection, Vector3.right).normalized;
            Vector3 perpendicular2 = Vector3.Cross(_baseDirection, perpendicular).normalized;

            // 叠加偏移后归一化。
            Vector3 spread = perpendicular * (Mathf.Cos(randomAngle) * randomRadius)
                           + perpendicular2 * (Mathf.Sin(randomAngle) * randomRadius);

            return (_baseDirection + spread).normalized;
        }

        #endregion

        #region Hit Part Resolution

        /// <summary>
        /// 从碰撞体上解析命中部位类型：弱点 > HitBox 标记 > 默认。
        /// </summary>
        private static CombatHitPartType ResolveHitPartType(Collider _collider)
        {
            if (_collider == null)
            {
                return CombatHitPartType.Default;
            }

            // 先检查弱点组件。
            WeakSpotComponent weakSpot = _collider.GetComponent<WeakSpotComponent>()
                ?? _collider.GetComponentInParent<WeakSpotComponent>();
            if (weakSpot != null && weakSpot.IsEnabled)
            {
                return CombatHitPartType.WeakPoint;
            }

            // 再检查通用 HitBox 标记。
            CombatHitBoxComponent hitBox = _collider.GetComponent<CombatHitBoxComponent>()
                ?? _collider.GetComponentInParent<CombatHitBoxComponent>();
            return hitBox != null ? hitBox.HitPartType : CombatHitPartType.Default;
        }

        #endregion

        #region AimPoint Provider Resolution

        /// <summary>
        /// 解析 ICameraAimPointProvider。
        /// 优先使用 Inspector 显式绑定，其次在场景中搜索一次。
        /// </summary>
        private void ResolveAimPointProvider()
        {
            if (cameraAimPointProvider != null)
            {
                return;
            }

            // 优先从显式绑定的 MonoBehaviour 获取接口。
            if (cameraAimPointProviderBehaviour != null)
            {
                cameraAimPointProvider = cameraAimPointProviderBehaviour as ICameraAimPointProvider;
                if (cameraAimPointProvider != null)
                {
                    return;
                }
            }

            // 降级：在场景中搜索一次。只在 Awake 或首次缺失时执行。
            MonoBehaviour[] candidates = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is ICameraAimPointProvider provider)
                {
                    cameraAimPointProviderBehaviour = candidates[i];
                    cameraAimPointProvider = provider;
                    return;
                }
            }
        }

        #endregion

        #region Logging

        private void LogMissingAimPointProvider()
        {
            if (hasLoggedMissingAimPointProvider)
            {
                return;
            }

            hasLoggedMissingAimPointProvider = true;
            Debug.LogWarning(
                $"[{nameof(HitScanService)}] 缺少 {nameof(ICameraAimPointProvider)}，已阻止射击。" +
                $"请确认场景中存在并正确绑定 CameraAimPointResolver。Object={name}", this);
        }

        #endregion

        #region Debug

        /// <summary>
        /// 记录本次射击的调试快照。
        /// </summary>
        private void RecordDebugSnapshot(
            in CameraAimPointContext _aimCtx,
            Vector3 _muzzlePosition,
            Vector3 _muzzleForward,
            in Ray _shotRay,
            float _convergenceAngle,
            bool _isConverged,
            bool _isMuzzleBuried,
            bool _hadHit,
            RaycastHit _hitInfo)
        {
            lastShotDebugSnapshot = new ShotDebugSnapshot
            {
                isValid = true,
                cameraRayOrigin = _aimCtx.RayOrigin,
                cameraRayDirection = _aimCtx.RayDirection,
                aimPoint = _aimCtx.AimPoint,
                muzzlePosition = _muzzlePosition,
                muzzleForward = _muzzleForward,
                shotOrigin = _shotRay.origin,
                shotDirection = _shotRay.direction,
                convergenceAngle = _convergenceAngle,
                isConverged = _isConverged,
                isMuzzleBuried = _isMuzzleBuried,
                hadHit = _hadHit
            };

            if (_hadHit)
            {
                lastShotDebugSnapshot.hitPoint = _hitInfo.point;
                lastShotDebugSnapshot.hitDistance = _hitInfo.distance;
                lastShotDebugSnapshot.hitColliderName = _hitInfo.collider != null
                    ? _hitInfo.collider.name : string.Empty;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (enableShotDebugGizmos == false || lastShotDebugSnapshot.isValid == false)
            {
                return;
            }

            ref ShotDebugSnapshot snap = ref lastShotDebugSnapshot;

            // 蓝线 + 蓝圈：相机中心 → 逻辑瞄点（准心目标位置）
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(snap.cameraRayOrigin, snap.aimPoint);
            Gizmos.DrawWireSphere(snap.aimPoint, 0.08f);

            // 白线：枪口实际 forward（短线段，方便观察 IK 当前朝向）
            Gizmos.color = Color.white;
            Gizmos.DrawLine(snap.muzzlePosition, snap.muzzlePosition + snap.muzzleForward * 1.5f);

            // 枪口位置标记：埋入时红色，正常时灰色
            Gizmos.color = snap.isMuzzleBuried ? Color.red : Color.gray;
            Gizmos.DrawWireSphere(snap.muzzlePosition, muzzleBuriedCheckRadius);

            // 射线：收敛时黄色（标准 TPS），未收敛时橙色（IK 过渡中）
            float drawDistance = snap.hadHit ? snap.hitDistance : 5f;
            Gizmos.color = snap.isConverged ? Color.yellow : new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(snap.shotOrigin, snap.shotOrigin + snap.shotDirection * drawDistance);

            // 绿球：最终命中点
            if (snap.hadHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(snap.hitPoint, 0.06f);
            }
        }

        private void OnValidate()
        {
            ResolveAimPointProvider();
        }
#endif

        #endregion
    }
}
