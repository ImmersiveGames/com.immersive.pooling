using Immersive.Pooling.Contracts;
using UnityEngine;

namespace Immersive.Pooling.Unity.Instances
{
    public abstract class PoolableBehaviour : MonoBehaviour, IPoolLifecycle
    {
        public bool IsTakenFromPool { get; private set; }

        public int RentCount { get; private set; }

        public void OnCreatedByPool()
        {
            IsTakenFromPool = false;
            HandleCreatedByPool();
        }

        public void OnTakenFromPool()
        {
            IsTakenFromPool = true;
            RentCount++;
            HandleTakenFromPool();
        }

        public void OnReturnedToPool()
        {
            IsTakenFromPool = false;
            HandleReturnedToPool();
        }

        public void OnDestroyedByPool()
        {
            IsTakenFromPool = false;
            HandleDestroyedByPool();
        }

        protected virtual void HandleCreatedByPool()
        {
        }

        protected virtual void HandleTakenFromPool()
        {
        }

        protected virtual void HandleReturnedToPool()
        {
        }

        protected virtual void HandleDestroyedByPool()
        {
        }
    }
}
