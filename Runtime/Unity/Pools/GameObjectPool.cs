using System;
using System.Collections.Generic;
using Immersive.Pooling.Contracts;
using Immersive.Pooling.Unity.Instances;
using Immersive.Pooling.Unity.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Pooling.Unity.Pools
{
    public sealed class GameObjectPool
    {
        private readonly Dictionary<GameObject, Entry> _entries = new Dictionary<GameObject, Entry>();
        private readonly Stack<GameObject> _available = new Stack<GameObject>();
        private readonly PoolAutoReturnTracker _autoReturnTracker;

        public GameObjectPool(GameObject prefab, Transform parent = null, int initialCapacity = 0)
            : this(prefab, parent, initialCapacity, int.MaxValue, true, 0f, null)
        {
        }

        public GameObjectPool(
            GameObject prefab,
            Transform parent,
            int initialCapacity,
            int maxSize,
            bool canExpand,
            float autoReturnSeconds = 0f,
            MonoBehaviour coroutineHost = null)
        {
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            Parent = parent;
            InitialCapacity = initialCapacity;
            MaxSize = maxSize;
            CanExpand = canExpand;
            AutoReturnSeconds = autoReturnSeconds;

            ValidateCapacity(initialCapacity, maxSize, autoReturnSeconds);

            if (AutoReturnSeconds > 0f)
            {
                _autoReturnTracker = new PoolAutoReturnTracker(coroutineHost);
            }

            Prewarm(initialCapacity);
        }

        public GameObject Prefab { get; }

        public Transform Parent { get; }

        public int InitialCapacity { get; }

        public int MaxSize { get; }

        public bool CanExpand { get; }

        public float AutoReturnSeconds { get; }

        public int AvailableCount => CountAvailableEntries();

        public int InactiveCount => AvailableCount;

        public int TakenCount => ActiveCount;

        public int ActiveCount => TotalCount - AvailableCount;

        public int TotalCount => _entries.Count;

        public void Prewarm(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (TotalCount < count)
            {
                if (!TryCreateInstance(available: true, out _))
                {
                    throw new InvalidOperationException(
                        $"Pool prewarm exceeded capacity. prefab='{Prefab.name}' requested='{count}' max='{MaxSize}'.");
                }
            }
        }

        public GameObject Take()
        {
            return Rent();
        }

        public GameObject Rent(Transform parent = null)
        {
            while (_available.Count > 0)
            {
                var instance = _available.Pop();

                if (instance == null)
                {
                    RemoveDestroyedEntries();
                    continue;
                }

                if (!_entries.TryGetValue(instance, out var entry) || !entry.isAvailable)
                {
                    continue;
                }

                entry.isAvailable = false;
                entry.rentCount++;
                BindReturnHandle(instance);
                MoveToParent(instance, parent ?? Parent);
                ActivateAndNotify(instance);
                TrackAutoReturn(instance);
                return instance;
            }

            if (!TryCreateInstance(available: false, out var created))
            {
                throw new InvalidOperationException(
                    $"Pool limit reached. prefab='{Prefab.name}' active='{ActiveCount}' inactive='{InactiveCount}' total='{TotalCount}' max='{MaxSize}' canExpand='{CanExpand}'.");
            }

            BindReturnHandle(created);
            MoveToParent(created, parent ?? Parent);
            ActivateAndNotify(created);
            TrackAutoReturn(created);
            return created;
        }

        public GameObject Spawn(Transform parent = null)
        {
            return Rent(parent);
        }

        public bool Return(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!_entries.TryGetValue(instance, out var entry))
            {
                return false;
            }

            if (entry.isAvailable)
            {
                return false;
            }

            CancelAutoReturn(instance);
            NotifyPoolables(instance, PoolNotification.Returned);
            MoveToParent(instance, Parent);
            instance.SetActive(false);

            entry.isAvailable = true;
            MarkReturnHandle(instance);
            _available.Push(instance);
            return true;
        }

        public int ReturnAll()
        {
            var instances = new List<GameObject>(_entries.Keys);
            var returned = 0;

            for (var i = 0; i < instances.Count; i++)
            {
                if (Return(instances[i]))
                {
                    returned++;
                }
            }

            return returned;
        }

        public void Clear()
        {
            _autoReturnTracker?.Clear();

            var entries = new List<GameObject>(_entries.Keys);

            for (var i = 0; i < entries.Count; i++)
            {
                DestroyInstance(entries[i]);
            }

            _available.Clear();
            _entries.Clear();
        }

        private bool TryCreateInstance(bool available, out GameObject instance)
        {
            instance = null;

            if (!CanCreate(available))
            {
                return false;
            }

            instance = Parent == null
                ? Object.Instantiate(Prefab)
                : Object.Instantiate(Prefab, Parent);

            MoveToParent(instance, Parent);
            instance.SetActive(false);
            EnsureReturnHandle(instance);

            _entries.Add(instance, new Entry { isAvailable = available });
            NotifyPoolables(instance, PoolNotification.Created);

            if (available)
            {
                _available.Push(instance);
            }

            return true;
        }

        private bool CanCreate(bool available)
        {
            if (TotalCount >= MaxSize)
            {
                return false;
            }

            if (available)
            {
                return true;
            }

            return TotalCount < InitialCapacity || CanExpand;
        }

        private void ActivateAndNotify(GameObject instance)
        {
            instance.SetActive(true);
            NotifyPoolables(instance, PoolNotification.Taken);
        }

        private static void NotifyPoolables(GameObject instance, PoolNotification notification)
        {
            var components = instance.GetComponentsInChildren<MonoBehaviour>(true);

            for (var i = 0; i < components.Length; i++)
            {
                switch (components[i])
                {
                    case IPoolLifecycle lifecycle when notification == PoolNotification.Created:
                        lifecycle.OnCreatedByPool();
                        break;
                    case IPoolLifecycle lifecycle when notification == PoolNotification.Destroyed:
                        lifecycle.OnDestroyedByPool();
                        break;
                    case IPoolable poolable when notification == PoolNotification.Taken:
                        poolable.OnTakenFromPool();
                        break;
                    case IPoolable poolable when notification == PoolNotification.Returned:
                        poolable.OnReturnedToPool();
                        break;
                }
            }
        }

        private static void MoveToParent(GameObject instance, Transform parent)
        {
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }
        }

        private void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            CancelAutoReturn(instance);
            NotifyPoolables(instance, PoolNotification.Destroyed);
            ClearReturnHandle(instance);

            if (Application.isPlaying)
            {
                Object.Destroy(instance);
                return;
            }

            Object.DestroyImmediate(instance);
        }

        private void TrackAutoReturn(GameObject instance)
        {
            if (_autoReturnTracker == null || AutoReturnSeconds <= 0f)
            {
                return;
            }

            _autoReturnTracker.Track(instance, AutoReturnSeconds, Return);
        }

        private void CancelAutoReturn(GameObject instance)
        {
            _autoReturnTracker?.Cancel(instance);
        }

        private void RemoveDestroyedEntries()
        {
            var destroyed = new List<GameObject>();

            foreach (var instance in _entries.Keys)
            {
                if (instance == null)
                {
                    destroyed.Add(instance);
                }
            }

            for (var i = 0; i < destroyed.Count; i++)
            {
                _entries.Remove(destroyed[i]);
            }
        }

        private static void ValidateCapacity(int initialCapacity, int maxSize, float autoReturnSeconds)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            if (maxSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize));
            }

            if (initialCapacity > maxSize)
            {
                throw new ArgumentException("Initial capacity must be less than or equal to max size.");
            }

            if (autoReturnSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(autoReturnSeconds));
            }
        }

        private sealed class Entry
        {
            public bool isAvailable;
            public int rentCount;
        }

        private enum PoolNotification
        {
            Created,
            Taken,
            Returned,
            Destroyed
        }

        private static PoolReturnHandle EnsureReturnHandle(GameObject instance)
        {
            var handle = instance.GetComponent<PoolReturnHandle>();

            if (handle != null)
            {
                return handle;
            }

            return instance.AddComponent<PoolReturnHandle>();
        }

        private void BindReturnHandle(GameObject instance)
        {
            var handle = EnsureReturnHandle(instance);
            handle.Bind(this);
        }

        private static void MarkReturnHandle(GameObject instance)
        {
            var handle = instance.GetComponent<PoolReturnHandle>();

            if (handle != null)
            {
                handle.MarkReturned();
            }
        }

        private static void ClearReturnHandle(GameObject instance)
        {
            var handle = instance.GetComponent<PoolReturnHandle>();

            if (handle != null)
            {
                handle.ClearBinding();
            }
        }

        private int CountAvailableEntries()
        {
            var count = 0;

            foreach (var entry in _entries.Values)
            {
                if (entry.isAvailable)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
