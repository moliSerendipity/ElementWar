namespace Game.Gameplay.Combat
{
    /// <summary>单一硬控制状态对本次有效请求的处理结果。</summary>
    public enum HardControlApplicationStatus
    {
        /// <summary>本次请求不对该目标施加硬控制。</summary>
        None,
        /// <summary>目标首次进入硬控制。</summary>
        Applied,
        /// <summary>现有硬控制被延长到更晚结束。</summary>
        Extended,
        /// <summary>请求有效，但结束时间没有晚于现有控制。</summary>
        Unchanged,
    }

    /// <summary>一次合并削韧与硬控制写入完成后的精简事实。</summary>
    public readonly struct EnemyControlApplicationResult
    {
        internal EnemyControlApplicationResult(
            float _appliedToughnessDamage,
            bool _didStagger,
            HardControlApplicationStatus _hardControlStatus)
        {
            IsAccepted = true;
            AppliedToughnessDamage = _appliedToughnessDamage;
            DidStagger = _didStagger;
            HardControlStatus = _hardControlStatus;
        }

        /// <summary>请求是否已经通过校验和去重并被当前目标处理。</summary>
        public bool IsAccepted { get; }

        /// <summary>本次攻击实际扣除的韧性；低于阈值或失衡期间为零。</summary>
        public float AppliedToughnessDamage { get; }

        /// <summary>本次请求是否刚好使目标进入失衡。</summary>
        public bool DidStagger { get; }

        /// <summary>本次请求对单一硬控制状态的处理结果。</summary>
        public HardControlApplicationStatus HardControlStatus { get; }

        /// <summary>本次请求是否实际扣韧性、首次施加硬控制或延长硬控制。</summary>
        public bool DidChangeState =>
            AppliedToughnessDamage > 0f ||
            HardControlStatus == HardControlApplicationStatus.Applied ||
            HardControlStatus == HardControlApplicationStatus.Extended;

    }
}
