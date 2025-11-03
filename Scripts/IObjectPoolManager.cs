#nullable enable
namespace UniT.Pooling
{
    using System;
    using UniT.Extensions;
    using UnityEngine;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public interface IObjectPoolManager
    {
        public event Action<GameObject> Instantiated;

        public event Action<GameObject> Spawned;

        public event Action<GameObject> Recycled;

        public event Action<GameObject> CleanedUp;

        public void Load(GameObject prefab, int count = 1);

        public void Load(string key, int count = 1);

        public GameObject Spawn(GameObject prefab, Vector3? position = null, Quaternion? rotation = null, Transform? parent = null, bool spawnInWorldSpace = true);

        public GameObject Spawn(string key, Vector3? position = null, Quaternion? rotation = null, Transform? parent = null, bool spawnInWorldSpace = true);

        public void Recycle(GameObject instance);

        public void RecycleAll(GameObject prefab);

        public void RecycleAll(string key);

        public void Cleanup(GameObject prefab, int retainCount = 1);

        public void Cleanup(string key, int retainCount = 1);

        public void Unload(GameObject prefab);

        public void Unload(string key);

        #region Component

        public void Load(Component prefab, int count = 1) => this.Load(prefab.gameObject, count);

        public T Spawn<T>(T prefab, Vector3 position = default, Quaternion rotation = default, Transform? parent = null, bool spawnInWorldSpace = true) where T : Component => this.Spawn(prefab.gameObject, position, rotation, parent, spawnInWorldSpace).GetComponent<T>();

        public T Spawn<T>(string key, Vector3 position = default, Quaternion rotation = default, Transform? parent = null, bool spawnInWorldSpace = true) => this.Spawn(key, position, rotation, parent, spawnInWorldSpace).GetComponentOrThrow<T>();

        public void Recycle(Component instance) => this.Recycle(instance.gameObject);

        public void RecycleAll(Component prefab) => this.RecycleAll(prefab.gameObject);

        public void Cleanup(Component prefab, int retainCount = 1) => this.Cleanup(prefab.gameObject, retainCount);

        public void Unload(Component prefab) => this.Unload(prefab.gameObject);

        #endregion

        #region Implicit Key

        public void Load<T>(int count = 1) => this.Load(typeof(T).GetKey(), count);

        public T Spawn<T>(Vector3 position = default, Quaternion rotation = default, Transform? parent = null) => this.Spawn<T>(typeof(T).GetKey(), position, rotation, parent);

        public void RecycleAll<T>() => this.RecycleAll(typeof(T).GetKey());

        public void Cleanup<T>(int retainCount = 1) => this.Cleanup(typeof(T).GetKey(), retainCount);

        public void Unload<T>() => this.Unload(typeof(T).GetKey());

        #endregion

        #region Async

        #if UNIT_UNITASK
        public UniTask LoadAsync(string key, int count = 1, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask LoadAsync<T>(int count = 1, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAsync(typeof(T).GetKey(), count, progress, cancellationToken);
        #else
        public IEnumerator LoadAsync(string key, int count = 1, Action? callback = null, IProgress<float>? progress = null);

        public IEnumerator LoadAsync<T>(int count = 1, Action? callback = null, IProgress<float>? progress = null) => this.LoadAsync(typeof(T).GetKey(), count, callback, progress);
        #endif

        #endregion
    }
}