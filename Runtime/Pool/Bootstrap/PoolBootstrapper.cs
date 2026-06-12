using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Scene component that registers PoolDatabase assets and PoolDatabaseGroup assets into PoolManager during Awake.
    /// </summary>
    public sealed class PoolBootstrapper : MonoBehaviour
    {
        [Header("Legacy Single Database")]
        [SerializeField] private PoolDatabase database;

        [Header("Multiple Databases")]
        [SerializeField] private List<PoolDatabase> databases = new();
        [SerializeField] private PoolDatabaseGroup databaseGroup;

        [Header("Lifetime")]
        [SerializeField] private PoolClearMode clearOnDestroy = PoolClearMode.None;

        private readonly List<PoolKey> _registeredKeys = new();

        private void Awake()
        {
            RegisterAll();
        }

        private void OnDestroy()
        {
            ClearByMode();
        }

        private void RegisterAll()
        {
            HashSet<PoolDatabase> visited = new();

            RegisterDatabase(database, visited);

            if (databases != null)
            {
                for (int i = 0; i < databases.Count; i++)
                {
                    RegisterDatabase(databases[i], visited);
                }
            }

            if (databaseGroup != null && databaseGroup.Databases != null)
            {
                IReadOnlyList<PoolDatabase> groupDatabases = databaseGroup.Databases;

                for (int i = 0; i < groupDatabases.Count; i++)
                {
                    RegisterDatabase(groupDatabases[i], visited);
                }
            }
        }

        private void RegisterDatabase(PoolDatabase targetDatabase, HashSet<PoolDatabase> visited)
        {
            if (targetDatabase == null)
            {
                return;
            }

            if (!visited.Add(targetDatabase))
            {
                return;
            }

            foreach (PoolEntry entry in targetDatabase.Entries)
            {
                if (entry == null || !entry.PoolKey.IsValid)
                {
                    continue;
                }

                if (!PoolManager.Register(entry))
                {
                    continue;
                }

                _registeredKeys.Add(entry.PoolKey);
            }
        }

        private void ClearByMode()
        {
            switch (clearOnDestroy)
            {
                case PoolClearMode.None:
                    return;

                case PoolClearMode.RegisteredKeysOnly:
                    UnregisterRegisteredKeys();
                    return;

                case PoolClearMode.All:
                    PoolManager.ClearAll();
                    return;
            }
        }

        private void UnregisterRegisteredKeys()
        {
            for (int i = 0; i < _registeredKeys.Count; i++)
            {
                PoolManager.Unregister(_registeredKeys[i], true);
            }

            _registeredKeys.Clear();
        }
    }
}
