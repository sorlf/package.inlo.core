namespace INLO.Core.Pooling
{
    /// <summary>
    /// Defines how PoolBootstrapper cleans up pool registrations and instances when destroyed.
    /// </summary>
    public enum PoolClearMode
    {
        /// <summary>
        /// Do not clean up on destroy. Recommended default for most scenes.
        /// </summary>
        None,

        /// <summary>
        /// Unregister only the keys registered by this bootstrapper.
        /// Shared keys remain registered until all owners unregister.
        /// </summary>
        RegisteredKeysOnly,

        /// <summary>
        /// Clears every pool registration, setting, and instance from PoolManager.
        /// Use only for controlled reset scenarios.
        /// </summary>
        All
    }
}
