#if UNIT_ZENJECT
#nullable enable
namespace UniT.Pooling.DI
{
    using UniT.Logging.DI;
    using UniT.ResourceManagement.DI;
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