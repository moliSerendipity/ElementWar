using System.Collections.Generic;
using System.Reflection;
using Game.Definition.Combat;
using Game.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Gameplay.Combat
{
    /// <summary>
    /// 验证范围查询的目标解析、过滤、几何事实和确定顺序契约。
    /// </summary>
    public sealed class CombatRangeQueryContractTests
    {
        private const int TargetLayer = 9;
        private readonly List<GameObject> ownedGameObjects = new();
        private Combatant source;

        [SetUp]
        public void SetUp()
        {
            source = CreateCombatant(
                "PlayerSource",
                Vector3.zero,
                CombatFaction.PlayerParty,
                0,
                false);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = ownedGameObjects.Count - 1; i >= 0; i--)
            {
                if (ownedGameObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedGameObjects[i]);
                }
            }

            ownedGameObjects.Clear();
            source = null;
        }

        /// <summary>
        /// 同一目标的多个 Collider 必须折叠为一个目标，并保留最近表面事实。
        /// </summary>
        [Test]
        public void MultipleCollidersResolveToOneTargetWithNearestSurfaceFact()
        {
            Combatant target = CreateCombatant(
                "MultiColliderTarget",
                new Vector3(3f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                false);
            BoxCollider fartherCollider = CreateChildBoxCollider(
                target.gameObject,
                "FartherCollider",
                new Vector3(0.75f, 0f, 0f));
            BoxCollider nearerCollider = CreateChildBoxCollider(
                target.gameObject,
                "NearerCollider",
                new Vector3(-0.75f, 0f, 0f));

            Physics.SyncTransforms();
            Vector3 expectedPoint = nearerCollider.ClosestPoint(Vector3.zero);
            Assert.That(
                Vector3.Distance(Vector3.zero, expectedPoint),
                Is.LessThan(Vector3.Distance(Vector3.zero, fartherCollider.ClosestPoint(Vector3.zero))));

            CombatRangeTarget[] results = Query(_radius: 6f);

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Target, Is.SameAs(target));
            Assert.That(
                Vector3.Distance(results[0].ClosestPoint, expectedPoint),
                Is.LessThan(0.0001f));
            Assert.That(results[0].Distance, Is.EqualTo(expectedPoint.magnitude).Within(0.0001f));
        }

        /// <summary>
        /// 查询只返回当前活动、存活且与来源阵营敌对的权威目标。
        /// </summary>
        [Test]
        public void QueryFiltersFriendlyUnassignedDeadDisabledAndWrongLayerTargets()
        {
            Combatant included = CreateCombatant(
                "IncludedEnemy",
                new Vector3(1f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            CreateCombatant(
                "FriendlyParty",
                new Vector3(2f, 0f, 0f),
                CombatFaction.PlayerParty,
                TargetLayer,
                true);
            CreateCombatant(
                "UnassignedTarget",
                new Vector3(3f, 0f, 0f),
                CombatFaction.Unassigned,
                TargetLayer,
                true);
            Combatant dead = CreateCombatant(
                "DeadEnemy",
                new Vector3(4f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true,
                10f);
            Combatant disabled = CreateCombatant(
                "DisabledEnemy",
                new Vector3(5f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            CreateCombatant(
                "WrongLayerEnemy",
                new Vector3(6f, 0f, 0f),
                CombatFaction.Enemy,
                10,
                true);

            DamageResult lethalResult = DamageResolver.ResolveAndApply(
                CreateDamageRequest(source, dead, 10f));
            disabled.gameObject.SetActive(false);
            Physics.SyncTransforms();

            CombatRangeTarget[] results = Query(_radius: 8f);

            Assert.That(lethalResult.IsApplied, Is.True);
            Assert.That(dead.Health.IsHealthDepleted, Is.True);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Target, Is.SameAs(included));
        }

        /// <summary>
        /// 结果先按表面距离排序，同距再按目标身份排序，数量上限最后应用。
        /// </summary>
        [Test]
        public void QuerySortsByDistanceThenIdentityAndAppliesMaximum()
        {
            Combatant firstCreated = CreateCombatant(
                "FirstTiedEnemy",
                new Vector3(2f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            Combatant secondCreated = CreateCombatant(
                "SecondTiedEnemy",
                new Vector3(-2f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            CreateCombatant(
                "FarEnemy",
                new Vector3(4f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            Physics.SyncTransforms();

            CombatRangeTarget[] firstResults = Query(_radius: 6f, _maxTargets: 2);
            CombatRangeTarget[] repeatedResults = Query(_radius: 6f, _maxTargets: 2);

            Assert.That(firstCreated.Id.Value, Is.LessThan(secondCreated.Id.Value));
            Assert.That(firstResults, Has.Length.EqualTo(2));
            Assert.That(firstResults[0].Target, Is.SameAs(firstCreated));
            Assert.That(firstResults[1].Target, Is.SameAs(secondCreated));
            Assert.That(repeatedResults[0].Target, Is.SameAs(firstResults[0].Target));
            Assert.That(repeatedResults[1].Target, Is.SameAs(firstResults[1].Target));
        }

        /// <summary>
        /// 半径按 Collider 表面判断，位于边界内的目标保留，边界外和 Trigger 候选排除。
        /// </summary>
        [Test]
        public void QueryUsesColliderBoundaryAndIgnoresTriggers()
        {
            Combatant boundaryTarget = CreateCombatant(
                "BoundaryEnemy",
                new Vector3(2.49f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            CreateCombatant(
                "OutsideEnemy",
                new Vector3(2.51f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            Combatant triggerTarget = CreateCombatant(
                "TriggerEnemy",
                new Vector3(1f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            triggerTarget.GetComponent<BoxCollider>().isTrigger = true;
            Physics.SyncTransforms();

            CombatRangeTarget[] results = Query(_radius: 2f);

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Target, Is.SameAs(boundaryTarget));
            Assert.That(results[0].Distance, Is.LessThanOrEqualTo(2f));
        }

        private CombatRangeTarget[] Query(float _radius, int _maxTargets = int.MaxValue)
        {
            return CombatRangeQuery.QueryDamageableTargets(
                source,
                Vector3.zero,
                _radius,
                (LayerMask)(1 << TargetLayer),
                false,
                default,
                _maxTargets);
        }

        private Combatant CreateCombatant(
            string _name,
            Vector3 _position,
            CombatFaction _faction,
            int _layer,
            bool _addCollider,
            float _maxHealth = 100f)
        {
            GameObject gameObject = new(_name)
            {
                layer = _layer,
            };
            ownedGameObjects.Add(gameObject);
            gameObject.transform.position = _position;

            DamageContractTestActorStat stat = gameObject.AddComponent<DamageContractTestActorStat>();
            stat.InitializeForTest(_maxHealth);
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);
            EnsureCombatantLifecycle(combatant);

            if (_addCollider)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            Assert.That(combatant.Id.IsValid, Is.True);
            return combatant;
        }

        private BoxCollider CreateChildBoxCollider(
            GameObject _parent,
            string _name,
            Vector3 _localPosition)
        {
            GameObject child = new(_name)
            {
                layer = TargetLayer,
            };
            ownedGameObjects.Add(child);
            child.transform.SetParent(_parent.transform, false);
            child.transform.localPosition = _localPosition;
            return child.AddComponent<BoxCollider>();
        }

        private static DamageRequest CreateDamageRequest(
            Combatant _source,
            Combatant _target,
            float _damage)
        {
            return new DamageRequest(
                AttackExecutionId.Create(),
                _source,
                _source,
                _target,
                ElementType.None,
                DamageDeliveryType.Direct,
                _damage,
                HitPartType.Default,
                1f,
                1f,
                Vector3.zero,
                Vector3.forward,
                _target.transform.position,
                Vector3.back,
                Time.time);
        }

        private static void SetPrivateField(object _target, string _fieldName, object _value)
        {
            FieldInfo field = _target.GetType().GetField(
                _fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_target, _value);
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
