namespace INLO.Core.Pooling
{
    /// <summary>
    /// Basic pool contract used by runtime pool implementations.
    /// </summary>
    /// <typeparam name="T">Type of item managed by the pool.</typeparam>
    public interface IPool<T>
    {
        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        T Get();

        /// <summary>
        /// Returns an item to the pool.
        /// </summary>
        void Release(T item);

        /// <summary>
        /// Creates inactive items ahead of time.
        /// </summary>
        void Preload(int count);

        /// <summary>
        /// Destroys all items managed by this pool and clears internal state.
        /// </summary>
        void Clear();
    }
}
