using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Pools;
using UnityEngine;

namespace Immersive.Pooling.Unity.Runtime
{
    public sealed class PoolRuntimeInstance
    {
        public PoolRuntimeInstance(PoolDefinitionAsset definition, GameObject instance, GameObjectPool pool)
        {
            Definition = definition;
            Instance = instance;
            Pool = pool;
        }

        public PoolDefinitionAsset Definition { get; }

        public GameObject Instance { get; }

        public GameObjectPool Pool { get; }
    }
}
