namespace INLO.Core.Pooling
{
    /// <summary>
    /// Read-only runtime diagnostics for a single pool.
    /// Used by editor/debug tools and optional runtime monitoring.
    /// </summary>
    public readonly struct PoolStats
    {
        public readonly PoolKey PoolKey;
        public readonly string PrefabName;
        public readonly int ActiveCount;
        public readonly int InactiveCount;
        public readonly int TotalCount;
        public readonly int MaxCount;
        public readonly int PeakActiveCount;
        public readonly PoolOverflowPolicy OverflowPolicy;

        /// <summary>
        /// Creates a pool stats snapshot.
        /// </summary>
        public PoolStats(
            PoolKey poolKey,
            string prefabName,
            int activeCount,
            int inactiveCount,
            int totalCount,
            int maxCount,
            int peakActiveCount,
            PoolOverflowPolicy overflowPolicy)
        {
            PoolKey = poolKey;
            PrefabName = prefabName;
            ActiveCount = activeCount;
            InactiveCount = inactiveCount;
            TotalCount = totalCount;
            MaxCount = maxCount;
            PeakActiveCount = peakActiveCount;
            OverflowPolicy = overflowPolicy;
        }

        /// <summary>
        /// True when total created instances are equal to or greater than MaxCount.
        /// </summary>
        public bool IsAtMax => MaxCount > 0 && TotalCount >= MaxCount;

        /// <summary>
        /// True when total created instances exceed MaxCount.
        /// This can happen with Expand policy.
        /// </summary>
        public bool IsOverMax => MaxCount > 0 && TotalCount > MaxCount;

        /// <summary>
        /// Current active count divided by MaxCount.
        /// </summary>
        public float ActiveUsageRatio => MaxCount > 0 ? (float)ActiveCount / MaxCount : 0f;

        /// <summary>
        /// Peak active count divided by MaxCount.
        /// Useful when tuning PreloadCount and MaxCount.
        /// </summary>
        public float PeakUsageRatio => MaxCount > 0 ? (float)PeakActiveCount / MaxCount : 0f;
    }
}
