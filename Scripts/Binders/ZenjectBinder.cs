#if UNIT_ZENJECT
#nullable enable
namespace UniT.Pooling
{
    using UniT.Logging;
    using UniT.ResourceManagement;
    using Zenject;

    public static class ZenjectBinder
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