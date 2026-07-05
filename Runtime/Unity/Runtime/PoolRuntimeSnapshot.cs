namespace Immersive.Pooling.Unity.Runtime
{
    public readonly struct PoolRuntimeSnapshot
    {
        public PoolRuntimeSnapshot(int activeCount, int inactiveCount, int totalCount)
        {
            ActiveCount = activeCount;
            InactiveCount = inactiveCount;
            TotalCount = totalCount;
        }

        public int ActiveCount { get; }

        public int InactiveCount { get; }

        public int TotalCount { get; }
    }
}
