using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Automatically returns a pooled object when a ParticleSystem stops being alive.
    /// Attach this to pooled particle effect prefabs.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticleRelease : PooledReturner
    {
        [SerializeField] private ParticleSystem targetParticle;
        [SerializeField] private bool playOnSpawned = true;
        [SerializeField] private bool stopOnDespawned = true;

        private void Reset()
        {
            targetParticle = GetComponent<ParticleSystem>();
        }

        private void Awake()
        {
            if (targetParticle == null)
            {
                targetParticle = GetComponent<ParticleSystem>();
            }
        }

        protected override void OnSpawnedInternal()
        {
            if (targetParticle == null)
            {
                return;
            }

            if (playOnSpawned)
            {
                targetParticle.Clear(true);
                targetParticle.Play(true);
            }
        }

        protected override void OnDespawnedInternal()
        {
            if (targetParticle == null || !stopOnDespawned)
            {
                return;
            }

            targetParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (targetParticle == null)
            {
                return;
            }

            if (targetParticle.IsAlive(true))
            {
                return;
            }

            Release();
        }
    }
}
