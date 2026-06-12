namespace INLO.Core.Pooling
{
    /// <summary>
    /// Defines how a pool behaves when no inactive object is available and the pool has reached MaxCount.
    /// </summary>
    public enum PoolOverflowPolicy
    {
        /// <summary>
        /// Creates additional instances beyond MaxCount. Safest for gameplay-critical objects.
        /// </summary>
        Expand,

        /// <summary>
        /// Ignores the request and logs a warning. Useful for optional visual objects.
        /// </summary>
        IgnoreRequest,

        /// <summary>
        /// Returns null without logging. Use when the caller is expected to handle failure quietly.
        /// </summary>
        ReturnNull,

        /// <summary>
        /// Recycles the oldest active object. Useful for damage text, floating messages, and replaceable effects.
        /// </summary>
        ReuseOldest
    }
}
