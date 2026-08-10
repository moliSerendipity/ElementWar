using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色与敌人共用的运行时数值基类
    /// 职责：
    /// 1. 保存 Combat / Buff 会直接读写的共用可变数值
    /// 2. 作为 HealthComponent / DamageResolver 的统一数值来源
    /// 3. 隔离配置表，让运行时不再直接读取 BaseStatConfig / ResistanceSetConfig
    ///
    /// 边界：
    /// 1. 这里只放可变数值，不放 grounded / aiming / reloading 这类事实状态
    /// 2. 这里只放角色和敌人都共享的数值，不放角色独有的移动/瞄准参数
    /// 3. 这里只提供运行时当前值，不回写任何配置资产
    /// </summary>
    public abstract class ActorStatBase : MonoBehaviour
    {
        [Header("Core Combat Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxShield;
        [SerializeField] private float attackPower = 10f;
        [SerializeField] private float defense;
        [SerializeField] private float toughness = 100f;
        [SerializeField] private float damageTakenMultiplier = 1f;
        [SerializeField] private float healingTakenMultiplier = 1f;

        [Header("Resistances")]
        [SerializeField] private float physicalResistance;
        [SerializeField] private float fireResistance;
        [SerializeField] private float waterResistance;
        [SerializeField] private float electricResistance;
        [SerializeField] private float iceResistance;
        [SerializeField] private float explosionResistance;

        [SerializeField] private bool isInitialized;

        public bool IsInitialized => isInitialized;
        public float MaxHealth => maxHealth;
        public float MaxShield => maxShield;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public float Toughness => toughness;
        public float DamageTakenMultiplier => damageTakenMultiplier;
        public float HealingTakenMultiplier => healingTakenMultiplier;
        public float PhysicalResistance => physicalResistance;
        public float FireResistance => fireResistance;
        public float WaterResistance => waterResistance;
        public float ElectricResistance => electricResistance;
        public float IceResistance => iceResistance;
        public float ExplosionResistance => explosionResistance;

        protected void ResetCombatStatRuntime()
        {
            maxHealth = 100f;
            maxShield = 0f;
            attackPower = 10f;
            defense = 0f;
            toughness = 100f;
            damageTakenMultiplier = 1f;
            healingTakenMultiplier = 1f;
            physicalResistance = 0f;
            fireResistance = 0f;
            waterResistance = 0f;
            electricResistance = 0f;
            iceResistance = 0f;
            explosionResistance = 0f;
            isInitialized = false;
        }

        protected void CommitCombatStatInitialization(
            float _maxHealth,
            float _maxShield,
            float _attackPower,
            float _defense,
            float _toughness,
            float _damageTakenMultiplier,
            float _healingTakenMultiplier,
            float _physicalResistance,
            float _fireResistance,
            float _waterResistance,
            float _electricResistance,
            float _iceResistance,
            float _explosionResistance)
        {
            maxHealth = Mathf.Max(1f, _maxHealth);
            maxShield = Mathf.Max(0f, _maxShield);
            attackPower = Mathf.Max(0f, _attackPower);
            defense = Mathf.Max(0f, _defense);
            toughness = Mathf.Max(0f, _toughness);
            damageTakenMultiplier = Mathf.Max(0f, _damageTakenMultiplier);
            healingTakenMultiplier = Mathf.Max(0f, _healingTakenMultiplier);
            physicalResistance = _physicalResistance;
            fireResistance = _fireResistance;
            waterResistance = _waterResistance;
            electricResistance = _electricResistance;
            iceResistance = _iceResistance;
            explosionResistance = _explosionResistance;
            isInitialized = true;
        }

        public void SetMaxHealth(float _value) => maxHealth = Mathf.Max(1f, _value);
        public void SetMaxShield(float _value) => maxShield = Mathf.Max(0f, _value);
        public void SetAttackPower(float _value) => attackPower = Mathf.Max(0f, _value);
        public void SetDefense(float _value) => defense = Mathf.Max(0f, _value);
        public void SetToughness(float _value) => toughness = Mathf.Max(0f, _value);
        public void SetDamageTakenMultiplier(float _value) => damageTakenMultiplier = Mathf.Max(0f, _value);
        public void SetHealingTakenMultiplier(float _value) => healingTakenMultiplier = Mathf.Max(0f, _value);
        public void SetPhysicalResistance(float _value) => physicalResistance = _value;
        public void SetFireResistance(float _value) => fireResistance = _value;
        public void SetWaterResistance(float _value) => waterResistance = _value;
        public void SetElectricResistance(float _value) => electricResistance = _value;
        public void SetIceResistance(float _value) => iceResistance = _value;
        public void SetExplosionResistance(float _value) => explosionResistance = _value;
    }
}
