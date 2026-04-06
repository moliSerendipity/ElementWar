using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Enemy;
using Game.Gameplay.Character;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人运行时属性容器。
    ///
    /// 职责：
    /// 1. 在初始化阶段从配置解析面板、移动和抗性数值
    /// 2. 作为敌人 Combat / AI / Locomotion / Attack 的统一运行时数值入口
    /// 3. 为 Buff / Debuff 修改敌人数值提供唯一改写入口
    ///
    /// 继承 ActorStatBase 获得战斗通用数值（MaxHealth、Defense、各抗性等）及其 Setter。
    /// 本类额外持有敌人特有的移动、感知和攻击参数。
    ///
    /// 注意：
    /// 攻击进入范围不再由基础面板配置单独维护，唯一真相源是 EnemyAttackConfig 的 MinUseRange / MaxUseRange。
    /// EnemyStat 仅保留通用攻击冷却与攻速倍率等运行时战斗参数。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyStat : ActorStatBase
    {
        #region Config

        [Header("Config")]
        [SerializeField] private string enemyDefinitionConfigId;

        #endregion

        #region Buffable Movement Stats

        [Header("Runtime Movement")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float turnSharpness = 12f;
        [SerializeField] private float stopDistance = 1.5f;

        #endregion

        #region Buffable Detection Stats

        [Header("Runtime Detection")]
        [SerializeField] private float detectRange = 15f;
        [SerializeField] private float loseTargetRange = 20f;
        [SerializeField] private float targetMemoryDuration = 1.5f;
        [SerializeField] private float scanInterval = 0.15f;

        #endregion

        #region Buffable Combat Stats

        [Header("Runtime Combat")]
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private float attackSpeedMultiplier = 1f;
        [SerializeField] private float weakPointDamageMultiplier = 1.5f;

        #endregion

        #region Cached Config References

        private EnemyDefinitionConfig enemyDefinitionConfig;
        private EnemyBaseStatConfig enemyBaseStatConfig;
        private EnemyMovementConfig enemyMovementConfig;
        private ResistanceSetConfig enemyResistanceSetConfig;

        #endregion

        #region Public Accessors

        public string EnemyDefinitionConfigId => enemyDefinitionConfigId;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float TurnSharpness => turnSharpness;
        public float StopDistance => stopDistance;
        public float DetectRange => detectRange;
        public float LoseTargetRange => loseTargetRange;
        public float TargetMemoryDuration => targetMemoryDuration;
        public float ScanInterval => scanInterval;
        public float AttackCooldown => attackCooldown;

        /// <summary>
        /// 攻速倍率。1 = 正常速度，2 = 两倍速。
        /// 影响 EnemyAttack 的阶段计时器消耗速度和 AnimationBridge 的 Animator.speed。
        /// </summary>
        public float AttackSpeedMultiplier => attackSpeedMultiplier;

        public float WeakPointDamageMultiplier => weakPointDamageMultiplier;

        #endregion

        #region Public Setters (Buff / Debuff)

        public void SetPatrolSpeed(float _value) => patrolSpeed = Mathf.Max(0f, _value);
        public void SetChaseSpeed(float _value) => chaseSpeed = Mathf.Max(0f, _value);
        public void SetTurnSharpness(float _value) => turnSharpness = Mathf.Max(0.01f, _value);
        public void SetStopDistance(float _value) => stopDistance = Mathf.Max(0f, _value);
        public void SetDetectRange(float _value) => detectRange = Mathf.Max(0f, _value);
        public void SetLoseTargetRange(float _value) => loseTargetRange = Mathf.Max(0f, _value);
        public void SetTargetMemoryDuration(float _value) => targetMemoryDuration = Mathf.Max(0f, _value);
        public void SetScanInterval(float _value) => scanInterval = Mathf.Max(0.05f, _value);
        public void SetAttackCooldown(float _value) => attackCooldown = Mathf.Max(0f, _value);
        public void SetAttackSpeedMultiplier(float _value) => attackSpeedMultiplier = Mathf.Max(0.1f, _value);
        public void SetWeakPointDamageMultiplier(float _value) => weakPointDamageMultiplier = Mathf.Max(1f, _value);

        #endregion

        #region Initialization

        /// <summary>
        /// 从 EnemyDefinitionConfig 初始化全部运行时数值。
        /// </summary>
        public bool TryInitialize(ConfigService _configService)
        {
            ResetRuntimeState();

            if (_configService == null || _configService.IsInitialized == false)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：ConfigService 不可用。Object={name}", this);
                return false;
            }

            if (ConfigIdUtility.IsValid(enemyDefinitionConfigId) == false)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：EnemyDefinitionConfigId 为空。Object={name}", this);
                return false;
            }

            if (_configService.TryGetConfig(enemyDefinitionConfigId, out EnemyDefinitionConfig resolvedConfig) == false)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：找不到 EnemyDefinitionConfig，Id={enemyDefinitionConfigId}。Object={name}", this);
                return false;
            }

            if (resolvedConfig.EnemyBaseStatConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] EnemyBaseStatConfig 缺失。EnemyId={enemyDefinitionConfigId}。Object={name}", this);
                return false;
            }

            if (resolvedConfig.EnemyMovementConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] EnemyMovementConfig 缺失。EnemyId={enemyDefinitionConfigId}。Object={name}", this);
                return false;
            }

            if (resolvedConfig.EnemyResistanceSetConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] ResistanceSetConfig 缺失。EnemyId={enemyDefinitionConfigId}。Object={name}", this);
                return false;
            }

            // 缓存配置引用。
            enemyDefinitionConfig = resolvedConfig;
            enemyBaseStatConfig = resolvedConfig.EnemyBaseStatConfig;
            enemyMovementConfig = resolvedConfig.EnemyMovementConfig;
            enemyResistanceSetConfig = resolvedConfig.EnemyResistanceSetConfig;

            // 初始化 ActorStatBase 的战斗通用数值。
            CommitCombatStatInitialization(
                enemyBaseStatConfig.MaxHealth,
                0f,
                enemyBaseStatConfig.AttackPower,
                enemyBaseStatConfig.Defense,
                enemyBaseStatConfig.Toughness,
                enemyBaseStatConfig.DamageTakenMultiplier,
                1f,
                enemyResistanceSetConfig.PhysicalResistance,
                enemyResistanceSetConfig.FireResistance,
                enemyResistanceSetConfig.ElectricResistance,
                enemyResistanceSetConfig.IceResistance,
                enemyResistanceSetConfig.ExplosionResistance);

            // 初始化敌人特有数值。
            patrolSpeed = Mathf.Max(0f, enemyMovementConfig.PatrolSpeed);
            chaseSpeed = Mathf.Max(0f, enemyMovementConfig.ChaseSpeed);
            turnSharpness = Mathf.Max(0.01f, enemyMovementConfig.TurnSharpness);
            stopDistance = Mathf.Max(0f, enemyMovementConfig.StopDistance);
            detectRange = Mathf.Max(0f, enemyBaseStatConfig.DetectRange);
            loseTargetRange = Mathf.Max(0f, enemyBaseStatConfig.LoseTargetRange);
            targetMemoryDuration = Mathf.Max(0f, enemyBaseStatConfig.TargetMemoryDuration);
            scanInterval = Mathf.Max(0.05f, enemyBaseStatConfig.ScanInterval);
            attackCooldown = Mathf.Max(0f, enemyBaseStatConfig.AttackCooldown);
            attackSpeedMultiplier = 1f;
            weakPointDamageMultiplier = Mathf.Max(1f, enemyBaseStatConfig.WeakPointDamageMultiplier);

            return true;
        }

        #endregion

        #region Reset

        public void ResetRuntimeState()
        {
            enemyDefinitionConfig = null;
            enemyBaseStatConfig = null;
            enemyMovementConfig = null;
            enemyResistanceSetConfig = null;

            patrolSpeed = 0f;
            chaseSpeed = 0f;
            turnSharpness = 12f;
            stopDistance = 1.5f;
            detectRange = 0f;
            loseTargetRange = 0f;
            targetMemoryDuration = 0f;
            scanInterval = 0.15f;
            attackCooldown = 0.5f;
            attackSpeedMultiplier = 1f;
            weakPointDamageMultiplier = 1.5f;

            ResetCombatStatRuntime();
        }

        #endregion
    }
}
