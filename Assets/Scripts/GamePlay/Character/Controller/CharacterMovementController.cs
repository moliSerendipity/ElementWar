using Game.Gameplay.Camera;
using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色移动执行器。
    /// 只消费 CharacterFramePlan 和已提交 CharacterFacts，不再自己做第二套合法性裁决。
    ///
    /// 边界：
    /// 1. 这里只负责执行移动、跳跃与重力推进；
    /// 2. 这里只暴露少量只读执行结果，不直接提交 CharacterFacts；
    /// 3. CharacterFacts 的最终写入必须统一回到 CharacterRoot.WriteBackFacts()。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private CharacterStat stat;
        [SerializeField] private CharacterFacingController facingController;

        [Space]
        [SerializeField] private float groundedStickVelocity = -2f;

        private Vector3 currentPlanarVelocity;
        private float currentVerticalSpeed;
        private bool jumpStartedThisFrame;

        public Vector3 CurrentPlanarVelocity => currentPlanarVelocity;
        public float CurrentVerticalSpeed => currentVerticalSpeed;
        public bool JumpStartedThisFrame => jumpStartedThisFrame;

        private void Awake()
        {
            ResolveReferences();
        }

        public void Execute(in CharacterFramePlan _plan, CharacterFacts _facts)
        {
            if (_facts == null || stat == null || stat.IsInitialized == false)
            {
                currentPlanarVelocity = Vector3.zero;
                currentVerticalSpeed = 0f;
                jumpStartedThisFrame = false;
                return;
            }

            jumpStartedThisFrame = false;

            UpdateVerticalMotion(_plan, _facts.IsGrounded);
            currentPlanarVelocity = BuildHorizontalVelocity(_plan, _facts.IsReloading);

            Vector3 displacement = (currentPlanarVelocity + Vector3.up * currentVerticalSpeed) * Time.deltaTime;
            characterController.Move(displacement);
        }

        private Vector3 BuildHorizontalVelocity(in CharacterFramePlan _plan, bool _isReloading)
        {
            Vector2 moveVector = Vector2.ClampMagnitude(_plan.MoveVector, 1f);
            if (moveVector.sqrMagnitude <= 0f)
            {
                return Vector3.zero;
            }

            float viewYaw = facingController != null ? facingController.CurrentYaw : transform.rotation.y;
            Quaternion viewRotation = Quaternion.Euler(0f, viewYaw, 0f);
            Vector3 planarDirection = viewRotation * new Vector3(moveVector.x, 0f, moveVector.y);
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude > 1f)
            {
                planarDirection.Normalize();
            }

            return planarDirection * ResolveMoveSpeed(_plan, _isReloading);
        }

        private float ResolveMoveSpeed(in CharacterFramePlan _plan, bool _isReloading)
        {
            if (_isReloading)
            {
                return stat.RunSpeed;
            }

            if (_plan.AimActive)
            {
                return stat.RunSpeed * stat.AimMoveSpeedMultiplier;
            }

            if (_plan.SprintActive)
            {
                return stat.SprintSpeed;
            }

            return stat.RunSpeed;
        }

        private void UpdateVerticalMotion(in CharacterFramePlan _plan, bool _isGrounded)
        {
            SyncGroundedVerticalSpeed(_isGrounded);
            TryStartJump(_plan, _isGrounded);
            ApplyGravity(_isGrounded);
        }

        private void SyncGroundedVerticalSpeed(bool _isGrounded)
        {
            if (_isGrounded && currentVerticalSpeed < 0f)
            {
                currentVerticalSpeed = groundedStickVelocity;
            }
        }

        private void TryStartJump(in CharacterFramePlan _plan, bool _isGrounded)
        {
            if (_plan.JumpTriggered == false || _isGrounded == false)
            {
                return;
            }

            currentVerticalSpeed = Mathf.Sqrt(stat.JumpHeight * -2f * stat.Gravity);
            jumpStartedThisFrame = true;
        }

        private void ApplyGravity(bool _isGrounded)
        {
            if (_isGrounded && currentVerticalSpeed <= groundedStickVelocity)
            {
                return;
            }

            currentVerticalSpeed += stat.Gravity * Time.deltaTime;
            currentVerticalSpeed = Mathf.Max(currentVerticalSpeed, stat.MaxFallSpeed);
        }

        private void ResolveReferences()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (stat == null)
            {
                stat = GetComponent<CharacterStat>();
            }

            if (facingController == null)
            {
                facingController = GetComponent<CharacterFacingController>();
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
