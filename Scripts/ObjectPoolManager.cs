#nullable enable
namespace TheOne.Pooling
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using TheOne.Extensions;
    using TheOne.Logging;
    using TheOne.ResourceManagement;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = TheOne.Logging.ILogger;
    using Object = UnityEngine.Object;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public class ObjectPoolManager : IObjectPoolManager, IDisposable
    {
        #region Constructor

        private readonly IAssetsManager assetsManager;
        private readonly ILogger        logger;

        private readonly Transform                                      poolsContainer = new GameObject(nameof(ObjectPoolManager)).DontDestroyOnLoad().transform;
        private readonly ConcurrentDictionary<string, GameObject>       keyToPrefab    = new ConcurrentDictionary<string, GameObject>();
        private readonly ConcurrentDictionary<GameObject, ObjectPool>   prefabToPool   = new ConcurrentDictionary<GameObject, ObjectPool>();
        private readonly ConcurrentDictionary<GameObject, ObjectPool>   instanceToPool = new ConcurrentDictionary<GameObject, ObjectPool>();
        private readonly object poolLock = new object();
        private bool disposed;

        [Preserve]
        public ObjectPoolManager(IAssetsManager assetsManager, ILoggerManager loggerManager)
        {
            this.assetsManager = assetsManager;
            this.logger        = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Public

        event Action<GameObject> IObjectPoolManager.Instantiated { add => this.instantiated += value; remove => this.instantiated -= value; }
        event Action<GameObject> IObjectPoolManager.Spawned      { add => this.spawned += value;      remove => this.spawned -= value; }
        event Action<GameObject> IObjectPoolManager.Recycled     { add => this.recycled += value;     remove => this.recycled -= value; }
        event Action<GameObject> IObjectPoolManager.CleanedUp    { add => this.cleanedUp += value;    remove => this.cleanedUp -= value; }

        void IObjectPoolManager.Load(GameObject prefab, int count) => this.Load(prefab, count);

        void IObjectPoolManager.Load(string key, int count)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
            
            var prefab = this.keyToPrefab.GetOrAdd(key, k => this.assetsManager.Load<GameObject>(k));
            this.Load(prefab, count);
        }

        #if THEONE_UNITASK
        async UniTask IObjectPoolManager.LoadAsync(string key, int count, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
            
            var prefab = await this.keyToPrefab.GetOrAddAsync(key, () => this.assetsManager.LoadAsync<GameObject>(key, progress, cancellationToken));
            this.Load(prefab, count);
        }
        #else
        IEnumerator IObjectPoolManager.LoadAsync(string key, int count, Action? callback, IProgress<float>? progress)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
            
            var prefab = default(GameObject)!;
            yield return this.keyToPrefab.GetOrAddAsync(
                key,
                callback => this.assetsManager.LoadAsync(key, callback, progress),
                result => prefab = result
            );
            this.Load(prefab, count);
            callback?.Invoke();
        }
        #endif

        GameObject IObjectPoolManager.Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform? parent, bool spawnInWorldSpace) => this.Spawn(prefab, position, rotation, parent, spawnInWorldSpace);

        GameObject IObjectPoolManager.Spawn(string key, Vector3 position, Quaternion rotation, Transform? parent, bool spawnInWorldSpace)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            var prefab = this.keyToPrefab.GetOrAdd(key, k => this.assetsManager.Load<GameObject>(k));
            return this.Spawn(prefab, position, rotation, parent, spawnInWorldSpace);
        }

        void IObjectPoolManager.Recycle(GameObject instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            if (!this.instanceToPool.TryRemove(instance, out var pool))
                throw new InvalidOperationException($"{instance.name} was not spawned from {nameof(ObjectPoolManager)}");
            
            // Check if object was destroyed
            if (instance == null || instance.Equals(null))
            {
                this.logger.Warning($"Attempted to recycle destroyed object");
                return;
            }
            
            pool.Recycle(instance);
            this.logger.Debug($"Recycled {instance.name}");
        }

        void IObjectPoolManager.RecycleAll(GameObject prefab) => this.RecycleAll(prefab);

        void IObjectPoolManager.RecycleAll(string key)
        {
            if (!this.TryGetPrefab(key, out var prefab)) return;
            this.RecycleAll(prefab);
        }

        void IObjectPoolManager.Cleanup(GameObject prefab, int retainCount) => this.Cleanup(prefab, retainCount);

        void IObjectPoolManager.Cleanup(string key, int retainCount)
        {
            if (!this.TryGetPrefab(key, out var prefab)) return;
            this.Cleanup(prefab, retainCount);
        }

        void IObjectPoolManager.Unload(GameObject prefab) => this.Unload(prefab);

        void IObjectPoolManager.Unload(string key)
        {
            if (!this.TryGetPrefab(key, out var prefab)) return;
            this.Unload(prefab);
            this.assetsManager.Unload(key);
            this.keyToPrefab.TryRemove(key, out _);
        }

        #endregion

        #region Private

        private Action<GameObject>? instantiated;
        private Action<GameObject>? spawned;
        private Action<GameObject>? recycled;
        private Action<GameObject>? cleanedUp;

        private void Load(GameObject prefab, int count)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
            
            var pool = this.prefabToPool.GetOrAdd(prefab, p =>
            {
                lock (this.poolLock)
                {
                    if (this.prefabToPool.TryGetValue(p, out var existing))
                        return existing;
                    
                    var newPool = ObjectPool.Construct(p, this.poolsContainer);
                    newPool.Instantiated += this.OnInstantiated;
                    newPool.Spawned      += this.OnSpawned;
                    newPool.Recycled     += this.OnRecycled;
                    newPool.CleanedUp    += this.OnCleanedUp;
                    this.logger.Debug($"Instantiated {newPool.name}");
                    return newPool;
                }
            });
            pool.Load(count);
        }

        private GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform? parent, bool spawnInWorldSpace)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            
            if (this.disposed)
                throw new ObjectDisposedException(nameof(ObjectPoolManager));
            
            if (!this.prefabToPool.TryGetValue(prefab, out var pool))
            {
                this.logger.Warning($"Auto loading {prefab.name} pool. Consider preloading it with `Load` or `LoadAsync` for better performance.");
                this.Load(prefab, 1);
                pool = this.prefabToPool[prefab];
            }
            
            var instance = pool.Spawn(position, rotation, parent, spawnInWorldSpace);
            this.instanceToPool.TryAdd(instance, pool);
            this.logger.Debug($"Spawned {instance.name}");
            return instance;
        }

        private void RecycleAll(GameObject prefab)
        {
            if (prefab == null) return;
            if (!this.TryGetPool(prefab, out var pool)) return;
            pool.RecycleAll();
            
            // Remove all instances from this pool from tracking
            var instancesToRemove = this.instanceToPool.Where(kvp => kvp.Value == pool).Select(kvp => kvp.Key).ToList();
            foreach (var inst in instancesToRemove)
            {
                this.instanceToPool.TryRemove(inst, out _);
            }
            
            this.logger.Debug($"Recycled all {pool.name}");
        }

        private void Cleanup(GameObject prefab, int retainCount)
        {
            if (prefab == null) return;
            if (!this.TryGetPool(prefab, out var pool)) return;
            pool.Cleanup(retainCount);
            this.logger.Debug($"Cleaned up {pool.name}");
        }

        private void Unload(GameObject prefab)
        {
            if (!this.TryGetPool(prefab, out var pool)) return;
            
            // Unsubscribe from events before destroying
            pool.Instantiated -= this.OnInstantiated;
            pool.Spawned      -= this.OnSpawned;
            pool.Recycled     -= this.OnRecycled;
            pool.CleanedUp    -= this.OnCleanedUp;
            
            pool.RecycleAll();
            pool.Cleanup(0);
            
            if (pool.gameObject != null)
                Object.Destroy(pool.gameObject);
            
            this.prefabToPool.TryRemove(prefab, out _);
            this.logger.Debug($"Unloaded {prefab.name}");
        }

        private bool TryGetPrefab(string key, [MaybeNullWhen(false)] out GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                prefab = null;
                return false;
            }
            return this.keyToPrefab.TryGetValue(key, out prefab);
        }

        private bool TryGetPool(GameObject prefab, [MaybeNullWhen(false)] out ObjectPool pool) => this.prefabToPool.TryGetValue(prefab, out pool);

        private void OnInstantiated(GameObject instance)
        {
            this.instantiated?.Invoke(instance);
            this.logger.Debug($"Instantiated {instance.name}");
        }

        private void OnSpawned(GameObject instance)
        {
            this.spawned?.Invoke(instance);
        }

        private void OnRecycled(GameObject instance)
        {
            this.recycled?.Invoke(instance);
        }

        private void OnCleanedUp(GameObject instance)
        {
            this.cleanedUp?.Invoke(instance);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                // Unload all pools
                foreach (var kvp in this.prefabToPool.ToArray())
                {
                    this.Unload(kvp.Key);
                }

                // Destroy the container
                if (this.poolsContainer != null)
                    Object.Destroy(this.poolsContainer.gameObject);

                // Clear collections
                this.keyToPrefab.Clear();
                this.prefabToPool.Clear();
                this.instanceToPool.Clear();
            }

            this.disposed = true;
            this.logger.Debug("Disposed");
        }

        ~ObjectPoolManager()
        {
            Dispose(false);
        }

        #endregion
    }
}