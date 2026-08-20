namespace Game.Gameplay.Element
{
    /// <summary>
    /// 一次元素请求的目标侧处理结果；待反应结果不会自行修改或消费已有附着。
    /// </summary>
    public readonly struct ElementApplicationResult
    {
        private ElementApplicationResult(
            ElementApplicationResolutionStatus _status,
            ElementApplicationRejectionReason _rejectionReason,
            in ElementApplicationRequest _request,
            in ElementAttachmentSnapshot _previousAttachment,
            in ElementAttachmentSnapshot _currentAttachment)
        {
            Status = _status;
            RejectionReason = _rejectionReason;
            Request = _request;
            PreviousAttachment = _previousAttachment;
            CurrentAttachment = _currentAttachment;
        }

        /// <summary>请求被拒绝、提交、保持不变或需要反应处理。</summary>
        public ElementApplicationResolutionStatus Status { get; }

        /// <summary>请求被拒绝时的确定原因；其他状态为 None。</summary>
        public ElementApplicationRejectionReason RejectionReason { get; }

        /// <summary>本次被处理的元素请求；可作为后续反应的触发输入。</summary>
        public ElementApplicationRequest Request { get; }

        /// <summary>处理前已有的附着；首次附着或无状态拒绝时为默认值。</summary>
        public ElementAttachmentSnapshot PreviousAttachment { get; }

        /// <summary>处理后仍有效的附着；拒绝且原状态不可见时为默认值。</summary>
        public ElementAttachmentSnapshot CurrentAttachment { get; }

        /// <summary>本次是否已经写入新附着或刷新已有附着。</summary>
        public bool IsCommitted =>
            Status == ElementApplicationResolutionStatus.Attached
            || Status == ElementApplicationResolutionStatus.Refreshed;

        /// <summary>是否需要由后续反应管线原子处理已有附着和触发请求。</summary>
        public bool RequiresReaction => Status == ElementApplicationResolutionStatus.ReactionRequired;

        internal static ElementApplicationResult Rejected(
            in ElementApplicationRequest _request,
            ElementApplicationRejectionReason _reason)
        {
            ElementAttachmentSnapshot currentAttachment = default;
            return Rejected(_request, _reason, currentAttachment);
        }

        internal static ElementApplicationResult Rejected(
            in ElementApplicationRequest _request,
            ElementApplicationRejectionReason _reason,
            in ElementAttachmentSnapshot _currentAttachment)
        {
            return new ElementApplicationResult(
                ElementApplicationResolutionStatus.Rejected,
                _reason,
                _request,
                _currentAttachment,
                _currentAttachment);
        }

        internal static ElementApplicationResult Committed(
            ElementApplicationResolutionStatus _status,
            in ElementApplicationRequest _request,
            in ElementAttachmentSnapshot _previousAttachment,
            in ElementAttachmentSnapshot _currentAttachment)
        {
            return new ElementApplicationResult(
                _status,
                ElementApplicationRejectionReason.None,
                _request,
                _previousAttachment,
                _currentAttachment);
        }

        internal static ElementApplicationResult Unchanged(
            in ElementApplicationRequest _request,
            in ElementAttachmentSnapshot _attachment)
        {
            return new ElementApplicationResult(
                ElementApplicationResolutionStatus.Unchanged,
                ElementApplicationRejectionReason.None,
                _request,
                _attachment,
                _attachment);
        }

        internal static ElementApplicationResult ReactionRequired(
            in ElementApplicationRequest _request,
            in ElementAttachmentSnapshot _attachment)
        {
            return new ElementApplicationResult(
                ElementApplicationResolutionStatus.ReactionRequired,
                ElementApplicationRejectionReason.None,
                _request,
                _attachment,
                _attachment);
        }
    }
}
