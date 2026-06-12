namespace INLO.Core.Pooling
{
    /// <summary>
    /// Receives lifecycle callbacks from the pooling system.
    /// Implement this on pooled objects that need to restore or reset state when reused.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called after the pooled GameObject is activated and before the instance is returned from PoolManager.Get.
        /// Use this for common restore logic, not per-use gameplay data injection.
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// Called before the pooled GameObject is deactivated and stored back in the pool.
        /// Use this to cancel timers, stop movement, clear references, and reset temporary state.
        /// </summary>
        void OnDespawned();
    }
}
