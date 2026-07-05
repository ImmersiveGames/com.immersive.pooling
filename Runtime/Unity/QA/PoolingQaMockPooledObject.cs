using Immersive.Pooling.Unity.Instances;

namespace Immersive.Pooling.Unity.QA
{
    public sealed class PoolingQaMockPooledObject : PoolableBehaviour
    {
        public int CreatedCount { get; private set; }

        public int ReturnedCount { get; private set; }

        public int DestroyedCount { get; private set; }

        protected override void HandleCreatedByPool()
        {
            CreatedCount++;
        }

        protected override void HandleReturnedToPool()
        {
            ReturnedCount++;
        }

        protected override void HandleDestroyedByPool()
        {
            DestroyedCount++;
        }
    }
}
