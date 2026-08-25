using Game.Definition.Element;

namespace Game.Gameplay.Element
{
    /// <summary>一次已提交反应的最小只读事实；默认值表示没有触发反应。</summary>
    public readonly struct ElementReactionResult
    {
        private ElementReactionResult(
            ElementReactionType _reactionType,
            in ElementAttachmentSnapshot _consumedAttachment,
            in ElementApplicationRequest _triggeringApplication)
        {
            ReactionType = _reactionType;
            ConsumedAttachment = _consumedAttachment;
            TriggeringApplication = _triggeringApplication;
        }

        /// <summary>成功提交的反应类型；默认值为 None。</summary>
        public ElementReactionType ReactionType { get; }

        /// <summary>反应提交时被消费的原有附着及其来源快照。</summary>
        public ElementAttachmentSnapshot ConsumedAttachment { get; }

        /// <summary>作为第二元素并承担反应归属的触发请求。</summary>
        public ElementApplicationRequest TriggeringApplication { get; }

        /// <summary>是否已经原子消费附着并提交一次反应事实。</summary>
        public bool DidTriggerReaction => ReactionType != ElementReactionType.None;

        internal static ElementReactionResult Triggered(
            ElementReactionType _reactionType,
            in ElementAttachmentSnapshot _consumedAttachment,
            in ElementApplicationRequest _triggeringApplication)
        {
            return new ElementReactionResult(
                _reactionType,
                _consumedAttachment,
                _triggeringApplication);
        }
    }
}
