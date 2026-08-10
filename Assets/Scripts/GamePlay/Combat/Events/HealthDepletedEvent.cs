using UnityEngine;

namespace Game.Gameplay.Combat.Events
{
    /// <summary>
    /// 目标生命值首次从正数降至零后发布的事实事件。
    /// </summary>
    public readonly struct HealthDepletedEvent
    {
        /// <summary>
        /// 创建一条已经发生的生命耗尽事实。
        /// </summary>
        public HealthDepletedEvent(GameObject _instigator, Object _sourceObject, HealthComponent _target)
        {
            Instigator = _instigator;
            SourceObject = _sourceObject;
            Target = _target;
        }

        /// <summary>承担本次生命耗尽归属的责任实体。</summary>
        public GameObject Instigator { get; }

        /// <summary>造成生命耗尽的具体来源对象。</summary>
        public Object SourceObject { get; }

        /// <summary>生命值已耗尽的目标。</summary>
        public HealthComponent Target { get; }
    }
}
