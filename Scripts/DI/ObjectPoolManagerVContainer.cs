#if UNIT_VCONTAINER
#nullable enable
namespace UniT.Pooling.DI
{
    using UniT.Logging.DI;
    using UniT.ResourceManagement.DI;
    using VContainer;

    public static class ObjectPoolManagerVContainer
    {
        public static void RegisterObjectPoolManager(this IContainerBuilder builder)
        {
            if (builder.Exists(typeof(IObjectPoolManager), true)) return;
            builder.RegisterLoggerManager();
            builder.RegisterAssetsManager();
            builder.Register<ObjectPoolManager>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
#endif