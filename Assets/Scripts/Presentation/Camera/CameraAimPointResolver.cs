using Game.Gameplay.Camera;
using UnityEngine;

namespace Game.Presentation.Camera
{
    /// <summary>
    /// 逻辑瞄点解析器。
    /// 回答一个核心问题："当前屏幕中心朝出去，Gameplay 和 IK 应该认为瞄准点在哪里？"
    ///
    /// 当前挂在 Main Camera 上，默认使用实际渲染相机发射屏幕中心射线。
    ///
    /// 最小瞄准距离机制：
    /// 当相机射线命中的表面太近时（如贴墙），原始命中点会导致 AimTarget
    /// 被放到角色身旁，使 Right Hand Aim Constraint 把手臂拧到不合理的角度。
    /// 通过设置最小瞄准距离，把过近的瞄准点沿射线方向推远到安全位置，
    /// 让 IK 始终获得一个合理的目标朝向。
    /// 射击逻辑不受影响：HitScanService 的 Raycast 从枪口沿该方向射出，
    /// 仍然会自然命中中间的任何墙壁。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraAimPointResolver : MonoBehaviour, ICameraAimPointProvider
    {
        #region Inspector

        [Header("References")]
        [SerializeField] private UnityEngine.Camera renderCamera;

        [Header("Aim Point Settings")]
        [SerializeField] private LayerMask defaultAimCollisionLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction defaultQueryTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Min(0.01f)] private float defaultResolveDistance = 200f;

        [Header("Minimum Aim Distance")]
        [Tooltip("逻辑瞄点距相机原点的最小距离（单位：米）。\n" +
                 "当相机射线命中的表面比这个值近时，瞄准点会被沿射线方向推远到此距离。\n" +
                 "主要用于防止 AimTarget 太近导致 IK 手臂拧歪。\n" +
                 "典型越肩相机距角色约 2~3m，设 3~5m 约等于角色前方 1~2m。")]
        [SerializeField, Min(0f)] private float minimumAimDistance = 3.5f;

        #endregion

        #region Runtime Fields

        private CameraAimPointContext currentAimPointContext = CameraAimPointContext.Default;

        #endregion

        #region Properties

        public CameraAimPointContext CurrentAimPointContext => currentAimPointContext;

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
            defaultResolveDistance = Mathf.Max(0.01f, defaultResolveDistance);
        }
#endif

        #endregion

        #region Public API

        /// <summary>
        /// 按当前武器射击查询参数解析本帧逻辑瞄点。
        /// 命中查询距离、LayerMask 与 Trigger 策略都必须与真实命中查询统一，
        /// 避免相机逻辑瞄点和 Hitscan 使用两套不同的过滤规则。
        /// </summary>
        public void TickResolve(
            float _resolveDistance,
            LayerMask _aimCollisionLayers,
            QueryTriggerInteraction _queryTriggerInteraction,
            bool _hasWeaponQuerySettings)
        {
            if (TryResolveRenderCamera(out UnityEngine.Camera activeRenderCamera) == false)
            {
                currentAimPointContext = CameraAimPointContext.Default;
                return;
            }

            // 确定本帧有效的查询参数：武器存在时用武器的，否则用本地默认值。
            float effectiveResolveDistance = Mathf.Max(0.01f,
                _resolveDistance > 0f ? _resolveDistance : defaultResolveDistance);
            LayerMask effectiveLayers = _hasWeaponQuerySettings
                ? _aimCollisionLayers : defaultAimCollisionLayers;
            QueryTriggerInteraction effectiveTrigger = _hasWeaponQuerySettings
                ? _queryTriggerInteraction : defaultQueryTriggerInteraction;

            // 以屏幕中心构建相机逻辑瞄点查询射线。
            Ray centerRay = activeRenderCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(centerRay, out RaycastHit hitInfo, effectiveResolveDistance,
                    effectiveLayers, effectiveTrigger))
            {
                // 命中世界表面。
                ResolveWithHit(centerRay, hitInfo);
            }
            else
            {
                // 未命中世界，使用最大查询距离上的回退点。
                ResolveWithoutHit(centerRay, effectiveResolveDistance);
            }
        }

        /// <summary>
        /// 供外部（HitScanService、CameraRoot）读取本帧逻辑瞄点。
        /// </summary>
        public bool TryGetCameraAimPointContext(out CameraAimPointContext _cameraAimPointContext)
        {
            _cameraAimPointContext = currentAimPointContext;
            return currentAimPointContext.Distance > 0f;
        }

        #endregion

        #region Aim Point Resolution

        /// <summary>
        /// 命中世界表面时的瞄点解析。
        /// 如果命中距离小于最小瞄准距离，将瞄准点沿射线方向推远，
        /// 防止 AimTarget 太近导致 IK 手臂拧歪。
        /// </summary>
        private void ResolveWithHit(Ray _centerRay, RaycastHit _hitInfo)
        {
            Vector3 aimPoint = _hitInfo.point;
            float aimDistance = _hitInfo.distance;

            // 命中距离太近时，把瞄准点沿射线方向推远到最小安全距离。
            // 这只影响 AimTarget（IK 目标）和射击方向；
            // HitScanService 的 Raycast 从枪口沿此方向射出，仍然会自然命中中间的墙壁。
            if (minimumAimDistance > 0f && aimDistance < minimumAimDistance)
            {
                aimDistance = minimumAimDistance;
                aimPoint = _centerRay.origin + _centerRay.direction * minimumAimDistance;
            }

            currentAimPointContext = new CameraAimPointContext(
                _centerRay.origin,
                _centerRay.direction,
                aimPoint,
                true,
                aimDistance);
        }

        /// <summary>
        /// 未命中世界时的瞄点解析。
        /// 回退点使用最大查询距离，保证准心、Hitscan 与视觉拖尾的射击语义一致。
        /// </summary>
        private void ResolveWithoutHit(Ray _centerRay, float _resolveDistance)
        {
            Vector3 fallbackAimPoint = _centerRay.origin + _centerRay.direction * _resolveDistance;
            currentAimPointContext = new CameraAimPointContext(
                _centerRay.origin,
                _centerRay.direction,
                fallbackAimPoint,
                false,
                _resolveDistance);
        }

        #endregion

        #region Camera Resolution

        private bool TryResolveRenderCamera(out UnityEngine.Camera _resolvedRenderCamera)
        {
            if (renderCamera == null)
            {
                renderCamera = UnityEngine.Camera.main;
            }

            _resolvedRenderCamera = renderCamera;
            return _resolvedRenderCamera != null;
        }

        private void ResolveReferences()
        {
            if (renderCamera == null)
            {
                renderCamera = GetComponent<UnityEngine.Camera>();
            }

            if (renderCamera == null)
            {
                renderCamera = UnityEngine.Camera.main;
            }
        }

        #endregion
    }
}
