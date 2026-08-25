using System.Collections;
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
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Gameplay.Element
{
    /// <summary>验证真实 MonoBehaviour 生命周期中的反应提交、重复调用与对象复用。</summary>
    public sealed class ElementReactionPipelinePlayModeTests
    {
        private readonly List<Object> ownedObjects = new();
        private readonly List<ElementAttachmentChangedEvent> attachmentEvents = new();

        private GameEventBus eventBus;
        private Combatant instigator;

        /// <summary>建立真实事件总线、活动玩家来源和完整反应表。</summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(GameEventBus.Instance, Is.Null);
            GameObject eventBusObject = CreateGameObject("ElementReactionPlayModeEventBus");
            eventBus = eventBusObject.AddComponent<GameEventBus>();
            eventBus.Subscribe<ElementAttachmentChangedEvent>(attachmentEvents.Add);
            instigator = CreateCombatant(
                "ElementReactionPlayModeInstigator",
                CombatFaction.PlayerParty,
                _withRuntime: false,
                out _);
        }

        /// <summary>销毁测试对象并等待 Unity 完成禁用与销毁回调。</summary>
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
            attachmentEvents.Clear();
            eventBus = null;
            instigator = null;
            yield return null;
            Assert.That(GameEventBus.Instance, Is.Null);
        }

        /// <summary>
        /// 真实目标应只接受同一执行的一次反应；禁用复用后旧批次拒绝，且新目标生命周期不继承去重。
        /// </summary>
        [UnityTest]
        public IEnumerator ReactionCommitsOnceAndTargetReuseStartsAFreshLedger()
        {
            Combatant target = CreateCombatant(
                "ReusableReactionTarget",
                CombatFaction.Enemy,
                _withRuntime: true,
                out ElementAttachmentRuntime runtime);
            ElementApplicationSourceSnapshot fire =
                CreateSource("ReusableReactionFire", ElementType.Fire);
            ElementApplicationSourceSnapshot electric =
                CreateSource("ReusableReactionElectric", ElementType.Electric);
            yield return null;

            AttackExecutionId executionId = AttackExecutionId.Create();
            float firstTime = Time.time;
            ElementApplicationRequest oldFireRequest =
                CreateRequest(fire, target, firstTime, executionId);
            ElementApplicationRequest oldElectricRequest =
                CreateRequest(electric, target, firstTime, executionId);
            ElementReactionResult first = ElementReactionPipeline.ResolveAndApply(
                oldFireRequest,
                oldElectricRequest);
            int firstEventCount = attachmentEvents.Count;
            ElementReactionResult duplicate = ElementReactionPipeline.ResolveAndApply(
                oldFireRequest,
                oldElectricRequest);

            Assert.That(first.DidTriggerReaction, Is.True);
            Assert.That(first.ReactionType, Is.EqualTo(ElementReactionType.Overload));
            Assert.That(first.TriggeringApplication.Source.InstigatorId, Is.EqualTo(electric.InstigatorId));
            Assert.That(runtime.TryGetPrimaryAttachment(out _), Is.False);
            Assert.That(firstEventCount, Is.EqualTo(2));
            Assert.That(duplicate.DidTriggerReaction, Is.False);
            Assert.That(attachmentEvents.Count, Is.EqualTo(firstEventCount));
            CombatantId oldTargetId = target.Id;

            target.gameObject.SetActive(false);
            yield return null;
            target.gameObject.SetActive(true);
            yield return null;

            Assert.That(target.Id.IsValid, Is.True);
            Assert.That(target.Id, Is.Not.EqualTo(oldTargetId));
            Assert.That(runtime.BoundTargetId, Is.EqualTo(target.Id));
            ElementReactionResult stale = ElementReactionPipeline.ResolveAndApply(
                oldFireRequest,
                oldElectricRequest);
            Assert.That(stale.DidTriggerReaction, Is.False);
            Assert.That(runtime.TryGetPrimaryAttachment(out _), Is.False);

            float reusedTime = Time.time;
            ElementReactionResult reused = ElementReactionPipeline.ResolveAndApply(
                CreateRequest(fire, target, reusedTime, executionId),
                CreateRequest(electric, target, reusedTime, executionId));

            Assert.That(reused.DidTriggerReaction, Is.True);
            Assert.That(reused.ReactionType, Is.EqualTo(ElementReactionType.Overload));
            Assert.That(reused.TriggeringApplication.TargetId, Is.EqualTo(target.Id));
            Assert.That(runtime.TryGetPrimaryAttachment(out _), Is.False);
            Assert.That(attachmentEvents.Count, Is.EqualTo(firstEventCount + 2));
        }

        private ElementApplicationSourceSnapshot CreateSource(
            string _profileId,
            ElementType _element)
        {
            ElementApplicationProfileConfig profile =
                ScriptableObject.CreateInstance<ElementApplicationProfileConfig>();
            ownedObjects.Add(profile);
            SetPrivateField(profile, "configId", _profileId);
            SetPrivateField(profile, "isEnabled", true);
            SetPrivateField(profile, "element", _element);
            SetPrivateField(profile, "sourceTargetIntervalSeconds", 0f);
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
            ElementAttachmentPlayModeTestActorStat stat =
                gameObject.AddComponent<ElementAttachmentPlayModeTestActorStat>();
            stat.InitializeForTest();
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
            _runtime = _withRuntime
                ? gameObject.AddComponent<ElementAttachmentRuntime>()
                : null;
            Assert.That(combatant.IsRuntimeActive, Is.True);
            return combatant;
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
