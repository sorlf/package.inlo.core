using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Returns a pooled object from an Animation Event.
    /// Add an animation event that calls ReleaseByAnimationEvent.
    /// </summary>
    public sealed class PooledAnimationEventRelease : PooledReturner
    {
        /// <summary>
        /// Animation Event entry point for returning this object to its pool.
        /// </summary>
        public void ReleaseByAnimationEvent()
        {
            Release();
        }
    }
}
