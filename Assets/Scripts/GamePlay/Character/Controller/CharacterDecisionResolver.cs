using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色域单一裁决点。
    /// 负责把 RawInput、CharacterFacts 与 Weapon 已提交事实收敛为 CharacterFramePlan。
    ///
    /// 注意：
    /// 1. 这里只做“这一帧允许做什么”的裁决；
    /// 2. 不在这里写 CharacterFacts；
    /// 3. Root 调用 Resolve 后，执行器通过显式参数吃 plan，不再自行回头读取 Resolver 当前状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterDecisionResolver : MonoBehaviour
    {
        [SerializeField] private CharacterFacts characterFacts;
        [SerializeField] private CharacterActionController actionController;

        private InputContext currentRawInput = InputContext.Empty;
        private CharacterFramePlan currentFramePlan = CharacterFramePlan.Empty;

        public InputContext CurrentRawInput => currentRawInput;
        public CharacterFramePlan CurrentFramePlan => currentFramePlan;

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>
        /// 解析当前帧计划，并返回显式结果给 Root 主链。
        /// </summary>
        public CharacterFramePlan Resolve(InputContext _rawInput)
        {
            currentRawInput = _rawInput;
            currentFramePlan = BuildPlan(_rawInput);
            return currentFramePlan;
        }

        private CharacterFramePlan BuildPlan(InputContext _rawInput)
        {
            if (characterFacts == null)
            {
                return CharacterFramePlan.Empty;
            }

            if (characterFacts.IsHealthDepleted || characterFacts.IsInputBlocked || characterFacts.IsControlLocked)
            {
                return CharacterFramePlan.Empty;
            }

            bool isReloading = actionController != null && actionController.IsWeaponReloading;
            Vector2 moveVector = Vector2.ClampMagnitude(_rawInput.MoveVector, 1f);
            Vector2 lookDelta = _rawInput.LookDelta;

            bool hasFireIntent = _rawInput.FireHeld || _rawInput.FirePressed;
            bool aimActive = !isReloading && _rawInput.AimHeld;
            bool sprintActive = !isReloading
                && !aimActive
                && !hasFireIntent
                && characterFacts.AllowSprint
                && _rawInput.SprintHeld
                && characterFacts.IsGrounded
                && moveVector.sqrMagnitude > 0f;

            bool jumpTriggered = _rawInput.JumpPressed && characterFacts.IsGrounded;
            bool firePressed = !isReloading && _rawInput.FirePressed;
            bool fireHeld = !isReloading && _rawInput.FireHeld;
            bool reloadTriggered = !isReloading && _rawInput.ReloadPressed;
            bool switchAmmoTriggered = !isReloading && _rawInput.SwitchAmmoPressed;
            bool switchTriggered = !isReloading && _rawInput.SwitchPressed;
            bool skillTriggered = !isReloading && _rawInput.SkillPressed;

            return new CharacterFramePlan(
                moveVector,
                lookDelta,
                aimActive,
                sprintActive,
                jumpTriggered,
                firePressed,
                fireHeld,
                reloadTriggered,
                switchAmmoTriggered,
                switchTriggered,
                skillTriggered);
        }

        private void ResolveReferences()
        {
            if (characterFacts == null)
            {
                characterFacts = GetComponent<CharacterFacts>();
            }

            if (actionController == null)
            {
                actionController = GetComponent<CharacterActionController>();
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
