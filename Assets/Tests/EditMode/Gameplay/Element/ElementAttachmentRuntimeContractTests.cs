using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.Element;
using Game.Foundation.Events;
using Game.Gameplay.Character;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using Game.Gameplay.Enemy;
using Game.Presentation.HUD;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.EditMode.Gameplay.Element
{
    /// <summary>为元素附着测试提供可直接初始化的最小生命数值。</summary>
    public sealed class ElementAttachmentTestActorStat : ActorStatBase
    {
        /// <summary>建立一个可接收伤害和附着的测试目标。</summary>
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

    /// <summary>
    /// 验证目标侧元素附着的提交、刷新、反应交接、间隔、清理与 Bootstrap 装配。
    /// </summary>
    public sealed class ElementAttachmentRuntimeContractTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap/Bootstrap.unity";

        private readonly List<Object> ownedObjects = new();
        private readonly List<ElementAttachmentChangedEvent> receivedEvents = new();

        private GameEventBus eventBus;
        private Combatant instigator;
        private Combatant target;
        private HealthComponent targetHealth;
        private ElementAttachmentRuntime targetRuntime;

        /// <summary>建立隔离事件总线、合法玩家来源和已初始化敌方附着目标。</summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(GameEventBus.Instance, Is.Null);

            GameObject eventBusObject = CreateGameObject("ElementAttachmentEventBus");
            eventBus = eventBusObject.AddComponent<GameEventBus>();
            InvokeLifecycle(eventBus, "Awake");
            eventBus.Subscribe<ElementAttachmentChangedEvent>(receivedEvents.Add);

            instigator = CreateCombatant(
                "ElementInstigator",
                CombatFaction.PlayerParty,
                _withAttachmentRuntime: false,
                _initializeHealth: false,
                out _,
                out _);
            target = CreateCombatant(
                "ElementTarget",
                CombatFaction.Enemy,
                _withAttachmentRuntime: true,
                _initializeHealth: true,
                out targetHealth,
                out targetRuntime);

            Assert.That(targetRuntime.BoundTargetId, Is.EqualTo(target.Id));
        }

        /// <summary>销毁测试拥有对象并显式释放事件总线单例。</summary>
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
            receivedEvents.Clear();
            eventBus = null;
            instigator = null;
            target = null;
            targetHealth = null;
            targetRuntime = null;
        }

        /// <summary>首次附着、完全重复、最近来源刷新与异元素反应交接必须保持原子语义。</summary>
        [Test]
        public void AttachDuplicateRefreshAndReactionHandoffAreAtomic()
        {
            ElementApplicationSourceSnapshot firstFire = CreateSource(
                "AttachmentFireA",
                ElementType.Fire,
                0f,
                6f);
            ElementApplicationSourceSnapshot latestFire = CreateSource(
                "AttachmentFireB",
                ElementType.Fire,
                0f,
                4f);
            ElementApplicationSourceSnapshot electric = CreateSource(
                "AttachmentElectric",
                ElementType.Electric,
                0f,
                6f);
            ElementApplicationRequest firstRequest = CreateRequest(firstFire, target, 1f);

            ElementApplicationResult attached =
                ElementApplicationResolver.ResolveAndApply(firstRequest);
            ElementApplicationResult duplicate =
                ElementApplicationResolver.ResolveAndApply(firstRequest);
            ElementApplicationResult refreshed = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(latestFire, target, 2f));
            ElementApplicationResult reactionRequired = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(electric, target, 3f));

            Assert.That(attached.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(attached.CurrentAttachment.Version, Is.EqualTo(1L));
            Assert.That(attached.CurrentAttachment.Source.SourceId, Is.EqualTo(firstFire.SourceId));
            Assert.That(attached.CurrentAttachment.ExpiresAt, Is.EqualTo(7f));

            Assert.That(duplicate.Status, Is.EqualTo(ElementApplicationResolutionStatus.Unchanged));
            Assert.That(duplicate.CurrentAttachment.Version, Is.EqualTo(1L));

            Assert.That(refreshed.Status, Is.EqualTo(ElementApplicationResolutionStatus.Refreshed));
            Assert.That(refreshed.PreviousAttachment.Version, Is.EqualTo(1L));
            Assert.That(refreshed.CurrentAttachment.Version, Is.EqualTo(2L));
            Assert.That(refreshed.CurrentAttachment.Source.SourceId, Is.EqualTo(latestFire.SourceId));
            Assert.That(refreshed.CurrentAttachment.ExecutionId, Is.EqualTo(refreshed.Request.ExecutionId));
            Assert.That(refreshed.CurrentAttachment.ExpiresAt, Is.EqualTo(6f));

            Assert.That(reactionRequired.Status, Is.EqualTo(ElementApplicationResolutionStatus.ReactionRequired));
            Assert.That(reactionRequired.RequiresReaction, Is.True);
            Assert.That(reactionRequired.PreviousAttachment.Version, Is.EqualTo(2L));
            Assert.That(reactionRequired.CurrentAttachment.Version, Is.EqualTo(2L));
            Assert.That(reactionRequired.Request.Source.Element, Is.EqualTo(ElementType.Electric));
            Assert.That(targetRuntime.TryGetPrimaryAttachment(out ElementAttachmentSnapshot current), Is.True);
            Assert.That(current.Version, Is.EqualTo(2L));
            Assert.That(current.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents[0].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.Attached));
            Assert.That(receivedEvents[1].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.Refreshed));
        }

        /// <summary>来源—目标间隔只阻止边界前提交，版本消费必须拒绝迟到和重复消费者。</summary>
        [Test]
        public void SourceTargetIntervalAndVersionedConsumptionUseExactBoundaries()
        {
            ElementApplicationSourceSnapshot fire = CreateSource(
                "IntervalFire",
                ElementType.Fire,
                2f,
                6f);
            ElementApplicationResult attached = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, 10f));
            ElementApplicationResult intervalRejected = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, 11f));
            ElementApplicationResult boundaryRefresh = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, 12f));

            Assert.That(intervalRejected.Status, Is.EqualTo(ElementApplicationResolutionStatus.Rejected));
            Assert.That(
                intervalRejected.RejectionReason,
                Is.EqualTo(ElementApplicationRejectionReason.SourceTargetIntervalActive));
            Assert.That(boundaryRefresh.Status, Is.EqualTo(ElementApplicationResolutionStatus.Refreshed));
            Assert.That(boundaryRefresh.CurrentAttachment.Version, Is.EqualTo(2L));

            Assert.That(
                targetRuntime.TryConsumePrimary(attached.CurrentAttachment.Version, 12f, out _),
                Is.False);
            Assert.That(
                targetRuntime.TryConsumePrimary(
                    boundaryRefresh.CurrentAttachment.Version,
                    12f,
                    out ElementAttachmentSnapshot consumed),
                Is.True);
            Assert.That(consumed.Version, Is.EqualTo(2L));
            Assert.That(
                targetRuntime.TryConsumePrimary(boundaryRefresh.CurrentAttachment.Version, 12f, out _),
                Is.False);
            Assert.That(targetRuntime.AttachmentCount, Is.Zero);
            Assert.That(receivedEvents.Count, Is.EqualTo(3));
            Assert.That(receivedEvents[2].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.Consumed));
        }

        /// <summary>到期清理必须幂等；Health 重置即使槽已被消费也必须清除残留间隔。</summary>
        [Test]
        public void ExpiryIsIdempotentAndHealthResetClearsIntervalsWithoutAnAttachment()
        {
            ElementApplicationSourceSnapshot expiringFire = CreateSource(
                "ExpiringFire",
                ElementType.Fire,
                0f,
                2f);
            ElementApplicationResult expiring = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(expiringFire, target, 1f));

            targetRuntime.Tick(3f);
            targetRuntime.Tick(4f);

            Assert.That(expiring.IsCommitted, Is.True);
            Assert.That(targetRuntime.AttachmentCount, Is.Zero);
            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents[1].ChangeKind, Is.EqualTo(ElementAttachmentChangeKind.Expired));

            ElementApplicationSourceSnapshot intervalFire = CreateSource(
                "ResetIntervalFire",
                ElementType.Fire,
                10f,
                6f);
            ElementApplicationResult beforeReset = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(intervalFire, target, 5f));
            Assert.That(
                targetRuntime.TryConsumePrimary(beforeReset.CurrentAttachment.Version, 5.1f, out _),
                Is.True);

            targetHealth.ResetRuntimeState();
            targetRuntime.Tick(5.2f);
            Assert.That(targetHealth.TryInitialize(), Is.True);
            ElementApplicationResult afterReset = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(intervalFire, target, 5.3f));

            Assert.That(afterReset.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(afterReset.RejectionReason, Is.EqualTo(ElementApplicationRejectionReason.None));
        }

        /// <summary>缺少目标所有者与逆序时间必须明确拒绝且不制造提交事件。</summary>
        [Test]
        public void MissingOwnerAndStaleTimeAreRejectedWithoutCommittedEvents()
        {
            Combatant targetWithoutRuntime = CreateCombatant(
                "TargetWithoutAttachmentRuntime",
                CombatFaction.Enemy,
                _withAttachmentRuntime: false,
                _initializeHealth: true,
                out _,
                out _);
            ElementApplicationSourceSnapshot fire = CreateSource(
                "RejectedFire",
                ElementType.Fire,
                0f,
                6f);

            ElementApplicationResult missingOwner = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, targetWithoutRuntime, 1f));
            ElementApplicationResult attached = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, 5f));
            ElementApplicationResult stale = ElementApplicationResolver.ResolveAndApply(
                CreateRequest(fire, target, 4f));

            Assert.That(missingOwner.Status, Is.EqualTo(ElementApplicationResolutionStatus.Rejected));
            Assert.That(
                missingOwner.RejectionReason,
                Is.EqualTo(ElementApplicationRejectionReason.MissingAttachmentOwner));
            Assert.That(attached.Status, Is.EqualTo(ElementApplicationResolutionStatus.Attached));
            Assert.That(stale.Status, Is.EqualTo(ElementApplicationResolutionStatus.Rejected));
            Assert.That(
                stale.RejectionReason,
                Is.EqualTo(ElementApplicationRejectionReason.StaleApplicationTime));
            Assert.That(receivedEvents.Count, Is.EqualTo(1));
        }

        /// <summary>Bootstrap 只为两处敌方根装配附着所有者，并在 EventBus 根提供调试 Presenter。</summary>
        [Test]
        public void BootstrapHasExactElementAttachmentRuntimeMigration()
        {
            Scene bootstrapScene = default;
            try
            {
                bootstrapScene = EditorSceneManager.OpenScene(
                    BootstrapScenePath,
                    OpenSceneMode.Additive);
                GameObject[] roots = bootstrapScene.GetRootGameObjects();
                Combatant[] combatants = roots
                    .SelectMany(_root => _root.GetComponentsInChildren<Combatant>(true))
                    .ToArray();
                Combatant[] enemies = combatants
                    .Where(_combatant => _combatant.Faction == CombatFaction.Enemy)
                    .ToArray();
                Combatant[] players = combatants
                    .Where(_combatant => _combatant.Faction == CombatFaction.PlayerParty)
                    .ToArray();

                Assert.That(enemies.Length, Is.EqualTo(2));
                Assert.That(players.Length, Is.EqualTo(1));
                for (int i = 0; i < enemies.Length; i++)
                {
                    ElementAttachmentRuntime[] runtimes =
                        enemies[i].GetComponents<ElementAttachmentRuntime>();
                    Assert.That(runtimes.Length, Is.EqualTo(1), enemies[i].name);
                    Assert.That(enemies[i].ElementAttachments, Is.SameAs(runtimes[0]));

                    EnemyRoot enemyRoot = enemies[i].GetComponent<EnemyRoot>();
                    Assert.That(enemyRoot, Is.Not.Null, enemies[i].name);
                    SerializedObject serializedRoot = new(enemyRoot);
                    Assert.That(
                        serializedRoot.FindProperty("elementAttachmentRuntime").objectReferenceValue,
                        Is.SameAs(runtimes[0]),
                        enemies[i].name);
                }

                Assert.That(players[0].GetComponent<ElementAttachmentRuntime>(), Is.Null);
                Assert.That(players[0].ElementAttachments, Is.Null);
                ElementAttachmentDebugPresenter[] presenters = roots
                    .SelectMany(
                        _root => _root.GetComponentsInChildren<ElementAttachmentDebugPresenter>(true))
                    .ToArray();
                Assert.That(presenters.Length, Is.EqualTo(1));
                Assert.That(presenters[0].gameObject.name, Is.EqualTo("EventBus"));

                int missingScriptCount = roots
                    .SelectMany(_root => _root.GetComponentsInChildren<Transform>(true))
                    .Sum(
                        _transform =>
                            GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                                _transform.gameObject));
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
            bool _withAttachmentRuntime,
            bool _initializeHealth,
            out HealthComponent _health,
            out ElementAttachmentRuntime _runtime)
        {
            GameObject gameObject = CreateGameObject(_name);
            ElementAttachmentTestActorStat stat =
                gameObject.AddComponent<ElementAttachmentTestActorStat>();
            stat.InitializeForTest();
            _health = gameObject.AddComponent<HealthComponent>();
            if (_initializeHealth)
            {
                Assert.That(_health.TryInitialize(), Is.True);
            }

            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
            _runtime = _withAttachmentRuntime
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
