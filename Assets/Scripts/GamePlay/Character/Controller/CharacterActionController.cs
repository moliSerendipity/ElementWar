using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色动作执行器。
    /// 只负责把角色域已裁决结果转成跨域请求，并读取当前装备武器的已提交事实。
    ///
    /// 约束：
    /// 1. 不显式推进 Weapon 域内部子阶段，只把请求交给当前装备武器
    /// 2. 不直接写 CharacterFacts
    /// 3. 当前装备武器必须通过显式绑定入口设置，避免后续多武器场景下拿错对象
    /// 4. 表现态（IsFiring）统一从 WeaponViewState 读取，不从 WeaponRuntime 读取
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterActionController : MonoBehaviour
    {
        [SerializeField] private WeaponRuntime currentWeaponRuntime;

        #region Public Accessors

        public WeaponRuntime CurrentWeaponRuntime => currentWeaponRuntime;

        public WeaponViewState CurrentWeaponViewState =>
            currentWeaponRuntime != null ? currentWeaponRuntime.WeaponViewState : null;

        /// <summary>本帧请求状态。供 CharacterRoot 写回 CharacterFacts 时读取。</summary>
        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool FireRequested { get; private set; }
        public bool ReloadRequested { get; private set; }
        public bool SkillRequested { get; private set; }
        public bool SwitchRequested { get; private set; }

        /// <summary>当前武器是否正在换弹。从 WeaponRuntime 运行时事实读取。</summary>
        public bool IsWeaponReloading =>
            currentWeaponRuntime != null
            && currentWeaponRuntime.IsInitialized
            && currentWeaponRuntime.IsReloading;

        /// <summary>本帧是否有开火成立。从 WeaponRuntime 运行时事实读取。</summary>
        public bool FireTriggeredThisFrame =>
            currentWeaponRuntime != null
            && currentWeaponRuntime.IsInitialized
            && currentWeaponRuntime.FireTriggeredThisFrame;

        /// <summary>
        /// 当前是否处于开火表现窗口。
        /// 从 WeaponViewState（表现态）读取，而非 WeaponRuntime（运行时事实）。
        /// </summary>
        public bool IsFiring
        {
            get
            {
                WeaponViewState viewState = CurrentWeaponViewState;
                return viewState != null && viewState.IsFiring;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResetRequests();
        }

        #endregion

        #region Pre-Tick

        /// <summary>
        /// 在 Character 裁决前，先推进当前武器的运行时事实和表现态。
        ///
        /// 目的：
        /// 1. 先把"换弹刚完成"的事实提交到 Weapon 域
        /// 2. 让 CharacterDecisionResolver 读取到本帧最新的 IsWeaponReloading，避免门控晚一帧
        /// 3. 刷新 WeaponViewState 的开火表现窗口到期状态
        /// </summary>
        /// <param name="_currentTime">当前帧时间（Time.time）。</param>
        /// <param name="_deltaTime">帧间隔（Time.deltaTime），用于换弹倒计时。</param>
        public void PreTickCurrentWeapon(float _currentTime, float _deltaTime)
        {
            if (currentWeaponRuntime == null || currentWeaponRuntime.IsInitialized == false)
            {
                return;
            }

            // 推进武器运行时事实（重置单帧标记、换弹倒计时）。
            currentWeaponRuntime.PreTickRuntimeFacts(_currentTime, _deltaTime);

            // 刷新表现态开火窗口到期。
            WeaponViewState viewState = CurrentWeaponViewState;
            if (viewState != null)
            {
                viewState.PreTick(_currentTime);
            }
        }

        #endregion

        #region Execute

        /// <summary>
        /// 执行当前帧动作，并把武器请求交给 Weapon 域自己的主链处理。
        /// </summary>
        public void Execute(in CharacterFramePlan _plan, CharacterFacts _facts, float _currentTime)
        {
            // 记录本帧请求状态，供外部查询。
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

            // 构建武器域请求并交给 WeaponRuntime 主链处理。
            WeaponRequest weaponRequest = new(
                _plan.FirePressed,
                _plan.FireHeld,
                _plan.ReloadTriggered);

            currentWeaponRuntime.TickWeaponRuntime(weaponRequest, _facts, _plan, _currentTime);
        }

        #endregion

        #region Weapon Binding

        /// <summary>
        /// 显式设置当前装备武器。
        /// 当前阶段通过装配或切枪入口调用；后续接 WeaponSlotController 时，
        /// 也应通过此入口切换，不让各系统自己搜索场景对象。
        /// </summary>
        public void SetCurrentWeaponRuntime(WeaponRuntime _weaponRuntime)
        {
            currentWeaponRuntime = _weaponRuntime;
        }

        /// <summary>
        /// 重置全部本帧请求状态。
        /// </summary>
        public void ResetRequests()
        {
            FireHeld = false;
            FirePressed = false;
            FireRequested = false;
            ReloadRequested = false;
            SkillRequested = false;
            SwitchRequested = false;
        }

        #endregion
    }
}
