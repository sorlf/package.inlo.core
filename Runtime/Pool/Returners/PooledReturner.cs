using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Base class for components that automatically return their GameObject to PoolManager.
    /// It also implements IPoolable to prevent duplicate releases after despawn.
    /// </summary>
    public abstract class PooledReturner : MonoBehaviour, IPoolable
    {
        private bool _isSpawned;

        /// <summary>
        /// True while this object is currently spawned from a pool.
        /// </summary>
        public bool IsSpawned => _isSpawned;

        public void OnSpawned()
        {
            _isSpawned = true;
            OnSpawnedInternal();
        }

        public void OnDespawned()
        {
            _isSpawned = false;
            OnDespawnedInternal();
        }

        /// <summary>
        /// Safely returns this GameObject to its pool if it is currently spawned.
        /// </summary>
        protected void Release()
        {
            if (!_isSpawned)
            {
                return;
            }

            PoolManager.Release(gameObject);
        }

        /// <summary>
        /// Override to run logic when the object is spawned.
        /// </summary>
        protected virtual void OnSpawnedInternal()
        {
        }

        /// <summary>
        /// Override to run logic when the object is despawned.
        /// </summary>
        protected virtual void OnDespawnedInternal()
        {
        }
    }
}
