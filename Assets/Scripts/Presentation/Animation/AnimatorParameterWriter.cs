using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.Animation
{
    /// <summary>
    /// Animator 参数统一写入器。
    /// 负责缓存参数哈希，并统一封装 Float / Bool / Trigger / LayerWeight 的写入，
    /// 避免桥接脚本散落字符串常量和重复 SetXxx 调用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimatorParameterWriter : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Parameter Names")]
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
        [SerializeField] private string inputXParameter = "InputX";
        [SerializeField] private string inputZParameter = "InputZ";
        [SerializeField] private string reloadMultiplierParameter = "ReloadMultiplier";
        [SerializeField] private string leftHandWeightParameter = "LeftHandWeight";
        [SerializeField] private string isGroundedParameter = "IsGrounded";
        [SerializeField] private string isAimingParameter = "IsAiming";
        [SerializeField] private string isFiringParameter = "IsFiring";
        [SerializeField] private string isReloadingParameter = "IsReloading";
        [SerializeField] private string isDeadParameter = "IsDead";
        [SerializeField] private string jumpTriggerParameter = "JumpTrigger";
        [SerializeField] private string fireTriggerParameter = "FireTrigger";
        [SerializeField] private string reloadTriggerParameter = "ReloadTrigger";
        [SerializeField] private string hitTriggerParameter = "HitTrigger";

        [Header("Layer Names")]
        [SerializeField] private string upperBodyLayerName = "UpperBody Layer";
        [SerializeField] private string additiveLayerName = "Additive Layer";

        private int moveSpeedHash;
        private int verticalSpeedHash;
        private int inputXHash;
        private int inputZHash;
        private int reloadMultiplierHash;
        private int leftHandWeightHash;
        private int isGroundedHash;
        private int isAimingHash;
        private int isFiringHash;
        private int isReloadingHash;
        private int isDeadHash;
        private int jumpTriggerHash;
        private int fireTriggerHash;
        private int reloadTriggerHash;
        private int hitTriggerHash;
        private int upperBodyLayerIndex = -1;
        private int additiveLayerIndex = -1;

        public Animator Animator => animator;
        public int UpperBodyLayerIndex => upperBodyLayerIndex;
        public int AdditiveLayerIndex => additiveLayerIndex;

        private void Awake()
        {
            ResolveReferences();
            RebuildHashes();
        }

        /// <summary>
        /// 读取 Animator 中由动画曲线驱动的 LeftHandWeight。
        /// 若参数不存在，则返回调用方提供的默认值。
        /// </summary>
        public float ReadLeftHandWeight(float _defaultValue = 0f)
        {
            if (animator == null)
            {
                return _defaultValue;
            }

            return animator.GetFloat(leftHandWeightHash);
        }

        /// <summary>
        /// 统一写入 locomotion 参数。
        /// InputX / InputZ 表达相对于镜头前方的平面输入，MoveSpeed 表达当前实际平面速度值。
        /// </summary>
        public void WriteLocomotion(float _moveSpeed, float _inputX, float _inputZ, float _verticalSpeed, bool _isGrounded, bool _isAiming)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(moveSpeedHash, _moveSpeed);
            animator.SetFloat(verticalSpeedHash, _verticalSpeed);
            animator.SetFloat(inputXHash, _inputX);
            animator.SetFloat(inputZHash, _inputZ);
            animator.SetBool(isGroundedHash, _isGrounded);
            animator.SetBool(isAimingHash, _isAiming);
        }

        public void WriteReloadMultiplier(float _reloadMultiplier)
        {
            if (animator != null)
            {
                animator.SetFloat(reloadMultiplierHash, _reloadMultiplier);
            }
        }

        public void WriteIsFiring(bool _isFiring)
        {
            if (animator != null)
            {
                animator.SetBool(isFiringHash, _isFiring);
            }
        }

        public void WriteIsReloading(bool _isReloading)
        {
            if (animator != null)
            {
                animator.SetBool(isReloadingHash, _isReloading);
            }
        }

        public void WriteIsDead(bool _isDead)
        {
            if (animator != null)
            {
                animator.SetBool(isDeadHash, _isDead);
            }
        }

        /// <summary>
        /// 写入单帧 Trigger。
        /// 动画 Trigger 只用于表现层进入时机，不决定逻辑是否成立。
        /// </summary>
        public void WriteJumpTrigger()
        {
            if (animator != null)
            {
                animator.SetTrigger(jumpTriggerHash);
            }
        }

        public void WriteFireTrigger()
        {
            if (animator != null)
            {
                animator.SetTrigger(fireTriggerHash);
            }
        }

        public void WriteReloadTrigger()
        {
            if (animator != null)
            {
                animator.SetTrigger(reloadTriggerHash);
            }
        }

        public void WriteHitTrigger()
        {
            if (animator != null)
            {
                animator.SetTrigger(hitTriggerHash);
            }
        }

        /// <summary>
        /// 统一写入 UpperBody / Additive 层权重。
        /// 具体层名可在 Inspector 中和当前 Animator Controller 对齐。
        /// </summary>
        public void WriteLayerWeights(float _upperBodyWeight, float _additiveWeight)
        {
            if (animator == null)
            {
                return;
            }

            if (upperBodyLayerIndex >= 0)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, Mathf.Clamp01(_upperBodyWeight));
            }

            if (additiveLayerIndex >= 0)
            {
                animator.SetLayerWeight(additiveLayerIndex, Mathf.Clamp01(_additiveWeight));
            }
        }

        private void ResolveReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void RebuildHashes()
        {
            moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
            verticalSpeedHash = Animator.StringToHash(verticalSpeedParameter);
            inputXHash = Animator.StringToHash(inputXParameter);
            inputZHash = Animator.StringToHash(inputZParameter);
            reloadMultiplierHash = Animator.StringToHash(reloadMultiplierParameter);
            leftHandWeightHash = Animator.StringToHash(leftHandWeightParameter);

            isGroundedHash = Animator.StringToHash(isGroundedParameter);
            isAimingHash = Animator.StringToHash(isAimingParameter);
            isFiringHash = Animator.StringToHash(isFiringParameter);
            isReloadingHash = Animator.StringToHash(isReloadingParameter);
            isDeadHash = Animator.StringToHash(isDeadParameter);

            fireTriggerHash = Animator.StringToHash(fireTriggerParameter);
            reloadTriggerHash = Animator.StringToHash(reloadTriggerParameter);
            jumpTriggerHash = Animator.StringToHash(jumpTriggerParameter);

            if (animator != null)
            {
                upperBodyLayerIndex = string.IsNullOrWhiteSpace(upperBodyLayerName) ? -1 : animator.GetLayerIndex(upperBodyLayerName);
                additiveLayerIndex = string.IsNullOrWhiteSpace(additiveLayerName) ? -1 : animator.GetLayerIndex(additiveLayerName);
            }
            else
            {
                upperBodyLayerIndex = -1;
                additiveLayerIndex = -1;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            RebuildHashes();
        }
#endif
    }
}
