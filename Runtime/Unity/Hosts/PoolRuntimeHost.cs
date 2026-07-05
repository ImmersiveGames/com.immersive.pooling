using System;
using System.Collections.Generic;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using Immersive.Pooling.Unity.Runtime;
using UnityEngine;

namespace Immersive.Pooling.Unity.Hosts
{
    public sealed class PoolRuntimeHost : MonoBehaviour
    {
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool persistAcrossScenes;
        [SerializeField] private List<PoolDefinitionAsset> poolDefinitions = new List<PoolDefinitionAsset>();

        private PoolService _service;

        public IPoolService Service => _service;

        public bool IsInitialized => _service != null && !_service.IsShutdown;

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        [ContextMenu("Pooling/Initialize")]
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            for (var i = 0; i < poolDefinitions.Count; i++)
            {
                var definition = poolDefinitions[i];
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"PoolRuntimeHost '{name}' has a null pool definition at index {i}.");
                }

                definition.ValidateOrThrow();
            }

            _service = new PoolService(transform);

            for (var i = 0; i < poolDefinitions.Count; i++)
            {
                _service.EnsureRegistered(poolDefinitions[i]);
            }
        }

        [ContextMenu("Pooling/Prewarm All")]
        public void PrewarmAll()
        {
            EnsureInitialized();

            for (var i = 0; i < poolDefinitions.Count; i++)
            {
                _service.Prewarm(poolDefinitions[i]);
            }
        }

        [ContextMenu("Pooling/Return All")]
        public void ReturnAll()
        {
            EnsureInitialized();

            for (var i = 0; i < poolDefinitions.Count; i++)
            {
                _service.ReturnAll(poolDefinitions[i]);
            }
        }

        [ContextMenu("Pooling/Clear All")]
        public void ClearAll()
        {
            if (_service == null)
            {
                return;
            }

            for (var i = 0; i < poolDefinitions.Count; i++)
            {
                _service.Clear(poolDefinitions[i]);
            }
        }

        [ContextMenu("Pooling/Shutdown")]
        public void Shutdown()
        {
            if (_service == null)
            {
                return;
            }

            _service.Shutdown();
            _service = null;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }
    }
}
