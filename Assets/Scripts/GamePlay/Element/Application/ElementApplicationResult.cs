namespace Game.Gameplay.Element
{
    /// <summary>
    /// 一次元素请求的目标侧处理结果；待反应结果不会自行修改或消费已有附着。
    /// </summary>
    public readonly struct ElementApplicationResult
    {
        /// <summary>创建一个状态、拒绝原因和相关附着已经相互对应的处理结果。</summary>
        /// <param name="_status">本次请求最终进入的处理状态。</param>
        /// <param name="_rejectionReason">拒绝状态的具体原因；非拒绝状态必须为 None。</param>
        /// <param name="_attachment">
        /// 与当前状态相关的附着：Committed 时是写入后的附着，保持不变时是现有附着，
        /// 待反应时是尚未消费的已有附着，拒绝时可以是目标当前仍保留的附着。
        /// </param>
        private ElementApplicationResult(
            ElementApplicationResolutionStatus _status,
            ElementApplicationRejectionReason _rejectionReason,
            in ElementAttachmentSnapshot _attachment)
        {
            Status = _status;
            RejectionReason = _rejectionReason;
            Attachment = _attachment;
        }

        /// <summary>请求被拒绝、提交、保持不变或需要反应处理。</summary>
        public ElementApplicationResolutionStatus Status { get; }

        /// <summary>请求被拒绝时的确定原因；其他状态为 None。</summary>
        public ElementApplicationRejectionReason RejectionReason { get; }

        /// <summary>处理完成后仍与本次结果相关的目标附着；无附着时为默认值。</summary>
        public ElementAttachmentSnapshot Attachment { get; }

        /// <summary>本次是否已经写入新附着或刷新已有附着。</summary>
        public bool IsCommitted =>
            Status == ElementApplicationResolutionStatus.Attached
            || Status == ElementApplicationResolutionStatus.Refreshed;

        /// <summary>是否需要由后续反应管线原子处理已有附着和触发请求。</summary>
        public bool RequiresReaction => Status == ElementApplicationResolutionStatus.ReactionRequired;

        /// <summary>创建一个没有相关附着快照的拒绝结果。</summary>
        /// <param name="_reason">请求未被目标 Runtime 接受的具体原因。</param>
        /// <returns>Attachment 为默认值的拒绝结果。</returns>
        internal static ElementApplicationResult Rejected(
            ElementApplicationRejectionReason _reason)
        {
            ElementAttachmentSnapshot attachment = default;
            return Rejected(_reason, attachment);
        }

        /// <summary>创建一个保留目标当前附着信息的拒绝结果。</summary>
        /// <param name="_reason">请求未被目标 Runtime 接受的具体原因。</param>
        /// <param name="_attachment">
        /// 拒绝发生后目标槽仍保留的当前附着；拒绝时没有相关附着则传入默认值。
        /// </param>
        /// <returns>没有改变目标状态的拒绝结果。</returns>
        internal static ElementApplicationResult Rejected(
            ElementApplicationRejectionReason _reason,
            in ElementAttachmentSnapshot _attachment)
        {
            return new ElementApplicationResult(
                ElementApplicationResolutionStatus.Rejected,
                _reason,
                _attachment);
        }

        /// <summary>创建一个已经附着或刷新成功的提交结果。</summary>
        /// <param name="_status">只能表示 Attached 或 Refreshed 的提交状态。</param>
        /// <param name="_attachment">本次写回后目标主要槽中的最新附着快照。</param>
        /// <returns>拒绝原因为 None 的已提交结果。</returns>
        internal static ElementApplicationResult Committed(
            ElementApplicationResolutionStatus _status,
            in ElementAttachmentSnapshot _attachment)
        {
            return new ElementApplicationResult(
                _status,
                ElementApplicationRejectionReason.None,
                _attachment);
        }

        /// <summary>创建一个重复请求已经由当前状态表达、因此无需再次写回的结果。</summary>
        /// <param name="_attachment">已经完整代表本次请求、且保持不变的当前附着快照。</param>
        /// <returns>拒绝原因为 None 的 Unchanged 结果。</returns>
        internal static ElementApplicationResult Unchanged(
            in ElementAttachmentSnapshot _attachment)
        {
            return new ElementApplicationResult(
                ElementApplicationResolutionStatus.Unchanged,
                ElementApplicationRejectionReason.None,
                _attachment);
        }

        /// <summary>创建一个需要后续反应管线继续处理的结果。</summary>
        /// <param name="_attachment">
        /// 目标槽中尚未被消费的异元素附着；反应管线用它校验版本并执行原子消费。
        /// </param>
        /// <returns>拒绝原因为 None、目标状态尚未改变的 ReactionRequired 结果。</returns>
        internal static ElementApplicationResult ReactionRequired(
            in ElementAttachmentSnapshot _attachment)
        {
            return new ElementApplicationResult(
                ElementApplicationResolutionStatus.ReactionRequired,
                ElementApplicationRejectionReason.None,
                _attachment);
        }
    }
}
