using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.Element;
using Game.Foundation.Events;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using Game.Presentation.HUD;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Gameplay.Element
{
    /// <summary>为 PlayMode 元素附着测试提供已初始化生命数值。</summary>
    public sealed class ElementAttachmentPlayModeTestActorStat : ActorStatBase
    {
        /// <summary>建立最小合法运行时数值。</summary>
        public void InitializeForTest(float _maxHealth = 100f)
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

    /// <summary>验证真实 MonoBehaviour 生命周期、生命清理和 Presentation 只读同步。</summary>
    public sealed class ElementAttachmentRuntimePlayModeTests
    {
        private readonly List<Object> ownedObjects = new();
        private readonly List<ElementAttachmentChangedEvent> receivedEvents = new();

        private GameEventBus eventBus;
        private ElementAttachmentDebugPresenter presenter;
        private Combatant instigator;

        /// <summary>建立真实事件总线、调试 Presenter 与活动玩家来源。</summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(GameEventBus.Instance, Is.Null);
            GameObject eventBusObject = CreateGameObject("ElementAttachmentPlayModeEventBus");
            eventBus = eventBusObject.AddComponent<GameEventBus>();
            presenter = eventBusObject.AddComponent<ElementAttachmentDebugPresenter>();
            eventBus.Subscribe<ElementAttachmentChangedEvent>(receivedEvents.Add);
            instigator = CreateCombatant(
                "ElementPlayModeInstigator",
                CombatFaction.PlayerParty,
                _withRuntime: false,
                _initializeHealth: false,
                out _,
                out _);
        }

        /// <summary>销毁用例对象并等待 Unity 完成禁用和销毁回调。</summary>
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
            receivedEvents.Clear();
            eventBus = null;
            presenter = null;
            instigator = null;
            yield return null;
            Assert.That(GameEventBus.Instance, Is.Null);
        }

        /// <summary>禁用和复用必须清除旧附着，分配新身份，并让 Presenter 移除旧目标。</summary>
        [UnityTest]
        public IEnumerator DisableAndReuseRejectsOldRequestAndPresenterTracksNewIdentity()
        {
            Combatant target = CreateCombatant(
                "ReusableElementTarget",
                CombatFaction.Enemy,
                _withRuntime: true,
                _initializeHealth: true,
                out _,
                out ElementAttachmentRuntime runtime);
            ElementApplicationSourceSnapshot fire = CreateSource(
                "ReusableFire",
                ElementType.Fire,
                0f,
                6f);
            yield return null;

            ElementApplicationRequest oldRequest = CreateRequest(fire, target, Time.time);
            ElementApplicationResult attached =
                ElementApplicationResolver.ResolveAndApply(oldRequest);
            CombatantId oldTargetId = target.Id;

            Assert.That(attached.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(presenter.TrackedAttachmentCount, Is.EqualTo(1));
            Assert.That(
                presenter.TryGetTrackedAttachment(oldTargetId, out ElementAttachmentSnapshot tracked),
                Is.True);
            Assert.That(tracked.Version, Is.EqualTo(attached.CurrentAttachment.Version));

            target.gameObject.SetActive(false);
            yield return null;

            Assert.That(runtime.AttachmentCount, Is.Zero);
            Assert.That(runtime.BoundTargetId.IsValid, Is.False);
            Assert.That(target.Id.IsValid, Is.False);
            Assert.That(presenter.TrackedAttachmentCount, Is.Zero);

            target.gameObject.SetActive(true);
            yield return null;

            Assert.That(target.Id.IsValid, Is.True);
            Assert.That(target.Id, Is.Not.EqualTo(oldTargetId));
            Assert.That(runtime.BoundTargetId, Is.EqualTo(target.Id));
            ElementApplicationResult staleLifecycle =
                ElementApplicationResolver.ResolveAndApply(oldRequest);
            Assert.That(staleLifecycle.Status, Is.EqualTo(ElementApplicationResolutionStatus.Rejected));
            Assert.That(
                staleLifecycle.RejectionReason,
                Is.EqualTo(ElementApplicationRejectionReason.InvalidTarget));

            ElementApplicationResult reused = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, Time.time));
            Assert.That(reused.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(reused.CurrentAttachment.Version, Is.EqualTo(1L));
            Assert.That(reused.CurrentAttachment.TargetId, Is.EqualTo(target.Id));
            Assert.That(presenter.TrackedAttachmentCount, Is.EqualTo(1));
            Assert.That(presenter.TryGetTrackedAttachment(oldTargetId, out _), Is.False);
            Assert.That(presenter.TryGetTrackedAttachment(target.Id, out _), Is.True);
        }

        /// <summary>生命耗尽和运行时重置必须各自提交一次清理，并同步移除 Presentation 状态。</summary>
        [UnityTest]
        public IEnumerator HealthDepletionAndResetClearCommittedAttachmentOnce()
        {
            Combatant target = CreateCombatant(
                "HealthElementTarget",
                CombatFaction.Enemy,
                _withRuntime: true,
                _initializeHealth: true,
                out HealthComponent health,
                out ElementAttachmentRuntime runtime);
            ElementApplicationSourceSnapshot fire = CreateSource(
                "HealthFire",
                ElementType.Fire,
                0f,
                6f);
            yield return null;

            float firstApplicationTime = Time.time;
            ElementApplicationResult first = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, firstApplicationTime));
            SetPrivateField(health, "currentHealth", 0f);
            runtime.Tick(firstApplicationTime + 0.1f);
            runtime.Tick(firstApplicationTime + 0.2f);

            Assert.That(first.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(health.IsHealthDepleted, Is.True);
            Assert.That(runtime.AttachmentCount, Is.Zero);
            Assert.That(presenter.TrackedAttachmentCount, Is.Zero);
            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents[1].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.TargetDepleted));

            health.RestoreFullHealth();
            float secondApplicationTime = firstApplicationTime + 0.3f;
            ElementApplicationResult second = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, secondApplicationTime));
            health.ResetRuntimeState();
            runtime.Tick(secondApplicationTime + 0.1f);
            runtime.Tick(secondApplicationTime + 0.2f);

            Assert.That(second.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(health.IsInitialized, Is.False);
            Assert.That(runtime.AttachmentCount, Is.Zero);
            Assert.That(presenter.TrackedAttachmentCount, Is.Zero);
            Assert.That(receivedEvents.Count, Is.EqualTo(4));
            Assert.That(receivedEvents[3].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.TargetReset));
            yield return null;
        }

        private ElementApplicationSourceSnapshot CreateSource(
            string _profileId,
            ElementType _element,
            float _intervalSeconds,
            float _durationSeconds)
        {
            ElementApplicationProfileConfig profile =
                ScriptableObject.CreateInstance<ElementApplicationProfileConfig>();
            ownedObjects.Add(profile);
            SetPrivateField(profile, "configId", _profileId);
            SetPrivateField(profile, "isEnabled", true);
            SetPrivateField(profile, "element", _element);
            SetPrivateField(profile, "sourceTargetIntervalSeconds", _intervalSeconds);
            SetPrivateField(profile, "attachmentDurationSeconds", _durationSeconds);

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
            in ElementApplicationSourceSnapshot _source,
            Combatant _target,
            float _applicationTime)
        {
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                _source,
                AttackExecutionId.Create(),
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
            bool _initializeHealth,
            out HealthComponent _health,
            out ElementAttachmentRuntime _runtime)
        {
            GameObject gameObject = CreateGameObject(_name);
            ElementAttachmentPlayModeTestActorStat stat =
                gameObject.AddComponent<ElementAttachmentPlayModeTestActorStat>();
            stat.InitializeForTest();
            _health = gameObject.AddComponent<HealthComponent>();
            if (_initializeHealth)
            {
                Assert.That(_health.TryInitialize(), Is.True);
            }

            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
            _runtime = _withRuntime
                ? gameObject.AddComponent<ElementAttachmentRuntime>()
                : null;
            Assert.That(combatant.IsRuntimeActive, Is.True);
            if (_runtime != null)
            {
                Assert.That(_runtime.BoundTargetId, Is.EqualTo(combatant.Id));
            }

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
