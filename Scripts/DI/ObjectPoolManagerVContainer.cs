#if THEONE_VCONTAINER
#nullable enable
namespace TheOne.Pooling.DI
{
    using TheOne.Logging.DI;
    using TheOne.ResourceManagement.DI;
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