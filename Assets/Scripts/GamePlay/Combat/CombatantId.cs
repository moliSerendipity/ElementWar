using System;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 标识一个 Combatant 活动生命周期的运行时身份；不跨运行、存档或网络保持稳定。
    /// </summary>
    public readonly struct CombatantId : IEquatable<CombatantId>
    {
        private static long nextValue;

        private CombatantId(long _value)
        {
            Value = _value;
        }

        /// <summary>当前运行期内的递增值；零表示无效身份。</summary>
        public long Value { get; }

        /// <summary>身份是否已经由活动 Combatant 建立。</summary>
        public bool IsValid => Value > 0L;

        internal static CombatantId Create()
        {
            return new CombatantId(Interlocked.Increment(ref nextValue));
        }

        /// <inheritdoc />
        public bool Equals(CombatantId _other) => Value == _other.Value;

        /// <inheritdoc />
        public override bool Equals(object _obj) => _obj is CombatantId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        /// <summary>判断两个运行时战斗目标身份是否相同。</summary>
        public static bool operator ==(CombatantId _left, CombatantId _right) => _left.Equals(_right);

        /// <summary>判断两个运行时战斗目标身份是否不同。</summary>
        public static bool operator !=(CombatantId _left, CombatantId _right) => _left.Equals(_right) == false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCounter()
        {
            Interlocked.Exchange(ref nextValue, 0L);
        }
    }
}
