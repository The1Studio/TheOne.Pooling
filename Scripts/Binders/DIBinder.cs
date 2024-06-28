#if UNIT_DI
#nullable enable
namespace UniT.Pooling
{
    using UniT.DI;
    using UniT.Logging;
    using UniT.ResourceManagement;

    public static class DIBinder
    {
        public static void AddObjectPoolManager(this DependencyContainer container)
        {
            if (container.Contains<IObjectPoolManager>()) return;
            container.AddLoggerManager();
            container.AddResourceManagers();
            container.AddInterfaces<ObjectPoolManager>();
        }
    }
}
#endif