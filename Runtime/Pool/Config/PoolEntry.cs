using System;
using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// A single PoolDatabase row that maps a PoolKey to a prefab and its runtime pool settings.
    /// </summary>
    [Serializable]
    public sealed class PoolEntry
    {
        [SerializeField] private string poolKey;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int preloadCount = 0;
        [SerializeField] private int maxCount = 100;
        [SerializeField] private PoolOverflowPolicy overflowPolicy = PoolOverflowPolicy.Expand;

        /// <summary>
        /// Key used by gameplay code or designer data to request this prefab.
        /// </summary>
        public PoolKey PoolKey => new PoolKey(poolKey);

        /// <summary>
        /// Raw key value used by editor validation and tooling.
        /// </summary>
        public string PoolKeyValue => poolKey;

        /// <summary>
        /// Prefab instantiated by the pool.
        /// </summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// Number of inactive instances created when this entry is registered.
        /// </summary>
        public int PreloadCount => Mathf.Max(0, preloadCount);

        /// <summary>
        /// Recommended maximum count before overflow policy is applied.
        /// </summary>
        public int MaxCount => Mathf.Max(1, maxCount);

        /// <summary>
        /// Overflow behavior for this pool.
        /// </summary>
        public PoolOverflowPolicy OverflowPolicy => overflowPolicy;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only setter used by PoolDatabase utility tools.
        /// </summary>
        public void EditorSetPoolKey(string value)
        {
            poolKey = value;
        }

        /// <summary>
        /// Editor-only normalization for count fields.
        /// </summary>
        public void EditorNormalizeCounts()
        {
            preloadCount = Mathf.Max(0, preloadCount);
            maxCount = Mathf.Max(1, maxCount);
        }
#endif
    }
}
