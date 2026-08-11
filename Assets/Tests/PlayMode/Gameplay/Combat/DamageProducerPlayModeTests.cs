using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Definition.Combat;
using Game.Definition.Enemy;
using Game.Foundation.Events;
using Game.Gameplay.Camera;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Enemy;
using Game.Gameplay.Weapon;
using Game.Gameplay.Weapon.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Gameplay.Combat
{
    /// <summary>
    /// 为生产入口 PlayMode 测试提供可直接初始化的目标数值组件。
    /// </summary>
    public sealed class DamageProducerTestActorStat : ActorStatBase
    {
        /// <summary>
        /// 使用无防御、无抗性的目标数值完成初始化。
        /// </summary>
        public void InitializeForTest(float _maxHealth)
        {
            CommitCombatStatInitialization(
                _maxHealth,
                0f,
                10f,
                0f,
                100f,
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

    /// <summary>
    /// 为 Hitscan 测试提供确定的相机中心瞄点。
    /// </summary>
    public sealed class DamageProducerTestAimPointProvider : MonoBehaviour, ICameraAimPointProvider
    {
        /// <summary>测试当前要返回的瞄点上下文。</summary>
        public CameraAimPointContext Context { get; set; }

        /// <summary>
        /// 始终返回测试设置的有效瞄点。
        /// </summary>
        public bool TryGetCameraAimPointContext(out CameraAimPointContext _cameraAimPointContext)
        {
            _cameraAimPointContext = Context;
            return true;
        }
    }

    /// <summary>
    /// 验证当前 Hitscan 步枪与 EnemyAttack 均通过正式伤害契约提交结果。
    /// </summary>
    public sealed class DamageProducerPlayModeTests
    {
        private readonly List<UnityEngine.Object> ownedObjects = new();
        private GameEventBus eventBus;

        /// <summary>
        /// 为每个用例建立隔离的同步事实总线。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(GameEventBus.Instance, Is.Null);
            GameObject eventBusGameObject = CreateGameObject(nameof(DamageProducerPlayModeTests) + "EventBus");
            eventBus = eventBusGameObject.AddComponent<GameEventBus>();
        }

        /// <summary>
        /// 销毁测试创建的运行时对象并等待 Unity 完成延迟销毁。
        /// </summary>
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
            eventBus = null;
            yield return null;

            Assert.That(GameEventBus.Instance, Is.Null);
        }

        /// <summary>
        /// 当前步枪成功命中时必须把角色根与 WeaponRuntime 分别写入归属字段。
        /// </summary>
        [UnityTest]
        public IEnumerator HitscanRifleUsesCharacterInstigatorAndWeaponRuntimeSource()
        {
            GameObject targetGameObject = CreateGameObject("RifleTarget");
            targetGameObject.transform.position = new Vector3(0f, 0f, 5f);
            targetGameObject.AddComponent<BoxCollider>();
            Combatant targetCombatant = AddInitializedCombatant(
                targetGameObject,
                100f,
                CombatFaction.Enemy);
            HealthComponent targetHealth = targetCombatant.Health;

            GameObject providerGameObject = CreateGameObject("AimPointProvider");
            DamageProducerTestAimPointProvider provider =
                providerGameObject.AddComponent<DamageProducerTestAimPointProvider>();
            provider.Context = new CameraAimPointContext(
                Vector3.zero,
                Vector3.forward,
                targetGameObject.transform.position,
                true,
                5f);

            GameObject characterGameObject = CreateGameObject("CharacterInstigator");
            characterGameObject.AddComponent<CharacterStat>();
            Combatant characterCombatant = characterGameObject.AddComponent<Combatant>();
            SetPrivateField(characterCombatant, "faction", CombatFaction.PlayerParty);

            GameObject weaponGameObject = CreateGameObject("RifleSource");
            weaponGameObject.transform.SetParent(characterGameObject.transform, false);
            WeaponAmmoComponent ammoComponent = weaponGameObject.AddComponent<WeaponAmmoComponent>();

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[WeaponRuntime\] 自动初始化失败：当前没有可用的共享 ConfigService.*"));
            WeaponRuntime weaponRuntime = weaponGameObject.AddComponent<WeaponRuntime>();
            ammoComponent.InitializeFromCapacity(5, 0);
            SetPrivateField(weaponRuntime, "damage", 25f);
            SetPrivateField(weaponRuntime, "fireInterval", 0.1f);
            SetPrivateField(weaponRuntime, "isInitialized", true);

            weaponGameObject.AddComponent<HitScanService>();
            WeaponFireExecutor fireExecutor = weaponGameObject.AddComponent<WeaponFireExecutor>();

            int damageEventCount = 0;
            DamageResult receivedResult = default;
            WeaponFiredEvent receivedWeaponFiredEvent = default;
            eventBus.Subscribe<DamageAppliedEvent>(_eventData =>
            {
                damageEventCount++;
                receivedResult = _eventData.DamageResult;
            });
            eventBus.Subscribe<WeaponFiredEvent>(_eventData => receivedWeaponFiredEvent = _eventData);

            Physics.SyncTransforms();
            yield return null;

            WeaponFramePlan firePlan = WeaponFramePlan.CreateResolved(
                true,
                false,
                false,
                false,
                false,
                0f,
                WeaponFireFailureReason.None,
                WeaponReloadFailureReason.None);

            bool didFire = fireExecutor.Execute(
                firePlan,
                null,
                CharacterFramePlan.Empty,
                Time.time);

            Assert.That(didFire, Is.True);
            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(receivedResult.ExecutionId.IsValid, Is.True);
            Assert.That(receivedResult.ExecutionId, Is.EqualTo(receivedWeaponFiredEvent.ExecutionId));
            Assert.That(receivedResult.InstigatorId, Is.EqualTo(characterCombatant.Id));
            Assert.That(receivedResult.TargetId, Is.EqualTo(targetCombatant.Id));
            Assert.That(receivedResult.Instigator, Is.SameAs(characterGameObject));
            Assert.That(receivedResult.SourceObject, Is.SameAs(weaponRuntime));
            Assert.That(receivedResult.Target, Is.SameAs(targetHealth));
            Assert.That(receivedResult.Element, Is.EqualTo(ElementType.None));
            Assert.That(receivedResult.Delivery, Is.EqualTo(DamageDeliveryType.Direct));
            Assert.That(receivedResult.FinalDamage, Is.EqualTo(25f).Within(0.0001f));
            Assert.That(targetHealth.CurrentHealth, Is.EqualTo(75f).Within(0.0001f));
        }

        /// <summary>
        /// 当前敌人 Strike 命中时必须保留敌人责任实体和实际攻击配置来源。
        /// </summary>
        [UnityTest]
        public IEnumerator EnemyAttackUsesEnemyInstigatorAndAttackConfigSource()
        {
            GameObject targetGameObject = CreateGameObject("EnemyAttackTarget");
            targetGameObject.transform.position = new Vector3(0f, 0f, 1.2f);
            targetGameObject.AddComponent<BoxCollider>();
            Combatant targetCombatant = AddInitializedCombatant(
                targetGameObject,
                100f,
                CombatFaction.PlayerParty);
            HealthComponent targetHealth = targetCombatant.Health;

            GameObject enemyGameObject = CreateGameObject("EnemyInstigator");
            EnemyStat enemyStat = enemyGameObject.AddComponent<EnemyStat>();
            SetPrivateField(enemyStat, "attackPower", 20f);
            SetPrivateField(enemyStat, "isInitialized", true);
            Combatant enemyCombatant = enemyGameObject.AddComponent<Combatant>();
            SetPrivateField(enemyCombatant, "faction", CombatFaction.Enemy);

            EnemyAttackConfig attackConfig = ScriptableObject.CreateInstance<EnemyAttackConfig>();
            ownedObjects.Add(attackConfig);
            SetPrivateField(attackConfig, "damageMultiplier", 1f);
            SetPrivateField(attackConfig, "element", ElementType.None);
            SetPrivateField(attackConfig, "delivery", DamageDeliveryType.Direct);
            SetPrivateField(attackConfig, "damageNormalizedTime", 0.05f);
            SetPrivateField(attackConfig, "shapeType", AttackShapeType.Sphere);
            SetPrivateField(attackConfig, "offsetDistance", 1.2f);
            SetPrivateField(attackConfig, "radius", 0.75f);
            SetPrivateField(attackConfig, "minUseRange", 0f);
            SetPrivateField(attackConfig, "maxUseRange", 2f);
            SetPrivateField(attackConfig, "selectionWeight", 1);

            EnemyAttack enemyAttack = enemyGameObject.AddComponent<EnemyAttack>();
            SetPrivateField(enemyAttack, "attackConfigs", new[] { attackConfig });
            SetPrivateField(enemyAttack, "attackTargetMask", (LayerMask)(~0));
            enemyAttack.Initialize(enemyStat);

            int damageEventCount = 0;
            DamageResult receivedResult = default;
            eventBus.Subscribe<DamageAppliedEvent>(_eventData =>
            {
                damageEventCount++;
                receivedResult = _eventData.DamageResult;
            });

            Physics.SyncTransforms();
            yield return null;

            Assert.That(enemyAttack.TryBeginAttack(1.2f), Is.True);
            enemyAttack.Tick(1f);
            enemyAttack.Tick(0.01f);

            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(receivedResult.ExecutionId.IsValid, Is.True);
            Assert.That(receivedResult.ExecutionId, Is.EqualTo(enemyAttack.ActiveExecutionId));
            Assert.That(receivedResult.InstigatorId, Is.EqualTo(enemyCombatant.Id));
            Assert.That(receivedResult.TargetId, Is.EqualTo(targetCombatant.Id));
            Assert.That(receivedResult.Instigator, Is.SameAs(enemyGameObject));
            Assert.That(receivedResult.SourceObject, Is.SameAs(attackConfig));
            Assert.That(receivedResult.Target, Is.SameAs(targetHealth));
            Assert.That(receivedResult.Element, Is.EqualTo(ElementType.None));
            Assert.That(receivedResult.Delivery, Is.EqualTo(DamageDeliveryType.Direct));
            Assert.That(receivedResult.FinalDamage, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(targetHealth.CurrentHealth, Is.EqualTo(80f).Within(0.0001f));
        }

        /// <summary>
        /// AOE 扫到同一角色多个 Collider 时只提交一次，并拒绝同阵营敌人目标。
        /// </summary>
        [UnityTest]
        public IEnumerator EnemyAreaAttackDeduplicatesMultiColliderTargetAndRejectsFriendlyTarget()
        {
            GameObject playerTargetGameObject = CreateGameObject("MultiColliderPlayerTarget");
            playerTargetGameObject.transform.position = new Vector3(0f, 0f, 1.2f);
            Combatant playerTarget = AddInitializedCombatant(
                playerTargetGameObject,
                100f,
                CombatFaction.PlayerParty);

            GameObject firstColliderGameObject = CreateGameObject("PlayerColliderA");
            firstColliderGameObject.transform.SetParent(playerTargetGameObject.transform, false);
            firstColliderGameObject.transform.localPosition = new Vector3(-0.1f, 0f, 0f);
            firstColliderGameObject.AddComponent<BoxCollider>();

            GameObject secondColliderGameObject = CreateGameObject("PlayerColliderB");
            secondColliderGameObject.transform.SetParent(playerTargetGameObject.transform, false);
            secondColliderGameObject.transform.localPosition = new Vector3(0.1f, 0f, 0f);
            secondColliderGameObject.AddComponent<SphereCollider>();

            GameObject friendlyTargetGameObject = CreateGameObject("FriendlyEnemyTarget");
            friendlyTargetGameObject.transform.position = new Vector3(0.35f, 0f, 1.2f);
            friendlyTargetGameObject.AddComponent<BoxCollider>();
            Combatant friendlyTarget = AddInitializedCombatant(
                friendlyTargetGameObject,
                100f,
                CombatFaction.Enemy);

            GameObject enemyGameObject = CreateGameObject("AreaAttackEnemy");
            EnemyStat enemyStat = enemyGameObject.AddComponent<EnemyStat>();
            SetPrivateField(enemyStat, "attackPower", 20f);
            SetPrivateField(enemyStat, "isInitialized", true);
            Combatant enemyCombatant = enemyGameObject.AddComponent<Combatant>();
            SetPrivateField(enemyCombatant, "faction", CombatFaction.Enemy);

            EnemyAttackConfig attackConfig = ScriptableObject.CreateInstance<EnemyAttackConfig>();
            ownedObjects.Add(attackConfig);
            SetPrivateField(attackConfig, "damageMultiplier", 1f);
            SetPrivateField(attackConfig, "element", ElementType.None);
            SetPrivateField(attackConfig, "delivery", DamageDeliveryType.Direct);
            SetPrivateField(attackConfig, "isAreaOfEffect", true);
            SetPrivateField(attackConfig, "damageNormalizedTime", 0.05f);
            SetPrivateField(attackConfig, "shapeType", AttackShapeType.Sphere);
            SetPrivateField(attackConfig, "offsetDistance", 1.2f);
            SetPrivateField(attackConfig, "radius", 0.8f);
            SetPrivateField(attackConfig, "minUseRange", 0f);
            SetPrivateField(attackConfig, "maxUseRange", 2f);
            SetPrivateField(attackConfig, "selectionWeight", 1);

            EnemyAttack enemyAttack = enemyGameObject.AddComponent<EnemyAttack>();
            SetPrivateField(enemyAttack, "attackConfigs", new[] { attackConfig });
            SetPrivateField(enemyAttack, "attackTargetMask", (LayerMask)(~0));
            enemyAttack.Initialize(enemyStat);

            int damageEventCount = 0;
            DamageResult receivedResult = default;
            eventBus.Subscribe<DamageAppliedEvent>(_eventData =>
            {
                damageEventCount++;
                receivedResult = _eventData.DamageResult;
            });

            Physics.SyncTransforms();
            yield return null;

            Assert.That(enemyAttack.TryBeginAttack(1.2f), Is.True);
            enemyAttack.Tick(1f);
            enemyAttack.Tick(0.01f);

            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(receivedResult.ExecutionId.IsValid, Is.True);
            Assert.That(receivedResult.TargetId, Is.EqualTo(playerTarget.Id));
            Assert.That(playerTarget.Health.CurrentHealth, Is.EqualTo(80f).Within(0.0001f));
            Assert.That(friendlyTarget.Health.CurrentHealth, Is.EqualTo(100f).Within(0.0001f));
        }

        /// <summary>
        /// 实际禁用/重新启用会更新目标身份、拒绝旧请求并清除上一生命周期去重记录。
        /// </summary>
        [UnityTest]
        public IEnumerator CombatantReuseRejectsStaleRequestAndAcceptsCurrentIdentity()
        {
            GameObject instigatorGameObject = CreateGameObject("LifecycleInstigator");
            Combatant instigator = instigatorGameObject.AddComponent<Combatant>();
            SetPrivateField(instigator, "faction", CombatFaction.PlayerParty);

            GameObject targetGameObject = CreateGameObject("LifecycleTarget");
            Combatant target = AddInitializedCombatant(targetGameObject, 100f, CombatFaction.Enemy);
            AttackExecutionId executionId = AttackExecutionId.Create();
            DamageRequest staleRequest = CreateRequest(executionId, instigator, target, 20f);

            DamageResult firstResult = DamageResolver.ResolveAndApply(staleRequest);
            CombatantId previousTargetId = target.Id;

            targetGameObject.SetActive(false);
            yield return null;
            Assert.That(target.Id.IsValid, Is.False);

            targetGameObject.SetActive(true);
            yield return null;

            DamageResult staleResult = DamageResolver.ResolveAndApply(staleRequest);
            DamageResult currentResult = DamageResolver.ResolveAndApply(
                CreateRequest(executionId, instigator, target, 20f));

            Assert.That(firstResult.IsApplied, Is.True);
            Assert.That(target.Id.IsValid, Is.True);
            Assert.That(target.Id, Is.Not.EqualTo(previousTargetId));
            Assert.That(staleResult.IsApplied, Is.False);
            Assert.That(staleResult.RejectionReason, Is.EqualTo(DamageRejectionReason.InvalidTarget));
            Assert.That(currentResult.IsApplied, Is.True);
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(60f).Within(0.0001f));
        }

        private Combatant AddInitializedCombatant(
            GameObject _gameObject,
            float _maxHealth,
            CombatFaction _faction)
        {
            DamageProducerTestActorStat stat = _gameObject.AddComponent<DamageProducerTestActorStat>();
            stat.InitializeForTest(_maxHealth);
            HealthComponent health = _gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = _gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
            Assert.That(combatant.Id.IsValid, Is.True);
            return combatant;
        }

        private static DamageRequest CreateRequest(
            AttackExecutionId _executionId,
            Combatant _instigator,
            Combatant _target,
            float _baseDamage)
        {
            return new DamageRequest(
                _executionId,
                _instigator,
                _instigator,
                _target,
                ElementType.None,
                DamageDeliveryType.Direct,
                _baseDamage,
                HitPartType.Default,
                1f,
                1f,
                Vector3.zero,
                Vector3.forward,
                Vector3.forward,
                Vector3.back,
                Time.time);
        }

        private GameObject CreateGameObject(string _name)
        {
            GameObject gameObject = new(_name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField(object _target, string _fieldName, object _value)
        {
            System.Type currentType = _target.GetType();
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
