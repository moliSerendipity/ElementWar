using System.Reflection;
using Game.Definition.Combat;
using Game.Foundation.Events;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Gameplay.Combat
{
    /// <summary>
    /// 为伤害契约测试提供可直接初始化的最小运行时数值组件。
    /// </summary>
    public sealed class DamageContractTestActorStat : ActorStatBase
    {
        /// <summary>
        /// 使用测试指定的防守侧数值完成运行时初始化。
        /// </summary>
        public void InitializeForTest(
            float _maxHealth = 1000f,
            float _defense = 0f,
            float _physicalResistance = 0f,
            float _fireResistance = 0f,
            float _waterResistance = 0f,
            float _electricResistance = 0f,
            float _iceResistance = 0f,
            float _explosionResistance = 0f,
            float _damageTakenMultiplier = 1f)
        {
            CommitCombatStatInitialization(
                _maxHealth,
                0f,
                10f,
                _defense,
                100f,
                _damageTakenMultiplier,
                1f,
                _physicalResistance,
                _fireResistance,
                _waterResistance,
                _electricResistance,
                _iceResistance,
                _explosionResistance);
        }
    }

    /// <summary>
    /// 验证伤害语义、归属、确定性倍率和生命耗尽的 EditMode 行为契约。
    /// </summary>
    public sealed class DamageContractTests
    {
        private GameObject eventBusGameObject;
        private GameObject targetGameObject;
        private GameObject instigatorGameObject;
        private GameObject sourceGameObject;
        private GameEventBus eventBus;
        private DamageContractTestActorStat targetStat;
        private HealthComponent targetHealth;
        private Combatant targetCombatant;
        private Combatant instigatorCombatant;

        /// <summary>
        /// 为每个用例创建隔离的事件总线、责任对象、来源对象和生命目标。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(GameEventBus.Instance, Is.Null);

            eventBusGameObject = new GameObject(nameof(DamageContractTests) + "EventBus");
            eventBus = eventBusGameObject.AddComponent<GameEventBus>();
            InvokeLifecycle(eventBus, "Awake");
            Assert.That(GameEventBus.Instance, Is.SameAs(eventBus));

            instigatorGameObject = new GameObject("Instigator");
            instigatorCombatant = instigatorGameObject.AddComponent<Combatant>();
            SetPrivateField(instigatorCombatant, "faction", CombatFaction.PlayerParty);
            EnsureCombatantLifecycle(instigatorCombatant);

            sourceGameObject = new GameObject("Source");
            targetGameObject = new GameObject("Target");
            targetStat = targetGameObject.AddComponent<DamageContractTestActorStat>();
            targetStat.InitializeForTest();
            targetHealth = targetGameObject.AddComponent<HealthComponent>();

            Assert.That(targetHealth.TryInitialize(), Is.True);
            targetCombatant = targetGameObject.AddComponent<Combatant>();
            SetPrivateField(targetCombatant, "faction", CombatFaction.Enemy);
            EnsureCombatantLifecycle(targetCombatant);
        }

        /// <summary>
        /// 销毁所有测试对象并清除单例与订阅，避免用例互相污染。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (eventBus != null)
            {
                InvokeLifecycle(eventBus, "OnDestroy");
            }

            DestroyImmediate(targetGameObject);
            DestroyImmediate(instigatorGameObject);
            DestroyImmediate(sourceGameObject);
            DestroyImmediate(eventBusGameObject);

            targetHealth = null;
            targetStat = null;
            targetCombatant = null;
            instigatorCombatant = null;
            eventBus = null;
        }

        /// <summary>
        /// 结果与事件必须完整保留责任角色、来源对象和正交伤害语义。
        /// </summary>
        [Test]
        public void ResolveAndApplyPreservesAttributionAndDamageSemantics()
        {
            DamageAppliedEvent receivedDamageEvent = default;
            int damageEventCount = 0;
            eventBus.Subscribe<DamageAppliedEvent>(_eventData =>
            {
                receivedDamageEvent = _eventData;
                damageEventCount++;
            });

            DamageResult result = DamageResolver.ResolveAndApply(CreateRequest(100f));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.RejectionReason, Is.EqualTo(DamageRejectionReason.None));
            Assert.That(result.ExecutionId.IsValid, Is.True);
            Assert.That(result.InstigatorId, Is.EqualTo(instigatorCombatant.Id));
            Assert.That(result.TargetId, Is.EqualTo(targetCombatant.Id));
            Assert.That(result.Instigator, Is.SameAs(instigatorGameObject));
            Assert.That(result.SourceObject, Is.SameAs(sourceGameObject));
            Assert.That(result.Element, Is.EqualTo(ElementType.None));
            Assert.That(result.Delivery, Is.EqualTo(DamageDeliveryType.Direct));
            Assert.That(result.FinalDamage, Is.EqualTo(100f).Within(0.0001f));
            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(receivedDamageEvent.DamageResult.ExecutionId, Is.EqualTo(result.ExecutionId));
            Assert.That(receivedDamageEvent.DamageResult.InstigatorId, Is.EqualTo(result.InstigatorId));
            Assert.That(receivedDamageEvent.DamageResult.TargetId, Is.EqualTo(result.TargetId));
            Assert.That(receivedDamageEvent.DamageResult.Instigator, Is.SameAs(instigatorGameObject));
            Assert.That(receivedDamageEvent.DamageResult.SourceObject, Is.SameAs(sourceGameObject));
            Assert.That(receivedDamageEvent.DamageResult.HitPartType, Is.EqualTo(result.HitPartType));
            Assert.That(receivedDamageEvent.DamageResult.HitPoint, Is.EqualTo(result.HitPoint));
            Assert.That(receivedDamageEvent.DamageResult.HitNormal, Is.EqualTo(result.HitNormal));
        }

        /// <summary>
        /// 完全相同的输入在生命重置后必须产生完全相同的结果。
        /// </summary>
        [Test]
        public void EquivalentRequestsAfterRestoreProduceIdenticalDamage()
        {
            DamageResult firstResult = DamageResolver.ResolveAndApply(CreateRequest(73f));
            targetHealth.RestoreFullHealth();
            DamageResult secondResult = DamageResolver.ResolveAndApply(CreateRequest(73f));

            Assert.That(firstResult.FinalDamage, Is.EqualTo(secondResult.FinalDamage));
            Assert.That(firstResult.RemainingHealth, Is.EqualTo(secondResult.RemainingHealth));
        }

        /// <summary>
        /// 头部和弱点只应用明确配置的确定性倍率。
        /// </summary>
        [Test]
        public void HeadAndWeakPointApplyDeterministicMultipliers()
        {
            DamageResult defaultResult = DamageResolver.ResolveAndApply(
                CreateRequest(10f, HitPartType.Default, 2f, 3f));
            targetHealth.RestoreFullHealth();
            DamageResult headResult = DamageResolver.ResolveAndApply(
                CreateRequest(10f, HitPartType.Head, 2f, 3f));
            targetHealth.RestoreFullHealth();
            DamageResult weakPointResult = DamageResolver.ResolveAndApply(
                CreateRequest(10f, HitPartType.WeakPoint, 2f, 3f));

            Assert.That(defaultResult.FinalDamage, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(headResult.FinalDamage, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(weakPointResult.FinalDamage, Is.EqualTo(30f).Within(0.0001f));
        }

        /// <summary>
        /// 火元素爆炸必须分别应用火抗与爆炸抗性，而不是二选一。
        /// </summary>
        [Test]
        public void FireExplosionAppliesElementAndDeliveryResistanceIndependently()
        {
            targetStat.SetFireResistance(0.25f);
            targetStat.SetExplosionResistance(0.2f);

            DamageResult result = DamageResolver.ResolveAndApply(CreateRequest(
                100f,
                _element: ElementType.Fire,
                _delivery: DamageDeliveryType.Explosion));

            Assert.That(result.FinalDamage, Is.EqualTo(60f).Within(0.0001f));
        }

        /// <summary>
        /// Water 必须拥有独立抗性映射，即使本阶段尚未实现元素附着。
        /// </summary>
        [Test]
        public void WaterUsesWaterResistance()
        {
            targetStat.SetWaterResistance(0.4f);

            DamageResult result = DamageResolver.ResolveAndApply(CreateRequest(
                100f,
                _element: ElementType.Water));

            Assert.That(result.FinalDamage, Is.EqualTo(60f).Within(0.0001f));
        }

        /// <summary>
        /// 生命首次归零只发布一次耗尽事实，后续伤害请求必须被拒绝。
        /// </summary>
        [Test]
        public void LethalDamagePublishesOneHealthDepletedEventAndRejectsLaterDamage()
        {
            int depletedEventCount = 0;
            HealthDepletedEvent receivedEvent = default;
            eventBus.Subscribe<HealthDepletedEvent>(_eventData =>
            {
                depletedEventCount++;
                receivedEvent = _eventData;
            });

            DamageResult lethalResult = DamageResolver.ResolveAndApply(CreateRequest(2000f));
            DamageResult rejectedResult = DamageResolver.ResolveAndApply(CreateRequest(10f));

            Assert.That(lethalResult.DidDepleteHealth, Is.True);
            Assert.That(targetHealth.CurrentHealth, Is.Zero);
            Assert.That(targetHealth.IsHealthDepleted, Is.True);
            Assert.That(depletedEventCount, Is.EqualTo(1));
            Assert.That(receivedEvent.Instigator, Is.SameAs(instigatorGameObject));
            Assert.That(receivedEvent.SourceObject, Is.SameAs(sourceGameObject));
            Assert.That(receivedEvent.ExecutionId, Is.EqualTo(lethalResult.ExecutionId));
            Assert.That(receivedEvent.InstigatorId, Is.EqualTo(lethalResult.InstigatorId));
            Assert.That(receivedEvent.TargetId, Is.EqualTo(lethalResult.TargetId));
            Assert.That(rejectedResult.IsApplied, Is.False);
            Assert.That(rejectedResult.RejectionReason, Is.EqualTo(DamageRejectionReason.TargetCannotReceiveDamage));
        }

        /// <summary>
        /// CharacterFacts 必须直接观察 Health 的耗尽事实，初始化默认事实不能覆盖它。
        /// </summary>
        [Test]
        public void CharacterFactsReflectsHealthDepletionWithoutStoredDeathState()
        {
            CharacterFacts facts = targetGameObject.AddComponent<CharacterFacts>();
            facts.InitializeDefaults();

            Assert.That(facts.IsHealthDepleted, Is.False);

            DamageResolver.ResolveAndApply(CreateRequest(2000f));
            facts.InitializeDefaults();

            Assert.That(facts.IsHealthDepleted, Is.True);
        }

        /// <summary>
        /// 首版只允许玩家队伍与敌人互相伤害，未分配与同阵营组合全部拒绝。
        /// </summary>
        [TestCase(CombatFaction.Unassigned, CombatFaction.Unassigned, false)]
        [TestCase(CombatFaction.Unassigned, CombatFaction.PlayerParty, false)]
        [TestCase(CombatFaction.Unassigned, CombatFaction.Enemy, false)]
        [TestCase(CombatFaction.PlayerParty, CombatFaction.Unassigned, false)]
        [TestCase(CombatFaction.PlayerParty, CombatFaction.PlayerParty, false)]
        [TestCase(CombatFaction.PlayerParty, CombatFaction.Enemy, true)]
        [TestCase(CombatFaction.Enemy, CombatFaction.Unassigned, false)]
        [TestCase(CombatFaction.Enemy, CombatFaction.PlayerParty, true)]
        [TestCase(CombatFaction.Enemy, CombatFaction.Enemy, false)]
        public void FactionMatrixAllowsOnlyOpposingAssignedFactions(
            CombatFaction _sourceFaction,
            CombatFaction _targetFaction,
            bool _expected)
        {
            Assert.That(CombatFactionRules.CanDamage(_sourceFaction, _targetFaction), Is.EqualTo(_expected));
        }

        /// <summary>
        /// 同一执行对同一目标只写回一次，并只发布一次已提交伤害事件。
        /// </summary>
        [Test]
        public void DuplicateExecutionAppliesOnceAndReturnsExplicitReason()
        {
            int damageEventCount = 0;
            eventBus.Subscribe<DamageAppliedEvent>(_ => damageEventCount++);
            AttackExecutionId executionId = AttackExecutionId.Create();

            DamageResult firstResult = DamageResolver.ResolveAndApply(CreateRequest(100f, _executionId: executionId));
            DamageResult duplicateResult = DamageResolver.ResolveAndApply(CreateRequest(100f, _executionId: executionId));

            Assert.That(firstResult.IsApplied, Is.True);
            Assert.That(duplicateResult.IsApplied, Is.False);
            Assert.That(duplicateResult.RejectionReason, Is.EqualTo(DamageRejectionReason.DuplicateExecution));
            Assert.That(duplicateResult.ExecutionId, Is.EqualTo(executionId));
            Assert.That(duplicateResult.TargetId, Is.EqualTo(firstResult.TargetId));
            Assert.That(targetHealth.CurrentHealth, Is.EqualTo(900f).Within(0.0001f));
            Assert.That(damageEventCount, Is.EqualTo(1));
        }

        /// <summary>
        /// 同一执行可以分别命中两个权威目标，不同目标不共享去重状态。
        /// </summary>
        [Test]
        public void SameExecutionCanApplyOnceToEachDifferentTarget()
        {
            GameObject secondTargetGameObject = new("SecondTarget");
            try
            {
                DamageContractTestActorStat secondStat = secondTargetGameObject.AddComponent<DamageContractTestActorStat>();
                secondStat.InitializeForTest();
                HealthComponent secondHealth = secondTargetGameObject.AddComponent<HealthComponent>();
                Assert.That(secondHealth.TryInitialize(), Is.True);
                Combatant secondCombatant = secondTargetGameObject.AddComponent<Combatant>();
                SetPrivateField(secondCombatant, "faction", CombatFaction.Enemy);
                EnsureCombatantLifecycle(secondCombatant);
                AttackExecutionId executionId = AttackExecutionId.Create();

                DamageResult firstResult = DamageResolver.ResolveAndApply(
                    CreateRequest(100f, _executionId: executionId));
                DamageResult secondResult = DamageResolver.ResolveAndApply(
                    CreateRequest(100f, _executionId: executionId, _targetCombatant: secondCombatant));

                Assert.That(firstResult.IsApplied, Is.True);
                Assert.That(secondResult.IsApplied, Is.True);
                Assert.That(firstResult.ExecutionId, Is.EqualTo(secondResult.ExecutionId));
                Assert.That(firstResult.TargetId, Is.Not.EqualTo(secondResult.TargetId));
                Assert.That(targetHealth.CurrentHealth, Is.EqualTo(900f).Within(0.0001f));
                Assert.That(secondHealth.CurrentHealth, Is.EqualTo(900f).Within(0.0001f));
            }
            finally
            {
                DestroyImmediate(secondTargetGameObject);
            }
        }

        /// <summary>
        /// 同阵营请求必须在生命写回和事件发布前被明确拒绝。
        /// </summary>
        [Test]
        public void SameFactionRequestIsRejectedWithoutCommittedEvent()
        {
            SetPrivateField(targetCombatant, "faction", CombatFaction.PlayerParty);
            int damageEventCount = 0;
            eventBus.Subscribe<DamageAppliedEvent>(_ => damageEventCount++);

            DamageResult result = DamageResolver.ResolveAndApply(CreateRequest(100f));

            Assert.That(result.IsApplied, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(DamageRejectionReason.FactionNotAllowed));
            Assert.That(targetHealth.CurrentHealth, Is.EqualTo(1000f).Within(0.0001f));
            Assert.That(damageEventCount, Is.Zero);
        }

        /// <summary>
        /// 子 Collider 必须解析到同一权威目标，而不是各自形成伤害目标。
        /// </summary>
        [Test]
        public void ChildCollidersResolveToSameCombatantRoot()
        {
            GameObject firstChild = new("FirstCollider");
            GameObject secondChild = new("SecondCollider");
            firstChild.transform.SetParent(targetGameObject.transform, false);
            secondChild.transform.SetParent(targetGameObject.transform, false);
            Collider firstCollider = firstChild.AddComponent<BoxCollider>();
            Collider secondCollider = secondChild.AddComponent<SphereCollider>();

            Assert.That(CombatTargetResolver.TryResolve(firstCollider, out Combatant firstTarget), Is.True);
            Assert.That(CombatTargetResolver.TryResolve(secondCollider, out Combatant secondTarget), Is.True);
            Assert.That(firstTarget, Is.SameAs(targetCombatant));
            Assert.That(secondTarget, Is.SameAs(targetCombatant));
            Assert.That(firstTarget.Id, Is.EqualTo(secondTarget.Id));
        }

        /// <summary>
        /// 旧生命周期请求在复用后失效，而新生命周期可以重新接受相同执行身份。
        /// </summary>
        [Test]
        public void ReenabledCombatantRejectsStaleIdentityAndClearsDedupeState()
        {
            AttackExecutionId executionId = AttackExecutionId.Create();
            DamageRequest staleRequest = CreateRequest(100f, _executionId: executionId);
            DamageResult firstResult = DamageResolver.ResolveAndApply(staleRequest);
            CombatantId previousTargetId = targetCombatant.Id;

            InvokeLifecycle(targetCombatant, "OnDisable");
            Assert.That(targetCombatant.Id.IsValid, Is.False);
            InvokeLifecycle(targetCombatant, "OnEnable");

            DamageResult staleResult = DamageResolver.ResolveAndApply(staleRequest);
            DamageResult currentResult = DamageResolver.ResolveAndApply(
                CreateRequest(100f, _executionId: executionId));

            Assert.That(firstResult.IsApplied, Is.True);
            Assert.That(targetCombatant.Id.IsValid, Is.True);
            Assert.That(targetCombatant.Id, Is.Not.EqualTo(previousTargetId));
            Assert.That(staleResult.IsApplied, Is.False);
            Assert.That(staleResult.RejectionReason, Is.EqualTo(DamageRejectionReason.InvalidTarget));
            Assert.That(currentResult.IsApplied, Is.True);
            Assert.That(targetHealth.CurrentHealth, Is.EqualTo(800f).Within(0.0001f));
        }

        private DamageRequest CreateRequest(
            float _baseDamage,
            HitPartType _hitPartType = HitPartType.Default,
            float _headShotDamageMultiplier = 1f,
            float _weakPointDamageMultiplier = 1f,
            ElementType _element = ElementType.None,
            DamageDeliveryType _delivery = DamageDeliveryType.Direct,
            AttackExecutionId _executionId = default,
            Combatant _targetCombatant = null)
        {
            if (_executionId.IsValid == false)
            {
                _executionId = AttackExecutionId.Create();
            }

            return new DamageRequest(
                _executionId,
                instigatorCombatant,
                sourceGameObject,
                _targetCombatant != null ? _targetCombatant : targetCombatant,
                _element,
                _delivery,
                _baseDamage,
                _hitPartType,
                _headShotDamageMultiplier,
                _weakPointDamageMultiplier,
                Vector3.zero,
                Vector3.forward,
                Vector3.forward,
                Vector3.back,
                7f);
        }

        private static void DestroyImmediate(GameObject _gameObject)
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        private static void EnsureCombatantLifecycle(Combatant _combatant)
        {
            if (_combatant.Id.IsValid)
            {
                return;
            }

            InvokeLifecycle(_combatant, "Awake");
            InvokeLifecycle(_combatant, "OnEnable");
        }

        private static void SetPrivateField(object _target, string _fieldName, object _value)
        {
            FieldInfo field = _target.GetType().GetField(
                _fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_target, _value);
        }

        private static void InvokeLifecycle(object _target, string _methodName)
        {
            MethodInfo method = _target.GetType().GetMethod(
                _methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_target, null);
        }
    }
}
