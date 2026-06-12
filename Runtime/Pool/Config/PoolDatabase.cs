using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// ScriptableObject database that registers PoolKey to prefab mappings and pool settings.
    /// Project-specific PoolDatabase assets should usually live under Assets, not inside the core package.
    /// </summary>
    [CreateAssetMenu(menuName = "INLO/Pooling/Pool Database", fileName = "PoolDatabase")]
    public sealed class PoolDatabase : ScriptableObject
    {
        [SerializeField] private List<PoolEntry> entries = new();

        /// <summary>
        /// Entries registered by PoolBootstrapper or PoolManager.Register.
        /// </summary>
        public IReadOnlyList<PoolEntry> Entries => entries;
    }
}
