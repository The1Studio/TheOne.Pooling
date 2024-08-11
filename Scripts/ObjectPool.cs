#nullable enable
namespace UniT.Pooling
{
    using System;
    using System.Collections.Generic;
    using UniT.Extensions;
    using UnityEngine;

    public sealed class ObjectPool : MonoBehaviour
    {
        #region Constructor

        [SerializeField] private GameObject prefab = null!;

        private readonly Queue<GameObject>   pooledObjects  = new Queue<GameObject>();
        private readonly HashSet<GameObject> spawnedObjects = new HashSet<GameObject>();

        public static ObjectPool Construct(GameObject prefab, Transform parent)
        {
            var pool = new GameObject
            {
                name      = $"{prefab.name} pool",
                transform = { parent = parent },
            }.AddComponent<ObjectPool>();
            pool.prefab = prefab;
            return pool;
        }

        // ReSharper disable once InconsistentNaming
        public new Transform transform { get; private set; } = null!;

        private void Awake()
        {
            this.transform = base.transform;
        }

        #endregion

        #region Public

        public event Action<GameObject>? OnInstantiate;

        public void Load(int count)
        {
            while (this.pooledObjects.Count < count)
            {
                var instance = this.Instantiate();
                instance.SetActive(false);
                this.pooledObjects.Enqueue(instance);
            }
        }

        public GameObject Spawn(Vector3 position = default, Quaternion rotation = default, Transform? parent = null)
        {
            var instance = this.pooledObjects.DequeueOrDefault(this.Instantiate);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(parent);
            instance.SetActive(true);
            this.spawnedObjects.Add(instance);
            return instance;
        }

        public T Spawn<T>(Vector3 position = default, Quaternion rotation = default, Transform? parent = null)
        {
            return this.Spawn(position, rotation, parent).GetComponentOrThrow<T>();
        }

        public void Recycle(GameObject instance)
        {
            if (!this.spawnedObjects.Remove(instance)) throw new InvalidOperationException($"{instance.name} was not spawned from {this.gameObject.name}");
            instance.SetActive(false);
            instance.transform.SetParent(this.transform);
            this.pooledObjects.Enqueue(instance);
        }

        public void Recycle<T>(T component) where T : Component
        {
            this.Recycle(component.gameObject);
        }

        public void RecycleAll()
        {
            this.spawnedObjects.SafeForEach(this.Recycle);
        }

        public void Cleanup(int retainCount = 1)
        {
            while (this.pooledObjects.Count > retainCount)
            {
                Destroy(this.pooledObjects.Dequeue());
            }
        }

        #endregion

        #region Private

        private GameObject Instantiate()
        {
            var instance = Instantiate(this.prefab, this.transform);
            this.OnInstantiate?.Invoke(instance);
            return instance;
        }

        #endregion
    }
}