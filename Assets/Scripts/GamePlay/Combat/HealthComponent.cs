using Game.Definition.Combat;
using Game.Gameplay.Character;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Combat 域长期生命事实组件。
    ///
    /// 职责：
    /// 1. 绑定实体运行时 Stat（由外部 Root 在 Inspector 或初始化阶段关联）
    /// 2. 保存 currentHealth / maxHealth / isDead 等长期事实
    /// 3. 接收并提交已由 DamageResolver 裁决完成的伤害结果
    ///
    /// 约束：
    /// 1. 不自行计算伤害、不自行派发事件
    /// 2. Combat 热路径统一通过 OwnerStat 读取战斗数值，不回查配置表
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private ActorStatBase ownerStat;

        [Header("Runtime")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxHealth;
        [SerializeField] private bool isDead;
        [SerializeField] private bool isInitialized;

        public ActorStatBase OwnerStat => ownerStat;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool CanReceiveDamage => isInitialized && !isDead;
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 从已初始化的 OwnerStat 读取生命上限并重置为满血状态。
        /// 调用前必须确保 OwnerStat 已经完成初始化。
        /// </summary>
        public bool TryInitialize()
        {
            ResetRuntimeState();

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
            isDead = false;
            isInitialized = true;
            return true;
        }

        /// <summary>
        /// 提交已由 DamageResolver 裁决完成的最终伤害。
        /// 这里只做事实写入，不反向计算伤害。
        /// </summary>
        public CombatDamageResult ApplyResolvedDamage(
            GameObject _attacker,
            CombatDamageKind _damageKind,
            CombatHitPartType _hitPartType,
            float _finalDamage,
            bool _isCritical,
            Vector3 _hitPoint,
            Vector3 _hitNormal,
            float _appliedTime)
        {
            if (CanReceiveDamage == false)
            {
                return CombatDamageResult.None;
            }

            float appliedDamage = Mathf.Max(0f, _finalDamage);
            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);

            bool wasKilled = currentHealth <= 0f;
            if (wasKilled)
            {
                isDead = true;
            }

            return new CombatDamageResult(
                true,
                _attacker,
                this,
                _damageKind,
                _hitPartType,
                appliedDamage,
                _isCritical,
                currentHealth,
                wasKilled,
                _hitPoint,
                _hitNormal,
                _appliedTime);
        }

        /// <summary>
        /// 重置为满血存活状态。用于复活或调试重置。
        /// </summary>
        public void RestoreFullHealth()
        {
            if (isInitialized == false)
            {
                return;
            }

            maxHealth = Mathf.Max(1f, ownerStat.MaxHealth);
            currentHealth = maxHealth;
            isDead = false;
        }

        /// <summary>
        /// 清空全部运行时状态，回到未初始化。
        /// </summary>
        public void ResetRuntimeState()
        {
            currentHealth = 0f;
            maxHealth = 0f;
            isDead = false;
            isInitialized = false;
        }
    }
}
