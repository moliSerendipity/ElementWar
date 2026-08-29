using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Definition.Enemy;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Enemy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode.Gameplay.Combat
{
    /// <summary>为 PlayMode 敌人控制请求建立最小合法玩家来源数值。</summary>
    public sealed class ToughnessControlPlayModeActorStat : ActorStatBase
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

    /// <summary>验证真实启停复用以及 EnemyBrain 的受控阻断行为。</summary>
    public sealed class ToughnessControlPlayModeTests
    {
        private readonly List<Object> ownedObjects = new();
        private Combatant instigator;

        /// <summary>为每个用例创建一个活动玩家来源。</summary>
        [SetUp]
        public void SetUp()
        {
            instigator = CreateInstigator();
        }

        /// <summary>按 Unity 销毁时序清理测试对象。</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.Destroy(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
            instigator = null;
            yield return null;
        }

        /// <summary>禁用复用建立新 TargetId、重置两个状态，并拒绝旧请求快照。</summary>
        [UnityTest]
        public IEnumerator DisableAndReuseRejectsOldControlRequest()
        {
            Combatant target = CreateEnemyTarget(
                "ReusableControlEnemy",
                EnemyTier.Normal,
                out _,
                out ToughnessComponent toughness,
                out HardControlComponent hardControl);
            yield return null;

            CombatantId oldTargetId = target.Id;
            EnemyControlApplicationRequest oldRequest = CreateRequest(
                target,
                20f,
                1f,
                20f);
            Assert.That(
                EnemyControlApplicationResolver.ResolveAndApply(oldRequest, Time.time).IsAccepted,
                Is.True);

            target.gameObject.SetActive(false);
            yield return null;
            Assert.That(target.Id.IsValid, Is.False);
            Assert.That(toughness.IsOperational, Is.False);
            Assert.That(hardControl.IsOperational, Is.False);

            target.gameObject.SetActive(true);
            yield return null;
            Assert.That(target.Id.IsValid, Is.True);
            Assert.That(target.Id, Is.Not.EqualTo(oldTargetId));
            Assert.That(toughness.IsOperational, Is.True);
            Assert.That(toughness.CurrentToughness, Is.EqualTo(120f));
            Assert.That(hardControl.IsOperational, Is.True);
            Assert.That(hardControl.IsHardControlled, Is.False);

            EnemyControlApplicationResult staleResult =
                EnemyControlApplicationResolver.ResolveAndApply(oldRequest, Time.time);
            Assert.That(staleResult.IsAccepted, Is.False);

            Assert.That(
                EnemyControlApplicationResolver.ResolveAndApply(
                    CreateRequest(target, 10f, 0.1f, 20f),
                    Time.time).IsAccepted,
                Is.True);
        }

        /// <summary>EnemyBrain 受控时取消攻击，控制结束后继续同一状态的攻击求值。</summary>
        [UnityTest]
        public IEnumerator EnemyBrainCancelsAttackAndResumesAfterControlEnds()
        {
            Combatant target = CreateEnemyTarget(
                "ControlledBrainEnemy",
                EnemyTier.Normal,
                out EnemyStat enemyStat,
                out _,
                out HardControlComponent hardControl);
            target.gameObject.SetActive(false);
            NavMeshAgent navMeshAgent = target.gameObject.AddComponent<NavMeshAgent>();
            navMeshAgent.enabled = false;
            EnemySensor sensor = target.gameObject.AddComponent<EnemySensor>();
            EnemyLocomotion locomotion = target.gameObject.AddComponent<EnemyLocomotion>();
            EnemyAttack attack = target.gameObject.AddComponent<EnemyAttack>();
            EnemyBrain brain = target.gameObject.AddComponent<EnemyBrain>();
            EnemyAttackConfig attackConfig = ScriptableObject.CreateInstance<EnemyAttackConfig>();
            ownedObjects.Add(attackConfig);
            SetPrivateField(attackConfig, "minUseRange", 0f);
            SetPrivateField(attackConfig, "maxUseRange", 2f);
            SetPrivateField(attackConfig, "selectionWeight", 1);
            SetPrivateField(attack, "attackConfigs", new[] { attackConfig });

            target.gameObject.SetActive(true);
            yield return null;
            instigator.transform.position = target.transform.position + Vector3.forward;
            sensor.Initialize(enemyStat);
            locomotion.Initialize(enemyStat);
            attack.Initialize(enemyStat);
            brain.Initialize(enemyStat);
            SetPrivateField(sensor, "currentTarget", instigator.transform);
            SetPrivateField(sensor, "hasLineOfSight", true);
            SetPrivateField(sensor, "nextScanTime", Time.time + 10f);
            SetPrivateField(brain, "currentState", EnemyState.Attack);
            Assert.That(attack.TryBeginAttack(1f), Is.True);

            EnemyControlApplicationResult result =
                EnemyControlApplicationResolver.ResolveAndApply(
                    CreateRequest(target, 0f, 0.1f, 20f),
                    Time.time);
            Assert.That(result.HardControlStatus, Is.EqualTo(HardControlApplicationStatus.Applied));
            Assert.That(brain.IsBehaviorBlockedByControl, Is.True);

            brain.Tick(0.016f);
            Assert.That(attack.IsAttacking, Is.False);
            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Attack));

            yield return new WaitForSeconds(0.12f);
            hardControl.Tick(Time.time);
            Assert.That(brain.IsBehaviorBlockedByControl, Is.False);

            SetPrivateField(sensor, "nextScanTime", Time.time + 10f);
            InvokePrivate(brain, "TickAttack", 0.016f);
            Assert.That(attack.IsAttacking, Is.True);
            Assert.That(brain.CurrentState, Is.EqualTo(EnemyState.Attack));
        }

        private Combatant CreateInstigator()
        {
            GameObject gameObject = CreateGameObject("ControlPlayModeInstigator");
            ToughnessControlPlayModeActorStat stat =
                gameObject.AddComponent<ToughnessControlPlayModeActorStat>();
            stat.InitializeForTest();
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", CombatFaction.PlayerParty);
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
            _toughness = gameObject.AddComponent<ToughnessComponent>();
            _hardControl = gameObject.AddComponent<HardControlComponent>();
            Assert.That(_toughness.TryInitialize(Time.time), Is.True);
            Assert.That(_hardControl.TryInitialize(Time.time), Is.True);
            EnemyRoot enemyRoot = gameObject.AddComponent<EnemyRoot>();
            enemyRoot.enabled = false;
            return combatant;
        }

        private EnemyControlApplicationRequest CreateRequest(
            Combatant _target,
            float _baseToughnessDamage,
            float _hardControlDuration,
            float _bossToughnessDamage)
        {
            return new EnemyControlApplicationRequest(
                AttackExecutionId.Create(),
                instigator,
                _target,
                _baseToughnessDamage,
                _hardControlDuration,
                _bossToughnessDamage);
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
            SetPrivateField(_enemyStat, "chaseSpeed", 4f);
            SetPrivateField(_enemyStat, "turnSharpness", 12f);
            SetPrivateField(_enemyStat, "stopDistance", 1.5f);
            SetPrivateField(_enemyStat, "detectRange", 15f);
            SetPrivateField(_enemyStat, "loseTargetRange", 20f);
            SetPrivateField(_enemyStat, "targetMemoryDuration", 1.5f);
            SetPrivateField(_enemyStat, "scanInterval", 10f);
            SetPrivateField(_enemyStat, "attackCooldown", 0f);
            SetPrivateField(_enemyStat, "attackSpeedMultiplier", 1f);
        }

        private GameObject CreateGameObject(string _name)
        {
            GameObject gameObject = new(_name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokePrivate(object _target, string _methodName, params object[] _arguments)
        {
            MethodInfo method = _target.GetType().GetMethod(
                _methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {_methodName}.");
            method.Invoke(_target, _arguments);
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
