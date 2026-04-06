using Game.Definition.ConfigSystem.Core;
using Game.Foundation.Events;
using Game.Gameplay.Weapon;
using Game.Gameplay.Character.Events;
using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色域唯一主驱动。
    ///
    /// 主链固定顺序：
    /// 1. 推进武器已提交事实（避免换弹完成滞后一帧）
    /// 2. 收集原始输入
    /// 3. 裁决当前帧计划
    /// 4. 显式执行 Facing / Movement / Action
    /// 5. 消费已提交后坐力增量
    /// 6. 统一写回长期事实
    /// 7. 同步只读表现态
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterRoot : MonoBehaviour
    {
        #region Inspector References

        [Header("Core References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private CharacterInputRouter inputRouter;
        [SerializeField] private CharacterFacts facts;
        [SerializeField] private CharacterStat stat;
        [SerializeField] private CharacterViewState viewState;
        [SerializeField] private CharacterDecisionResolver decisionResolver;
        [SerializeField] private CharacterFacingController facingController;
        [SerializeField] private CharacterMovementController movementController;
        [SerializeField] private CharacterActionController actionController;
        [SerializeField] private Game.Gameplay.Combat.HealthComponent healthComponent;

        #endregion

        #region Public Accessors

        public CharacterController CharacterController => characterController;
        public CharacterFacts Facts => facts;
        public CharacterStat Stat => stat;
        public CharacterViewState ViewState => viewState;
        public CharacterFacingController FacingController => facingController;
        public CharacterActionController ActionController => actionController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
            facts?.InitializeDefaults();
        }

        private void Start()
        {
            stat.TryInitialize(ConfigService.Active);
            healthComponent.TryInitialize();
        }

        private void Update()
        {
            if (CheckSetupReady() == false)
            {
                return;
            }

            // ① 先推进 Weapon 已提交事实，让本帧裁决拿到最新的换弹完成状态。
            actionController.PreTickCurrentWeapon(Time.time, Time.deltaTime);

            // ② 收集统一语义化输入。
            InputContext rawInput = CollectRawInput();

            // ③ 裁决本帧执行计划。
            CharacterFramePlan plan = decisionResolver.Resolve(rawInput);

            // ④ 按固定顺序执行。
            facingController.Execute(plan);
            movementController.Execute(plan, facts);
            actionController.Execute(plan, facts, Time.time);

            // ⑤ 消费本帧已提交的真实后坐力增量（如果有的话）。
            if (actionController.CurrentWeaponRuntime != null
                && actionController.CurrentWeaponRuntime.ConsumePendingRecoil(out float recoilPitch, out float recoilYaw))
            {
                facingController.ApplyRecoil(recoilPitch, recoilYaw);
            }

            // ⑥ 统一写回长期事实。
            WriteBackFacts(plan);

            // ⑦ 同步只读表现态。
            SyncViewState(plan);
        }

        #endregion

        #region Setup

        /// <summary>
        /// 检查角色主链所需的核心引用是否全部就位。
        /// </summary>
        public bool CheckSetupReady()
        {
            return characterController != null
                && inputRouter != null
                && facts != null
                && stat != null
                && stat.IsInitialized
                && viewState != null
                && decisionResolver != null
                && facingController != null
                && movementController != null
                && actionController != null;
        }

        #endregion

        #region Main Chain Steps

        /// <summary>
        /// 通过 InputRouter 收集本帧统一语义化输入。
        /// </summary>
        private InputContext CollectRawInput()
        {
            if (inputRouter == null)
            {
                return InputContext.Empty;
            }

            return inputRouter.TryBuildRawInputContext(out InputContext rawInput)
                ? rawInput
                : InputContext.Empty;
        }

        /// <summary>
        /// 角色域唯一事实提交点。
        /// 执行器只暴露少量只读执行结果；最终长期事实统一在这里收口，避免同一帧被多处来回改写。
        /// </summary>
        private void WriteBackFacts(CharacterFramePlan _plan)
        {
            // Grounded 必须在 Move 后读取 CharacterController 结果，才能拿到最新碰撞解算后的落地事实。
            bool isGrounded = characterController.isGrounded && !movementController.JumpStartedThisFrame;
            bool isReloading = actionController.IsWeaponReloading;
            float planarSpeed = movementController.CurrentPlanarVelocity.magnitude;
            float verticalSpeed = movementController.CurrentVerticalSpeed;

            facts.SetGrounded(isGrounded);
            facts.SetMoving(planarSpeed > 0.0001f);
            facts.SetPlanarSpeed(planarSpeed);
            facts.SetVerticalSpeed(verticalSpeed);
            facts.SetReloading(isReloading);
            facts.SetAiming(_plan.AimActive && !isReloading);
            facts.SetSprinting(_plan.SprintActive && isGrounded && !isReloading);
            facts.SetJumping(!isGrounded && verticalSpeed > 0.05f);
        }

        /// <summary>
        /// 同步只读表现态，供 Camera / Animation / HUD 统一消费。
        /// </summary>
        private void SyncViewState(CharacterFramePlan _plan)
        {
            viewState.Sync(
                facts,
                _plan,
                facingController.CurrentYaw,
                facingController.CurrentPitch,
                actionController.FireTriggeredThisFrame,
                actionController.IsFiring);
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (inputRouter == null)
            {
                inputRouter = GetComponent<CharacterInputRouter>();
            }

            if (facts == null)
            {
                facts = GetComponent<CharacterFacts>();
            }

            if (stat == null)
            {
                stat = GetComponent<CharacterStat>();
            }

            if (viewState == null)
            {
                viewState = GetComponent<CharacterViewState>();
            }

            if (decisionResolver == null)
            {
                decisionResolver = GetComponent<CharacterDecisionResolver>();
            }

            if (facingController == null)
            {
                facingController = GetComponent<CharacterFacingController>();
            }

            if (movementController == null)
            {
                movementController = GetComponent<CharacterMovementController>();
            }

            if (actionController == null)
            {
                actionController = GetComponent<CharacterActionController>();
            }

            if (healthComponent == null)
            {
                healthComponent = GetComponent<Game.Gameplay.Combat.HealthComponent>();
            }
        }

        #endregion
    }
}
