using Game.Gameplay.Camera;
using Game.Gameplay.Character;
using Game.Gameplay.Weapon;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game.Presentation.Animation
{
    /// <summary>
    /// 角色动画桥接器。
    ///
    /// 职责只保留三件事：
    /// 1. 把 CharacterViewState 写入 Animator 参数；
    /// 2. 维护 Reload 期间的弹匣父子关系与左手 IK 权重；
    /// 3. 让 Aim Target 与对枪 Rig 稳定追随逻辑瞄点。
    ///
    /// 当前版本明确不在这里做武器逻辑、伤害逻辑或相机状态裁决。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimatorParameterWriter))]
    public sealed class CharacterAnimationBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterRoot characterRoot;
        [SerializeField] private AnimatorParameterWriter parameterWriter;

        [Header("Locomotion Smoothing")]
        [SerializeField, Min(0f)] private float moveSpeedSmoothTime = 0.08f;
        [SerializeField, Min(0f)] private float inputSmoothTime = 0.08f;

        [Header("Reload Presentation")]
        [SerializeField] private Transform magazineTransform;
        [SerializeField] private Transform leftHandMagazineTransform;
        [SerializeField] private Transform leftHandTransform;

        [Header("Aim Target")]
        [SerializeField] private Transform aimTarget;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private MonoBehaviour cameraAimPointProviderBehaviour;
        [SerializeField, Min(0.1f)] private float minAimTargetDistance = 1.5f;

        [Header("Rig")]
        [SerializeField] private TwoBoneIKConstraint rightHandIKConstraint;
        [SerializeField] private MultiAimConstraint bodyAimConstraint;
        [SerializeField] private MultiAimConstraint rightHandAimConstraint;
        [SerializeField] private TwoBoneIKConstraint leftHandIKConstraint;
        [SerializeField, Min(0f)] private float rigWeightBlendSpeed = 5f;

        [Header("Layer Weight")]
        [SerializeField, Range(0f, 1f)] private float upperBodyLayerWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float additiveLayerWeight = 1f;

        private ICameraAimPointProvider cameraAimPointProvider;
        private Transform cachedMagazineOriginalParent;
        private Vector3 cachedMagazineOriginalLocalPosition;
        private Quaternion cachedMagazineOriginalLocalRotation;
        private bool isMagazineAttachedToLeftHand;

        private float currentMoveSpeed;
        private float currentInputX;
        private float currentInputZ;
        private float moveSpeedVelocity;
        private float inputXVelocity;
        private float inputZVelocity;

        private float originalReloadClipLength;

        private bool previousReloading;

        private void Awake()
        {
            ResolveReferences();
            ResolveCameraAimPointProvider();

            if (magazineTransform != null)
            {
                cachedMagazineOriginalParent = magazineTransform.parent;
                cachedMagazineOriginalLocalPosition = magazineTransform.localPosition;
                cachedMagazineOriginalLocalRotation = magazineTransform.localRotation;
            }
        }

        private void Start()
        {
            AnimationClip[] clips = parameterWriter.Animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; ++i)
            {
                if (clips[i] != null && clips[i].name.Equals("Reload", System.StringComparison.OrdinalIgnoreCase))
                {
                    originalReloadClipLength = clips[i].length;
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            if (parameterWriter == null || characterRoot == null || characterRoot.ViewState == null)
            {
                return;
            }

            CharacterViewState characterViewState = characterRoot.ViewState;
            WeaponViewState weaponViewState = characterRoot.ActionController.CurrentWeaponViewState;
            float reloadMultiplier = weaponViewState.IsReloading && weaponViewState.ActualReloadDuration != 0 ?
                originalReloadClipLength / weaponViewState.ActualReloadDuration : 1f;

            UpdateLocomotionParameters(characterViewState);
            UpdateActionTriggers(characterViewState);
            parameterWriter.WriteReloadMultiplier(reloadMultiplier);
            parameterWriter.WriteIsFiring(characterViewState.IsFiring && !characterViewState.IsReloading);
            parameterWriter.WriteIsReloading(characterViewState.IsReloading);
            parameterWriter.WriteLayerWeights(upperBodyLayerWeight, additiveLayerWeight);

            UpdateAimTarget();
            UpdateAimConstraintWeights(characterViewState);
            UpdateLeftHandIKWeight();

            previousReloading = characterViewState.IsReloading;
        }

        private void UpdateLocomotionParameters(CharacterViewState _state)
        {
            currentMoveSpeed = Mathf.SmoothDamp(currentMoveSpeed, _state.PlanarSpeed, ref moveSpeedVelocity, moveSpeedSmoothTime);
            currentInputX = Mathf.SmoothDamp(currentInputX, _state.InputX, ref inputXVelocity, inputSmoothTime);
            currentInputZ = Mathf.SmoothDamp(currentInputZ, _state.InputZ, ref inputZVelocity, inputSmoothTime);

            parameterWriter.WriteLocomotion(
                currentMoveSpeed,
                currentInputX,
                currentInputZ,
                _state.VerticalSpeed,
                _state.IsGrounded,
                _state.IsAiming);

            parameterWriter.WriteIsDead(_state.IsDead);
        }

        private void UpdateActionTriggers(CharacterViewState _state)
        {
            if (_state.FireTriggeredThisFrame)
            {
                parameterWriter.WriteFireTrigger();
            }

            if (_state.IsReloading && previousReloading == false)
            {
                parameterWriter.WriteReloadTrigger();
            }

            if (_state.JumpTriggeredThisFrame)
            {
                parameterWriter.WriteJumpTrigger();
            }
        }

        /// <summary>
        /// 统一刷新 Aim Target 世界坐标。
        ///
        /// 注意：
        /// 1. 这里更新的是 Rig 目标，不改角色根节点俯仰；
        /// 2. 逻辑瞄点统一来自 CameraAimPointResolver；
        /// 3. 对过近瞄点做最小距离钳制，避免约束翻折或抽搐。
        /// </summary>
        private void UpdateAimTarget()
        {
            if (aimTarget == null || cameraAimPointProvider == null)
            {
                return;
            }
            if (cameraAimPointProvider.TryGetCameraAimPointContext(out CameraAimPointContext aimPointContext) == false)
            {
                return;
            }

            Vector3 targetPoint = aimPointContext.AimPoint;
            Transform referenceOrigin = aimOrigin != null ? aimOrigin : transform;
            Vector3 fromOrigin = targetPoint - referenceOrigin.position;

            // 防止瞄点落在角色身边或身体内部时，Rig 为了强行对准近点产生明显抖动。
            if (fromOrigin.sqrMagnitude < minAimTargetDistance * minAimTargetDistance)
            {
                if (fromOrigin.sqrMagnitude <= 0.0001f)
                {
                    fromOrigin = referenceOrigin.forward;
                }

                targetPoint = referenceOrigin.position + fromOrigin.normalized * minAimTargetDistance;
            }

            aimTarget.position = targetPoint;
        }

        private void UpdateAimConstraintWeights(CharacterViewState _state)
        {
            float targetRightHandIkWeight;
            float targetBodyAimConstraintWeight;
            float targetRightHandAimConstraintWeight;

            if (_state.IsReloading || _state.IsSprinting)
            {
                targetRightHandIkWeight = 1f;
                targetBodyAimConstraintWeight = 0;
                targetRightHandAimConstraintWeight = 0;
            }
            else if (_state.IsAiming)
            {
                targetRightHandIkWeight = 1f;
                targetBodyAimConstraintWeight = 1;
                targetRightHandAimConstraintWeight = 1;
            }
            else if (_state.IsFiring)
            {
                targetRightHandIkWeight = 0f;
                targetBodyAimConstraintWeight = 0;
                targetRightHandAimConstraintWeight = 1;
            }
            else if (_state.PlanarSpeed > 0.1f)
            {
                targetRightHandIkWeight = 0f;
                targetBodyAimConstraintWeight = 1;
                targetRightHandAimConstraintWeight = 1;
            }
            else
            {
                targetRightHandIkWeight = 1f;
                targetBodyAimConstraintWeight = 0;
                targetRightHandAimConstraintWeight = 0;
            }

            UpdateConstraintWeight(rightHandIKConstraint, targetRightHandIkWeight);
            UpdateConstraintWeight(bodyAimConstraint, targetBodyAimConstraintWeight);
            UpdateConstraintWeight(rightHandAimConstraint, targetRightHandAimConstraintWeight);
        }

        private void UpdateConstraintWeight(TwoBoneIKConstraint _constraint, float _targetWeight)
        {
            if (_constraint == null)
            {
                return;
            }
            _constraint.weight = Mathf.MoveTowards(
                _constraint.weight,
                Mathf.Clamp01(_targetWeight),
                rigWeightBlendSpeed * Time.deltaTime);
        }

        private void UpdateConstraintWeight(MultiAimConstraint _constraint, float _targetWeight)
        {
            if (_constraint == null)
            {
                return;
            }

            _constraint.weight = Mathf.MoveTowards(
                _constraint.weight,
                Mathf.Clamp01(_targetWeight),
                rigWeightBlendSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Reload 动画事件：把弹匣临时切到左手。
        /// 这里只处理纯表现层父子关系切换，不改变任何 Gameplay 结果。
        /// </summary>
        public void RemoveMagazine()
        {
            if (magazineTransform == null || leftHandTransform == null || leftHandMagazineTransform == null)
            {
                return;
            }

            if (isMagazineAttachedToLeftHand)
            {
                return;
            }

            cachedMagazineOriginalParent = magazineTransform.parent;
            cachedMagazineOriginalLocalPosition = magazineTransform.localPosition;
            cachedMagazineOriginalLocalRotation = magazineTransform.localRotation;

            magazineTransform.parent = leftHandTransform;
            magazineTransform.localPosition = leftHandMagazineTransform.localPosition;
            magazineTransform.localRotation = leftHandMagazineTransform.localRotation;
            isMagazineAttachedToLeftHand = true;
        }

        public void InsertMagazine()
        {
            if (magazineTransform == null || isMagazineAttachedToLeftHand == false)
            {
                return;
            }

            magazineTransform.parent = cachedMagazineOriginalParent;
            magazineTransform.localPosition = cachedMagazineOriginalLocalPosition;
            magazineTransform.localRotation = cachedMagazineOriginalLocalRotation;
            isMagazineAttachedToLeftHand = false;
        }

        private void UpdateLeftHandIKWeight()
        {
            if (leftHandIKConstraint == null || parameterWriter == null)
            {
                return;
            }

            leftHandIKConstraint.weight = Mathf.Clamp01(parameterWriter.ReadLeftHandWeight(leftHandIKConstraint.weight));
        }

        private bool ResolveCameraAimPointProvider()
        {
            if (cameraAimPointProviderBehaviour is ICameraAimPointProvider _provider)
            {
                cameraAimPointProvider = _provider;
                return true;
            }
            cameraAimPointProvider = null;
            return false;
        }

        private void ResolveReferences()
        {
            if (parameterWriter == null)
            {
                parameterWriter = GetComponent<AnimatorParameterWriter>();
            }

            if (characterRoot == null)
            {
                characterRoot = GetComponentInParent<CharacterRoot>();
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
