using Game.Definition.ConfigSystem.Core;
using Game.Foundation.Events;
using Game.Gameplay.Weapon;
using Game.Gameplay.Character.Events;
using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色域唯一主驱动
    ///
    /// 主链固定为
    /// 1. 收集原始输入
    /// 2. 裁决当前帧计划
    /// 3. 显式执行 Facing / Movement / Action
    /// 4. 统一提交长期事实
    /// 5. 同步只读表现态
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterRoot : MonoBehaviour
    {
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
        [SerializeField] private Game.Gameplay.Combat.HealthComponent damageReceiver;

        public CharacterController CharacterController => characterController;
        public CharacterFacts Facts => facts;
        public CharacterStat Stat => stat;
        public CharacterViewState ViewState => viewState;
        public CharacterFacingController FacingController => facingController;
        public CharacterActionController ActionController => actionController;

        private void Awake()
        {
            ResolveReferences();
            facts?.InitializeDefaults();
        }

        private void Start()
        {
            stat.TryInitialize(ConfigService.Active);
            damageReceiver.TryInitialize(ConfigService.Active);
        }

        private void Update()
        {
            if (CheckSetupReady() == false)
            {
                return;
            }

            // 先推进 Weapon 已提交事实，再裁决 Character 本帧计划，避免“换弹刚完成但 Character 还按旧状态裁决”的一帧滞后。
            actionController.PreTickCurrentWeapon(Time.time);

            InputContext rawInput = CollectRawInput();
            CharacterFramePlan plan = decisionResolver.Resolve(rawInput);

            facingController.Execute(plan);
            movementController.Execute(plan, facts);
            actionController.Execute(plan, facts, Time.time);

            // 当前帧只消费一次已提交增量；若没有开火成立，就不会拿到任何真实后坐力。
            if (actionController.CurrentWeaponRuntime.ConsumePendingRecoil(out float recoilPitch, out float recoilYaw))
            {
                facingController.ApplyRecoil(recoilPitch, recoilYaw);
            }

            WriteBackFacts(plan);
            SyncViewState(plan);
        }

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
        /// 执行器只暴露少量执行结果；最终长期事实统一在这里收口，避免同一帧被多处来回改写。
        /// </summary>
        private void WriteBackFacts(CharacterFramePlan _plan)
        {
            bool wasAiming = facts.IsAiming;
            bool wasSprinting = facts.IsSprinting;

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

            //PublishFactEvents(wasAiming, wasSprinting);
        }

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

        //private void PublishFactEvents(bool _wasAiming, bool _wasSprinting)
        //{
        //    GameEventBus eventBus = GameEventBus.Instance;
        //    if (eventBus == null)
        //    {
        //        return;
        //    }

        //    if (_wasAiming != facts.IsAiming)
        //    {
        //        eventBus.Publish(new AimStateChangedEvent(gameObject, facts.IsAiming));
        //    }

        //    if (_wasSprinting != facts.IsSprinting)
        //    {
        //        eventBus.Publish(new SprintStateChangedEvent(gameObject, facts.IsSprinting));
        //    }
        //}

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

            if (damageReceiver == null)
            {
                damageReceiver = GetComponent<Game.Gameplay.Combat.HealthComponent>();
            }
        }
    }
}
