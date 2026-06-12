using System;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Stable identifier used to resolve a pooled prefab without referencing the prefab directly from gameplay data.
    /// PoolKey is the bridge between designer-authored data and the pooling runtime.
    /// </summary>
    [Serializable]
    public readonly struct PoolKey : IEquatable<PoolKey>
    {
        /// <summary>
        /// Raw string value of the key.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Creates a PoolKey from a string value.
        /// </summary>
        public PoolKey(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns true when the key contains non-whitespace text.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        /// <summary>
        /// Compares this key with another key using ordinal string comparison.
        /// </summary>
        public bool Equals(PoolKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PoolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
        }

        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Allows string literals or string fields to be passed directly to PoolKey-based APIs.
        /// </summary>
        public static implicit operator PoolKey(string value)
        {
            return new PoolKey(value);
        }

        public static bool operator ==(PoolKey left, PoolKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PoolKey left, PoolKey right)
        {
            return !left.Equals(right);
        }
    }
}
