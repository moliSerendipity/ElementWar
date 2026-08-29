using Game.Gameplay.Enemy;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 保存单个敌人的硬控制结束时间；敌人等级与 Boss 转削韧规则由外部解析入口负责。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStat), typeof(HealthComponent))]
    public sealed class HardControlComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyStat enemyStat;
        [SerializeField] private HealthComponent healthComponent;

        [Header("Runtime (Read Only)")]
        [SerializeField] private bool isHardControlled;
        [SerializeField] private float controlEndsAt;
        [SerializeField] private float lastUpdatedAt;
        [SerializeField] private bool isConfigured;

        /// <summary>目标当前是否处于硬控制。</summary>
        public bool IsHardControlled => IsOperational && isHardControlled;

        /// <summary>当前硬控制的绝对结束时间；未受控时为零。</summary>
        public float ControlEndsAt => IsHardControlled ? controlEndsAt : 0f;

        /// <summary>组件配置、生命和启用状态均允许接收硬控制时为真。</summary>
        public bool IsOperational =>
            isConfigured &&
            isActiveAndEnabled &&
            enemyStat != null &&
            enemyStat.IsInitialized &&
            healthComponent != null &&
            healthComponent.IsInitialized &&
            healthComponent.IsHealthDepleted == false;

        /// <summary>对象池复用不会继承上一个启用周期的硬控制。</summary>
        private void OnEnable()
        {
            ResolveReferences();
            if (isConfigured)
            {
                ResetState(Time.time);
            }
        }

        /// <summary>禁用时只清理本组件的易失状态；配置引用保留给下一次启用。</summary>
        private void OnDisable()
        {
            ResetState(0f);
        }

        /// <summary>使用同一对象上已初始化的敌人数值建立空闲硬控制状态。</summary>
        /// <param name="_currentTime">初始化时的有限非负运行时时间。</param>
        /// <returns>引用、数值快照和生命状态均可用时返回 <see langword="true"/>。</returns>
        public bool TryInitialize(float _currentTime)
        {
            ResolveReferences();
            if (enemyStat == null ||
                enemyStat.IsInitialized == false ||
                isActiveAndEnabled == false ||
                healthComponent == null ||
                healthComponent.IsInitialized == false ||
                healthComponent.IsHealthDepleted ||
                IsFiniteNonNegative(_currentTime) == false)
            {
                return false;
            }

            isConfigured = true;
            ResetState(_currentTime);
            return IsOperational;
        }

        /// <summary>推进硬控制到期或清除生命失效后的状态。</summary>
        /// <param name="_currentTime">有限非负运行时时间；早于已处理时间时不会倒退状态。</param>
        public void Tick(float _currentTime)
        {
            if (IsFiniteNonNegative(_currentTime) == false)
            {
                return;
            }

            if (IsOperational == false)
            {
                ResetState(Mathf.Max(lastUpdatedAt, _currentTime));
                return;
            }

            AdvanceTo(_currentTime);
        }

        /// <summary>写入解析器已经按敌人等级换算后的硬控制时长。</summary>
        /// <param name="_duration">最终硬控制时长，单位秒。</param>
        /// <param name="_applicationTime">本次写入对应的有限非负运行时时间。</param>
        /// <returns>首次施加、延长、有效但不变或没有硬控制效果。</returns>
        internal HardControlApplicationStatus ApplyResolvedDuration(
            float _duration,
            float _applicationTime)
        {
            if (IsOperational == false || _duration <= 0f)
            {
                return HardControlApplicationStatus.None;
            }

            // 先清掉在攻击时刻之前已经结束的控制，再比较新的绝对结束时间。
            float effectiveTime = AdvanceTo(_applicationTime);
            float requestedEndsAt = effectiveTime + _duration;
            if (IsFiniteNonNegative(requestedEndsAt) == false)
            {
                return HardControlApplicationStatus.None;
            }

            if (isHardControlled == false)
            {
                isHardControlled = true;
                controlEndsAt = requestedEndsAt;
                return HardControlApplicationStatus.Applied;
            }

            if (requestedEndsAt <= controlEndsAt)
            {
                return HardControlApplicationStatus.Unchanged;
            }

            controlEndsAt = requestedEndsAt;
            return HardControlApplicationStatus.Extended;
        }

        /// <summary>按不倒退的时间线清理已经到期的单一硬控制状态。</summary>
        /// <param name="_currentTime">已经由公共入口验证的运行时时间。</param>
        /// <returns>实际用于本次结算的非倒退时间。</returns>
        private float AdvanceTo(float _currentTime)
        {
            float effectiveTime = Mathf.Max(lastUpdatedAt, _currentTime);
            lastUpdatedAt = effectiveTime;

            if (isHardControlled && effectiveTime >= controlEndsAt)
            {
                isHardControlled = false;
                controlEndsAt = 0f;
            }

            return effectiveTime;
        }

        /// <summary>统一建立初始化、不可用或禁用后的无控制状态，避免维护多套重置路径。</summary>
        /// <param name="_currentTime">重置后的本地时间基线。</param>
        private void ResetState(float _currentTime)
        {
            isHardControlled = false;
            controlEndsAt = 0f;
            lastUpdatedAt = _currentTime;
        }

        private void ResolveReferences()
        {
            if (enemyStat == null)
            {
                enemyStat = GetComponent<EnemyStat>();
            }

            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }
        }

        private static bool IsFiniteNonNegative(float _value)
        {
            return float.IsNaN(_value) == false &&
                float.IsInfinity(_value) == false &&
                _value >= 0f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
