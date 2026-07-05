using System.Collections.Generic;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using Immersive.Pooling.Unity.Hosts;
using UnityEngine;

namespace Immersive.Pooling.Unity.QA
{
    public sealed class PoolingQaContextMenuDriver : MonoBehaviour
    {
        [SerializeField] private PoolRuntimeHost host;
        [SerializeField] private PoolDefinitionAsset definition;
        [SerializeField] private Transform rentParent;
        [SerializeField] [Min(1)] private int burstCount = 3;

        private readonly List<GameObject> _rented = new List<GameObject>();

        [ContextMenu("Pooling QA/Ensure Pool")]
        private void EnsurePool()
        {
            GetService().EnsureRegistered(definition);
        }

        [ContextMenu("Pooling QA/Prewarm")]
        private void Prewarm()
        {
            GetService().Prewarm(definition);
        }

        [ContextMenu("Pooling QA/Rent One")]
        private void RentOne()
        {
            _rented.Add(GetService().Rent(definition, rentParent));
        }

        [ContextMenu("Pooling QA/Rent Burst")]
        private void RentBurst()
        {
            var service = GetService();

            for (var i = 0; i < burstCount; i++)
            {
                _rented.Add(service.Rent(definition, rentParent));
            }
        }

        [ContextMenu("Pooling QA/Return Last")]
        private void ReturnLast()
        {
            if (_rented.Count == 0)
            {
                return;
            }

            var lastIndex = _rented.Count - 1;
            var instance = _rented[lastIndex];
            _rented.RemoveAt(lastIndex);
            GetService().Return(definition, instance);
        }

        [ContextMenu("Pooling QA/Return All")]
        private void ReturnAll()
        {
            var service = GetService();

            for (var i = _rented.Count - 1; i >= 0; i--)
            {
                service.Return(definition, _rented[i]);
            }

            _rented.Clear();
        }

        [ContextMenu("Pooling QA/Clear Pool")]
        private void ClearPool()
        {
            GetService().Clear(definition);
            _rented.Clear();
        }

        [ContextMenu("Pooling QA/Run Basic Scenario")]
        private void RunBasicScenario()
        {
            EnsurePool();
            Prewarm();
            RentOne();
            ReturnLast();
            RentBurst();
            ReturnAll();
        }

        private IPoolService GetService()
        {
            if (definition == null)
            {
                throw new MissingReferenceException("Pooling QA requires a PoolDefinitionAsset.");
            }

            if (host == null)
            {
                host = GetComponentInParent<PoolRuntimeHost>();
            }

            if (host == null)
            {
                throw new MissingReferenceException("Pooling QA requires a PoolRuntimeHost.");
            }

            if (!host.IsInitialized)
            {
                host.Initialize();
            }

            return host.Service;
        }
    }
}
