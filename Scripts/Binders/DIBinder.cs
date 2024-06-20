#if UNIT_DI
#nullable enable
namespace UniT.Pooling
{
    using UniT.DI;

    public static class DIBinder
    {
        public static void AddObjectPoolManager(this DependencyContainer container)
        {
            container.AddInterfaces<ObjectPoolManager>();
        }
    }
}
#endif