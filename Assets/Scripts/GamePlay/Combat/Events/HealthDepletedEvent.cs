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
        /// <param name="_damageResult">首次使目标生命从正数降至零的已提交结果。</param>
        public HealthDepletedEvent(in DamageResult _damageResult)
        {
            ExecutionId = _damageResult.ExecutionId;
            InstigatorId = _damageResult.InstigatorId;
            Instigator = _damageResult.Instigator;
            SourceObject = _damageResult.SourceObject;
            TargetId = _damageResult.TargetId;
            Target = _damageResult.Target;
        }

        /// <summary>造成生命耗尽的攻击执行。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>请求创建时冻结的责任实体身份。</summary>
        public CombatantId InstigatorId { get; }

        /// <summary>承担本次生命耗尽归属的责任实体。</summary>
        public GameObject Instigator { get; }

        /// <summary>造成生命耗尽的具体来源对象。</summary>
        public Object SourceObject { get; }

        /// <summary>请求创建时冻结的目标身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>生命值已耗尽的目标。</summary>
        public HealthComponent Target { get; }
    }
}
