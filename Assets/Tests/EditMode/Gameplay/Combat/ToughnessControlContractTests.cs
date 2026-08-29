using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.Enemy;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Enemy;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.Gameplay.Combat
{
    /// <summary>为敌人控制契约建立最小合法玩家来源数值。</summary>
    public sealed class ToughnessControlContractActorStat : ActorStatBase
    {
        /// <summary>初始化无防御、无抗性的测试数值。</summary>
        public void InitializeForTest()
        {
            CommitCombatStatInitialization(
                100f,
                0f,
                10f,
                0f,
                1f,
                1f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f);
        }
    }

    /// <summary>验证敌人韧性恢复、最低阈值、等级换算、合并去重与状态生命周期。</summary>
    public sealed class ToughnessControlContractTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap/Bootstrap.unity";

        private readonly List<Object> ownedObjects = new();
        private Combatant instigator;

        /// <summary>为每个用例创建一个活动玩家来源。</summary>
        [SetUp]
        public void SetUp()
        {
            instigator = CreateInstigator();
        }

        /// <summary>销毁本用例创建的场景对象和配置对象。</summary>
        [TearDown]
        public void TearDown()
        {
            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
            instigator = null;
        }

        /// <summary>不同敌人配置必须产生各自的韧性上限、恢复速度与等级快照。</summary>
        [Test]
        public void EnemyDefinitionsInitializeDistinctToughnessSnapshots()
        {
            EnemyDefinitionConfig normalDefinition = CreateEnemyDefinition(
                "NormalDefinition",
                EnemyTier.Normal,
                120f,
                24f);
            EnemyDefinitionConfig eliteDefinition = CreateEnemyDefinition(
                "EliteDefinition",
                EnemyTier.Elite,
                240f,
                12f);
            ConfigRegistry registry = CreateScriptableObject<ConfigRegistry>();
            SetPrivateField(
                registry,
                "enemyDefinitions",
                new List<EnemyDefinitionConfig> { normalDefinition, eliteDefinition });
            ConfigService configService = new(registry);
            configService.Initialize();

            EnemyStat normalStat = CreateGameObject("NormalConfigEnemy").AddComponent<EnemyStat>();
            EnemyStat eliteStat = CreateGameObject("EliteConfigEnemy").AddComponent<EnemyStat>();
            SetPrivateField(normalStat, "enemyDefinitionConfigId", "NormalDefinition");
            SetPrivateField(eliteStat, "enemyDefinitionConfigId", "EliteDefinition");

            Assert.That(normalStat.TryInitialize(configService), Is.True);
            Assert.That(eliteStat.TryInitialize(configService), Is.True);
            Assert.That(normalStat.MaxToughness, Is.EqualTo(120f));
            Assert.That(normalStat.ToughnessRecoveryPerSecond, Is.EqualTo(24f));
            Assert.That(normalStat.EnemyTier, Is.EqualTo(EnemyTier.Normal));
            Assert.That(eliteStat.MaxToughness, Is.EqualTo(240f));
            Assert.That(eliteStat.ToughnessRecoveryPerSecond, Is.EqualTo(12f));
            Assert.That(eliteStat.EnemyTier, Is.EqualTo(EnemyTier.Elite));
        }

        /// <summary>严格低于最低阈值的多次独立攻击永远不累计削韧。</summary>
        [Test]
        public void BelowMinimumDamageNeverAccumulates()
        {
            Combatant target = CreateEnemyTarget(
                "ThresholdEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out _);

            for (int i = 0; i < 20; i++)
            {
                EnemyControlApplicationResult result = ApplyControl(
                    target,
                    9f,
                    0f,
                    0f,
                    i * 0.05f);
                Assert.That(result.IsAccepted, Is.True);
                Assert.That(result.DidChangeState, Is.False);
            }

            Assert.That(toughness.CurrentToughness, Is.EqualTo(120f));
        }

        /// <summary>等于最低阈值的削韧生效一次，且不改变生命值。</summary>
        [Test]
        public void MinimumDamageBoundaryAppliesWithoutHealthDamage()
        {
            Combatant target = CreateEnemyTarget(
                "BoundaryEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out _);

            EnemyControlApplicationResult result = ApplyControl(target, 10f, 0f, 0f, 0f);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.AppliedToughnessDamage, Is.EqualTo(10f));
            Assert.That(toughness.CurrentToughness, Is.EqualTo(110f));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(target.Health.MaxHealth));
        }

        /// <summary>连续恢复会抵消低频压力，但高频有效攻击仍可破韧。</summary>
        [Test]
        public void RecoverySeparatesLowAndHighFrequencyOutcomes()
        {
            Combatant recoveringTarget = CreateEnemyTarget(
                "RecoveringEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent recoveringToughness,
                out _);
            ApplyControl(recoveringTarget, 40f, 0f, 0f, 0f);
            recoveringToughness.Tick(1f);
            Assert.That(recoveringToughness.CurrentToughness, Is.EqualTo(104f).Within(0.001f));
            recoveringToughness.Tick(2f);
            Assert.That(recoveringToughness.CurrentToughness, Is.EqualTo(120f));

            Combatant pressureTarget = CreateEnemyTarget(
                "PressureEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent pressureToughness,
                out _);
            EnemyControlApplicationResult finalResult = default;
            for (int i = 0; i < 5; i++)
            {
                finalResult = ApplyControl(pressureTarget, 30f, 0f, 0f, i * 0.1f);
            }

            Assert.That(finalResult.DidStagger, Is.True);
            Assert.That(pressureToughness.CurrentToughness, Is.Zero);
            Assert.That(pressureToughness.IsStaggered, Is.True);
        }

        /// <summary>同一执行只能整体提交一次削韧与硬控制。</summary>
        [Test]
        public void DuplicateExecutionCannotRepeatEitherEffect()
        {
            Combatant target = CreateEnemyTarget(
                "DuplicateEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out HardControlComponent hardControl);
            EnemyControlApplicationRequest request = CreateRequest(
                target,
                20f,
                4f,
                20f,
                AttackExecutionId.Create());

            EnemyControlApplicationResult first =
                EnemyControlApplicationResolver.ResolveAndApply(request, 0f);
            EnemyControlApplicationResult duplicate =
                EnemyControlApplicationResolver.ResolveAndApply(request, 0f);

            Assert.That(first.IsAccepted, Is.True);
            Assert.That(first.HardControlStatus, Is.EqualTo(HardControlApplicationStatus.Applied));
            Assert.That(duplicate.IsAccepted, Is.False);
            Assert.That(toughness.CurrentToughness, Is.EqualTo(100f));
            Assert.That(hardControl.ControlEndsAt, Is.EqualTo(4f));
        }

        /// <summary>同一攻击执行可以各提交一次生命伤害和合并控制效果。</summary>
        [Test]
        public void DamageAndControlUseIndependentExecutionDedupe()
        {
            Combatant target = CreateEnemyTarget(
                "DamageAndControlEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out HardControlComponent hardControl);
            AttackExecutionId executionId = AttackExecutionId.Create();
            DamageRequest damageRequest = new(
                executionId,
                instigator,
                null,
                target,
                ElementType.None,
                DamageDeliveryType.Direct,
                1f,
                HitPartType.Default,
                1f,
                1f,
                Vector3.zero,
                Vector3.forward,
                target.transform.position,
                Vector3.up,
                0f);
            EnemyControlApplicationRequest controlRequest = CreateRequest(
                target,
                10f,
                1f,
                20f,
                executionId);

            DamageResult damageResult = DamageResolver.ResolveAndApply(damageRequest);
            EnemyControlApplicationResult controlResult =
                EnemyControlApplicationResolver.ResolveAndApply(controlRequest, 0f);

            Assert.That(damageResult.IsApplied, Is.True);
            Assert.That(controlResult.IsAccepted, Is.True);
            Assert.That(toughness.CurrentToughness, Is.EqualTo(110f));
            Assert.That(hardControl.IsHardControlled, Is.True);
        }

        /// <summary>失衡期间保持零韧性并忽略削韧，结束时一次回满。</summary>
        [Test]
        public void StaggerPausesRecoveryAndRefillsAtExpiry()
        {
            Combatant target = CreateEnemyTarget(
                "StaggerEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out _);
            EnemyControlApplicationResult breakResult = ApplyControl(
                target,
                120f,
                0f,
                0f,
                0f);

            Assert.That(breakResult.DidStagger, Is.True);
            toughness.Tick(0.5f);
            Assert.That(toughness.CurrentToughness, Is.Zero);
            Assert.That(toughness.IsStaggered, Is.True);

            EnemyControlApplicationResult duringStagger = ApplyControl(
                target,
                10f,
                0f,
                0f,
                0.5f);
            Assert.That(duringStagger.IsAccepted, Is.True);
            Assert.That(duringStagger.AppliedToughnessDamage, Is.Zero);

            toughness.Tick(1f);
            Assert.That(toughness.IsStaggered, Is.False);
            Assert.That(toughness.CurrentToughness, Is.EqualTo(120f));
        }

        /// <summary>Normal 全时长、Elite 半时长；Boss 把同次两种削韧相加后只过一次阈值。</summary>
        [Test]
        public void EnemyTiersResolveFullHalfAndAdditiveBossToughness()
        {
            Combatant normalTarget = CreateEnemyTarget(
                "NormalControlEnemy",
                EnemyTier.Normal,
                out _,
                out _,
                out HardControlComponent normalControl);
            Combatant eliteTarget = CreateEnemyTarget(
                "EliteControlEnemy",
                EnemyTier.Elite,
                out _,
                out _,
                out HardControlComponent eliteControl);
            Combatant bossTarget = CreateEnemyTarget(
                "BossControlEnemy",
                EnemyTier.Boss,
                out _,
                out ToughnessComponent bossToughness,
                out HardControlComponent bossControl);

            EnemyControlApplicationResult normal = ApplyControl(normalTarget, 6f, 4f, 6f, 0f);
            EnemyControlApplicationResult elite = ApplyControl(eliteTarget, 6f, 4f, 6f, 0f);
            EnemyControlApplicationResult boss = ApplyControl(bossTarget, 6f, 4f, 6f, 0f);

            Assert.That(normal.AppliedToughnessDamage, Is.Zero);
            Assert.That(normalControl.ControlEndsAt, Is.EqualTo(4f));
            Assert.That(elite.AppliedToughnessDamage, Is.Zero);
            Assert.That(eliteControl.ControlEndsAt, Is.EqualTo(2f));
            Assert.That(boss.AppliedToughnessDamage, Is.EqualTo(12f));
            Assert.That(bossToughness.CurrentToughness, Is.EqualTo(108f));
            Assert.That(boss.HardControlStatus, Is.EqualTo(HardControlApplicationStatus.None));
            Assert.That(bossControl.IsHardControlled, Is.False);

            Combatant separateTarget = CreateEnemyTarget(
                "SeparateBossHits",
                EnemyTier.Boss,
                out _,
                out ToughnessComponent separateToughness,
                out _);
            ApplyControl(separateTarget, 6f, 0f, 0f, 0f);
            ApplyControl(separateTarget, 6f, 0f, 0f, 0.1f);
            Assert.That(separateToughness.CurrentToughness, Is.EqualTo(120f));
        }

        /// <summary>单一硬控制只接受更晚结束时间，不建立并行计时器。</summary>
        [Test]
        public void HardControlOnlyExtendsToLaterEndTime()
        {
            Combatant target = CreateEnemyTarget(
                "ExtendingControlEnemy",
                EnemyTier.Normal,
                out _,
                out _,
                out HardControlComponent hardControl);

            EnemyControlApplicationResult first = ApplyControl(target, 0f, 4f, 20f, 0f);
            float firstEndsAt = hardControl.ControlEndsAt;
            EnemyControlApplicationResult shorter = ApplyControl(target, 0f, 1f, 20f, 1f);
            float shorterEndsAt = hardControl.ControlEndsAt;
            EnemyControlApplicationResult longer = ApplyControl(target, 0f, 5f, 20f, 1f);

            Assert.That(first.HardControlStatus, Is.EqualTo(HardControlApplicationStatus.Applied));
            Assert.That(firstEndsAt, Is.EqualTo(4f));
            Assert.That(shorter.HardControlStatus, Is.EqualTo(HardControlApplicationStatus.Unchanged));
            Assert.That(shorterEndsAt, Is.EqualTo(4f));
            Assert.That(longer.HardControlStatus, Is.EqualTo(HardControlApplicationStatus.Extended));
            Assert.That(hardControl.ControlEndsAt, Is.EqualTo(6f));

            hardControl.Tick(6f);
            Assert.That(hardControl.IsHardControlled, Is.False);
        }

        /// <summary>生命耗尽后两个状态组件都清空并停止接收效果。</summary>
        [Test]
        public void HealthDepletionClearsBothStates()
        {
            Combatant target = CreateEnemyTarget(
                "DepletedControlEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out HardControlComponent hardControl);
            ApplyControl(target, 20f, 4f, 20f, 0f);

            SetPrivateField(target.Health, "currentHealth", 0f);
            toughness.Tick(0.1f);
            hardControl.Tick(0.1f);

            Assert.That(toughness.IsOperational, Is.False);
            Assert.That(toughness.CurrentToughness, Is.Zero);
            Assert.That(toughness.IsStaggered, Is.False);
            Assert.That(hardControl.IsOperational, Is.False);
            Assert.That(hardControl.IsHardControlled, Is.False);
        }

        /// <summary>Bootstrap 只给两个敌人装配控制状态，引用完整且玩家保持未装配。</summary>
        [Test]
        public void BootstrapHasExactEnemyControlMigration()
        {
            Scene bootstrapScene = default;
            try
            {
                bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
                GameObject[] roots = bootstrapScene.GetRootGameObjects();
                Combatant[] combatants = roots
                    .SelectMany(_root => _root.GetComponentsInChildren<Combatant>(true))
                    .ToArray();
                Combatant[] enemies = combatants
                    .Where(_combatant => _combatant.Faction == CombatFaction.Enemy)
                    .ToArray();
                Combatant player = combatants.Single(
                    _combatant => _combatant.Faction == CombatFaction.PlayerParty);

                Assert.That(enemies.Length, Is.EqualTo(2));
                for (int i = 0; i < enemies.Length; i++)
                {
                    EnemyStat enemyStat = enemies[i].GetComponent<EnemyStat>();
                    ToughnessComponent toughness = enemies[i].GetComponent<ToughnessComponent>();
                    HardControlComponent hardControl = enemies[i].GetComponent<HardControlComponent>();
                    EnemyRoot root = enemies[i].GetComponent<EnemyRoot>();
                    Assert.That(enemyStat, Is.Not.Null, enemies[i].name);
                    Assert.That(toughness, Is.Not.Null, enemies[i].name);
                    Assert.That(hardControl, Is.Not.Null, enemies[i].name);
                    Assert.That(root, Is.Not.Null, enemies[i].name);
                    Assert.That(root.Toughness, Is.SameAs(toughness));
                    Assert.That(root.HardControl, Is.SameAs(hardControl));

                    SerializedObject serializedToughness = new(toughness);
                    SerializedObject serializedHardControl = new(hardControl);
                    Assert.That(
                        serializedToughness.FindProperty("enemyStat").objectReferenceValue,
                        Is.SameAs(enemyStat));
                    Assert.That(
                        serializedHardControl.FindProperty("enemyStat").objectReferenceValue,
                        Is.SameAs(enemyStat));
                }

                Assert.That(player.GetComponent<ToughnessComponent>(), Is.Null);
                Assert.That(player.GetComponent<HardControlComponent>(), Is.Null);

                int missingScriptCount = roots
                    .SelectMany(_root => _root.GetComponentsInChildren<Transform>(true))
                    .Sum(_transform =>
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(_transform.gameObject));
                Assert.That(missingScriptCount, Is.Zero);
            }
            finally
            {
                if (bootstrapScene.IsValid())
                {
                    EditorSceneManager.CloseScene(bootstrapScene, true);
                }
            }
        }

        private Combatant CreateInstigator()
        {
            GameObject gameObject = CreateGameObject("ControlInstigator");
            ToughnessControlContractActorStat stat =
                gameObject.AddComponent<ToughnessControlContractActorStat>();
            stat.InitializeForTest();
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", CombatFaction.PlayerParty);
            EnsureCombatantLifecycle(combatant);
            return combatant;
        }

        private Combatant CreateEnemyTarget(
            string _name,
            EnemyTier _enemyTier,
            out EnemyStat _enemyStat,
            out ToughnessComponent _toughness,
            out HardControlComponent _hardControl)
        {
            GameObject gameObject = CreateGameObject(_name);
            _enemyStat = gameObject.AddComponent<EnemyStat>();
            ConfigureEnemyStat(_enemyStat, _enemyTier);
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", CombatFaction.Enemy);
            EnsureCombatantLifecycle(combatant);
            _toughness = gameObject.AddComponent<ToughnessComponent>();
            _hardControl = gameObject.AddComponent<HardControlComponent>();
            Assert.That(_toughness.TryInitialize(0f), Is.True);
            Assert.That(_hardControl.TryInitialize(0f), Is.True);
            EnemyRoot enemyRoot = gameObject.AddComponent<EnemyRoot>();
            enemyRoot.enabled = false;
            return combatant;
        }

        private EnemyControlApplicationResult ApplyControl(
            Combatant _target,
            float _baseToughnessDamage,
            float _hardControlDuration,
            float _bossToughnessDamage,
            float _time)
        {
            EnemyControlApplicationRequest request = CreateRequest(
                _target,
                _baseToughnessDamage,
                _hardControlDuration,
                _bossToughnessDamage,
                AttackExecutionId.Create());
            return EnemyControlApplicationResolver.ResolveAndApply(request, _time);
        }

        private EnemyControlApplicationRequest CreateRequest(
            Combatant _target,
            float _baseToughnessDamage,
            float _hardControlDuration,
            float _bossToughnessDamage,
            AttackExecutionId _executionId)
        {
            return new EnemyControlApplicationRequest(
                _executionId,
                instigator,
                _target,
                _baseToughnessDamage,
                _hardControlDuration,
                _bossToughnessDamage);
        }

        private EnemyDefinitionConfig CreateEnemyDefinition(
            string _configId,
            EnemyTier _enemyTier,
            float _maxToughness,
            float _recoveryPerSecond)
        {
            EnemyBaseStatConfig baseStat = CreateScriptableObject<EnemyBaseStatConfig>();
            SetPrivateField(baseStat, "configId", _configId + "Base");
            SetPrivateField(baseStat, "maxToughness", _maxToughness);
            SetPrivateField(baseStat, "toughnessRecoveryPerSecond", _recoveryPerSecond);
            SetPrivateField(baseStat, "minimumToughnessDamage", 10f);
            SetPrivateField(baseStat, "staggerDuration", 1f);

            EnemyMovementConfig movement = CreateScriptableObject<EnemyMovementConfig>();
            SetPrivateField(movement, "configId", _configId + "Movement");
            ResistanceSetConfig resistance = CreateScriptableObject<ResistanceSetConfig>();
            SetPrivateField(resistance, "configId", _configId + "Resistance");

            EnemyDefinitionConfig definition = CreateScriptableObject<EnemyDefinitionConfig>();
            SetPrivateField(definition, "configId", _configId);
            SetPrivateField(definition, "enemyTier", _enemyTier);
            SetPrivateField(definition, "enemyBaseStatConfig", baseStat);
            SetPrivateField(definition, "enemyMovementConfig", movement);
            SetPrivateField(definition, "enemyResistanceSetConfig", resistance);
            return definition;
        }

        private static void ConfigureEnemyStat(EnemyStat _enemyStat, EnemyTier _enemyTier)
        {
            SetPrivateField(_enemyStat, "maxHealth", 100f);
            SetPrivateField(_enemyStat, "damageTakenMultiplier", 1f);
            SetPrivateField(_enemyStat, "healingTakenMultiplier", 1f);
            SetPrivateField(_enemyStat, "isInitialized", true);
            SetPrivateField(_enemyStat, "enemyTier", _enemyTier);
            SetPrivateField(_enemyStat, "maxToughness", 120f);
            SetPrivateField(_enemyStat, "toughnessRecoveryPerSecond", 24f);
            SetPrivateField(_enemyStat, "minimumToughnessDamage", 10f);
            SetPrivateField(_enemyStat, "staggerDuration", 1f);
        }

        private TConfig CreateScriptableObject<TConfig>()
            where TConfig : ScriptableObject
        {
            TConfig config = ScriptableObject.CreateInstance<TConfig>();
            ownedObjects.Add(config);
            return config;
        }

        private GameObject CreateGameObject(string _name)
        {
            GameObject gameObject = new(_name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static void EnsureCombatantLifecycle(Combatant _combatant)
        {
            InvokeLifecycle(_combatant, "Awake");
            if (_combatant.Id.IsValid == false)
            {
                InvokeLifecycle(_combatant, "OnEnable");
            }
        }

        private static void InvokeLifecycle(object _target, string _methodName)
        {
            MethodInfo method = _target.GetType().GetMethod(
                _methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing lifecycle method {_methodName}.");
            method.Invoke(_target, null);
        }

        private static void SetPrivateField(object _target, string _fieldName, object _value)
        {
            Type currentType = _target.GetType();
            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(
                    _fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(_target, _value);
                    return;
                }

                currentType = currentType.BaseType;
            }

            Assert.Fail($"Field '{_fieldName}' was not found on {_target.GetType().FullName}.");
        }
    }
}
