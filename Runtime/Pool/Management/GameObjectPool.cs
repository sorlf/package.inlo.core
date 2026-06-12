using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.Pooling
{
    /// <summary>
    /// Runtime pool implementation for GameObject instances.
    /// This class is normally accessed through PoolManager rather than used directly in gameplay code.
    /// </summary>
    public sealed class GameObjectPool : IPool<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Stack<GameObject> _inactiveObjects = new();
        private readonly Queue<GameObject> _activeOrder = new();
        private readonly HashSet<GameObject> _activeObjects = new();
        private readonly HashSet<GameObject> _allObjects = new();
        private readonly Dictionary<GameObject, IPoolable[]> _poolablesByInstance = new();

        private readonly int _maxCount;
        private readonly PoolOverflowPolicy _overflowPolicy;

        private int _peakActiveCount;

        public GameObject Prefab => _prefab;
        public int ActiveCount => _activeObjects.Count;
        public int InactiveCount => _inactiveObjects.Count;
        public int TotalCount => _allObjects.Count;
        public int MaxCount => _maxCount;
        public int PeakActiveCount => _peakActiveCount;
        public PoolOverflowPolicy OverflowPolicy => _overflowPolicy;

        public GameObjectPool(
            GameObject prefab,
            Transform root,
            int maxCount = 100,
            PoolOverflowPolicy overflowPolicy = PoolOverflowPolicy.Expand)
        {
            _prefab = prefab;
            _root = root;
            _maxCount = Mathf.Max(1, maxCount);
            _overflowPolicy = overflowPolicy;
        }

        public GameObject Get()
        {
            GameObject instance;

            if (_inactiveObjects.Count > 0)
            {
                instance = _inactiveObjects.Pop();
            }
            else
            {
                if (_allObjects.Count >= _maxCount)
                {
                    instance = HandleOverflow();

                    if (instance == null)
                    {
                        return null;
                    }
                }
                else
                {
                    instance = CreateInstance();
                }
            }

            MarkAsActive(instance);

            instance.SetActive(true);
            InvokeOnSpawned(instance);

            return instance;
        }

        public void Release(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (!_allObjects.Contains(item))
            {
                Debug.LogWarning($"[GameObjectPool] This object does not belong to this pool. Object: {item.name}");
                return;
            }

            if (!_activeObjects.Remove(item))
            {
                Debug.LogWarning($"[GameObjectPool] Object is already released. Object: {item.name}");
                return;
            }

            Despawn(item);

            _inactiveObjects.Push(item);
        }

        public void Preload(int count)
        {
            count = Mathf.Max(0, count);

            for (int i = 0; i < count; i++)
            {
                if (_allObjects.Count >= _maxCount && _overflowPolicy != PoolOverflowPolicy.Expand)
                {
                    Debug.LogWarning($"[GameObjectPool] Preload stopped by max count. Prefab: {_prefab.name}");
                    break;
                }

                GameObject instance = CreateInstance();
                instance.SetActive(false);
                _inactiveObjects.Push(instance);
            }
        }

        public void Clear()
        {
            foreach (GameObject obj in _allObjects)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            _inactiveObjects.Clear();
            _activeOrder.Clear();
            _activeObjects.Clear();
            _allObjects.Clear();
            _poolablesByInstance.Clear();
            _peakActiveCount = 0;
        }

        public void ResetPeak()
        {
            _peakActiveCount = _activeObjects.Count;
        }

        public bool Contains(GameObject instance)
        {
            return _allObjects.Contains(instance);
        }

        private GameObject HandleOverflow()
        {
            switch (_overflowPolicy)
            {
                case PoolOverflowPolicy.Expand:
                    return CreateInstance();

                case PoolOverflowPolicy.IgnoreRequest:
                    Debug.LogWarning($"[GameObjectPool] Pool limit reached. Request ignored. Prefab: {_prefab.name}");
                    return null;

                case PoolOverflowPolicy.ReturnNull:
                    return null;

                case PoolOverflowPolicy.ReuseOldest:
                    return ReuseOldestActiveObject();

                default:
                    return null;
            }
        }

        private GameObject ReuseOldestActiveObject()
        {
            while (_activeOrder.Count > 0)
            {
                GameObject candidate = _activeOrder.Dequeue();

                if (candidate == null)
                {
                    continue;
                }

                if (!_activeObjects.Remove(candidate))
                {
                    continue;
                }

                if (!_allObjects.Contains(candidate))
                {
                    continue;
                }

                Despawn(candidate);
                return candidate;
            }

            Debug.LogWarning($"[GameObjectPool] ReuseOldest failed because no active object was available. Prefab: {_prefab.name}");
            return null;
        }

        private void MarkAsActive(GameObject instance)
        {
            _activeObjects.Add(instance);
            _activeOrder.Enqueue(instance);

            if (_activeObjects.Count > _peakActiveCount)
            {
                _peakActiveCount = _activeObjects.Count;
            }
        }

        private void Despawn(GameObject item)
        {
            InvokeOnDespawned(item);

            item.transform.SetParent(_root);
            item.SetActive(false);
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Object.Instantiate(_prefab, _root);
            instance.name = _prefab.name;

            _allObjects.Add(instance);
            CachePoolables(instance);

            return instance;
        }

        private void CachePoolables(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            _poolablesByInstance[instance] = poolables;
        }

        private void InvokeOnSpawned(GameObject instance)
        {
            if (!_poolablesByInstance.TryGetValue(instance, out IPoolable[] poolables))
            {
                CachePoolables(instance);
                poolables = _poolablesByInstance[instance];
            }

            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnSpawned();
            }
        }

        private void InvokeOnDespawned(GameObject instance)
        {
            if (!_poolablesByInstance.TryGetValue(instance, out IPoolable[] poolables))
            {
                CachePoolables(instance);
                poolables = _poolablesByInstance[instance];
            }

            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnDespawned();
            }
        }
    }
}
