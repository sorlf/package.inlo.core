namespace INLO.Core.Pooling
{
    /// <summary>
    /// Runtime settings used when creating a GameObjectPool for a registered PoolKey or prefab.
    /// Settings are kept separately from created pool instances so pools can be recreated safely.
    /// </summary>
    public readonly struct PoolSettings
    {
        /// <summary>
        /// Recommended maximum number of pooled instances before overflow policy is applied.
        /// </summary>
        public readonly int MaxCount;

        /// <summary>
        /// Policy used when the pool reaches MaxCount and no inactive object is available.
        /// </summary>
        public readonly PoolOverflowPolicy OverflowPolicy;

        /// <summary>
        /// Creates pool settings.
        /// </summary>
        public PoolSettings(int maxCount, PoolOverflowPolicy overflowPolicy)
        {
            MaxCount = maxCount < 1 ? 1 : maxCount;
            OverflowPolicy = overflowPolicy;
        }

        /// <summary>
        /// Default settings used for direct prefab pooling when no PoolDatabase entry exists.
        /// </summary>
        public static PoolSettings Default => new PoolSettings(100, PoolOverflowPolicy.Expand);
    }
}
