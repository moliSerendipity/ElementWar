using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.Element;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Gameplay.Element
{
    /// <summary>
    /// 验证元素请求在真实 MonoBehaviour 生命周期中不依赖伤害，并正确跟随目标身份变化。
    /// </summary>
    public sealed class ElementApplicationPlayModeTests
    {
        private readonly List<Object> ownedObjects = new();

        /// <summary>延迟销毁本用例创建的对象，避免 PlayMode 生命周期泄漏到后续测试。</summary>
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
            yield return null;
        }

        /// <summary>
        /// 未初始化 Health 且没有伤害请求时仍可建立元素请求；禁用复用后必须使用新目标身份。
        /// </summary>
        [UnityTest]
        public IEnumerator RequestWithoutDamageTracksTargetLifecycleIdentity()
        {
            ElementApplicationProfileConfig profile = CreateProfile();
            ConfigService configService = CreateConfigService(profile);
            Combatant instigator = CreateCombatant("ElementInstigator", CombatFaction.PlayerParty);
            Combatant target = CreateCombatant("ElementTarget", CombatFaction.Enemy);
            GameObject sourceObject = CreateGameObject("ElementSourceRuntime");
            ElementApplicationSourceId sourceId = ElementApplicationSourceId.Create();

            yield return null;

            Assert.That(instigator.Health.IsInitialized, Is.False);
            Assert.That(target.Health.IsInitialized, Is.False);
            Assert.That(ElementApplicationRequestFactory.TryCreateSourceSnapshot(
                configService,
                profile.ConfigId,
                sourceId,
                instigator,
                sourceObject,
                out ElementApplicationSourceSnapshot snapshot,
                out ElementApplicationFailureReason snapshotFailure), Is.True);
            Assert.That(snapshotFailure, Is.EqualTo(ElementApplicationFailureReason.None));
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                snapshot,
                AttackExecutionId.Create(),
                target,
                Time.time,
                out ElementApplicationRequest firstRequest,
                out ElementApplicationFailureReason firstFailure), Is.True);
            Assert.That(firstFailure, Is.EqualTo(ElementApplicationFailureReason.None));
            Assert.That(firstRequest.Source.SourceId, Is.EqualTo(sourceId));
            Assert.That(firstRequest.TargetId, Is.EqualTo(target.Id));
            CombatantId firstTargetId = target.Id;

            target.gameObject.SetActive(false);
            yield return null;

            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                snapshot,
                AttackExecutionId.Create(),
                target,
                Time.time,
                out _,
                out ElementApplicationFailureReason disabledFailure), Is.False);
            Assert.That(disabledFailure, Is.EqualTo(ElementApplicationFailureReason.InvalidTarget));

            target.gameObject.SetActive(true);
            yield return null;

            Assert.That(target.Id.IsValid, Is.True);
            Assert.That(target.Id, Is.Not.EqualTo(firstTargetId));
            Assert.That(ElementApplicationRequestFactory.TryCreateRequest(
                snapshot,
                AttackExecutionId.Create(),
                target,
                Time.time,
                out ElementApplicationRequest reusedRequest,
                out ElementApplicationFailureReason reusedFailure), Is.True);
            Assert.That(reusedFailure, Is.EqualTo(ElementApplicationFailureReason.None));
            Assert.That(reusedRequest.Source.SourceId, Is.EqualTo(sourceId));
            Assert.That(reusedRequest.Source, Is.SameAs(firstRequest.Source));
            Assert.That(reusedRequest.TargetId, Is.EqualTo(target.Id));
            Assert.That(reusedRequest.TargetId, Is.Not.EqualTo(firstRequest.TargetId));
            Assert.That(target.Health.IsInitialized, Is.False);
        }

        private ElementApplicationProfileConfig CreateProfile()
        {
            ElementApplicationProfileConfig profile =
                ScriptableObject.CreateInstance<ElementApplicationProfileConfig>();
            ownedObjects.Add(profile);
            SetPrivateField(profile, "configId", "PlayModeFireApplication");
            SetPrivateField(profile, "element", ElementType.Fire);
            SetPrivateField(profile, "sourceTargetIntervalSeconds", 0f);
            SetPrivateField(profile, "attachmentDurationSeconds", 6f);
            return profile;
        }

        private ConfigService CreateConfigService(ElementApplicationProfileConfig _profile)
        {
            ConfigRegistry registry = ScriptableObject.CreateInstance<ConfigRegistry>();
            ownedObjects.Add(registry);
            SetPrivateField(
                registry,
                "elementApplicationProfiles",
                new List<ElementApplicationProfileConfig> { _profile });
            ConfigService configService = new(registry);
            configService.Initialize();
            return configService;
        }

        private Combatant CreateCombatant(string _name, CombatFaction _faction)
        {
            GameObject gameObject = CreateGameObject(_name);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
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
