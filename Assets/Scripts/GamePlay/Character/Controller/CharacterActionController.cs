using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色动作执行器。
    /// 只负责把角色域已裁决结果转成跨域请求，并读取当前装备武器的已提交事实。
    ///
    /// 约束：
    /// 1. 它不显式推进 Weapon 域内部子阶段，只把请求交给当前装备武器；
    /// 2. 它不直接写 CharacterFacts；
    /// 3. 它不再通过“在子节点里找第一个 WeaponRuntime”来隐式决定当前武器；
    /// 4. 当前装备武器必须通过显式绑定入口设置，避免后续多武器场景下拿错对象。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterActionController : MonoBehaviour
    {
        [SerializeField] private WeaponRuntime currentWeaponRuntime;

        public WeaponRuntime CurrentWeaponRuntime => currentWeaponRuntime;
        public WeaponViewState CurrentWeaponViewState => currentWeaponRuntime != null ? currentWeaponRuntime.WeaponViewState : null;

        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool FireRequested { get; private set; }
        public bool ReloadRequested { get; private set; }
        public bool SkillRequested { get; private set; }
        public bool SwitchRequested { get; private set; }

        public bool IsWeaponReloading => currentWeaponRuntime != null && currentWeaponRuntime.IsInitialized && currentWeaponRuntime.IsReloading;
        public bool FireTriggeredThisFrame => currentWeaponRuntime != null && currentWeaponRuntime.IsInitialized && currentWeaponRuntime.FireTriggeredThisFrame;
        public bool IsFiring => currentWeaponRuntime != null && currentWeaponRuntime.IsInitialized && currentWeaponRuntime.IsFiring;

        private void Awake()
        {
            ResetRequests();
        }

        /// <summary>
        /// 在 Character 裁决前，先推进当前武器的运行时事实。
        ///
        /// 目的：
        /// 1. 先把“换弹刚完成”的事实提交到 Weapon 域；
        /// 2. 让 CharacterDecisionResolver 读取到本帧最新的 IsWeaponReloading，避免门控晚一帧。
        ///
        /// 注意：这里只推进已提交的长期事实，不执行本帧新请求。
        /// </summary>
        public void PreTickCurrentWeapon(float _currentTime)
        {
            if (currentWeaponRuntime == null || currentWeaponRuntime.IsInitialized == false)
            {
                return;
            }

            currentWeaponRuntime.PreTickWeaponRuntimeFacts(_currentTime);
        }

        /// <summary>
        /// 执行当前帧动作，并把武器请求交给 Weapon 域自己的主链处理。
        /// </summary>
        public void Execute(in CharacterFramePlan _plan, CharacterFacts _facts, float _currentTime)
        {
            FireHeld = _plan.FireHeld;
            FirePressed = _plan.FirePressed;
            FireRequested = _plan.FireRequested;
            ReloadRequested = _plan.ReloadTriggered;
            SkillRequested = _plan.SkillTriggered;
            SwitchRequested = _plan.SwitchTriggered;

            if (currentWeaponRuntime == null || currentWeaponRuntime.IsInitialized == false)
            {
                return;
            }

            WeaponRequest weaponRequest = new(
                _plan.FirePressed,
                _plan.FireHeld,
                _plan.ReloadTriggered);

            currentWeaponRuntime.TickWeaponRuntime(weaponRequest, _facts, _currentTime);
        }

        /// <summary>
        /// 显式设置当前装备武器。
        /// 当前阶段先通过装配或切枪入口调用这里；后续真正接 WeaponSlotController 时，
        /// 也应该只通过这个入口切换当前武器，而不是让各系统自己搜索场景对象。
        /// </summary>
        public void SetCurrentWeaponRuntime(WeaponRuntime _weaponRuntime)
        {
            currentWeaponRuntime = _weaponRuntime;
        }

        public void ResetRequests()
        {
            FireHeld = false;
            FirePressed = false;
            FireRequested = false;
            ReloadRequested = false;
            SkillRequested = false;
            SwitchRequested = false;
        }
    }
}
