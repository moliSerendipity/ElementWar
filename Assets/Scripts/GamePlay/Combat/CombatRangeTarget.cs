using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 一次范围查询解析出的权威战斗目标，以及该目标距离查询中心最近的几何事实。
    /// </summary>
    public readonly struct CombatRangeTarget
    {
        /// <summary>
        /// 创建一个已经完成 Collider 根解析和多 Collider 去重的范围目标。
        /// </summary>
        /// <param name="_target">当前活动的权威战斗目标根。</param>
        /// <param name="_closestPoint">目标 Collider 距查询中心最近的世界坐标。</param>
        /// <param name="_distance">查询中心到最近点的世界距离。</param>
        internal CombatRangeTarget(
            Combatant _target,
            Vector3 _closestPoint,
            float _distance)
        {
            Target = _target;
            ClosestPoint = _closestPoint;
            Distance = _distance;
        }

        /// <summary>当前查询时解析出的权威战斗目标根。</summary>
        public Combatant Target { get; }

        /// <summary>目标 Collider 距查询中心最近的世界坐标。</summary>
        public Vector3 ClosestPoint { get; }

        /// <summary>查询中心到 <see cref="ClosestPoint"/> 的世界距离。</summary>
        public float Distance { get; }
    }
}
