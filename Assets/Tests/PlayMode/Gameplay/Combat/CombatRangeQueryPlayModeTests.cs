using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Gameplay.Combat
{
    /// <summary>
    /// 验证真实物理场景中的范围目标集合、遮挡和生命周期行为。
    /// </summary>
    public sealed class CombatRangeQueryPlayModeTests
    {
        private const int TargetLayer = 9;
        private const int ObstructionLayer = 10;
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

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = ownedGameObjects.Count - 1; i >= 0; i--)
            {
                if (ownedGameObjects[i] != null)
                {
                    Object.Destroy(ownedGameObjects[i]);
                }
            }

            ownedGameObjects.Clear();
            source = null;
            yield return null;
        }

        /// <summary>
        /// 真实 Physics 查询必须把多个子 Collider 折叠为一个目标，并排除友方目标。
        /// </summary>
        [UnityTest]
        public IEnumerator PhysicalQueryDeduplicatesChildrenAndFiltersFriendlyTarget()
        {
            Combatant enemy = CreateCombatant(
                "MultiColliderEnemy",
                new Vector3(3f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                false);
            CreateChildBoxCollider(enemy.gameObject, "EnemyBodyA", new Vector3(-0.5f, 0f, 0f));
            CreateChildBoxCollider(enemy.gameObject, "EnemyBodyB", new Vector3(0.5f, 0f, 0f));
            CreateCombatant(
                "FriendlyParty",
                new Vector3(1f, 0f, 0f),
                CombatFaction.PlayerParty,
                TargetLayer,
                true);

            Physics.SyncTransforms();
            yield return null;

            CombatRangeTarget[] results = Query(_radius: 6f);

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Target, Is.SameAs(enemy));
        }

        /// <summary>
        /// LOS 必须在排序和数量上限前剔除环境遮挡目标。
        /// </summary>
        [UnityTest]
        public IEnumerator LineOfSightFiltersOccludedTargetBeforeMaximumIsApplied()
        {
            Combatant visible = CreateCombatant(
                "VisibleEnemy",
                new Vector3(-3f, 0f, 4f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            Combatant blocked = CreateCombatant(
                "BlockedEnemy",
                new Vector3(4f, 0f, 4f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            Combatant fartherVisible = CreateCombatant(
                "FartherVisibleEnemy",
                new Vector3(-5f, 0f, 4f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            CreateObstruction(
                "Wall",
                new Vector3(2f, 0f, 2f),
                new Vector3(1.5f, 3f, 1.5f));

            Physics.SyncTransforms();
            yield return null;

            CombatRangeTarget[] withoutLineOfSight = Query(_radius: 10f);
            CombatRangeTarget[] visibleResults = Query(
                _radius: 10f,
                _requireLineOfSight: true,
                _maxTargets: 1);

            Assert.That(withoutLineOfSight, Has.Length.EqualTo(3));
            Assert.That(ContainsTarget(withoutLineOfSight, blocked), Is.True);
            Assert.That(visibleResults, Has.Length.EqualTo(1));
            Assert.That(visibleResults[0].Target, Is.SameAs(visible));
            Assert.That(ContainsTarget(visibleResults, fartherVisible), Is.False);
        }

        /// <summary>
        /// 禁用目标立即退出查询，重新启用后以新身份重新进入且不残留旧结果。
        /// </summary>
        [UnityTest]
        public IEnumerator DisableAndReenableRefreshesRangeEligibility()
        {
            Combatant target = CreateCombatant(
                "ReusableEnemy",
                new Vector3(2f, 0f, 0f),
                CombatFaction.Enemy,
                TargetLayer,
                true);
            Physics.SyncTransforms();
            yield return null;

            CombatantId previousId = target.Id;
            Assert.That(Query(_radius: 4f), Has.Length.EqualTo(1));

            target.gameObject.SetActive(false);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(Query(_radius: 4f), Is.Empty);

            target.gameObject.SetActive(true);
            yield return null;
            Physics.SyncTransforms();
            CombatRangeTarget[] currentResults = Query(_radius: 4f);

            Assert.That(target.Id, Is.Not.EqualTo(previousId));
            Assert.That(currentResults, Has.Length.EqualTo(1));
            Assert.That(currentResults[0].Target, Is.SameAs(target));
        }

        private CombatRangeTarget[] Query(
            float _radius,
            bool _requireLineOfSight = false,
            int _maxTargets = int.MaxValue)
        {
            return CombatRangeQuery.QueryDamageableTargets(
                source,
                Vector3.zero,
                _radius,
                (LayerMask)(1 << TargetLayer),
                _requireLineOfSight,
                (LayerMask)(1 << ObstructionLayer),
                _maxTargets);
        }

        private Combatant CreateCombatant(
            string _name,
            Vector3 _position,
            CombatFaction _faction,
            int _layer,
            bool _addCollider)
        {
            GameObject gameObject = new(_name)
            {
                layer = _layer,
            };
            ownedGameObjects.Add(gameObject);
            gameObject.transform.position = _position;

            DamageProducerTestActorStat stat = gameObject.AddComponent<DamageProducerTestActorStat>();
            stat.InitializeForTest(100f);
            HealthComponent health = gameObject.AddComponent<HealthComponent>();
            Assert.That(health.TryInitialize(), Is.True);
            Combatant combatant = gameObject.AddComponent<Combatant>();
            SetPrivateField(combatant, "faction", _faction);

            if (_addCollider)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            Assert.That(combatant.Id.IsValid, Is.True);
            return combatant;
        }

        private void CreateChildBoxCollider(
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
            child.AddComponent<BoxCollider>();
        }

        private void CreateObstruction(string _name, Vector3 _position, Vector3 _size)
        {
            GameObject obstruction = new(_name)
            {
                layer = ObstructionLayer,
            };
            ownedGameObjects.Add(obstruction);
            obstruction.transform.position = _position;
            BoxCollider collider = obstruction.AddComponent<BoxCollider>();
            collider.size = _size;
        }

        private static bool ContainsTarget(
            IReadOnlyList<CombatRangeTarget> _results,
            Combatant _target)
        {
            for (int i = 0; i < _results.Count; i++)
            {
                if (_results[i].Target == _target)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetPrivateField(object _target, string _fieldName, object _value)
        {
            FieldInfo field = _target.GetType().GetField(
                _fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_target, _value);
        }
    }
}
