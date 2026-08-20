using System;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 标识一个运行时元素来源生命周期；同一来源跨多次攻击保持稳定，重建或复用时必须创建新身份。
    /// </summary>
    public readonly struct ElementApplicationSourceId : IEquatable<ElementApplicationSourceId>
    {
        private static long nextValue;

        private ElementApplicationSourceId(long _value)
        {
            Value = _value;
        }

        /// <summary>当前运行期内的递增值；零表示无效来源。</summary>
        public long Value { get; }

        /// <summary>是否代表一个已经建立的运行时元素来源。</summary>
        public bool IsValid => Value > 0L;

        /// <summary>
        /// 为一个新的元素来源生命周期创建运行时身份。
        /// </summary>
        /// <returns>当前运行期内唯一的有效来源身份。</returns>
        public static ElementApplicationSourceId Create()
        {
            return new ElementApplicationSourceId(Interlocked.Increment(ref nextValue));
        }

        /// <inheritdoc />
        public bool Equals(ElementApplicationSourceId _other) => Value == _other.Value;

        /// <inheritdoc />
        public override bool Equals(object _obj) =>
            _obj is ElementApplicationSourceId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>判断两个运行时元素来源身份是否相同。</summary>
        public static bool operator ==(
            ElementApplicationSourceId _left,
            ElementApplicationSourceId _right) => _left.Equals(_right);

        /// <summary>判断两个运行时元素来源身份是否不同。</summary>
        public static bool operator !=(
            ElementApplicationSourceId _left,
            ElementApplicationSourceId _right) => _left.Equals(_right) == false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCounter()
        {
            Interlocked.Exchange(ref nextValue, 0L);
        }
    }
}
