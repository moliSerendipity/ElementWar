using Cinemachine;
using Game.Foundation.Events;
using Game.Gameplay.Camera;
using Game.Gameplay.Character;
using Game.Gameplay.Weapon.Events;
using UnityEngine;

namespace Game.Presentation.Camera
{
    /// <summary>
    /// FreeLook 跟随控制器。
    /// 把 CharacterFacingController 维护的 yaw / pitch 同步到所有 FreeLook，
    /// 保证正常相机和瞄准相机切换时没有视角断层。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraFollowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraRoot cameraRoot;
        [SerializeField] private CharacterFacingController facingController;

        [Header("Visual Kick")]
        [SerializeField] private bool enableVisualKick = true;
        [SerializeField] private float visualKickRecoverSpeed = 12f;

        private float currentYaw;
        private float currentPitch;
        private float visualYawOffset;
        private float visualPitchOffset;

        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;

        private void Awake()
        {
            ResolveReferences();
            SyncInitialAngles();
        }

        private void OnEnable()
        {
            if (GameEventBus.Instance != null)
            {
                GameEventBus.Instance.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            }
        }

        private void OnDisable()
        {
            if (GameEventBus.Instance != null)
            {
                GameEventBus.Instance.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
            }
        }

        /// <summary>
        /// 开火后只追加短时视觉 kick。
        ///
        /// 注意：
        /// 1. 主后坐力已经在 CharacterFacingController 中写入真实控制视角；
        /// 2. 这里不负责真实抬枪，只负责更短、更快的镜头冲击感；
        /// 3. 因此允许平滑回零，而不会把玩家压枪后的真实视角拖回去。
        /// </summary>
        private void OnWeaponFired(WeaponFiredEvent _eventArgs)
        {
            if (enableVisualKick == false || facingController == null)
            {
                return;
            }

            if (_eventArgs.WeaponObject == null || _eventArgs.WeaponObject.transform.IsChildOf(facingController.transform) == false)
            {
                return;
            }

            // 视觉 kick 直接消费已提交开火事件中的表现参数，不再在相机层自带一套常量。
            visualPitchOffset += _eventArgs.CameraKickPitch;
            visualYawOffset += Random.Range(-Mathf.Abs(_eventArgs.CameraKickYaw), Mathf.Abs(_eventArgs.CameraKickYaw));
        }

        public void TickFollow()
        {
            RecoverVisualKick();

            if (TryGetPreviewViewContext(out CameraViewContext previewViewContext) == false)
            {
                return;
            }

            currentYaw = previewViewContext.Yaw;
            currentPitch = previewViewContext.Pitch;

            ApplyAxis(cameraRoot != null ? cameraRoot.NormalFreeLookCamera : null, currentYaw, currentPitch);
            ApplyAxis(cameraRoot != null ? cameraRoot.AimingFreeLookCamera : null, currentYaw, currentPitch);
        }

        private void RecoverVisualKick()
        {
            if (enableVisualKick == false)
            {
                visualYawOffset = 0f;
                visualPitchOffset = 0f;
                return;
            }

            visualYawOffset = Mathf.MoveTowards(visualYawOffset, 0f, visualKickRecoverSpeed * Time.deltaTime);
            visualPitchOffset = Mathf.MoveTowards(visualPitchOffset, 0f, visualKickRecoverSpeed * Time.deltaTime);
        }

        public bool TryGetPreviewViewContext(out CameraViewContext _cameraViewContext)
        {
            _cameraViewContext = CameraViewContext.Default;

            if (facingController == null)
            {
                return false;
            }

            _cameraViewContext = new CameraViewContext(
                facingController.CurrentYaw + visualYawOffset,
                facingController.CurrentPitch + visualPitchOffset);
            return true;
        }

        public void RebindFacingController(CharacterFacingController _facingController)
        {
            facingController = _facingController;
            SyncInitialAngles();
        }

        private void ApplyAxis(CinemachineFreeLook _camera, float _yaw, float _pitch)
        {
            if (_camera == null)
            {
                return;
            }

            _camera.m_XAxis.Value = _yaw;
            _camera.m_YAxis.Value = Mathf.InverseLerp(facingController.MinPitch, facingController.MaxPitch, _pitch);
        }

        private void SyncInitialAngles()
        {
            Transform followTarget = cameraRoot != null ? cameraRoot.FollowTarget : null;
            currentYaw = followTarget != null ? followTarget.eulerAngles.y : transform.eulerAngles.y;
            currentPitch = 0f;
        }

        private void ResolveReferences()
        {
            if (cameraRoot == null)
            {
                cameraRoot = GetComponent<CameraRoot>();
            }

            if (facingController == null && cameraRoot != null && cameraRoot.FollowTarget != null)
            {
                facingController = cameraRoot.FollowTarget.GetComponentInParent<CharacterFacingController>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
