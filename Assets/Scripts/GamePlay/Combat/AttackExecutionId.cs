using System;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 标识一次已经成立的攻击执行；同一执行可以各命中多个目标，但每个目标至多接受一次。
    /// </summary>
    public readonly struct AttackExecutionId : IEquatable<AttackExecutionId>
    {
        private static long nextValue;

        private AttackExecutionId(long _value)
        {
            Value = _value;
        }

        /// <summary>当前运行期内的递增值；零表示无效执行。</summary>
        public long Value { get; }

        /// <summary>是否代表一次已经建立的攻击执行。</summary>
        public bool IsValid => Value > 0L;

        /// <summary>
        /// 为一次新成立的攻击创建运行时身份。
        /// </summary>
        /// <returns>当前运行期内唯一的有效身份。</returns>
        public static AttackExecutionId Create()
        {
            return new AttackExecutionId(Interlocked.Increment(ref nextValue));
        }

        /// <inheritdoc />
        public bool Equals(AttackExecutionId _other) => Value == _other.Value;

        /// <inheritdoc />
        public override bool Equals(object _obj) => _obj is AttackExecutionId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>判断两个运行时攻击执行身份是否相同。</summary>
        public static bool operator ==(AttackExecutionId _left, AttackExecutionId _right) => _left.Equals(_right);

        /// <summary>判断两个运行时攻击执行身份是否不同。</summary>
        public static bool operator !=(AttackExecutionId _left, AttackExecutionId _right) => _left.Equals(_right) == false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCounter()
        {
            Interlocked.Exchange(ref nextValue, 0L);
        }
    }
}
