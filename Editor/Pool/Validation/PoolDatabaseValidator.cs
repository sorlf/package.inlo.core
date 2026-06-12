using System.Collections.Generic;
using INLO.Core.Pooling;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolDatabaseValidator
    {
        public static PoolDatabaseValidationResult Validate(PoolDatabase database)
        {
            PoolDatabaseValidationResult result = new();

            if (database == null)
            {
                result.Add(PoolDatabaseValidationSeverity.Error, -1, "PoolDatabase is null.");
                return result;
            }

            IReadOnlyList<PoolEntry> entries = database.Entries;

            if (entries == null || entries.Count == 0)
            {
                result.Add(PoolDatabaseValidationSeverity.Warning, -1, "PoolDatabase has no entries.");
                return result;
            }

            Dictionary<string, int> keyIndices = new();
            Dictionary<GameObject, int> prefabIndices = new();

            for (int i = 0; i < entries.Count; i++)
            {
                PoolEntry entry = entries[i];

                if (entry == null)
                {
                    result.Add(PoolDatabaseValidationSeverity.Error, i, "Entry is null.");
                    continue;
                }

                string key = entry.PoolKeyValue;

                if (string.IsNullOrWhiteSpace(key))
                {
                    result.Add(PoolDatabaseValidationSeverity.Error, i, "PoolKey is empty.");
                }
                else
                {
                    string normalizedKey = PoolKeyEditorUtility.NormalizeKey(key);

                    if (normalizedKey != key)
                    {
                        result.Add(PoolDatabaseValidationSeverity.Warning, i, $"PoolKey '{key}' is not normalized. Suggested: '{normalizedKey}'.");
                    }

                    if (keyIndices.TryGetValue(key, out int previousIndex))
                    {
                        result.Add(PoolDatabaseValidationSeverity.Error, i, $"Duplicate PoolKey '{key}'. First entry index: {previousIndex}.");
                    }
                    else
                    {
                        keyIndices.Add(key, i);
                    }
                }

                if (entry.Prefab == null)
                {
                    result.Add(PoolDatabaseValidationSeverity.Error, i, $"Prefab is missing. PoolKey: '{key}'.");
                }
                else
                {
                    ValidatePrefab(result, entry, i);

                    if (prefabIndices.TryGetValue(entry.Prefab, out int previousPrefabIndex))
                    {
                        result.Add(PoolDatabaseValidationSeverity.Warning, i, $"Same prefab is used by multiple entries. First entry index: {previousPrefabIndex}.");
                    }
                    else
                    {
                        prefabIndices.Add(entry.Prefab, i);
                    }
                }

                if (entry.PreloadCount > entry.MaxCount && entry.OverflowPolicy != PoolOverflowPolicy.Expand)
                {
                    result.Add(PoolDatabaseValidationSeverity.Warning, i, "PreloadCount is greater than MaxCount. Preload will stop at MaxCount.");
                }

                if (entry.OverflowPolicy == PoolOverflowPolicy.ReuseOldest)
                {
                    result.Add(PoolDatabaseValidationSeverity.Info, i, "ReuseOldest will recycle the oldest active object when the pool reaches MaxCount.");
                }
            }

            if (result.Messages.Count == 0)
            {
                result.Add(PoolDatabaseValidationSeverity.Info, -1, "PoolDatabase is valid.");
            }

            return result;
        }

        private static void ValidatePrefab(PoolDatabaseValidationResult result, PoolEntry entry, int index)
        {
            GameObject prefab = entry.Prefab;

            bool hasParticle = prefab.GetComponentInChildren<ParticleSystem>(true) != null;
            bool hasParticleReturner = prefab.GetComponentInChildren<PooledParticleRelease>(true) != null;
            bool hasLifetimeReturner = prefab.GetComponentInChildren<PooledLifetimeRelease>(true) != null;
            bool hasAnimationEventReturner = prefab.GetComponentInChildren<PooledAnimationEventRelease>(true) != null;
            int poolableCount = prefab.GetComponentsInChildren<IPoolable>(true).Length;

            if (hasParticle && !hasParticleReturner)
            {
                result.Add(
                    PoolDatabaseValidationSeverity.Warning,
                    index,
                    "Prefab contains ParticleSystem but has no PooledParticleRelease. It must be released manually or by another returner.");
            }

            if (!hasParticleReturner && !hasLifetimeReturner && !hasAnimationEventReturner)
            {
                result.Add(
                    PoolDatabaseValidationSeverity.Info,
                    index,
                    "Prefab has no built-in returner. Make sure it calls PoolManager.Release manually.");
            }

            if (poolableCount > 1)
            {
                result.Add(
                    PoolDatabaseValidationSeverity.Info,
                    index,
                    $"Prefab has multiple IPoolable components ({poolableCount}). All will receive OnSpawned and OnDespawned.");
            }
        }
    }
}
