using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.ConfigSystem.Validation;
using Game.Definition.Element;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode.Gameplay.Element
{
    /// <summary>
    /// 验证元素应用配置、来源快照、独立请求与明确失败原因。
    /// </summary>
    public sealed class ElementApplicationContractTests
    {
        private const string FireAssetPath =
            "Assets/Configs/Element/RifleAmmoFireApplicationProfile.asset";
        private const string ElectricAssetPath =
            "Assets/Configs/Element/RifleAmmoElectricApplicationProfile.asset";
        private const string RegistryAssetPath =
            "Assets/Configs/Common/ConfigRegistry_Default.asset";

        private readonly List<Object> ownedObjects = new();

        /// <summary>销毁测试创建的运行时对象和临时配置，避免用例互相污染。</summary>
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
        }

        /// <summary>空、重复或非法 Profile 数据必须在统一配置校验中产生明确错误。</summary>
        [Test]
        public void ValidationRejectsEmptyDuplicateAndInvalidProfileData()
        {
            ElementApplicationProfileConfig emptyId = CreateProfile(
                string.Empty,
                ElementType.Fire,
                0f,
                6f);
            ElementApplicationProfileConfig duplicateA = CreateProfile(
                "DuplicateApplication",
                ElementType.Fire,
                0f,
                6f);
            ElementApplicationProfileConfig duplicateB = CreateProfile(
                "DuplicateApplication",
                ElementType.Electric,
                0f,
                6f);
            ElementApplicationProfileConfig invalidData = CreateProfile(
                "InvalidApplication",
                ElementType.None,
                -1f,
                0f);

            ConfigValidationContext context = ValidateProfiles(
                emptyId,
                duplicateA,
                duplicateB,
                invalidData);

            Assert.That(context.HasError, Is.True);
            AssertMessageContains(context, "ConfigId 不能为空");
            AssertMessageContains(context, "重复 ConfigId");
            AssertMessageContains(context, "element 必须是");
            AssertMessageContains(context, "sourceTargetIntervalSeconds");
            AssertMessageContains(context, "attachmentDurationSeconds");
        }

        /// <summary>来源快照建立后不能随原始 Profile 字段变化而改变。</summary>
        [Test]
        public void SourceSnapshotFreezesRegisteredProfileAndAttribution()
        {
            ElementApplicationProfileConfig profile = CreateProfile(
                "SkillFireApplication",
                ElementType.Fire,
                1.5f,
                6f);
            ConfigService configService = CreateConfigService(profile);
            Combatant instigator = CreateCombatant("Instigator", CombatFaction.PlayerParty);
            GameObject sourceObject = CreateGameObject("SkillRuntime");
            ElementApplicationSourceId sourceId = ElementApplicationSourceId.Create();

            bool created = ElementApplicationRequestFactory.TryCreateSourceSnapshot(
                configService,
                " SkillFireApplication ",
                sourceId,
                instigator,
                sourceObject,
                out ElementApplicationSourceSnapshot snapshot,
                out ElementApplicationFailureReason failureReason);

            SetPrivateField(profile, "element", ElementType.Electric);
            SetPrivateField(profile, "sourceTargetIntervalSeconds", 9f);
            SetPrivateField(profile, "attachmentDurationSeconds", 12f);
            SetPrivateField(profile, "configId", "ChangedAfterSnapshot");

            Assert.That(created, Is.True);
            Assert.That(failureReason, Is.EqualTo(ElementApplicationFailureReason.None));
            Assert.That(snapshot.SourceId, Is.EqualTo(sourceId));
            Assert.That(snapshot.ProfileId, Is.EqualTo("SkillFireApplication"));
            Assert.That(snapshot.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(snapshot.SourceTargetIntervalSeconds, Is.EqualTo(1.5f));
            Assert.That(snapshot.AttachmentDurationSeconds, Is.EqualTo(6f));
            Assert.That(snapshot.InstigatorCombatant, Is.SameAs(instigator));
            Assert.That(snapshot.InstigatorId, Is.EqualTo(instigator.Id));
            Assert.That(snapshot.InstigatorFaction, Is.EqualTo(CombatFaction.PlayerParty));
            Assert.That(snapshot.SourceObject, Is.SameAs(sourceObject));
        }

        /// <summary>
        /// 元素请求不需要伤害请求或结果；来源—目标间隔键跨执行稳定并随目标变化。
        /// </summary>
        [Test]
        public void RequestsDoNotRequireDamageAndUseSourceTargetIntervalKey()
        {
            ElementApplicationProfileConfig profile = CreateProfile(
                "RifleFireApplication",
                ElementType.Fire,
                0f,
                6f);
            ConfigService configService = CreateConfigService(profile);
            Combatant instigator = CreateCombatant("Instigator", CombatFaction.PlayerParty);
            Combatant firstTarget = CreateCombatant("FirstTarget", CombatFaction.Enemy);
            Combatant secondTarget = CreateCombatant("SecondTarget", CombatFaction.Enemy);
            GameObject sourceObject = CreateGameObject("RifleRuntime");
            ElementApplicationSourceId sourceId = ElementApplicationSourceId.Create();
            Assert.That(ElementApplicationRequestFactory.TryCreateSourceSnapshot(
                configService,
                profile.ConfigId,
                sourceId,
                instigator,
                sourceObject,
                out ElementApplicationSourceSnapshot snapshot,
                out _), Is.True);

            AttackExecutionId firstExecution = AttackExecutionId.Create();
            AttackExecutionId secondExecution = AttackExecutionId.Create();
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                snapshot,
                firstExecution,
                firstTarget,
                2f,
                out ElementApplicationRequest firstRequest,
                out _), Is.True);
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                snapshot,
                secondExecution,
                firstTarget,
                3f,
                out ElementApplicationRequest repeatedTargetRequest,
                out _), Is.True);
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                snapshot,
                firstExecution,
                secondTarget,
                2f,
                out ElementApplicationRequest secondTargetRequest,
                out _), Is.True);

            Assert.That(firstRequest.ExecutionId, Is.EqualTo(firstExecution));
            Assert.That(firstRequest.Source.SourceId, Is.EqualTo(sourceId));
            Assert.That(firstRequest.TargetCombatant, Is.SameAs(firstTarget));
            Assert.That(firstRequest.TargetId, Is.EqualTo(firstTarget.Id));
            Assert.That(firstRequest.IntervalKey.IsValid, Is.True);
            Assert.That(firstRequest.IntervalKey, Is.EqualTo(repeatedTargetRequest.IntervalKey));
            Assert.That(firstRequest.IntervalKey, Is.Not.EqualTo(secondTargetRequest.IntervalKey));
            Assert.That(firstRequest.ExecutionId, Is.Not.EqualTo(repeatedTargetRequest.ExecutionId));
        }

        /// <summary>配置、来源、执行、目标、阵营和时间错误必须返回各自失败原因。</summary>
        [Test]
        public void FactoryReturnsExplicitFailureReasons()
        {
            ElementApplicationProfileConfig validProfile = CreateProfile(
                "ValidApplication",
                ElementType.Fire,
                0f,
                6f);
            ConfigService validService = CreateConfigService(validProfile);
            Combatant player = CreateCombatant("Player", CombatFaction.PlayerParty);
            Combatant enemy = CreateCombatant("Enemy", CombatFaction.Enemy);
            Combatant friendly = CreateCombatant("Friendly", CombatFaction.PlayerParty);
            GameObject sourceObject = CreateGameObject("Source");
            ElementApplicationSourceId sourceId = ElementApplicationSourceId.Create();

            AssertSourceFailure(
                null,
                validProfile.ConfigId,
                sourceId,
                player,
                sourceObject,
                ElementApplicationFailureReason.ConfigServiceUnavailable);
            AssertSourceFailure(
                validService,
                " ",
                sourceId,
                player,
                sourceObject,
                ElementApplicationFailureReason.InvalidProfileId);
            AssertSourceFailure(
                validService,
                "MissingApplication",
                sourceId,
                player,
                sourceObject,
                ElementApplicationFailureReason.ProfileNotFound);

            ElementApplicationProfileConfig disabledProfile = CreateProfile(
                "DisabledApplication",
                ElementType.Fire,
                0f,
                6f,
                false);
            AssertSourceFailure(
                CreateConfigService(disabledProfile),
                disabledProfile.ConfigId,
                sourceId,
                player,
                sourceObject,
                ElementApplicationFailureReason.ProfileDisabled);

            ElementApplicationProfileConfig invalidProfile = CreateProfile(
                "InvalidDataApplication",
                ElementType.None,
                0f,
                6f);
            AssertSourceFailure(
                CreateConfigService(invalidProfile),
                invalidProfile.ConfigId,
                sourceId,
                player,
                sourceObject,
                ElementApplicationFailureReason.InvalidProfileData);
            AssertSourceFailure(
                validService,
                validProfile.ConfigId,
                default,
                player,
                sourceObject,
                ElementApplicationFailureReason.InvalidSourceId);
            AssertSourceFailure(
                validService,
                validProfile.ConfigId,
                sourceId,
                null,
                sourceObject,
                ElementApplicationFailureReason.InvalidInstigator);
            AssertSourceFailure(
                validService,
                validProfile.ConfigId,
                sourceId,
                player,
                null,
                ElementApplicationFailureReason.MissingSourceObject);

            Assert.That(ElementApplicationRequestFactory.TryCreateSourceSnapshot(
                validService,
                validProfile.ConfigId,
                sourceId,
                player,
                sourceObject,
                out ElementApplicationSourceSnapshot snapshot,
                out _), Is.True);

            AssertRequestFailure(
                default,
                AttackExecutionId.Create(),
                enemy,
                1f,
                ElementApplicationFailureReason.InvalidSourceSnapshot);
            AssertRequestFailure(
                snapshot,
                default,
                enemy,
                1f,
                ElementApplicationFailureReason.InvalidExecution);
            AssertRequestFailure(
                snapshot,
                AttackExecutionId.Create(),
                null,
                1f,
                ElementApplicationFailureReason.InvalidTarget);
            AssertRequestFailure(
                snapshot,
                AttackExecutionId.Create(),
                friendly,
                1f,
                ElementApplicationFailureReason.FactionNotAllowed);
            AssertRequestFailure(
                snapshot,
                AttackExecutionId.Create(),
                enemy,
                float.NaN,
                ElementApplicationFailureReason.InvalidApplicationTime);
        }

        /// <summary>两个真实 Profile 必须能从默认 Registry 加载、查询并通过完整校验。</summary>
        [Test]
        public void RealApplicationProfilesLoadRegisterAndValidate()
        {
            ElementApplicationProfileConfig fireProfile =
                AssetDatabase.LoadAssetAtPath<ElementApplicationProfileConfig>(FireAssetPath);
            ElementApplicationProfileConfig electricProfile =
                AssetDatabase.LoadAssetAtPath<ElementApplicationProfileConfig>(ElectricAssetPath);
            ConfigRegistry registry = AssetDatabase.LoadAssetAtPath<ConfigRegistry>(RegistryAssetPath);

            Assert.That(fireProfile, Is.Not.Null);
            Assert.That(electricProfile, Is.Not.Null);
            Assert.That(registry, Is.Not.Null);

            ConfigService configService = new(registry);
            configService.Initialize();
            ConfigValidationContext context =
                new ConfigValidationRunner(registry, configService).Run();

            Assert.That(context.HasError, Is.False, FormatMessages(context));
            Assert.That(configService.TryGetConfig(
                "RifleAmmoFireApplication",
                out ElementApplicationProfileConfig registeredFire), Is.True);
            Assert.That(configService.TryGetConfig(
                "RifleAmmoElectricApplication",
                out ElementApplicationProfileConfig registeredElectric), Is.True);
            Assert.That(registeredFire, Is.SameAs(fireProfile));
            Assert.That(registeredElectric, Is.SameAs(electricProfile));
            Assert.That(fireProfile.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(electricProfile.Element, Is.EqualTo(ElementType.Electric));
            Assert.That(fireProfile.SourceTargetIntervalSeconds, Is.Zero);
            Assert.That(electricProfile.SourceTargetIntervalSeconds, Is.Zero);
            Assert.That(fireProfile.AttachmentDurationSeconds, Is.EqualTo(6f));
            Assert.That(electricProfile.AttachmentDurationSeconds, Is.EqualTo(6f));
        }

        private ConfigValidationContext ValidateProfiles(
            params ElementApplicationProfileConfig[] _profiles)
        {
            ConfigRegistry registry = CreateRegistry(_profiles);
            ConfigService configService = new(registry);
            configService.Initialize();
            return new ConfigValidationRunner(registry, configService).Run();
        }

        private ConfigService CreateConfigService(
            params ElementApplicationProfileConfig[] _profiles)
        {
            ConfigRegistry registry = CreateRegistry(_profiles);
            ConfigService configService = new(registry);
            configService.Initialize();
            return configService;
        }

        private ConfigRegistry CreateRegistry(
            params ElementApplicationProfileConfig[] _profiles)
        {
            ConfigRegistry registry = ScriptableObject.CreateInstance<ConfigRegistry>();
            ownedObjects.Add(registry);
            SetPrivateField(
                registry,
                "elementApplicationProfiles",
                new List<ElementApplicationProfileConfig>(_profiles));
            return registry;
        }

        private ElementApplicationProfileConfig CreateProfile(
            string _configId,
            ElementType _element,
            float _intervalSeconds,
            float _durationSeconds,
            bool _isEnabled = true)
        {
            ElementApplicationProfileConfig profile =
                ScriptableObject.CreateInstance<ElementApplicationProfileConfig>();
            ownedObjects.Add(profile);
            SetPrivateField(profile, "configId", _configId);
            SetPrivateField(profile, "isEnabled", _isEnabled);
            SetPrivateField(profile, "element", _element);
            SetPrivateField(profile, "sourceTargetIntervalSeconds", _intervalSeconds);
            SetPrivateField(profile, "attachmentDurationSeconds", _durationSeconds);
            return profile;
        }

        private Combatant CreateCombatant(string _name, CombatFaction _faction)
        {
            GameObject gameObject = CreateGameObject(_name);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
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

        private static void AssertSourceFailure(
            ConfigService _configService,
            string _profileId,
            ElementApplicationSourceId _sourceId,
            Combatant _instigator,
            Object _sourceObject,
            ElementApplicationFailureReason _expectedReason)
        {
            bool created = ElementApplicationRequestFactory.TryCreateSourceSnapshot(
                _configService,
                _profileId,
                _sourceId,
                _instigator,
                _sourceObject,
                out ElementApplicationSourceSnapshot snapshot,
                out ElementApplicationFailureReason failureReason);

            Assert.That(created, Is.False);
            Assert.That(snapshot, Is.EqualTo(default(ElementApplicationSourceSnapshot)));
            Assert.That(failureReason, Is.EqualTo(_expectedReason));
        }

        private static void AssertRequestFailure(
            in ElementApplicationSourceSnapshot _source,
            AttackExecutionId _executionId,
            Combatant _target,
            float _applicationTime,
            ElementApplicationFailureReason _expectedReason)
        {
            bool created = ElementApplicationRequestFactory.TryCreateRequest(
                _source,
                _executionId,
                _target,
                _applicationTime,
                out ElementApplicationRequest request,
                out ElementApplicationFailureReason failureReason);

            Assert.That(created, Is.False);
            Assert.That(request, Is.EqualTo(default(ElementApplicationRequest)));
            Assert.That(failureReason, Is.EqualTo(_expectedReason));
        }

        private static void AssertMessageContains(
            ConfigValidationContext _context,
            string _expectedText)
        {
            Assert.That(
                _context.Messages.Any(_message => _message.Message.Contains(_expectedText)),
                Is.True,
                FormatMessages(_context));
        }

        private static string FormatMessages(ConfigValidationContext _context)
        {
            return string.Join(
                " | ",
                _context.Messages.Select(
                    _message => $"{_message.Severity}:{_message.Source}:{_message.Message}"));
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
