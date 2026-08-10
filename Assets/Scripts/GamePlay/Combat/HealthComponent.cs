using Game.Definition.Combat;
using Game.Gameplay.Character;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 保存实体生命数值并提交已由 <see cref="DamageResolver"/> 裁决的结果。
    /// 当前生命值是唯一存储事实；生命耗尽状态由它派生。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private ActorStatBase ownerStat;

        [Header("Runtime")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxHealth;
        [SerializeField] private bool isInitialized;

        /// <summary>提供生命上限与防守侧数值的运行时所有者。</summary>
        public ActorStatBase OwnerStat => ownerStat;

        /// <summary>当前已提交生命值。</summary>
        public float CurrentHealth => currentHealth;

        /// <summary>本次初始化采用的生命上限。</summary>
        public float MaxHealth => maxHealth;

        /// <summary>组件已初始化且当前生命值不大于零时为真。</summary>
        public bool IsHealthDepleted => isInitialized && currentHealth <= 0f;

        /// <summary>已初始化且生命尚未耗尽时可以接收伤害。</summary>
        public bool CanReceiveDamage => isInitialized && IsHealthDepleted == false;

        /// <summary>是否已经从 OwnerStat 建立运行时生命状态。</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 从已初始化的 OwnerStat 读取生命上限并重置为满血状态。
        /// </summary>
        /// <returns>OwnerStat 有效且已经初始化时返回 <see langword="true"/>。</returns>
        public bool TryInitialize()
        {
            ResetRuntimeState();
            ResolveReferences();

            if (ownerStat == null)
            {
                Debug.LogError($"[{nameof(HealthComponent)}] 初始化失败：OwnerStat 未绑定。Object={name}", this);
                return false;
            }

            if (ownerStat.IsInitialized == false)
            {
                Debug.LogError($"[{nameof(HealthComponent)}] 初始化失败：OwnerStat 尚未初始化。Object={name}", this);
                return false;
            }

            maxHealth = Mathf.Max(1f, ownerStat.MaxHealth);
            currentHealth = maxHealth;
            isInitialized = true;
            return true;
        }

        /// <summary>
        /// 写入已裁决的最终伤害。该入口只供伤害域使用，不重新计算任何乘区。
        /// </summary>
        internal DamageResult ApplyResolvedDamage(
            GameObject _instigator,
            Object _sourceObject,
            ElementType _element,
            DamageDeliveryType _delivery,
            HitPartType _hitPartType,
            float _finalDamage,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _appliedTime)
        {
            if (CanReceiveDamage == false)
            {
                return DamageResult.None;
            }

            float previousHealth = currentHealth;
            float appliedDamage = Mathf.Max(0f, _finalDamage);
            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
            bool didDepleteHealth = previousHealth > 0f && currentHealth <= 0f;

            return new DamageResult(
                true,
                _instigator,
                _sourceObject,
                this,
                _element,
                _delivery,
                _hitPartType,
                appliedDamage,
                currentHealth,
                didDepleteHealth,
                _hitPoint,
                _hitNormal,
                _appliedTime);
        }

        /// <summary>
        /// 把已初始化的生命状态重置为当前上限；不实现倒地或复活流程。
        /// </summary>
        public void RestoreFullHealth()
        {
            if (isInitialized == false || ownerStat == null)
            {
                return;
            }

            maxHealth = Mathf.Max(1f, ownerStat.MaxHealth);
            currentHealth = maxHealth;
        }

        /// <summary>
        /// 清空运行时生命状态并回到未初始化状态。
        /// </summary>
        public void ResetRuntimeState()
        {
            currentHealth = 0f;
            maxHealth = 0f;
            isInitialized = false;
        }

        private void ResolveReferences()
        {
            if (ownerStat == null)
            {
                ownerStat = GetComponent<ActorStatBase>();
            }
        }
    }
}
