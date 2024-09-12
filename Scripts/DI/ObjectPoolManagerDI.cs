#if UNIT_DI
#nullable enable
namespace UniT.Pooling.DI
{
    using UniT.DI;
    using UniT.Logging.DI;
    using UniT.ResourceManagement.DI;

    public static class ObjectPoolManagerDI
    {
        public static void AddObjectPoolManager(this DependencyContainer container)
        {
            if (container.Contains<IObjectPoolManager>()) return;
            container.AddLoggerManager();
            container.AddAssetsManager();
            container.AddInterfaces<ObjectPoolManager>();
        }
    }
}
#endif