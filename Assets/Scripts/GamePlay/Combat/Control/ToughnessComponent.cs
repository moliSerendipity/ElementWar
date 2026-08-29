using Game.Gameplay.Enemy;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 保存单个敌人的当前韧性、连续恢复和失衡状态；攻击身份与等级策略由外部解析入口负责。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStat), typeof(HealthComponent))]
    public sealed class ToughnessComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyStat enemyStat;
        [SerializeField] private HealthComponent healthComponent;

        [Header("Runtime (Read Only)")]
        [SerializeField] private float currentToughness;
        [SerializeField] private bool isStaggered;
        [SerializeField] private float staggerEndsAt;
        [SerializeField] private float lastUpdatedAt;
        [SerializeField] private bool isConfigured;

        /// <summary>当前韧性；目标不能接收控制时为零。</summary>
        public float CurrentToughness => IsOperational ? currentToughness : 0f;

        /// <summary>当前配置快照中的单次最低生效削韧。</summary>
        internal float MinimumDamage =>
            isConfigured && enemyStat != null ? enemyStat.MinimumToughnessDamage : 0f;

        /// <summary>目标当前是否处于失衡期。</summary>
        public bool IsStaggered => IsOperational && isStaggered;

        /// <summary>组件配置、生命和启用状态均允许接收削韧时为真。</summary>
        public bool IsOperational =>
            isConfigured &&
            isActiveAndEnabled &&
            enemyStat != null &&
            enemyStat.IsInitialized &&
            healthComponent != null &&
            healthComponent.IsInitialized &&
            healthComponent.IsHealthDepleted == false;

        /// <summary>对象池复用会重新获得满韧性，不继承上一个启用周期的失衡或恢复进度。</summary>
        private void OnEnable()
        {
            ResolveReferences();
            if (isConfigured)
            {
                ResetState(enemyStat != null ? enemyStat.MaxToughness : 0f, Time.time);
            }
        }

        /// <summary>禁用时只清理本组件的易失状态；配置引用保留给下一次启用。</summary>
        private void OnDisable()
        {
            ResetState(0f, 0f);
        }

        /// <summary>使用同一对象上已初始化的敌人数值建立韧性状态。</summary>
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
            ResetState(enemyStat.MaxToughness, _currentTime);
            return IsOperational;
        }

        /// <summary>把恢复或失衡到期推进到指定运行时时间。</summary>
        /// <param name="_currentTime">有限非负运行时时间；早于已处理时间时不会倒退状态。</param>
        public void Tick(float _currentTime)
        {
            if (IsFiniteNonNegative(_currentTime) == false)
            {
                return;
            }

            // 死亡或初始化失效时，韧性不再作为可恢复事实保留。
            if (IsOperational == false)
            {
                ResetState(0f, Mathf.Max(lastUpdatedAt, _currentTime));
                return;
            }

            AdvanceTo(_currentTime);
        }

        /// <summary>
        /// 写入解析器已经合并等级策略后的单次削韧；最低阈值只在这里判断一次。
        /// </summary>
        /// <param name="_damage">本次攻击最终削韧量。</param>
        /// <param name="_applicationTime">本次写入对应的有限非负运行时时间。</param>
        /// <param name="_didStagger">返回本次写入是否刚好触发失衡。</param>
        /// <returns>目标本次实际损失的韧性；未生效时为零。</returns>
        internal float ApplyResolvedDamage(
            float _damage,
            float _applicationTime,
            out bool _didStagger)
        {
            _didStagger = false;
            if (IsOperational == false)
            {
                return 0f;
            }

            // 先结算到攻击时刻，避免恢复量与本次扣减使用不同的状态基线。
            float effectiveTime = AdvanceTo(_applicationTime);
            if (isStaggered || _damage < MinimumDamage)
            {
                return 0f;
            }

            float previousToughness = currentToughness;
            currentToughness = Mathf.Max(0f, currentToughness - _damage);
            float appliedDamage = previousToughness - currentToughness;

            // 只有本次从正韧性降到零才建立失衡期，期间不继续累计削韧。
            if (currentToughness <= 0f)
            {
                isStaggered = true;
                staggerEndsAt = effectiveTime + enemyStat.StaggerDuration;
                _didStagger = true;
            }

            return appliedDamage;
        }

        /// <summary>
        /// 按不倒退的时间线结算失衡结束或线性恢复；失衡结束时直接回满。
        /// </summary>
        /// <param name="_currentTime">已经由公共入口验证的运行时时间。</param>
        /// <returns>实际用于本次结算的非倒退时间。</returns>
        private float AdvanceTo(float _currentTime)
        {
            float effectiveTime = Mathf.Max(lastUpdatedAt, _currentTime);
            float elapsedTime = effectiveTime - lastUpdatedAt;
            lastUpdatedAt = effectiveTime;

            if (isStaggered)
            {
                if (effectiveTime >= staggerEndsAt)
                {
                    isStaggered = false;
                    staggerEndsAt = 0f;
                    currentToughness = enemyStat.MaxToughness;
                }

                return effectiveTime;
            }

            if (currentToughness < enemyStat.MaxToughness &&
                enemyStat.ToughnessRecoveryPerSecond > 0f)
            {
                currentToughness = Mathf.Min(
                    enemyStat.MaxToughness,
                    currentToughness + enemyStat.ToughnessRecoveryPerSecond * elapsedTime);
            }

            return effectiveTime;
        }

        /// <summary>统一建立满韧性、不可用或禁用后的本地状态，避免维护多套重置路径。</summary>
        /// <param name="_currentValue">重置后的韧性；不可用或禁用时为零。</param>
        /// <param name="_currentTime">重置后的本地时间基线。</param>
        private void ResetState(float _currentValue, float _currentTime)
        {
            currentToughness = _currentValue;
            isStaggered = false;
            staggerEndsAt = 0f;
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
