namespace Immersive.Pooling.Contracts
{
    public interface IPoolLifecycle : IPoolable
    {
        void OnCreatedByPool();

        void OnDestroyedByPool();
    }
}
