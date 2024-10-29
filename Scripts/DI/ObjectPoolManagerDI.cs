#if THEONE_DI
#nullable enable
namespace TheOne.Pooling.DI
{
    using TheOne.DI;
    using TheOne.Logging.DI;
    using TheOne.ResourceManagement.DI;

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