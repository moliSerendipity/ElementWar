using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色长期运行时事实。
    /// 只保存已经提交的角色事实状态，不保存请求、计划和门控中间结果。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterFacts : MonoBehaviour
    {
        [SerializeField] private bool isGrounded = true;
        [SerializeField] private bool isMoving;
        [SerializeField] private bool isSprinting;
        [SerializeField] private bool isAiming;
        [SerializeField] private bool isReloading;
        [SerializeField] private bool isJumping;
        [SerializeField] private bool isDead;
        [SerializeField] private bool isInputBlocked;
        [SerializeField] private bool isControlLocked;
        [SerializeField] private bool allowSprint;
        [SerializeField] private float planarSpeed;
        [SerializeField] private float verticalSpeed;

        public bool IsGrounded => isGrounded;
        public bool IsAirborne => !isGrounded;
        public bool IsMoving => isMoving;
        public bool IsSprinting => isSprinting;
        public bool IsAiming => isAiming;
        public bool IsReloading => isReloading;
        public bool IsJumping => isJumping;
        public bool IsDead => isDead;
        public bool IsInputBlocked => isInputBlocked;
        public bool IsControlLocked => isControlLocked;
        public bool AllowSprint => allowSprint;
        public float PlanarSpeed => planarSpeed;
        public float VerticalSpeed => verticalSpeed;

        /// <summary>
        /// 初始化默认事实。
        /// 当前版本默认角色出生时处于地面可控状态。
        /// </summary>
        public void InitializeDefaults()
        {
            isGrounded = true;
            isMoving = false;
            isSprinting = false;
            isAiming = false;
            isReloading = false;
            isJumping = false;
            isDead = false;
            isInputBlocked = false;
            isControlLocked = false;
            allowSprint = true;
            planarSpeed = 0f;
            verticalSpeed = 0f;
        }

        public void SetGrounded(bool _value)
        {
            isGrounded = _value;
            if (_value)
            {
                isJumping = false;
            }
        }

        public void SetMoving(bool _value) => isMoving = _value;
        public void SetSprinting(bool _value) => isSprinting = _value;
        public void SetAiming(bool _value) => isAiming = _value;
        public void SetReloading(bool _value) => isReloading = _value;
        public void SetJumping(bool _value) => isJumping = _value;
        public void SetDead(bool _value) => isDead = _value;
        public void SetInputBlocked(bool _value) => isInputBlocked = _value;
        public void SetControlLocked(bool _value) => isControlLocked = _value;
        public void SetAllowSprint(bool _value) => allowSprint = _value;
        public void SetPlanarSpeed(float _value) => planarSpeed = Mathf.Max(0f, _value);
        public void SetVerticalSpeed(float _value) => verticalSpeed = _value;
    }
}
