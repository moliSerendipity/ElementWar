using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 命中查询已确认击中合法受击目标后发布的事实事件。
    ///
    /// 说明：
    /// 1. 该事件只说明“本次命中链找到了合法受击对象”；
    /// 2. 它不表示最终一定造成了伤害，最终伤害结果以 DamageAppliedEvent 为准；
    /// 3. 该事件只允许在 Combat 域完成合法性校验后，通过 GameEventBus 对外广播。
    /// </summary>
    public readonly struct HitConfirmedEvent
    {
        public HitConfirmedEvent(
            GameObject _instigator,
            Object _sourceObject,
            GameObject _target,
            HitPartType _hitPartType,
            Vector3 _hitPoint,
            Vector3 _hitNormal)
        {
            Instigator = _instigator;
            SourceObject = _sourceObject;
            Target = _target;
            HitPartType = _hitPartType;
            HitPoint = _hitPoint;
            HitNormal = _hitNormal;
        }

        /// <summary>承担命中归属的责任实体。</summary>
        public GameObject Instigator { get; }

        /// <summary>产生本次命中的具体来源对象。</summary>
        public Object SourceObject { get; }

        /// <summary>被命中的目标实体。</summary>
        public GameObject Target { get; }

        /// <summary>被命中的部位。</summary>
        public HitPartType HitPartType { get; }

        /// <summary>命中世界坐标。</summary>
        public Vector3 HitPoint { get; }

        /// <summary>命中表面法线。</summary>
        public Vector3 HitNormal { get; }
    }
}
