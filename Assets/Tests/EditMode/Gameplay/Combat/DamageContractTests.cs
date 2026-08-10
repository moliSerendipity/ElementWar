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
            sourceGameObject = new GameObject("Source");
            targetGameObject = new GameObject("Target");
            targetStat = targetGameObject.AddComponent<DamageContractTestActorStat>();
            targetStat.InitializeForTest();
            targetHealth = targetGameObject.AddComponent<HealthComponent>();

            Assert.That(targetHealth.TryInitialize(), Is.True);
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
            eventBus = null;
        }

        /// <summary>
        /// 结果与事件必须完整保留责任角色、来源对象和正交伤害语义。
        /// </summary>
        [Test]
        public void ResolveAndApplyPreservesAttributionAndDamageSemantics()
        {
            DamageAppliedEvent receivedDamageEvent = default;
            HitConfirmedEvent receivedHitEvent = default;
            int damageEventCount = 0;
            int hitEventCount = 0;
            eventBus.Subscribe<DamageAppliedEvent>(_eventData =>
            {
                receivedDamageEvent = _eventData;
                damageEventCount++;
            });
            eventBus.Subscribe<HitConfirmedEvent>(_eventData =>
            {
                receivedHitEvent = _eventData;
                hitEventCount++;
            });

            DamageResult result = DamageResolver.ResolveAndApply(CreateRequest(100f));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.Instigator, Is.SameAs(instigatorGameObject));
            Assert.That(result.SourceObject, Is.SameAs(sourceGameObject));
            Assert.That(result.Element, Is.EqualTo(ElementType.None));
            Assert.That(result.Delivery, Is.EqualTo(DamageDeliveryType.Direct));
            Assert.That(result.FinalDamage, Is.EqualTo(100f).Within(0.0001f));
            Assert.That(damageEventCount, Is.EqualTo(1));
            Assert.That(hitEventCount, Is.EqualTo(1));
            Assert.That(receivedDamageEvent.DamageResult.SourceObject, Is.SameAs(sourceGameObject));
            Assert.That(receivedHitEvent.Instigator, Is.SameAs(instigatorGameObject));
            Assert.That(receivedHitEvent.SourceObject, Is.SameAs(sourceGameObject));
        }

        /// <summary>
        /// 完全相同的输入在生命重置后必须产生完全相同的结果。
        /// </summary>
        [Test]
        public void RepeatedRequestAfterRestoreProducesIdenticalDamage()
        {
            DamageRequest request = CreateRequest(73f);

            DamageResult firstResult = DamageResolver.ResolveAndApply(request);
            targetHealth.RestoreFullHealth();
            DamageResult secondResult = DamageResolver.ResolveAndApply(request);

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
            Assert.That(rejectedResult.IsApplied, Is.False);
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

        private DamageRequest CreateRequest(
            float _baseDamage,
            HitPartType _hitPartType = HitPartType.Default,
            float _headShotDamageMultiplier = 1f,
            float _weakPointDamageMultiplier = 1f,
            ElementType _element = ElementType.None,
            DamageDeliveryType _delivery = DamageDeliveryType.Direct)
        {
            return new DamageRequest(
                instigatorGameObject,
                sourceGameObject,
                targetHealth,
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

        private static void InvokeLifecycle(GameEventBus _eventBus, string _methodName)
        {
            MethodInfo method = typeof(GameEventBus).GetMethod(
                _methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_eventBus, null);
        }
    }
}
