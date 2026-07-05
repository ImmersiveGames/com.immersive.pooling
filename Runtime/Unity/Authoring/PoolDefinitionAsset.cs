using System;
using Immersive.Pooling.Policies;
using UnityEngine;

namespace Immersive.Pooling.Unity.Authoring
{
    [CreateAssetMenu(
        fileName = "PoolDefinition",
        menuName = "Immersive/Pooling/Pool Definition",
        order = 20)]
    public sealed class PoolDefinitionAsset : ScriptableObject
    {
        [Header("Object")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private string poolLabel = "pool";

        [Header("Capacity")]
        [SerializeField] [Min(0)] private int initialCapacity = 1;
        [SerializeField] [Min(1)] private int maxSize = 32;
        [SerializeField] private bool canExpand = true;
        [SerializeField] private bool prewarmOnRegister;

        [Header("Lifetime")]
        [SerializeField] private PoolLifetimeScope lifetimeScope = PoolLifetimeScope.Temporary;
        [SerializeField] private PoolRegistrationMode registrationMode = PoolRegistrationMode.LazyOnFirstRent;
        [SerializeField] [Min(0f)] private float autoReturnSeconds;

        public GameObject Prefab => prefab;

        public string PoolLabel => string.IsNullOrWhiteSpace(poolLabel) ? name : poolLabel;

        public int InitialCapacity => initialCapacity;

        public int MaxSize => maxSize;

        public bool CanExpand => canExpand;

        public bool PrewarmOnRegister => prewarmOnRegister;

        public PoolLifetimeScope LifetimeScope => lifetimeScope;

        public PoolRegistrationMode RegistrationMode => registrationMode;

        public float AutoReturnSeconds => autoReturnSeconds;

        public void ValidateOrThrow()
        {
            if (prefab == null)
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' requires a prefab.");
            }

            if (initialCapacity < 0)
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' requires initialCapacity >= 0.");
            }

            if (maxSize <= 0)
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' requires maxSize > 0.");
            }

            if (initialCapacity > maxSize)
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' requires initialCapacity <= maxSize.");
            }

            if (!canExpand && initialCapacity == 0)
            {
                throw new InvalidOperationException(
                    $"PoolDefinitionAsset '{name}' requires initialCapacity > 0 when canExpand is false.");
            }

            if (autoReturnSeconds < 0f)
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' requires autoReturnSeconds >= 0.");
            }

            if (!Enum.IsDefined(typeof(PoolLifetimeScope), lifetimeScope))
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' has an invalid lifetimeScope.");
            }

            if (!Enum.IsDefined(typeof(PoolRegistrationMode), registrationMode))
            {
                throw new InvalidOperationException($"PoolDefinitionAsset '{name}' has an invalid registrationMode.");
            }
        }
    }
}
