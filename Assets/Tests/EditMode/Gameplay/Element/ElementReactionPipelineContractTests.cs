using System.Collections.Generic;
using System.Reflection;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.Element;
using Game.Foundation.Events;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.Gameplay.Element
{
    /// <summary>验证固定反应映射、有序处理、原子消费、间隔、去重和目标复用。</summary>
    public sealed class ElementReactionPipelineContractTests
    {
        private readonly List<Object> ownedObjects = new();
        private readonly List<ElementAttachmentChangedEvent> attachmentEvents = new();

        private GameEventBus eventBus;
        private Combatant instigator;
        private Combatant target;
        private ElementAttachmentRuntime targetRuntime;

        /// <summary>建立隔离事件总线、玩家来源和已初始化的敌方目标。</summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(GameEventBus.Instance, Is.Null);
            GameObject eventBusObject = CreateGameObject("ElementReactionEventBus");
            eventBus = eventBusObject.AddComponent<GameEventBus>();
            InvokeLifecycle(eventBus, "Awake");
            eventBus.Subscribe<ElementAttachmentChangedEvent>(attachmentEvents.Add);

            instigator = CreateCombatant(
                "ElementReactionInstigator",
                CombatFaction.PlayerParty,
                _withRuntime: false,
                out _);
            target = CreateCombatant(
                "ElementReactionTarget",
                CombatFaction.Enemy,
                _withRuntime: true,
                out targetRuntime);
        }

        /// <summary>销毁测试对象并显式释放事件总线单例。</summary>
        [TearDown]
        public void TearDown()
        {
            if (eventBus != null)
            {
                InvokeLifecycle(eventBus, "OnDestroy");
            }

            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
            attachmentEvents.Clear();
            eventBus = null;
            instigator = null;
            target = null;
            targetRuntime = null;
        }

        [TestCase(ElementType.Fire, ElementType.Water, ElementReactionType.Vaporize)]
        [TestCase(ElementType.Fire, ElementType.Ice, ElementReactionType.Melt)]
        [TestCase(ElementType.Fire, ElementType.Electric, ElementReactionType.Overload)]
        [TestCase(ElementType.Water, ElementType.Electric, ElementReactionType.ElectroCharged)]
        [TestCase(ElementType.Water, ElementType.Ice, ElementReactionType.Freeze)]
        [TestCase(ElementType.Electric, ElementType.Ice, ElementReactionType.Superconduct)]
        public void FixedReactionMappingIsSymmetric(
            ElementType _first,
            ElementType _second,
            ElementReactionType _expected)
        {
            Assert.That(
                ElementReactionPipeline.TryResolveReactionType(_first, _second, out ElementReactionType forward),
                Is.True);
            Assert.That(
                ElementReactionPipeline.TryResolveReactionType(_second, _first, out ElementReactionType reverse),
                Is.True);
            Assert.That(forward, Is.EqualTo(_expected));
            Assert.That(reverse, Is.EqualTo(_expected));
        }

        /// <summary>空目标先附着弹药元素，再由技能作为第二元素触发并承担归因。</summary>
        [Test]
        public void AmmoThenSkillTriggersAndAttributesTheSecondElement()
        {
            ElementApplicationSourceSnapshot ammoFire = CreateSource("OrderedAmmoFire", ElementType.Fire);
            ElementApplicationSourceSnapshot skillElectric = CreateSource("OrderedSkillElectric", ElementType.Electric);
            AttackExecutionId executionId = AttackExecutionId.Create();

            ElementReactionResult result = ElementReactionPipeline.ResolveAndApply(
                CreateRequest(ammoFire, target, 1f, executionId),
                CreateRequest(skillElectric, target, 1f, executionId));

            Assert.That(result.DidTriggerReaction, Is.True);
            Assert.That(result.ReactionType, Is.EqualTo(ElementReactionType.Overload));
            Assert.That(result.ConsumedAttachment.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(result.ConsumedAttachment.Source, Is.SameAs(ammoFire));
            Assert.That(result.TriggeringApplication.Source, Is.SameAs(skillElectric));
            Assert.That(result.TriggeringApplication.Source.InstigatorId, Is.EqualTo(skillElectric.InstigatorId));
            Assert.That(result.TriggeringApplication.Source.SourceObject, Is.SameAs(skillElectric.SourceObject));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out _), Is.False);
            Assert.That(attachmentEvents.Count, Is.EqualTo(2));
            Assert.That(attachmentEvents[0].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.Attached));
            Assert.That(attachmentEvents[1].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.Consumed));
        }

        /// <summary>弹药已经触发反应时不处理技能，也不提前写入技能来源间隔。</summary>
        [Test]
        public void AmmoReactionStopsTheSkillStage()
        {
            ElementApplicationSourceSnapshot existingFire = CreateSource("ExistingFire", ElementType.Fire);
            ElementApplicationSourceSnapshot ammoElectric = CreateSource("TriggeringAmmoElectric", ElementType.Electric);
            ElementApplicationSourceSnapshot skippedSkillIce = CreateSource("SkippedSkillIce", ElementType.Ice, 10f);
            ApplySingle(CreateRequest(existingFire, target, 1f, AttackExecutionId.Create()));

            AttackExecutionId hitExecution = AttackExecutionId.Create();
            ElementReactionResult reaction = ElementReactionPipeline.ResolveAndApply(
                CreateRequest(ammoElectric, target, 2f, hitExecution),
                CreateRequest(skippedSkillIce, target, 2f, hitExecution));

            Assert.That(reaction.ReactionType, Is.EqualTo(ElementReactionType.Overload));
            Assert.That(reaction.TriggeringApplication.Source, Is.SameAs(ammoElectric));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out _), Is.False);

            // 若技能阶段被错误处理，这个带 10 秒间隔的同来源请求会在 2.1 秒被拒绝。
            ApplySingle(CreateRequest(skippedSkillIce, target, 2.1f, AttackExecutionId.Create()));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out ElementAttachmentSnapshot current), Is.True);
            Assert.That(current.Element, Is.EqualTo(ElementType.Ice));
        }

        /// <summary>同一执行成功反应后，重复命中不能再次处理或产生事件。</summary>
        [Test]
        public void DuplicateExecutionDoesNotTriggerAgain()
        {
            ElementApplicationSourceSnapshot fire = CreateSource("DuplicateFire", ElementType.Fire);
            ElementApplicationSourceSnapshot electric = CreateSource("DuplicateElectric", ElementType.Electric);
            AttackExecutionId executionId = AttackExecutionId.Create();
            ElementApplicationRequest fireRequest = CreateRequest(fire, target, 1f, executionId);
            ElementApplicationRequest electricRequest = CreateRequest(electric, target, 1f, executionId);

            ElementReactionResult first = ElementReactionPipeline.ResolveAndApply(fireRequest, electricRequest);
            int committedEventCount = attachmentEvents.Count;
            ElementReactionResult duplicate = ElementReactionPipeline.ResolveAndApply(fireRequest, electricRequest);

            Assert.That(first.DidTriggerReaction, Is.True);
            Assert.That(duplicate.DidTriggerReaction, Is.False);
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out _), Is.False);
            Assert.That(attachmentEvents.Count, Is.EqualTo(committedEventCount));
        }

        /// <summary>双请求身份或时间不一致时，弹药阶段也不能产生部分附着。</summary>
        [Test]
        public void MismatchedRequestsAreRejectedBeforeStateChanges()
        {
            ElementApplicationSourceSnapshot fire = CreateSource("PreflightFire", ElementType.Fire);
            ElementApplicationSourceSnapshot electric = CreateSource("PreflightElectric", ElementType.Electric);

            ElementReactionPipeline.ResolveAndApply(
                CreateRequest(fire, target, 1f, AttackExecutionId.Create()),
                CreateRequest(electric, target, 1f, AttackExecutionId.Create()));

            AttackExecutionId sharedExecution = AttackExecutionId.Create();
            ElementReactionPipeline.ResolveAndApply(
                CreateRequest(fire, target, 2f, sharedExecution),
                CreateRequest(electric, target, 2.1f, sharedExecution));

            Assert.That(targetRuntime.TryGetPrimaryAttachment(out _), Is.False);
            Assert.That(attachmentEvents, Is.Empty);
        }

        /// <summary>成功反应会提交触发来源间隔，边界前重试不能消费新的已有附着。</summary>
        [Test]
        public void ReactionCommitsTriggerSourceInterval()
        {
            ElementApplicationSourceSnapshot firstFire = CreateSource("IntervalExistingFire", ElementType.Fire);
            ElementApplicationSourceSnapshot triggerElectric = CreateSource("IntervalTriggerElectric", ElementType.Electric, 10f);
            AttackExecutionId reactionExecution = AttackExecutionId.Create();
            ElementReactionResult reaction = ElementReactionPipeline.ResolveAndApply(
                CreateRequest(firstFire, target, 1f, reactionExecution),
                CreateRequest(triggerElectric, target, 1f, reactionExecution));
            Assert.That(reaction.DidTriggerReaction, Is.True);

            ElementApplicationSourceSnapshot newFire = CreateSource("IntervalNewFire", ElementType.Fire);
            ApplySingle(CreateRequest(newFire, target, 2f, AttackExecutionId.Create()));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out ElementAttachmentSnapshot beforeRetry), Is.True);

            ApplySingle(CreateRequest(triggerElectric, target, 2.1f, AttackExecutionId.Create()));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out ElementAttachmentSnapshot afterRetry), Is.True);
            Assert.That(afterRetry.Version, Is.EqualTo(beforeRetry.Version));
            Assert.That(afterRetry.Source, Is.SameAs(newFire));
        }

        /// <summary>旧目标生命周期的请求不能消费复用后同版本的新附着，新请求仍可正常反应。</summary>
        [Test]
        public void TargetReuseRejectsStaleRequestAndStartsFreshLedger()
        {
            ElementApplicationSourceSnapshot oldFire = CreateSource("OldLifecycleFire", ElementType.Fire);
            ElementApplicationSourceSnapshot electric = CreateSource("LifecycleElectric", ElementType.Electric);
            ApplySingle(CreateRequest(oldFire, target, 1f, AttackExecutionId.Create()));
            ElementApplicationRequest staleElectric =
                CreateRequest(electric, target, 2f, AttackExecutionId.Create());
            CombatantId oldTargetId = target.Id;

            InvokeLifecycle(target, "OnDisable");
            InvokeLifecycle(target, "OnEnable");
            Assert.That(target.Id, Is.Not.EqualTo(oldTargetId));

            ElementApplicationSourceSnapshot newFire = CreateSource("NewLifecycleFire", ElementType.Fire);
            ApplySingle(CreateRequest(newFire, target, 1f, AttackExecutionId.Create()));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out ElementAttachmentSnapshot beforeStale), Is.True);
            Assert.That(beforeStale.Version, Is.EqualTo(1L));

            ApplySingle(staleElectric);
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out ElementAttachmentSnapshot afterStale), Is.True);
            Assert.That(afterStale.Source, Is.SameAs(newFire));
            Assert.That(afterStale.Version, Is.EqualTo(beforeStale.Version));

            ElementReactionResult freshReaction = ApplySingle(
                CreateRequest(electric, target, 2f, AttackExecutionId.Create()));
            Assert.That(freshReaction.DidTriggerReaction, Is.True);
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out _), Is.False);
        }

        private static ElementReactionResult ApplySingle(in ElementApplicationRequest _application)
        {
            return ElementReactionPipeline.ResolveAndApply(_application);
        }

        private ElementApplicationSourceSnapshot CreateSource(
            string _profileId,
            ElementType _element,
            float _intervalSeconds = 0f)
        {
            ElementApplicationProfileConfig profile =
                ScriptableObject.CreateInstance<ElementApplicationProfileConfig>();
            ownedObjects.Add(profile);
            SetPrivateField(profile, "configId", _profileId);
            SetPrivateField(profile, "isEnabled", true);
            SetPrivateField(profile, "element", _element);
            SetPrivateField(profile, "sourceTargetIntervalSeconds", _intervalSeconds);
            SetPrivateField(profile, "attachmentDurationSeconds", 6f);

            ConfigRegistry registry = ScriptableObject.CreateInstance<ConfigRegistry>();
            ownedObjects.Add(registry);
            SetPrivateField(
                registry,
                "elementApplicationProfiles",
                new List<ElementApplicationProfileConfig> { profile });
            ConfigService configService = new(registry);
            configService.Initialize();
            GameObject sourceObject = CreateGameObject(_profileId + "Source");

            Assert.That(ElementApplicationRequestFactory.TryCreateSourceSnapshot(
                configService,
                _profileId,
                ElementApplicationSourceId.Create(),
                instigator,
                sourceObject,
                out ElementApplicationSourceSnapshot snapshot,
                out ElementApplicationFailureReason failureReason), Is.True);
            Assert.That(failureReason, Is.EqualTo(ElementApplicationFailureReason.None));
            return snapshot;
        }

        private static ElementApplicationRequest CreateRequest(
            ElementApplicationSourceSnapshot _source,
            Combatant _target,
            float _applicationTime,
            AttackExecutionId _executionId)
        {
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                _source,
                _executionId,
                _target,
                _applicationTime,
                out ElementApplicationRequest request,
                out ElementApplicationFailureReason failureReason), Is.True);
            Assert.That(failureReason, Is.EqualTo(ElementApplicationFailureReason.None));
            return request;
        }

        private Combatant CreateCombatant(
            string _name,
            CombatFaction _faction,
            bool _withRuntime,
            out ElementAttachmentRuntime _runtime)
        {
            GameObject gameObject = CreateGameObject(_name);
            ElementAttachmentTestActorStat stat =
                gameObject.AddComponent<ElementAttachmentTestActorStat>();
            stat.InitializeForTest();
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
            _runtime = _withRuntime
                ? gameObject.AddComponent<ElementAttachmentRuntime>()
                : null;
            EnsureCombatantLifecycle(combatant);
            Assert.That(combatant.IsRuntimeActive, Is.True);
            return combatant;
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
