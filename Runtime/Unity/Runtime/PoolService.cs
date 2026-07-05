using System;
using System.Collections.Generic;
using Immersive.Pooling.Policies;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using Immersive.Pooling.Unity.Pools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Pooling.Unity.Runtime
{
    public sealed class PoolService : IPoolService
    {
        private readonly Dictionary<PoolDefinitionAsset, RegisteredPool> _pools =
            new Dictionary<PoolDefinitionAsset, RegisteredPool>();

        private readonly Transform _root;
        private readonly bool _ownsRoot;
        private readonly MonoBehaviour _coroutineHost;

        public PoolService(Transform root = null)
        {
            if (root == null)
            {
                var rootObject = new GameObject("Immersive Pooling Runtime");
                _root = rootObject.transform;
                _ownsRoot = true;
            }
            else
            {
                _root = root;
            }

            _coroutineHost = _root.GetComponent<PoolServiceCoroutineHost>();
            if (_coroutineHost == null)
            {
                _coroutineHost = _root.gameObject.AddComponent<PoolServiceCoroutineHost>();
            }
        }

        public bool IsShutdown { get; private set; }

        public void EnsureRegistered(PoolDefinitionAsset definition)
        {
            ThrowIfShutdown();
            var validDefinition = ValidateDefinition(definition);

            if (_pools.ContainsKey(validDefinition))
            {
                return;
            }

            var poolRootObject = new GameObject(GetPoolRootName(validDefinition));
            var poolRoot = poolRootObject.transform;
            poolRoot.SetParent(_root, false);

            var pool = new GameObjectPool(
                validDefinition.Prefab,
                poolRoot,
                0,
                validDefinition.MaxSize,
                validDefinition.CanExpand,
                validDefinition.AutoReturnSeconds,
                _coroutineHost);

            _pools.Add(validDefinition, new RegisteredPool(pool, poolRoot));

            if (validDefinition.PrewarmOnRegister)
            {
                pool.Prewarm(validDefinition.InitialCapacity);
            }
        }

        public void Prewarm(PoolDefinitionAsset definition)
        {
            ThrowIfShutdown();
            var validDefinition = ValidateDefinition(definition);
            EnsureRegistered(validDefinition);
            _pools[validDefinition].Pool.Prewarm(validDefinition.InitialCapacity);
        }

        public GameObject Rent(PoolDefinitionAsset definition, Transform parent = null)
        {
            ThrowIfShutdown();
            var pool = GetPoolForRent(ValidateDefinition(definition));
            return pool.Rent(parent);
        }

        public GameObject Spawn(PoolDefinitionAsset definition, Transform parent = null)
        {
            return Rent(definition, parent);
        }

        public bool Return(PoolDefinitionAsset definition, GameObject instance)
        {
            ThrowIfShutdown();

            if (instance == null)
            {
                return false;
            }

            return TryGetRegisteredPool(ValidateDefinition(definition), out var pool) && pool.Return(instance);
        }

        public int ReturnAll(PoolDefinitionAsset definition)
        {
            ThrowIfShutdown();
            return TryGetRegisteredPool(ValidateDefinition(definition), out var pool) ? pool.ReturnAll() : 0;
        }

        public void Clear(PoolDefinitionAsset definition)
        {
            ThrowIfShutdown();
            var validDefinition = ValidateDefinition(definition);

            if (!_pools.TryGetValue(validDefinition, out var registeredPool))
            {
                return;
            }

            registeredPool.Pool.Clear();
            DestroyObject(registeredPool.Root.gameObject);
            _pools.Remove(validDefinition);
        }

        public int ClearPoolsForScope(PoolLifetimeScope scope)
        {
            ThrowIfShutdown();
            var definitions = new List<PoolDefinitionAsset>();

            foreach (var pair in _pools)
            {
                if (pair.Key != null && pair.Key.LifetimeScope == scope)
                {
                    definitions.Add(pair.Key);
                }
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                Clear(definitions[i]);
            }

            return definitions.Count;
        }

        public bool TryGetSnapshot(PoolDefinitionAsset definition, out PoolRuntimeSnapshot snapshot)
        {
            snapshot = default;

            if (definition == null || !_pools.TryGetValue(definition, out var registeredPool))
            {
                return false;
            }

            var pool = registeredPool.Pool;
            snapshot = new PoolRuntimeSnapshot(pool.ActiveCount, pool.InactiveCount, pool.TotalCount);
            return true;
        }

        public void Shutdown()
        {
            if (IsShutdown)
            {
                return;
            }

            var definitions = new List<PoolDefinitionAsset>(_pools.Keys);
            for (var i = 0; i < definitions.Count; i++)
            {
                Clear(definitions[i]);
            }

            IsShutdown = true;

            if (_ownsRoot && _root != null)
            {
                DestroyObject(_root.gameObject);
            }
        }

        private GameObjectPool GetPoolForRent(PoolDefinitionAsset definition)
        {
            if (TryGetRegisteredPool(definition, out var pool))
            {
                return pool;
            }

            if (definition.RegistrationMode != PoolRegistrationMode.LazyOnFirstRent)
            {
                throw new InvalidOperationException(
                    $"Pool '{definition.name}' requires explicit registration before rent.");
            }

            EnsureRegistered(definition);
            return _pools[definition].Pool;
        }

        private bool TryGetRegisteredPool(PoolDefinitionAsset definition, out GameObjectPool pool)
        {
            if (_pools.TryGetValue(definition, out var registeredPool))
            {
                pool = registeredPool.Pool;
                return true;
            }

            pool = null;
            return false;
        }

        private void ThrowIfShutdown()
        {
            if (IsShutdown)
            {
                throw new InvalidOperationException("PoolService is already shutdown.");
            }
        }

        private static PoolDefinitionAsset ValidateDefinition(PoolDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.ValidateOrThrow();
            return definition;
        }

        private static string GetPoolRootName(PoolDefinitionAsset definition)
        {
            return $"Pool - {definition.PoolLabel}";
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }

            Object.DestroyImmediate(target);
        }

        private sealed class RegisteredPool
        {
            public RegisteredPool(GameObjectPool pool, Transform root)
            {
                Pool = pool;
                Root = root;
            }

            public GameObjectPool Pool { get; }

            public Transform Root { get; }
        }
    }
}
