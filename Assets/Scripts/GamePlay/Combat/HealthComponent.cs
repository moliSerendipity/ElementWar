using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Gameplay.Character;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Combat 域长期生命事实组件
    ///
    /// 职责：
    /// 1. 绑定实体运行时 Stat
    /// 2. 缓存战斗规则所需的只读开关
    /// 3. 保存 currentHealth / maxHealth / isDead 等长期事实
    /// 4. 提交已经裁决完成的伤害结果
    ///
    /// 约束：
    /// 1. 不再直接暴露配置表
    /// 2. Combat 热路径统一读取 OwnerStat，而不是再读 BaseStatConfig / ResistanceSetConfig
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private ActorStatBase ownerStat;
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

        public bool TryInitialize(ConfigService _configService)
        {
            ResetRuntimeState();

            maxHealth = Mathf.Max(1f, ownerStat.MaxHealth);
            currentHealth = maxHealth;
            isDead = false;
            isInitialized = true;
            return true;
        }

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

        public void ResetRuntimeState()
        {
            currentHealth = 0f;
            maxHealth = 0f;
            isDead = false;
            isInitialized = false;
        }
    }
}
