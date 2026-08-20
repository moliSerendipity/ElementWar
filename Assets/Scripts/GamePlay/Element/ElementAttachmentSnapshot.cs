using Game.Definition.Combat;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 一个已提交主要元素附着的只读快照；版本只在对应目标生命周期内递增。
    /// </summary>
    public readonly struct ElementAttachmentSnapshot
    {
        internal ElementAttachmentSnapshot(
            long _version,
            in ElementApplicationRequest _request,
            float _expiresAt)
        {
            Version = _version;
            Element = _request.Source.Element;
            Source = _request.Source;
            ExecutionId = _request.ExecutionId;
            TargetCombatant = _request.TargetCombatant;
            TargetId = _request.TargetId;
            ApplicationTime = _request.ApplicationTime;
            ExpiresAt = _expiresAt;
        }

        /// <summary>目标生命周期内的递增版本；零表示不存在附着。</summary>
        public long Version { get; }

        /// <summary>当前主要槽保存的元素。</summary>
        public ElementType Element { get; }

        /// <summary>最近一次成功附着或刷新的来源快照。</summary>
        public ElementApplicationSourceSnapshot Source { get; }

        /// <summary>最近一次成功附着或刷新的攻击或技能执行。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>提交时对应的权威目标根。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>提交时冻结的目标生命周期身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>最近一次成功附着或刷新的运行时时间戳。</summary>
        public float ApplicationTime { get; }

        /// <summary>该附着应被清除的运行时时间戳。</summary>
        public float ExpiresAt { get; }

        /// <summary>快照是否描述一个结构完整的已提交附着。</summary>
        public bool IsValid =>
            Version > 0L
            && Element != ElementType.None
            && Source.SourceId.IsValid
            && ExecutionId.IsValid
            && TargetCombatant != null
            && TargetId.IsValid
            && IsFinite(ApplicationTime)
            && IsFinite(ExpiresAt)
            && ExpiresAt > ApplicationTime;

        /// <summary>
        /// 计算指定运行时时间下的剩余持续时间；已经到期或时间非法时返回零。
        /// </summary>
        /// <param name="_currentTime">与请求使用同一时间轴的当前时间。</param>
        /// <returns>不小于零的剩余秒数。</returns>
        public float GetRemainingSeconds(float _currentTime)
        {
            return IsFinite(_currentTime)
                ? Mathf.Max(0f, ExpiresAt - _currentTime)
                : 0f;
        }

        internal bool RepresentsSameApplication(
            in ElementApplicationRequest _request,
            float _expiresAt)
        {
            return IsValid
                && Element == _request.Source.Element
                && Source.SourceId == _request.Source.SourceId
                && ExecutionId == _request.ExecutionId
                && TargetId == _request.TargetId
                && ApplicationTime.Equals(_request.ApplicationTime)
                && ExpiresAt.Equals(_expiresAt);
        }

        private static bool IsFinite(float _value)
        {
            return float.IsNaN(_value) == false && float.IsInfinity(_value) == false;
        }
    }
}
