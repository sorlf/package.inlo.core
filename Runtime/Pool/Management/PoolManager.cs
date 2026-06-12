using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Global entry point for registering pool data, spawning pooled objects, returning objects, and reading diagnostics.
    /// Gameplay code should normally use PoolKey-based Get/TryGet and Release/TryRelease.
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<GameObject, GameObjectPool> PoolsByPrefab = new();
        private static readonly Dictionary<GameObject, GameObjectPool> PoolsByInstance = new();
        private static readonly Dictionary<PoolKey, GameObject> PrefabsByKey = new();
        private static readonly Dictionary<GameObject, PoolKey> KeysByPrefab = new();
        private static readonly Dictionary<PoolKey, PoolSettings> SettingsByKey = new();
        private static readonly Dictionary<GameObject, PoolSettings> SettingsByPrefab = new();
        private static readonly Dictionary<PoolKey, int> RegistrationCountsByKey = new();

        private static Transform _root;

        /// <summary>
        /// Gets an instance using a prefab reference directly. Prefer PoolKey-based APIs for normal gameplay code.
        /// Logs errors for invalid prefab or missing component.
        /// </summary>
        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation)
            where T : Component
        {
            ValidateRuntimeState();

            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Prefab is null.");
                return null;
            }

            GameObject instance = Get(prefab.gameObject, position, rotation);

            if (instance == null)
            {
                return null;
            }

            T component = instance.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"[PoolManager] Spawned object does not contain component: {typeof(T).Name}");
                Release(instance);
                return null;
            }

            return component;
        }

        /// <summary>
        /// Gets a GameObject instance using a prefab reference directly. Prefer PoolKey-based APIs for normal gameplay code.
        /// Logs an error if the prefab is invalid.
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            ValidateRuntimeState();

            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Prefab is null.");
                return null;
            }

            GameObjectPool pool = GetOrCreatePool(prefab, GetSettings(prefab));
            GameObject instance = pool.Get();

            if (instance == null)
            {
                return null;
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            PoolsByInstance[instance] = pool;

            return instance;
        }

        /// <summary>
        /// Gets a component instance by PoolKey. Recommended API for gameplay code using designer-authored data.
        /// Logs errors if the key is invalid, unregistered, or the spawned object does not contain T.
        /// </summary>
        public static T Get<T>(PoolKey poolKey, Vector3 position, Quaternion rotation)
            where T : Component
        {
            GameObject instance = Get(poolKey, position, rotation);

            if (instance == null)
            {
                return null;
            }

            T component = instance.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"[PoolManager] Spawned object for key '{poolKey}' does not contain component: {typeof(T).Name}");
                Release(instance);
                return null;
            }

            return component;
        }

        /// <summary>
        /// Gets a GameObject instance by PoolKey. Logs errors if the key is invalid or unregistered.
        /// </summary>
        public static GameObject Get(PoolKey poolKey, Vector3 position, Quaternion rotation)
        {
            ValidateRuntimeState();

            if (!poolKey.IsValid)
            {
                Debug.LogError("[PoolManager] PoolKey is invalid.");
                return null;
            }

            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                Debug.LogError($"[PoolManager] PoolKey is not registered. Key: {poolKey}");
                return null;
            }

            GameObjectPool pool = GetOrCreatePool(prefab, GetSettings(poolKey));
            GameObject instance = pool.Get();

            if (instance == null)
            {
                return null;
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            PoolsByInstance[instance] = pool;

            return instance;
        }

        /// <summary>
        /// Attempts to get a component instance by PoolKey without logging expected failure cases.
        /// Use this when spawn failure is a valid gameplay branch.
        /// </summary>
        public static bool TryGet<T>(PoolKey poolKey, Vector3 position, Quaternion rotation, out T instance)
            where T : Component
        {
            instance = null;

            GameObject gameObject = TryGet(poolKey, position, rotation);

            if (gameObject == null)
            {
                return false;
            }

            instance = gameObject.GetComponent<T>();

            if (instance != null)
            {
                return true;
            }

            Release(gameObject);
            return false;
        }

        /// <summary>
        /// Attempts to get a component instance from a prefab reference without logging expected failure cases.
        /// Prefer PoolKey-based TryGet for normal gameplay code.
        /// </summary>
        public static bool TryGet<T>(T prefab, Vector3 position, Quaternion rotation, out T instance)
            where T : Component
        {
            instance = null;

            if (prefab == null)
            {
                return false;
            }

            GameObject gameObject = TryGet(prefab.gameObject, position, rotation);

            if (gameObject == null)
            {
                return false;
            }

            instance = gameObject.GetComponent<T>();

            if (instance != null)
            {
                return true;
            }

            Release(gameObject);
            return false;
        }

        /// <summary>
        /// Attempts to get a GameObject by PoolKey without logging expected failure cases.
        /// </summary>
        public static GameObject TryGet(PoolKey poolKey, Vector3 position, Quaternion rotation)
        {
            ValidateRuntimeState();

            if (!poolKey.IsValid)
            {
                return null;
            }

            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                return null;
            }

            GameObjectPool pool = GetOrCreatePool(prefab, GetSettings(poolKey));
            GameObject instance = pool.Get();

            if (instance == null)
            {
                return null;
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            PoolsByInstance[instance] = pool;

            return instance;
        }

        /// <summary>
        /// Attempts to get a GameObject from a prefab reference without logging expected failure cases.
        /// </summary>
        public static GameObject TryGet(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            ValidateRuntimeState();

            if (prefab == null)
            {
                return null;
            }

            GameObjectPool pool = GetOrCreatePool(prefab, GetSettings(prefab));
            GameObject instance = pool.Get();

            if (instance == null)
            {
                return null;
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            PoolsByInstance[instance] = pool;

            return instance;
        }

        /// <summary>
        /// Returns a managed pooled component to its pool. Logs a warning if the object is not managed.
        /// </summary>
        public static void Release(Component instance)
        {
            if (instance == null)
            {
                return;
            }

            Release(instance.gameObject);
        }

        /// <summary>
        /// Returns a managed pooled GameObject to its pool. Logs a warning if the object is not managed.
        /// </summary>
        public static void Release(GameObject instance)
        {
            ValidateRuntimeState();

            if (instance == null)
            {
                return;
            }

            if (!PoolsByInstance.TryGetValue(instance, out GameObjectPool pool))
            {
                Debug.LogWarning($"[PoolManager] Object is not managed by PoolManager. Object: {instance.name}");
                return;
            }

            pool.Release(instance);
        }

        /// <summary>
        /// Attempts to return a pooled component without logging if it is not managed.
        /// </summary>
        public static bool TryRelease(Component instance)
        {
            if (instance == null)
            {
                return false;
            }

            return TryRelease(instance.gameObject);
        }

        /// <summary>
        /// Attempts to return a pooled GameObject without logging if it is not managed.
        /// </summary>
        public static bool TryRelease(GameObject instance)
        {
            ValidateRuntimeState();

            if (instance == null)
            {
                return false;
            }

            if (!PoolsByInstance.TryGetValue(instance, out GameObjectPool pool))
            {
                return false;
            }

            pool.Release(instance);
            return true;
        }

        /// <summary>
        /// Returns true if the GameObject is currently known by PoolManager.
        /// </summary>
        public static bool IsManaged(GameObject instance)
        {
            ValidateRuntimeState();

            return instance != null && PoolsByInstance.ContainsKey(instance);
        }

        /// <summary>
        /// Returns true if the component's GameObject is currently known by PoolManager.
        /// </summary>
        public static bool IsManaged(Component instance)
        {
            return instance != null && IsManaged(instance.gameObject);
        }

        /// <summary>
        /// Preloads inactive instances from a prefab reference. Prefer PoolDatabase PreloadCount or PoolKey-based Preload when possible.
        /// </summary>
        public static void Preload<T>(T prefab, int count)
            where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Prefab is null.");
                return;
            }

            Preload(prefab.gameObject, count);
        }

        /// <summary>
        /// Preloads inactive GameObject instances from a prefab reference.
        /// </summary>
        public static void Preload(GameObject prefab, int count)
        {
            ValidateRuntimeState();

            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Prefab is null.");
                return;
            }

            GameObjectPool pool = GetOrCreatePool(prefab, GetSettings(prefab));
            pool.Preload(count);
        }

        /// <summary>
        /// Preloads inactive instances for a registered PoolKey.
        /// </summary>
        public static void Preload(PoolKey poolKey, int count)
        {
            ValidateRuntimeState();

            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                Debug.LogError($"[PoolManager] PoolKey is not registered. Key: {poolKey}");
                return;
            }

            GameObjectPool pool = GetOrCreatePool(prefab, GetSettings(poolKey));
            pool.Preload(count);
        }

        /// <summary>
        /// Registers all entries in a PoolDatabase. Intended for PoolBootstrapper or controlled initialization code.
        /// </summary>
        public static int Register(PoolDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("[PoolManager] PoolDatabase is null.");
                return 0;
            }

            int registeredCount = 0;

            foreach (PoolEntry entry in database.Entries)
            {
                if (Register(entry))
                {
                    registeredCount++;
                }
            }

            return registeredCount;
        }

        /// <summary>
        /// Registers a single PoolEntry. Intended for PoolBootstrapper or controlled initialization code.
        /// </summary>
        public static bool Register(PoolEntry entry)
        {
            ValidateRuntimeState();

            if (entry == null)
            {
                return false;
            }

            if (!entry.PoolKey.IsValid)
            {
                Debug.LogWarning("[PoolManager] PoolEntry has invalid PoolKey.");
                return false;
            }

            if (entry.Prefab == null)
            {
                Debug.LogWarning($"[PoolManager] PoolEntry prefab is null. Key: {entry.PoolKey}");
                return false;
            }

            PoolSettings settings = new(entry.MaxCount, entry.OverflowPolicy);

            if (PrefabsByKey.TryGetValue(entry.PoolKey, out GameObject registeredPrefab))
            {
                if (registeredPrefab != entry.Prefab)
                {
                    Debug.LogWarning($"[PoolManager] PoolKey is already registered with another prefab. Key: {entry.PoolKey}");
                    return false;
                }

                SettingsByKey[entry.PoolKey] = settings;
                SettingsByPrefab[entry.Prefab] = settings;
                IncrementRegistrationCount(entry.PoolKey);
                return true;
            }

            PrefabsByKey.Add(entry.PoolKey, entry.Prefab);
            SettingsByKey.Add(entry.PoolKey, settings);
            SettingsByPrefab[entry.Prefab] = settings;
            RegistrationCountsByKey[entry.PoolKey] = 1;

            if (!KeysByPrefab.ContainsKey(entry.Prefab))
            {
                KeysByPrefab.Add(entry.Prefab, entry.PoolKey);
            }

            GameObjectPool pool = GetOrCreatePool(entry.Prefab, settings);

            if (entry.PreloadCount > 0)
            {
                pool.Preload(entry.PreloadCount);
            }

            return true;
        }

        /// <summary>
        /// Decrements the registration reference count for a PoolKey and unregisters it when no owners remain.
        /// Intended for bootstrapper lifetime management.
        /// </summary>
        public static bool Unregister(PoolKey poolKey, bool clearPoolWhenUnused = true)
        {
            if (!poolKey.IsValid)
            {
                return false;
            }

            if (!RegistrationCountsByKey.TryGetValue(poolKey, out int count))
            {
                return false;
            }

            count--;

            if (count > 0)
            {
                RegistrationCountsByKey[poolKey] = count;
                return false;
            }

            RegistrationCountsByKey.Remove(poolKey);

            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                SettingsByKey.Remove(poolKey);
                return true;
            }

            PrefabsByKey.Remove(poolKey);
            SettingsByKey.Remove(poolKey);

            if (clearPoolWhenUnused)
            {
                ClearPoolInstances(prefab);
                SettingsByPrefab.Remove(prefab);
                KeysByPrefab.Remove(prefab);
            }

            return true;
        }

        /// <summary>
        /// Unregisters all entries in a PoolDatabase using registration reference counts.
        /// Intended for bootstrapper lifetime management.
        /// </summary>
        public static int Unregister(PoolDatabase database, bool clearPoolWhenUnused = true)
        {
            if (database == null)
            {
                return 0;
            }

            int unregisteredCount = 0;

            foreach (PoolEntry entry in database.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (Unregister(entry.PoolKey, clearPoolWhenUnused))
                {
                    unregisteredCount++;
                }
            }

            return unregisteredCount;
        }

        /// <summary>
        /// Returns true if the PoolKey is currently registered.
        /// </summary>
        public static bool IsRegistered(PoolKey poolKey)
        {
            return poolKey.IsValid && PrefabsByKey.ContainsKey(poolKey);
        }

        /// <summary>
        /// Returns how many registration owners currently hold this PoolKey.
        /// Primarily used by diagnostics and editor tools.
        /// </summary>
        public static int GetRegistrationCount(PoolKey poolKey)
        {
            if (!poolKey.IsValid)
            {
                return 0;
            }

            return RegistrationCountsByKey.TryGetValue(poolKey, out int count) ? count : 0;
        }

        /// <summary>
        /// Returns diagnostics for all currently created pools.
        /// Primarily used by debug windows and runtime diagnostics.
        /// </summary>
        public static IReadOnlyList<PoolStats> GetAllStats()
        {
            ValidateRuntimeState();

            List<PoolStats> stats = new();

            foreach (KeyValuePair<GameObject, GameObjectPool> pair in PoolsByPrefab)
            {
                GameObject prefab = pair.Key;
                GameObjectPool pool = pair.Value;

                KeysByPrefab.TryGetValue(prefab, out PoolKey poolKey);

                stats.Add(new PoolStats(
                    poolKey,
                    prefab != null ? prefab.name : "Missing Prefab",
                    pool.ActiveCount,
                    pool.InactiveCount,
                    pool.TotalCount,
                    pool.MaxCount,
                    pool.PeakActiveCount,
                    pool.OverflowPolicy
                ));
            }

            return stats;
        }

        /// <summary>
        /// Attempts to get diagnostics for a registered PoolKey.
        /// </summary>
        public static bool TryGetStats(PoolKey poolKey, out PoolStats stats)
        {
            stats = default;

            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                return false;
            }

            return TryGetStats(prefab, out stats);
        }

        /// <summary>
        /// Attempts to get diagnostics for a prefab-backed pool.
        /// </summary>
        public static bool TryGetStats(GameObject prefab, out PoolStats stats)
        {
            stats = default;

            if (prefab == null)
            {
                return false;
            }

            if (!PoolsByPrefab.TryGetValue(prefab, out GameObjectPool pool))
            {
                return false;
            }

            KeysByPrefab.TryGetValue(prefab, out PoolKey poolKey);

            stats = new PoolStats(
                poolKey,
                prefab.name,
                pool.ActiveCount,
                pool.InactiveCount,
                pool.TotalCount,
                pool.MaxCount,
                pool.PeakActiveCount,
                pool.OverflowPolicy
            );

            return true;
        }

        /// <summary>
        /// Resets PeakActiveCount for all currently created pools.
        /// Does not clear pool instances.
        /// </summary>
        public static void ResetAllPeaks()
        {
            foreach (GameObjectPool pool in PoolsByPrefab.Values)
            {
                pool.ResetPeak();
            }
        }

        /// <summary>
        /// Resets PeakActiveCount for a registered PoolKey.
        /// </summary>
        public static void ResetPeak(PoolKey poolKey)
        {
            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                Debug.LogWarning($"[PoolManager] PoolKey is not registered. Key: {poolKey}");
                return;
            }

            ResetPeak(prefab);
        }

        /// <summary>
        /// Resets PeakActiveCount for a prefab-backed pool.
        /// </summary>
        public static void ResetPeak(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            if (!PoolsByPrefab.TryGetValue(prefab, out GameObjectPool pool))
            {
                return;
            }

            pool.ResetPeak();
        }

        /// <summary>
        /// Clears all pool registrations, settings, created instances, and diagnostic state.
        /// This is the strongest reset API and should not be used in ordinary gameplay flow.
        /// </summary>
        public static void ClearAll()
        {
            foreach (GameObjectPool pool in PoolsByPrefab.Values)
            {
                pool.Clear();
            }

            PoolsByPrefab.Clear();
            PoolsByInstance.Clear();
            PrefabsByKey.Clear();
            KeysByPrefab.Clear();
            SettingsByKey.Clear();
            SettingsByPrefab.Clear();
            RegistrationCountsByKey.Clear();

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }

        /// <summary>
        /// Clears registration, settings, and created instances for a PoolKey.
        /// Use ClearPoolInstances when you only need to destroy created instances while keeping registration.
        /// </summary>
        public static void ClearRegistrationAndInstances(PoolKey poolKey)
        {
            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                Debug.LogWarning($"[PoolManager] PoolKey is not registered. Key: {poolKey}");
                return;
            }

            RegistrationCountsByKey.Remove(poolKey);
            SettingsByKey.Remove(poolKey);
            PrefabsByKey.Remove(poolKey);
            ClearPoolInstances(prefab);
            SettingsByPrefab.Remove(prefab);
            KeysByPrefab.Remove(prefab);
        }

        /// <summary>
        /// Clears registration, settings, and created instances for a prefab.
        /// Use ClearPoolInstances when you only need to destroy created instances while keeping settings where possible.
        /// </summary>
        public static void ClearRegistrationAndInstances(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            ClearPoolInstances(prefab);

            SettingsByPrefab.Remove(prefab);
            KeysByPrefab.Remove(prefab);

            List<PoolKey> keyRemoveTargets = new();

            foreach (KeyValuePair<PoolKey, GameObject> pair in PrefabsByKey)
            {
                if (pair.Value == prefab)
                {
                    keyRemoveTargets.Add(pair.Key);
                }
            }

            foreach (PoolKey key in keyRemoveTargets)
            {
                PrefabsByKey.Remove(key);
                SettingsByKey.Remove(key);
                RegistrationCountsByKey.Remove(key);
            }
        }

        /// <summary>
        /// Clears only created instances for a PoolKey while keeping registration and settings.
        /// The pool can be recreated with the same settings on the next Get call.
        /// </summary>
        public static void ClearPoolInstances(PoolKey poolKey)
        {
            if (!PrefabsByKey.TryGetValue(poolKey, out GameObject prefab))
            {
                Debug.LogWarning($"[PoolManager] PoolKey is not registered. Key: {poolKey}");
                return;
            }

            ClearPoolInstances(prefab);
        }

        /// <summary>
        /// Clears only created instances for a prefab-backed pool while keeping registration and settings where possible.
        /// </summary>
        public static void ClearPoolInstances(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            if (!PoolsByPrefab.TryGetValue(prefab, out GameObjectPool pool))
            {
                return;
            }

            pool.Clear();
            PoolsByPrefab.Remove(prefab);
            RemoveInstanceMappings(pool);
        }

        /// <summary>
        /// Deprecated compatibility wrapper for ClearAll.
        /// </summary>
        [System.Obsolete("Use ClearAll() instead. Clear() will be removed in a future version.", false)]
        public static void Clear()
        {
            ClearAll();
        }

        /// <summary>
        /// Deprecated compatibility wrapper for ClearRegistrationAndInstances(PoolKey).
        /// </summary>
        [System.Obsolete("Use ClearRegistrationAndInstances(PoolKey) instead. Clear(PoolKey) will be removed in a future version.", false)]
        public static void Clear(PoolKey poolKey)
        {
            ClearRegistrationAndInstances(poolKey);
        }

        /// <summary>
        /// Deprecated compatibility wrapper for ClearRegistrationAndInstances(GameObject).
        /// </summary>
        [System.Obsolete("Use ClearRegistrationAndInstances(GameObject) instead. Clear(GameObject) will be removed in a future version.", false)]
        public static void Clear(GameObject prefab)
        {
            ClearRegistrationAndInstances(prefab);
        }

        /// <summary>
        /// Deprecated compatibility wrapper for ClearPoolInstances(PoolKey).
        /// </summary>
        [System.Obsolete("Use ClearPoolInstances(PoolKey) instead. ClearInstances(PoolKey) will be removed in a future version.", false)]
        public static void ClearInstances(PoolKey poolKey)
        {
            ClearPoolInstances(poolKey);
        }

        /// <summary>
        /// Deprecated compatibility wrapper for ClearPoolInstances(GameObject).
        /// </summary>
        [System.Obsolete("Use ClearPoolInstances(GameObject) instead. ClearInstances(GameObject) will be removed in a future version.", false)]
        public static void ClearInstances(GameObject prefab)
        {
            ClearPoolInstances(prefab);
        }

        /// <summary>
        /// Removes invalid runtime references left by scene unloads or destroyed pool roots.
        /// Called automatically by PoolRuntimeInitializer, but exposed for debug tools.
        /// </summary>
        public static void CleanupInvalidReferences()
        {
            ValidateRuntimeState();
        }

        internal static void ResetStaticState()
        {
            PoolsByPrefab.Clear();
            PoolsByInstance.Clear();
            PrefabsByKey.Clear();
            KeysByPrefab.Clear();
            SettingsByKey.Clear();
            SettingsByPrefab.Clear();
            RegistrationCountsByKey.Clear();
            _root = null;
        }

        private static PoolSettings GetSettings(PoolKey poolKey)
        {
            return SettingsByKey.TryGetValue(poolKey, out PoolSettings settings)
                ? settings
                : PoolSettings.Default;
        }

        private static PoolSettings GetSettings(GameObject prefab)
        {
            return SettingsByPrefab.TryGetValue(prefab, out PoolSettings settings)
                ? settings
                : PoolSettings.Default;
        }

        private static void IncrementRegistrationCount(PoolKey poolKey)
        {
            if (!RegistrationCountsByKey.TryGetValue(poolKey, out int count))
            {
                count = 0;
            }

            RegistrationCountsByKey[poolKey] = count + 1;
        }

        private static GameObjectPool GetOrCreatePool(GameObject prefab, PoolSettings settings)
        {
            if (PoolsByPrefab.TryGetValue(prefab, out GameObjectPool existingPool))
            {
                return existingPool;
            }

            EnsureRoot();

            GameObject poolRootObject = new GameObject($"{prefab.name} Pool");
            poolRootObject.transform.SetParent(_root);

            GameObjectPool pool = new GameObjectPool(
                prefab,
                poolRootObject.transform,
                settings.MaxCount,
                settings.OverflowPolicy
            );

            PoolsByPrefab.Add(prefab, pool);

            return pool;
        }

        private static void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("[PoolManager]");
            _root = rootObject.transform;
        }

        private static void ValidateRuntimeState()
        {
            if (_root == null && PoolsByPrefab.Count > 0)
            {
                ClearRuntimePoolsOnly();
                return;
            }

            RemoveInvalidInstanceMappings();
            RemoveInvalidPrefabMappings();
        }

        private static void RemoveInvalidInstanceMappings()
        {
            if (PoolsByInstance.Count == 0)
            {
                return;
            }

            List<GameObject> removeTargets = null;

            foreach (KeyValuePair<GameObject, GameObjectPool> pair in PoolsByInstance)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    continue;
                }

                removeTargets ??= new List<GameObject>();
                removeTargets.Add(pair.Key);
            }

            if (removeTargets == null)
            {
                return;
            }

            for (int i = 0; i < removeTargets.Count; i++)
            {
                PoolsByInstance.Remove(removeTargets[i]);
            }
        }

        private static void RemoveInvalidPrefabMappings()
        {
            if (PoolsByPrefab.Count == 0)
            {
                return;
            }

            List<GameObject> invalidPrefabs = null;

            foreach (KeyValuePair<GameObject, GameObjectPool> pair in PoolsByPrefab)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    continue;
                }

                invalidPrefabs ??= new List<GameObject>();
                invalidPrefabs.Add(pair.Key);
            }

            if (invalidPrefabs == null)
            {
                return;
            }

            for (int i = 0; i < invalidPrefabs.Count; i++)
            {
                ClearPoolInstances(invalidPrefabs[i]);
            }
        }

        private static void RemoveInstanceMappings(GameObjectPool pool)
        {
            List<GameObject> removeTargets = new();

            foreach (KeyValuePair<GameObject, GameObjectPool> pair in PoolsByInstance)
            {
                if (pair.Value == pool)
                {
                    removeTargets.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeTargets.Count; i++)
            {
                PoolsByInstance.Remove(removeTargets[i]);
            }
        }

        private static void ClearRuntimePoolsOnly()
        {
            PoolsByPrefab.Clear();
            PoolsByInstance.Clear();
            _root = null;
        }
    }
}
