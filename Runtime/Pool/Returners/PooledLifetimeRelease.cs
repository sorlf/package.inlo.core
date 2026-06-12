using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Automatically returns a pooled object after a lifetime timer.
    /// Useful for temporary effects, projectiles, and short-lived objects.
    /// </summary>
    public sealed class PooledLifetimeRelease : PooledReturner
    {
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private bool releaseOnSpawned = true;

        private float _remainingTime;
        private bool _isCounting;

        protected override void OnSpawnedInternal()
        {
            if (!releaseOnSpawned)
            {
                return;
            }

            StartTimer(lifetime);
        }

        protected override void OnDespawnedInternal()
        {
            StopTimer();
        }

        private void Update()
        {
            if (!_isCounting)
            {
                return;
            }

            _remainingTime -= Time.deltaTime;

            if (_remainingTime > 0f)
            {
                return;
            }

            StopTimer();
            Release();
        }

        /// <summary>
        /// Starts or restarts the release timer.
        /// </summary>
        public void StartTimer(float duration)
        {
            _remainingTime = Mathf.Max(0f, duration);
            _isCounting = true;
        }

        /// <summary>
        /// Stops the release timer.
        /// </summary>
        public void StopTimer()
        {
            _remainingTime = 0f;
            _isCounting = false;
        }
    }
}
