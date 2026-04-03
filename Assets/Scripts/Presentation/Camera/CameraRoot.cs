using Cinemachine;
using Game.Gameplay.Camera;
using Game.Gameplay.Character;
using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Presentation.Camera
{
    /// <summary>
    /// 主相机根节点。
    ///
    /// 当前版本相机需求已经收敛为两件事：
    /// 1. 正常状态使用 normalFreeLook；
    /// 2. 瞄准状态使用 aimingFreeLook。
    ///
    /// 因此不再保留额外的 CameraStateController 去二次包一层状态机。
    /// 这里直接根据 CharacterFacts 选择当前虚拟相机，并统一驱动跟随与逻辑瞄点解析。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraRoot : MonoBehaviour, ICameraViewProvider, ICameraAimPointProvider
    {
        [Header("Virtual Cameras")]
        [SerializeField] private CinemachineFreeLook normalFreeLookCamera;
        [SerializeField] private CinemachineFreeLook aimingFreeLookCamera;

        [Header("References")]
        [SerializeField] private CharacterRoot characterRoot;
        [SerializeField] private CameraFollowController followController;
        [SerializeField] private CameraAimPointResolver aimPointResolver;

        [Header("Targets")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookTarget;

        [Header("Priorities")]
        [SerializeField] private int activePriority = 100;
        [SerializeField] private int inactivePriority = 10;

        private CinemachineFreeLook currentCamera;

        public CinemachineFreeLook CurrentCamera => currentCamera;
        public CinemachineFreeLook NormalFreeLookCamera => normalFreeLookCamera;
        public CinemachineFreeLook AimingFreeLookCamera => aimingFreeLookCamera;
        public Transform FollowTarget => followTarget;
        public Transform LookTarget => lookTarget != null ? lookTarget : followTarget;

        private void Awake()
        {
            ResolveReferences();
            BindTargets();
            ClearBuiltInAxisInput();
            RebindControllers();
            RefreshCurrentCamera(ResolveUseAimingCamera());
        }

        /// <summary>
        /// 按当前角色状态更新相机跟随、虚拟相机切换与逻辑瞄点解析。
        /// </summary>
        private void LateUpdate()
        {
            // 先更新跟随控制器，保证当前帧相机朝向已经跟上角色控制朝向。
            followController?.TickFollow();
            // 再根据角色事实选择当前应该激活的虚拟相机。
            RefreshCurrentCamera(ResolveUseAimingCamera());

            WeaponViewState currentWeaponViewState = characterRoot != null && characterRoot.ActionController != null
                ? characterRoot.ActionController.CurrentWeaponViewState
                : null;
            // 只有当前角色持有有效武器表现态时，才使用统一后的武器射击查询口径解析逻辑瞄点。
            bool hasWeaponQuerySettings = currentWeaponViewState != null;
            // 当前武器射程是逻辑瞄点最大解析距离的正式来源；无武器时交给 Resolver 自己兜底。
            float resolveDistance = hasWeaponQuerySettings ? currentWeaponViewState.ShotDistance : 0f;
            // 当前武器命中层级是逻辑瞄点过滤层级的正式来源；无武器时交给 Resolver 自己兜底。
            LayerMask aimCollisionLayers = hasWeaponQuerySettings ? currentWeaponViewState.HitLayerMask : default;
            // 当前武器 Trigger 查询策略是逻辑瞄点 Trigger 口径的正式来源；无武器时交给 Resolver 自己兜底。
            QueryTriggerInteraction queryTriggerInteraction = hasWeaponQuerySettings
                ? currentWeaponViewState.HitTriggerInteraction
                : QueryTriggerInteraction.Ignore;

            // 使用与真实 Hitscan 完全一致的查询参数解析逻辑瞄点，避免相机与武器各自维护不同的射击过滤规则。
            aimPointResolver?.TickResolve(resolveDistance, aimCollisionLayers, queryTriggerInteraction, hasWeaponQuerySettings);
        }

        public bool TryGetCameraViewContext(out CameraViewContext _cameraViewContext)
        {
            _cameraViewContext = CameraViewContext.Default;
            return followController != null && followController.TryGetPreviewViewContext(out _cameraViewContext);
        }

        public bool TryGetCameraAimPointContext(out CameraAimPointContext _cameraAimPointContext)
        {
            _cameraAimPointContext = CameraAimPointContext.Default;
            return aimPointResolver != null && aimPointResolver.TryGetCameraAimPointContext(out _cameraAimPointContext);
        }

        public void SetTargets(Transform _followTarget, Transform _lookTarget)
        {
            followTarget = _followTarget;
            lookTarget = _lookTarget;
            BindTargets();
            RebindControllers();
        }


        public void RebindControllers()
        {
            if (characterRoot == null && followTarget != null)
            {
                characterRoot = followTarget.GetComponentInParent<CharacterRoot>();
            }

            CharacterFacingController facingController = followTarget != null
                ? followTarget.GetComponentInParent<CharacterFacingController>()
                : null;

            followController?.RebindFacingController(facingController);
        }

        private void BindTargets()
        {
            BindTargets(normalFreeLookCamera);
            BindTargets(aimingFreeLookCamera);
        }

        private void BindTargets(CinemachineFreeLook _camera)
        {
            if (_camera == null)
            {
                return;
            }

            _camera.Follow = followTarget;
            _camera.LookAt = LookTarget;
        }

        private bool ResolveUseAimingCamera()
        {
            CharacterFacts facts = characterRoot != null ? characterRoot.Facts : null;
            return facts != null && facts.IsAiming && !facts.IsReloading && !facts.IsDead;
        }

        private void RefreshCurrentCamera(bool _useAimingCamera)
        {
            CinemachineFreeLook nextCamera = _useAimingCamera && aimingFreeLookCamera != null
                ? aimingFreeLookCamera
                : normalFreeLookCamera;

            currentCamera = nextCamera;

            if (normalFreeLookCamera != null)
            {
                normalFreeLookCamera.Priority = nextCamera == normalFreeLookCamera ? activePriority : inactivePriority;
            }

            if (aimingFreeLookCamera != null)
            {
                aimingFreeLookCamera.Priority = nextCamera == aimingFreeLookCamera ? activePriority : inactivePriority;
            }
        }

        private void ClearBuiltInAxisInput()
        {
            ClearBuiltInAxisInput(normalFreeLookCamera);
            ClearBuiltInAxisInput(aimingFreeLookCamera);
        }

        private void ClearBuiltInAxisInput(CinemachineFreeLook _camera)
        {
            if (_camera == null)
            {
                return;
            }

            _camera.m_XAxis.m_InputAxisName = string.Empty;
            _camera.m_YAxis.m_InputAxisName = string.Empty;
        }

        private void ResolveReferences()
        {
            if (followController == null)
            {
                followController = GetComponent<CameraFollowController>();
            }

            if (aimPointResolver == null)
            {
                aimPointResolver = GetComponent<CameraAimPointResolver>();
            }

            if (characterRoot == null && followTarget != null)
            {
                characterRoot = followTarget.GetComponentInParent<CharacterRoot>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            BindTargets();
        }
#endif
    }
}
