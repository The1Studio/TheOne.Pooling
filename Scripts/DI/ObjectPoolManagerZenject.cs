#if THEONE_ZENJECT
#nullable enable
namespace TheOne.Pooling.DI
{
    using TheOne.Logging.DI;
    using TheOne.ResourceManagement.DI;
    using Zenject;

    public static class ObjectPoolManagerZenject
    {
        public static void BindObjectPoolManager(this DiContainer container)
        {
            if (container.HasBinding<IObjectPoolManager>()) return;
            container.BindLoggerManager();
            container.BindAssetsManager();
            container.BindInterfacesTo<ObjectPoolManager>().AsSingle();
        }
    }
}
#endif