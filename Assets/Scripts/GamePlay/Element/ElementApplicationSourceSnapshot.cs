using Game.Definition.Combat;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 在元素来源建立时冻结配置与归属；后续攻击不得重新读取可变配置或当前控制角色。
    /// </summary>
    public readonly struct ElementApplicationSourceSnapshot
    {
        internal ElementApplicationSourceSnapshot(
            ElementApplicationSourceId _sourceId,
            string _profileId,
            ElementType _element,
            float _sourceTargetIntervalSeconds,
            float _attachmentDurationSeconds,
            Combatant _instigatorCombatant,
            CombatantId _instigatorId,
            CombatFaction _instigatorFaction,
            Object _sourceObject)
        {
            SourceId = _sourceId;
            ProfileId = _profileId;
            Element = _element;
            SourceTargetIntervalSeconds = _sourceTargetIntervalSeconds;
            AttachmentDurationSeconds = _attachmentDurationSeconds;
            InstigatorCombatant = _instigatorCombatant;
            InstigatorId = _instigatorId;
            InstigatorFaction = _instigatorFaction;
            SourceObject = _sourceObject;
            IsCreated = true;
        }

        /// <summary>本运行时来源生命周期的身份。</summary>
        public ElementApplicationSourceId SourceId { get; }

        /// <summary>创建快照时解析到的配置逻辑键。</summary>
        public string ProfileId { get; }

        /// <summary>本来源尝试施加的元素。</summary>
        public ElementType Element { get; }

        /// <summary>同一来源再次尝试影响同一目标前的最短间隔，单位为秒。</summary>
        public float SourceTargetIntervalSeconds { get; }

        /// <summary>成功附着后的默认持续时间，单位为秒。</summary>
        public float AttachmentDurationSeconds { get; }

        /// <summary>来源建立时承担归属的战斗实体引用。</summary>
        public Combatant InstigatorCombatant { get; }

        /// <summary>来源建立时冻结的责任实体身份。</summary>
        public CombatantId InstigatorId { get; }

        /// <summary>来源建立时冻结的责任实体阵营。</summary>
        public CombatFaction InstigatorFaction { get; }

        /// <summary>产生元素应用的具体武器、技能运行时、持续区域或配置对象。</summary>
        public Object SourceObject { get; }

        internal bool IsCreated { get; }
    }
}
