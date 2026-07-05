using Immersive.Pooling.Policies;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Runtime;
using UnityEngine;

namespace Immersive.Pooling.Unity.Contracts
{
    public interface IPoolService
    {
        bool IsShutdown { get; }

        void EnsureRegistered(PoolDefinitionAsset definition);

        void Prewarm(PoolDefinitionAsset definition);

        GameObject Rent(PoolDefinitionAsset definition, Transform parent = null);

        GameObject Spawn(PoolDefinitionAsset definition, Transform parent = null);

        bool Return(PoolDefinitionAsset definition, GameObject instance);

        int ReturnAll(PoolDefinitionAsset definition);

        void Clear(PoolDefinitionAsset definition);

        int ClearPoolsForScope(PoolLifetimeScope scope);

        bool TryGetSnapshot(PoolDefinitionAsset definition, out PoolRuntimeSnapshot snapshot);

        void Shutdown();
    }
}
