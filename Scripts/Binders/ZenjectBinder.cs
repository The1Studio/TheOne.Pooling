#if UNIT_ZENJECT
#nullable enable
namespace UniT.Pooling
{
    using Zenject;

    public static class ZenjectBinder
    {
        public static void BindObjectPoolManager(this DiContainer container)
        {
            container.BindInterfacesTo<ObjectPoolManager>().AsSingle();
        }
    }
}
#endif