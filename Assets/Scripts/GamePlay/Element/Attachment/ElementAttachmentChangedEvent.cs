using Game.Gameplay.Combat;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 目标元素附着事实已经提交、刷新或清除后的通知；Presentation 只能读取该事实。
    /// </summary>
    public readonly struct ElementAttachmentChangedEvent
    {
        /// <summary>创建一条已经完成附着事实写回的通知。</summary>
        /// <param name="_changeKind">本次事实变化的确定原因。</param>
        /// <param name="_targetCombatant">发生变化的权威目标根。</param>
        /// <param name="_targetId">发生变化时对应的目标生命周期身份。</param>
        /// <param name="_previousAttachment">变化前的附着；首次附着时为默认值。</param>
        /// <param name="_currentAttachment">变化后的附着；清除时为默认值。</param>
        /// <param name="_changeTime">与元素请求相同时间轴上的变化时间。</param>
        public ElementAttachmentChangedEvent(
            ElementAttachmentChangeKind _changeKind,
            Combatant _targetCombatant,
            CombatantId _targetId,
            in ElementAttachmentSnapshot _previousAttachment,
            in ElementAttachmentSnapshot _currentAttachment,
            float _changeTime)
        {
            ChangeKind = _changeKind;
            TargetCombatant = _targetCombatant;
            TargetId = _targetId;
            PreviousAttachment = _previousAttachment;
            CurrentAttachment = _currentAttachment;
            ChangeTime = _changeTime;
        }

        /// <summary>本次事实变化的确定原因。</summary>
        public ElementAttachmentChangeKind ChangeKind { get; }

        /// <summary>发生变化的权威目标根。</summary>
        public Combatant TargetCombatant { get; }

        /// <summary>发生变化时对应的目标生命周期身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>变化前的附着；首次附着时为默认值。</summary>
        public ElementAttachmentSnapshot PreviousAttachment { get; }

        /// <summary>变化后的附着；清除时为默认值。</summary>
        public ElementAttachmentSnapshot CurrentAttachment { get; }

        /// <summary>与元素请求相同时间轴上的变化时间。</summary>
        public float ChangeTime { get; }
    }
}
