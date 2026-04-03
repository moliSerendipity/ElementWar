using Game.Gameplay.Camera;
using UnityEngine;

namespace Game.Presentation.Camera
{
    /// <summary>
    /// 逻辑瞄点解析器。
    /// 它只回答一件事：
    /// “当前屏幕中心朝出去，Gameplay 应该认为瞄准点在哪里？”
    /// 当前挂在 Main Camera 上，因此默认直接使用实际渲染相机发射屏幕中心射线。
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
            // 解析距离必须保持正值，避免生成零长度逻辑瞄点。
            defaultResolveDistance = Mathf.Max(0.01f, defaultResolveDistance);
        }
#endif

        #endregion

        #region Public API

        /// <summary>
        /// 按当前武器射击查询参数解析本帧逻辑瞄点。
        /// 当前版本中，命中查询距离、LayerMask 与 Trigger 策略都必须与真实命中查询统一，
        /// 避免相机逻辑瞄点和 Hitscan 使用两套不同的过滤规则。
        /// </summary>
        public void TickResolve(float resolveDistance, LayerMask aimCollisionLayers, QueryTriggerInteraction queryTriggerInteraction, bool hasWeaponQuerySettings)
        {
            // 没有有效渲染相机时，当前帧逻辑瞄点无效。
            if (TryResolveRenderCamera(out UnityEngine.Camera activeRenderCamera) == false)
            {
                currentAimPointContext = CameraAimPointContext.Default;
                return;
            }

            // 当前武器存在时，优先使用武器统一后的射击查询距离；否则才退回相机本地默认值。
            float effectiveResolveDistance = Mathf.Max(0.01f, resolveDistance > 0f ? resolveDistance : defaultResolveDistance);
            // 当前武器存在时，优先使用武器统一后的命中层级；否则才退回相机本地默认值。
            LayerMask effectiveAimCollisionLayers = hasWeaponQuerySettings ? aimCollisionLayers : defaultAimCollisionLayers;
            // 当前武器存在时，优先使用武器统一后的 Trigger 查询策略；否则才退回相机本地默认值。
            QueryTriggerInteraction effectiveQueryTriggerInteraction = hasWeaponQuerySettings ? queryTriggerInteraction : defaultQueryTriggerInteraction;
            // 以屏幕中心构建相机逻辑瞄点查询射线。
            Ray centerRay = activeRenderCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // 命中世界时，逻辑瞄点直接落在真实命中点上。
            if (Physics.Raycast(
                centerRay,
                out RaycastHit hitInfo,
                effectiveResolveDistance,
                effectiveAimCollisionLayers,
                effectiveQueryTriggerInteraction))
            {
                currentAimPointContext = new CameraAimPointContext(
                    centerRay.origin,
                    centerRay.direction,
                    hitInfo.point,
                    true,
                    hitInfo.distance);
                return;
            }

            // 未命中世界时，回退点与命中查询共用同一套查询口径，保证准心、Hitscan 与视觉拖尾的射击语义一致。
            Vector3 fallbackAimPoint = centerRay.origin + centerRay.direction * effectiveResolveDistance;
            currentAimPointContext = new CameraAimPointContext(
                centerRay.origin,
                centerRay.direction,
                fallbackAimPoint,
                false,
                effectiveResolveDistance);
        }

        public bool TryGetCameraAimPointContext(out CameraAimPointContext cameraAimPointContext)
        {
            cameraAimPointContext = currentAimPointContext;
            return currentAimPointContext.Distance > 0f;
        }

        #endregion

        #region Private Methods

        private bool TryResolveRenderCamera(out UnityEngine.Camera resolvedRenderCamera)
        {
            if (renderCamera == null)
            {
                renderCamera = UnityEngine.Camera.main;
            }

            resolvedRenderCamera = renderCamera;
            return resolvedRenderCamera != null;
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
