using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Groups multiple PoolDatabase assets so a single PoolBootstrapper can register them together.
    /// Useful when pools are separated by domain such as bullets, effects, monsters, and UI.
    /// </summary>
    [CreateAssetMenu(menuName = "INLO/Pooling/Pool Database Group", fileName = "PoolDatabaseGroup")]
    public sealed class PoolDatabaseGroup : ScriptableObject
    {
        [SerializeField] private List<PoolDatabase> databases = new();

        /// <summary>
        /// Databases included in this group.
        /// </summary>
        public IReadOnlyList<PoolDatabase> Databases => databases;
    }
}
