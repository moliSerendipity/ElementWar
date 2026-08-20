using System;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 以运行时来源生命周期和目标生命周期标识独立的元素应用间隔槽。
    /// </summary>
    public readonly struct ElementApplicationIntervalKey : IEquatable<ElementApplicationIntervalKey>
    {
        internal ElementApplicationIntervalKey(
            ElementApplicationSourceId _sourceId,
            CombatantId _targetId)
        {
            SourceId = _sourceId;
            TargetId = _targetId;
        }

        /// <summary>参与间隔计算的运行时来源身份。</summary>
        public ElementApplicationSourceId SourceId { get; }

        /// <summary>参与间隔计算的目标生命周期身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>来源和目标身份是否都有效。</summary>
        public bool IsValid => SourceId.IsValid && TargetId.IsValid;

        /// <inheritdoc />
        public bool Equals(ElementApplicationIntervalKey _other) =>
            SourceId == _other.SourceId && TargetId == _other.TargetId;

        /// <inheritdoc />
        public override bool Equals(object _obj) =>
            _obj is ElementApplicationIntervalKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (SourceId.GetHashCode() * 397) ^ TargetId.GetHashCode();
            }
        }

        /// <summary>判断两个来源—目标间隔键是否相同。</summary>
        public static bool operator ==(
            ElementApplicationIntervalKey _left,
            ElementApplicationIntervalKey _right) => _left.Equals(_right);

        /// <summary>判断两个来源—目标间隔键是否不同。</summary>
        public static bool operator !=(
            ElementApplicationIntervalKey _left,
            ElementApplicationIntervalKey _right) => _left.Equals(_right) == false;
    }
}
