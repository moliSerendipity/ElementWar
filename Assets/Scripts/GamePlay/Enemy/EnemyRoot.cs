using Game.Definition.ConfigSystem.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人装配根。
    ///
    /// 职责：
    /// 1. 在 Start 阶段从配置初始化所有子系统（Stat → Health → Sensor → Locomotion → Attack → Brain）
    /// 2. 在 Update 中驱动 Brain 的每帧 Tick
    /// 3. 作为敌人 GameObject 的唯一生命周期入口，不允许子系统各自独立 Update
    ///
    /// 初始化顺序很重要：
    /// Stat 先于 Health（Health 需要读 MaxHealth），
    /// Sensor / Locomotion / Attack 先于 Brain（Brain 需要它们就位才能工作）。
    /// Brain / Sensor / Locomotion / Attack 都持有 EnemyStat 引用，并在需要时直接读取。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Combatant))]
    public sealed class EnemyRoot : MonoBehaviour
    {
        #region Inspector

        [Header("Core Components")]
        [SerializeField] private EnemyStat enemyStat;
        [SerializeField] private HealthComponent healthComponent;
        [SerializeField] private ElementAttachmentRuntime elementAttachmentRuntime;

        [Header("Behavior Components")]
        [SerializeField] private EnemySensor sensor;
        [SerializeField] private EnemyBrain brain;
        [SerializeField] private EnemyLocomotion locomotion;
        [SerializeField] private EnemyAttack attack;

        [Header("State")]
        [SerializeField] private bool isFullyInitialized;

        #endregion

        #region Public Accessors

        public EnemyStat Stat => enemyStat;
        public HealthComponent Health => healthComponent;
        public EnemyBrain Brain => brain;
        public bool IsFullyInitialized => isFullyInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            isFullyInitialized = TryInitializeAll();

            if (isFullyInitialized == false)
            {
                Debug.LogError($"[{nameof(EnemyRoot)}] 初始化未完全成功，敌人可能无法正常行动。Object={name}", this);
            }
        }

        private void Update()
        {
            // 附着属于目标事实，即使 AI 初始化失败也需要继续处理到期或生命清理。
            elementAttachmentRuntime?.Tick(Time.time);

            if (isFullyInitialized == false)
            {
                return;
            }

            // 敌人全部行为由 Brain 统一驱动，子系统不各自跑 Update。
            brain.Tick(Time.deltaTime);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 按依赖顺序初始化所有子系统。任何一步失败都会记录错误但尝试继续。
        /// </summary>
        private bool TryInitializeAll()
        {
            bool allSucceeded = true;

            // 初始化运行时数值（从 EnemyDefinitionConfig 读取面板、移动、抗性）。
            if (InitializeStat() == false)
            {
                allSucceeded = false;
            }

            // 初始化生命组件（依赖 Stat.MaxHealth 已就位）。
            if (InitializeHealth() == false)
            {
                allSucceeded = false;
            }

            // 将 EnemyStat 注入给各行为子系统。
            InitializeBehaviorSubsystems();

            return allSucceeded;
        }

        /// <summary>
        /// 从 EnemyDefinitionConfig 初始化运行时数值。
        /// </summary>
        private bool InitializeStat()
        {
            if (enemyStat == null)
            {
                Debug.LogError($"[{nameof(EnemyRoot)}] EnemyStat 缺失。Object={name}", this);
                return false;
            }

            ConfigService configService = ConfigService.Active;
            if (configService == null || configService.IsInitialized == false)
            {
                Debug.LogError($"[{nameof(EnemyRoot)}] ConfigService 不可用。Object={name}", this);
                return false;
            }

            return enemyStat.TryInitialize(configService);
        }

        /// <summary>
        /// 初始化 HealthComponent。必须在 Stat 之后。
        /// </summary>
        private bool InitializeHealth()
        {
            if (healthComponent == null)
            {
                Debug.LogError($"[{nameof(EnemyRoot)}] HealthComponent 缺失。Object={name}", this);
                return false;
            }

            return healthComponent.TryInitialize();
        }

        /// <summary>
        /// 将 EnemyStat 注入给各行为子系统。
        /// </summary>
        private void InitializeBehaviorSubsystems()
        {
            if (enemyStat == null || enemyStat.IsInitialized == false)
            {
                return;
            }

            if (sensor != null)
            {
                sensor.Initialize(enemyStat);
            }

            if (locomotion != null)
            {
                locomotion.Initialize(enemyStat);
            }

            if (attack != null)
            {
                attack.Initialize(enemyStat);
            }

            if (brain != null)
            {
                brain.Initialize(enemyStat);
            }
        }

        #endregion

        #region Reference Resolution

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

            if (elementAttachmentRuntime == null)
            {
                elementAttachmentRuntime = GetComponent<ElementAttachmentRuntime>();
            }

            if (sensor == null)
            {
                sensor = GetComponent<EnemySensor>();
            }

            if (brain == null)
            {
                brain = GetComponent<EnemyBrain>();
            }

            if (locomotion == null)
            {
                locomotion = GetComponent<EnemyLocomotion>();
            }

            if (attack == null)
            {
                attack = GetComponent<EnemyAttack>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        #endregion
    }
}
