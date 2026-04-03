using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色只读表现态。
    /// Presentation 统一读取这里，而不是下钻 Gameplay 内部控制器临时状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterViewState : MonoBehaviour
    {
        [SerializeField] private float currentYaw;
        [SerializeField] private float currentPitch;
        [SerializeField] private float planarSpeed;
        [SerializeField] private float verticalSpeed;
        [SerializeField] private float inputX;
        [SerializeField] private float inputZ;
        [SerializeField] private bool isGrounded = true;
        [SerializeField] private bool isSprinting;
        [SerializeField] private bool isAiming;
        [SerializeField] private bool isFiring;
        [SerializeField] private bool isReloading;
        [SerializeField] private bool isDead;
        [SerializeField] private bool jumpTriggeredThisFrame;
        [SerializeField] private bool fireTriggeredThisFrame;

        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;
        public float PlanarSpeed => planarSpeed;
        public float VerticalSpeed => verticalSpeed;
        public float InputX => inputX;
        public float InputZ => inputZ;
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsAiming => isAiming;
        public bool IsFiring => isFiring;
        public bool IsReloading => isReloading;
        public bool IsDead => isDead;
        public bool JumpTriggeredThisFrame => jumpTriggeredThisFrame;
        public bool FireTriggeredThisFrame => fireTriggeredThisFrame;

        /// <summary>
        /// 由 CharacterRoot 在主链末尾统一刷新表现态。
        /// </summary>
        public void Sync(
            CharacterFacts _facts,
            CharacterFramePlan _plan,
            float _yaw,
            float _pitch,
            bool _fireTriggeredThisFrame,
            bool _isFiring)
        {
            if (_facts == null)
            {
                return;
            }

            currentYaw = _yaw;
            currentPitch = _pitch;
            planarSpeed = _facts.PlanarSpeed;
            verticalSpeed = _facts.VerticalSpeed;
            inputX = _plan.MoveVector.x;
            inputZ = _plan.MoveVector.y;
            isGrounded = _facts.IsGrounded;
            isSprinting = _facts.IsSprinting;
            isAiming = _facts.IsAiming;
            isReloading = _facts.IsReloading;
            isFiring = _isFiring;
            isDead = _facts.IsDead;
            jumpTriggeredThisFrame = _plan.JumpTriggered;
            fireTriggeredThisFrame = _fireTriggeredThisFrame;
        }
    }
}
