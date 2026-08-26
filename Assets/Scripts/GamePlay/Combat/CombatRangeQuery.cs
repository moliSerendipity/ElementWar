using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 把球形物理候选统一解析为可受来源伤害的权威战斗目标集合。
    /// </summary>
    public static class CombatRangeQuery
    {
        /// <summary>
        /// 查询当前可受来源伤害的目标，按最近表面距离与目标身份确定排序。
        /// </summary>
        /// <param name="_source">承担伤害归属和阵营过滤的活动战斗实体。</param>
        /// <param name="_center">球形查询中心的世界坐标。</param>
        /// <param name="_radius">查询半径，必须是有限正数。</param>
        /// <param name="_targetMask">物理候选所在层；只承担粗筛。</param>
        /// <param name="_requireLineOfSight">是否剔除被阻挡的目标。</param>
        /// <param name="_obstructionMask">启用视线过滤时可阻挡查询的环境层。</param>
        /// <param name="_maxTargets">过滤和排序后最多返回的目标数。</param>
        /// <returns>
        /// 查询时活动、存活、阵营合法且满足可选视线策略的去重目标；没有目标时返回空数组。
        /// </returns>
        public static CombatRangeTarget[] QueryDamageableTargets(
            Combatant _source,
            Vector3 _center,
            float _radius,
            LayerMask _targetMask,
            bool _requireLineOfSight = false,
            LayerMask _obstructionMask = default,
            int _maxTargets = int.MaxValue)
        {
            if (CanQuery(_source, _radius, _targetMask, _maxTargets) == false)
            {
                return Array.Empty<CombatRangeTarget>();
            }

            // 物理层只做候选粗筛；触发器不承担主线战斗目标实体。
            Collider[] hitColliders = Physics.OverlapSphere(
                _center,
                _radius,
                _targetMask,
                QueryTriggerInteraction.Ignore);

            // 同一 Combatant 的多个 Collider 只保留距中心最近的确定几何事实。
            Dictionary<CombatantId, CombatRangeTarget> targetsById = new();
            for (int i = 0; i < hitColliders.Length; i++)
            {
                Collider hitCollider = hitColliders[i];
                if (CombatTargetResolver.TryResolve(hitCollider, out Combatant target) == false
                    || CanIncludeTarget(_source, target) == false)
                {
                    continue;
                }

                Vector3 closestPoint = hitCollider.ClosestPoint(_center);
                CombatRangeTarget candidate = new(
                    target,
                    closestPoint,
                    Vector3.Distance(_center, closestPoint));

                if (targetsById.TryGetValue(target.Id, out CombatRangeTarget current) == false
                    || IsBetterCandidate(candidate, current))
                {
                    targetsById[target.Id] = candidate;
                }
            }

            // LOS 使用已去重目标的最近点；遮挡层必须只包含环境阻挡物。
            List<CombatRangeTarget> targets = new(targetsById.Count);
            foreach (CombatRangeTarget target in targetsById.Values)
            {
                if (_requireLineOfSight
                    && HasLineOfSight(_center, target.ClosestPoint, _obstructionMask) == false)
                {
                    continue;
                }

                targets.Add(target);
            }

            // Physics 返回顺序没有契约，最终顺序必须与物理枚举顺序无关。
            targets.Sort(CompareTargets);
            if (targets.Count > _maxTargets)
            {
                targets.RemoveRange(_maxTargets, targets.Count - _maxTargets);
            }

            return targets.ToArray();
        }

        /// <summary>
        /// 验证本查询入口拥有的瞬时参数；目标生命周期与生命状态在候选解析阶段裁决。
        /// </summary>
        private static bool CanQuery(
            Combatant _source,
            float _radius,
            LayerMask _targetMask,
            int _maxTargets)
        {
            return _source != null
                && _source.IsRuntimeActive
                && _radius > 0f
                && float.IsNaN(_radius) == false
                && float.IsInfinity(_radius) == false
                && _targetMask.value != 0
                && _maxTargets > 0;
        }

        /// <summary>
        /// 查询阶段剔除当前已经无效的目标；DamageResolver 仍会在实际提交时重验可变状态。
        /// </summary>
        private static bool CanIncludeTarget(Combatant _source, Combatant _target)
        {
            return _target != null
                && _target.Health != null
                && _target.Health.CanReceiveDamage
                && CombatFactionRules.CanDamage(_source.Faction, _target.Faction);
        }

        /// <summary>
        /// 多 Collider 距离相同时按最近点坐标确定选择，避免依赖 Physics 枚举顺序。
        /// </summary>
        private static bool IsBetterCandidate(
            in CombatRangeTarget _candidate,
            in CombatRangeTarget _current)
        {
            int distanceComparison = _candidate.Distance.CompareTo(_current.Distance);
            if (distanceComparison != 0)
            {
                return distanceComparison < 0;
            }

            return ComparePoints(_candidate.ClosestPoint, _current.ClosestPoint) < 0;
        }

        /// <summary>
        /// 只在调用方要求时检查环境遮挡；中心位于目标表面时视为有视线。
        /// </summary>
        private static bool HasLineOfSight(
            Vector3 _center,
            Vector3 _closestPoint,
            LayerMask _obstructionMask)
        {
            if ((_closestPoint - _center).sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            return Physics.Linecast(
                _center,
                _closestPoint,
                _obstructionMask,
                QueryTriggerInteraction.Ignore) == false;
        }

        /// <summary>
        /// 先按最近表面距离排序，同距时使用当前运行期稳定的 CombatantId。
        /// </summary>
        private static int CompareTargets(CombatRangeTarget _left, CombatRangeTarget _right)
        {
            int distanceComparison = _left.Distance.CompareTo(_right.Distance);
            return distanceComparison != 0
                ? distanceComparison
                : _left.Target.Id.Value.CompareTo(_right.Target.Id.Value);
        }

        private static int ComparePoints(Vector3 _left, Vector3 _right)
        {
            int xComparison = _left.x.CompareTo(_right.x);
            if (xComparison != 0)
            {
                return xComparison;
            }

            int yComparison = _left.y.CompareTo(_right.y);
            return yComparison != 0
                ? yComparison
                : _left.z.CompareTo(_right.z);
        }
    }
}
