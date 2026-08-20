using Game.Gameplay.Combat;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 一次已经解析到权威战斗目标的元素施加尝试；它与伤害请求并列，不要求产生生命伤害。
    /// </summary>
    public readonly struct ElementApplicationRequest
    {
        internal ElementApplicationRequest(
            in ElementApplicationSourceSnapshot _source,
            AttackExecutionId _executionId,
            Combatant _targetCombatant,
            CombatantId _targetId,
            float _applicationTime)
        {
            Source = _source;
            ExecutionId = _executionId;
            TargetCombatant = _targetCombatant;
            TargetId = _targetId;
            ApplicationTime = _applicationTime;
            IntervalKey = new ElementApplicationIntervalKey(_source.SourceId, _targetId);
        }

        /// <summary>在来源生命周期建立时冻结的配置与归属。</summary>
        public ElementApplicationSourceSnapshot Source { get; }

        /// <summary>产生本次元素施加尝试的攻击或技能执行身份。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>请求创建时解析到的权威目标引用。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>请求创建时冻结的目标生命周期身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>来源—目标应用间隔所使用的稳定运行时键。</summary>
        public ElementApplicationIntervalKey IntervalKey { get; }

        /// <summary>本次尝试成立的运行时时间戳。</summary>
        public float ApplicationTime { get; }
    }
}
