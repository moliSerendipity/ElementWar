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
    /// 1. 解析敌人基础面板、移动和抗性配置；
    /// 2. 作为敌人 Combat / AI 统一读取的运行时数值入口；
    /// 3. 为后续 Buff / Debuff 修改敌人数值提供唯一入口。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyStat : ActorStatBase
    {
        [Header("Config")]
        [SerializeField] private string enemyDefinitionConfigId;

        [Header("Runtime Movement")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float combatMoveSpeed = 3f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float turnSharpness = 12f;
        [SerializeField] private float stopDistance = 1.5f;
        [SerializeField] private float weakPointDamageMultiplier = 1.5f;

        private EnemyDefinitionConfig enemyDefinitionConfig;
        private EnemyBaseStatConfig enemyBaseStatConfig;
        private EnemyMovementConfig enemyMovementConfig;
        private ResistanceSetConfig enemyResistanceSetConfig;

        public float PatrolSpeed => patrolSpeed;
        public float CombatMoveSpeed => combatMoveSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float TurnSharpness => turnSharpness;
        public float StopDistance => stopDistance;
        public float WeakPointDamageMultiplier => weakPointDamageMultiplier;

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

            if (_configService.TryGetConfig(enemyDefinitionConfigId, out EnemyDefinitionConfig resolvedDefinitionConfig) == false)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：找不到 EnemyDefinitionConfig，Id={enemyDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.EnemyBaseStatConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：EnemyBaseStatConfig 缺失。EnemyId={enemyDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.EnemyMovementConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：EnemyMovementConfig 缺失。EnemyId={enemyDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.EnemyResistanceSetConfig == null)
            {
                Debug.LogError($"[{nameof(EnemyStat)}] 初始化失败：ResistanceSetConfig 缺失。EnemyId={enemyDefinitionConfigId}，Object={name}", this);
                return false;
            }

            enemyDefinitionConfig = resolvedDefinitionConfig;
            enemyBaseStatConfig = resolvedDefinitionConfig.EnemyBaseStatConfig;
            enemyMovementConfig = resolvedDefinitionConfig.EnemyMovementConfig;
            enemyResistanceSetConfig = resolvedDefinitionConfig.EnemyResistanceSetConfig;

            CommitCombatStatInitialization(
                enemyBaseStatConfig.MaxHealth,
                0f,
                enemyBaseStatConfig.AttackPower,
                enemyBaseStatConfig.Defense,
                enemyBaseStatConfig.Toughness,
                enemyBaseStatConfig.DamageTakenMultiplier,
                1f,
                enemyResistanceSetConfig != null ? enemyResistanceSetConfig.PhysicalResistance : 0f,
                enemyResistanceSetConfig != null ? enemyResistanceSetConfig.FireResistance : 0f,
                enemyResistanceSetConfig != null ? enemyResistanceSetConfig.ElectricResistance : 0f,
                enemyResistanceSetConfig != null ? enemyResistanceSetConfig.IceResistance : 0f,
                enemyResistanceSetConfig != null ? enemyResistanceSetConfig.ExplosionResistance : 0f);

            patrolSpeed = Mathf.Max(0f, enemyMovementConfig.PatrolSpeed);
            combatMoveSpeed = Mathf.Max(0f, enemyMovementConfig.CombatMoveSpeed);
            chaseSpeed = Mathf.Max(0f, enemyMovementConfig.ChaseSpeed);
            turnSharpness = Mathf.Max(0.01f, enemyMovementConfig.TurnSharpness);
            stopDistance = Mathf.Max(0f, enemyMovementConfig.StopDistance);
            weakPointDamageMultiplier = Mathf.Max(1f, enemyBaseStatConfig.WeakPointDamageMultiplier);
            return true;
        }

        public void ResetRuntimeState()
        {
            enemyDefinitionConfig = null;
            enemyBaseStatConfig = null;
            enemyMovementConfig = null;
            enemyResistanceSetConfig = null;
            patrolSpeed = 0f;
            combatMoveSpeed = 0f;
            chaseSpeed = 0f;
            turnSharpness = 12f;
            stopDistance = 1.5f;
            weakPointDamageMultiplier = 1.5f;
            ResetCombatStatRuntime();
        }
    }
}
