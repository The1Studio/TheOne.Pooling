# TheOne.Pooling

Object Pool Manager for Unity

## Installation

### Option 1: Unity Scoped Registry (Recommended)

Add the following scoped registry to your project's `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "TheOne Studio",
      "url": "https://upm.the1studio.org/",
      "scopes": [
        "com.theone"
      ]
    }
  ],
  "dependencies": {
    "com.theone.pooling": "1.1.0"
  }
}
```

### Option 2: Git URL

Add to Unity Package Manager:
```
https://github.com/The1Studio/TheOne.Pooling.git
```

## Features

- High-performance object pooling system for GameObjects and Components
- Automatic pool management with lazy loading
- Resource integration via TheOne.ResourceManagement
- Support for both prefab references and string keys (Addressables/Resources)
- Event-driven lifecycle notifications (Instantiated, Spawned, Recycled, Cleaned Up)
- Flexible spawning with position, rotation, and parenting options
- Memory management with cleanup and unloading capabilities
- Type-safe component spawning and recycling
- Async loading support with progress reporting
- Integration with dependency injection frameworks
- Comprehensive logging for debugging and monitoring

## Dependencies

- TheOne.Extensions
- TheOne.Logging
- TheOne.ResourceManagement

## Usage

### Basic Object Pooling

```csharp
using TheOne.Pooling;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject enemyPrefab;
    
    private IObjectPoolManager poolManager;
    
    private void Start()
    {
        // Get pool manager from DI container
        poolManager = GetComponent<ObjectPoolManager>();
        
        // Pre-load pools for better performance
        poolManager.Load(bulletPrefab, 50);
        poolManager.Load(enemyPrefab, 10);
    }
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Spawn bullet at mouse position
            var bullet = poolManager.Spawn(bulletPrefab, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            
            // Automatically recycle after 3 seconds
            StartCoroutine(RecycleAfterDelay(bullet, 3f));
        }
    }
    
    private IEnumerator RecycleAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        poolManager.Recycle(obj);
    }
}
```

### String Key-Based Pooling (Addressables/Resources)

```csharp
public class SpawnManager : MonoBehaviour
{
    private IObjectPoolManager poolManager;
    
    #if THEONE_UNITASK
    private async UniTaskVoid Start()
    {
        // Load pool using string key (works with Addressables or Resources)
        await poolManager.LoadAsync("Enemies/Goblin", 20);
        await poolManager.LoadAsync("Effects/Explosion", 10);
        
        // Spawn using keys
        var enemy = poolManager.Spawn("Enemies/Goblin", transform.position);
        var effect = poolManager.Spawn("Effects/Explosion", enemy.transform.position);
    }
    #else
    private void Start()
    {
        StartCoroutine(InitializeAsync());
    }
    
    private IEnumerator InitializeAsync()
    {
        // Load pool using string key (works with Addressables or Resources)
        yield return poolManager.LoadAsync("Enemies/Goblin", 20);
        yield return poolManager.LoadAsync("Effects/Explosion", 10);
        
        // Spawn using keys
        var enemy = poolManager.Spawn("Enemies/Goblin", transform.position);
        var effect = poolManager.Spawn("Effects/Explosion", enemy.transform.position);
    }
    #endif
}
```

### Type-Safe Component Pooling

```csharp
public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    private IObjectPoolManager poolManager;
    
    private void Start()
    {
        // Load component-based pool
        poolManager.Load(bulletPrefab, 100);
    }
    
    public void FireBullet(Vector3 direction)
    {
        // Spawn component directly - no need for GetComponent
        var bullet = poolManager.Spawn(bulletPrefab, transform.position, Quaternion.LookRotation(direction));
        bullet.Fire(direction);
        
        // Recycle the component
        StartCoroutine(RecycleBullet(bullet, 2f));
    }
    
    private IEnumerator RecycleBullet(Bullet bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        poolManager.Recycle(bullet); // Can recycle by component reference
    }
}
```

### Advanced Pool Management

```csharp
public class PoolController : MonoBehaviour
{
    private IObjectPoolManager poolManager;
    
    private void Start()
    {
        // Subscribe to pool events for monitoring
        poolManager.Instantiated += OnObjectInstantiated;
        poolManager.Spawned += OnObjectSpawned;
        poolManager.Recycled += OnObjectRecycled;
        poolManager.CleanedUp += OnObjectCleanedUp;
    }
    
    public void ManagePools()
    {
        // Recycle all instances of a specific prefab
        poolManager.RecycleAll("Enemies/Orc");
        
        // Clean up excess pooled objects (keep only 5)
        poolManager.Cleanup("Effects/Explosion", retainCount: 5);
        
        // Completely unload a pool
        poolManager.Unload("Temporary/SpecialEffect");
    }
    
    private void OnObjectInstantiated(GameObject obj) => Debug.Log($"Instantiated: {obj.name}");
    private void OnObjectSpawned(GameObject obj) => Debug.Log($"Spawned: {obj.name}");
    private void OnObjectRecycled(GameObject obj) => Debug.Log($"Recycled: {obj.name}");
    private void OnObjectCleanedUp(GameObject obj) => Debug.Log($"Cleaned up: {obj.name}");
}
```

### Implicit Key Pooling (Type-Based)

```csharp
public class TypeBasedPooling : MonoBehaviour
{
    private IObjectPoolManager poolManager;
    
    #if THEONE_UNITASK
    private async UniTaskVoid Start()
    {
        // Load using type name as key
        await poolManager.LoadAsync<Enemy>(15);
        await poolManager.LoadAsync<PowerUp>(5);
        
        // Spawn using implicit type keys
        var enemy = poolManager.Spawn<Enemy>(transform.position);
        var powerUp = poolManager.Spawn<PowerUp>(enemy.transform.position + Vector3.up);
        
        // Manage by type
        poolManager.RecycleAll<Enemy>();
        poolManager.Cleanup<PowerUp>(retainCount: 2);
        poolManager.Unload<Enemy>();
    }
    #else
    private void Start()
    {
        StartCoroutine(InitializeAsync());
    }
    
    private IEnumerator InitializeAsync()
    {
        // Load using type name as key
        yield return poolManager.LoadAsync<Enemy>(15);
        yield return poolManager.LoadAsync<PowerUp>(5);
        
        // Spawn using implicit type keys
        var enemy = poolManager.Spawn<Enemy>(transform.position);
        var powerUp = poolManager.Spawn<PowerUp>(enemy.transform.position + Vector3.up);
        
        // Manage by type
        poolManager.RecycleAll<Enemy>();
        poolManager.Cleanup<PowerUp>(retainCount: 2);
        poolManager.Unload<Enemy>();
    }
    #endif
}
```

### Async Loading with Progress

```csharp
public class AsyncPoolLoader : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    private IObjectPoolManager poolManager;
    
    #if THEONE_UNITASK
    private async UniTaskVoid Start()
    {
        var progress = new Progress<float>(value => 
        {
            progressBar.value = value;
            Debug.Log($"Loading progress: {value:P}");
        });
        
        // Load multiple pools with progress reporting
        await poolManager.LoadAsync("Characters/Player", 1, progress);
        await poolManager.LoadAsync("Weapons/Sword", 10, progress);
        await poolManager.LoadAsync("Items/HealthPotion", 20, progress);
        
        Debug.Log("All pools loaded!");
    }
    #else
    private void Start()
    {
        StartCoroutine(LoadPoolsAsync());
    }
    
    private IEnumerator LoadPoolsAsync()
    {
        var progress = new Progress<float>(value => 
        {
            progressBar.value = value;
            Debug.Log($"Loading progress: {value:P}");
        });
        
        // Load multiple pools with progress reporting
        yield return poolManager.LoadAsync("Characters/Player", 1, 
            callback: () => Debug.Log("Player pool loaded"), progress);
        yield return poolManager.LoadAsync("Weapons/Sword", 10, 
            callback: () => Debug.Log("Sword pool loaded"), progress);
        yield return poolManager.LoadAsync("Items/HealthPotion", 20, 
            callback: () => Debug.Log("Health potion pool loaded"), progress);
        
        Debug.Log("All pools loaded!");
    }
    #endif
}
```

## Architecture

### Folder Structure

```
TheOne.Pooling/
├── Scripts/
│   ├── IObjectPoolManager.cs         # Main pool manager interface
│   ├── ObjectPoolManager.cs          # Pool manager implementation
│   ├── ObjectPool.cs                 # Individual pool for specific prefabs
│   └── DI/                           # Dependency injection extensions
│       ├── ObjectPoolManagerDI.cs
│       ├── ObjectPoolManagerVContainer.cs
│       └── ObjectPoolManagerZenject.cs
```

### Core Classes

#### `IObjectPoolManager`
Central interface for all pooling operations:

**Loading Operations:**
- `Load(GameObject prefab, int count = 1)` - Pre-load pool with instances
- `Load(string key, int count = 1)` - Pre-load pool using string key
- `Load<T>(int count = 1)` - Type-based implicit key loading

**Async Loading:**
```csharp
#if THEONE_UNITASK
UniTask LoadAsync(string key, int count = 1, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
UniTask LoadAsync<T>(int count = 1, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
#else
IEnumerator LoadAsync(string key, int count = 1, Action? callback = null, IProgress<float>? progress = null);
IEnumerator LoadAsync<T>(int count = 1, Action? callback = null, IProgress<float>? progress = null);
#endif
```

**Spawning Operations:**
- `Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform? parent = null, bool spawnInWorldSpace = true)` - Spawn from pool
- `Spawn(string key, Vector3 position = default, Quaternion rotation = default, Transform? parent = null, bool spawnInWorldSpace = true)` - Spawn using string key
- `T Spawn<T>(T prefab, Vector3 position = default, Quaternion rotation = default, Transform? parent = null, bool spawnInWorldSpace = true)` - Component-based spawning
- `T Spawn<T>(string key, Vector3 position = default, Quaternion rotation = default, Transform? parent = null, bool spawnInWorldSpace = true)` - Type-safe spawning with string key
- `T Spawn<T>(Vector3 position = default, Quaternion rotation = default, Transform? parent = null)` - Implicit key spawning

**Lifecycle Management:**
- `Recycle(GameObject instance)` - Return object to pool
- `Recycle(Component instance)` - Return component's GameObject to pool
- `RecycleAll(GameObject prefab)` / `RecycleAll(string key)` / `RecycleAll<T>()` - Recycle all spawned instances
- `Cleanup(GameObject prefab, int retainCount = 1)` / `Cleanup(string key, int retainCount = 1)` / `Cleanup<T>(int retainCount = 1)` - Remove excess pooled objects
- `Unload(GameObject prefab)` / `Unload(string key)` / `Unload<T>()` - Completely destroy pool

**Events:**
- `Instantiated` - Fired when new instances are created
- `Spawned` - Fired when objects are taken from pool
- `Recycled` - Fired when objects are returned to pool
- `CleanedUp` - Fired when excess objects are destroyed

#### `ObjectPoolManager`
Main implementation providing:
- Resource integration for loading prefabs by key
- Dictionary-based pool management for efficient lookup
- Event forwarding and lifecycle tracking
- Auto-loading when pools don't exist (with performance warnings)
- Logging integration for debugging and monitoring

#### `ObjectPool`
Individual pool implementation for specific prefabs:
- Queue-based pooled object management
- HashSet tracking of spawned instances
- Transform management and parenting
- Component-aware spawning and recycling
- Memory cleanup and instance destruction

### Resource Integration

The pooling system integrates seamlessly with TheOne.ResourceManagement:

```csharp
// Works with Resources folder
poolManager.Load("Prefabs/Enemy", 10);

// Works with Addressables
poolManager.LoadAsync("enemies.goblin", 5);

// Automatic resource loading and caching
var enemy = poolManager.Spawn("enemies.orc", transform.position);
```

### Design Patterns

- **Object Pool Pattern**: Classic pooling for memory management
- **Factory Pattern**: ObjectPoolManager creates and manages pools
- **Observer Pattern**: Event-driven lifecycle notifications
- **Strategy Pattern**: Different resource loading strategies
- **Lazy Initialization**: Pools created on-demand when needed
- **Resource Management**: Integration with asset loading systems

### Code Style & Conventions

- **Namespace**: All code under `TheOne.Pooling` namespace
- **Null Safety**: Uses `#nullable enable` directive
- **Interfaces**: Prefixed with `I` (e.g., `IObjectPoolManager`)
- **Generic Methods**: Type-safe operations where applicable
- **Extension Methods**: Component and implicit key overloads
- **Event Handling**: Null-safe event invocation
- **Resource Keys**: String-based keys for asset references

### Performance Optimizations

```csharp
public class OptimizedPooling
{
    private IObjectPoolManager poolManager;
    
    public void PerformanceExample()
    {
        // Good - Pre-load pools during loading screens
        poolManager.Load("Bullets", 100);
        poolManager.Load("Enemies", 20);
        
        // Efficient - Direct prefab reference (no dictionary lookup)
        var bullet = poolManager.Spawn(bulletPrefab, position);
        
        // Batch operations - Recycle all instances at once
        poolManager.RecycleAll("Enemies");
        
        // Memory management - Clean up excess objects
        poolManager.Cleanup("Bullets", retainCount: 10);
    }
    
    public void AvoidThesePatterns()
    {
        // Bad - Creates pool on first spawn (causes frame drop)
        var enemy = poolManager.Spawn("NewEnemy", position); // Warning logged
        
        // Inefficient - String lookup every time
        for (int i = 0; i < 100; i++)
        {
            poolManager.Spawn("Bullet", positions[i]); // Use prefab reference instead
        }
    }
}
```

### Integration with DI Frameworks

#### VContainer
```csharp
using TheOne.Pooling.DI;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Register pool manager
        builder.RegisterObjectPoolManager();
        
        // Services can now inject IObjectPoolManager
        builder.Register<SpawnManager>(Lifetime.Singleton);
    }
}
```

#### Zenject
```csharp
using TheOne.Pooling.DI;

public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Register pool manager
        Container.BindObjectPoolManager();
        
        // Services automatically get pooling support
        Container.BindInterfacesTo<EnemySpawner>().AsSingle();
    }
}
```

#### Custom DI
```csharp
using TheOne.Pooling.DI;

// Register with your DI container
container.RegisterObjectPoolManager();

// Or manual registration
container.Register<IObjectPoolManager, ObjectPoolManager>();
```

### Advanced Usage Patterns

#### Pool Warming Strategy
```csharp
public class PoolWarmingService : MonoBehaviour
{
    private IObjectPoolManager poolManager;
    
    #if THEONE_UNITASK
    private async UniTaskVoid Start()
    {
        // Warm pools during loading screen
        await WarmCriticalPools();
        
        // Warm secondary pools in background
        _ = WarmSecondaryPools().Forget();
    }
    #else
    private void Start()
    {
        StartCoroutine(InitializePoolWarming());
    }
    
    private IEnumerator InitializePoolWarming()
    {
        // Warm pools during loading screen
        yield return WarmCriticalPools();
        
        // Warm secondary pools in background
        StartCoroutine(WarmSecondaryPools());
    }
    #endif
    
    private async Task WarmCriticalPools()
    {
        var progress = new Progress<float>();
        await poolManager.LoadAsync("Player/Bullet", 200, progress);
        await poolManager.LoadAsync("Enemies/Basic", 50, progress);
    }
    
    private IEnumerator WarmSecondaryPools()
    {
        yield return poolManager.LoadAsync("Effects/Explosion", 20);
        yield return poolManager.LoadAsync("Items/Coin", 100);
    }
}
```

#### Dynamic Pool Management
```csharp
public class DynamicPoolManager : MonoBehaviour
{
    private IObjectPoolManager poolManager;
    private Dictionary<string, int> poolUsageStats = new();
    
    private void Update()
    {
        // Monitor pool usage and adjust sizes
        if (Time.frameCount % 300 == 0) // Every 5 seconds
        {
            OptimizePools();
        }
    }
    
    private void OptimizePools()
    {
        foreach (var (key, usage) in poolUsageStats)
        {
            if (usage > 50)
            {
                // High usage - expand pool
                poolManager.Load(key, 20);
            }
            else if (usage < 5)
            {
                // Low usage - shrink pool
                poolManager.Cleanup(key, retainCount: 5);
            }
        }
        
        poolUsageStats.Clear();
    }
}
```

## Performance Considerations

- **Pre-loading**: Load pools during loading screens to prevent runtime hitches
- **Batch Operations**: Use `RecycleAll()` instead of individual recycle calls
- **Memory Management**: Regular cleanup prevents memory bloat
- **Reference Caching**: Cache prefab references to avoid string lookups
- **Pool Sizing**: Size pools based on expected peak usage
- **Hierarchy Management**: Pooled objects are parented to pool containers
- **Event Subscriptions**: Minimal overhead for lifecycle event handling

## Best Practices

1. **Pool Sizing**: Pre-load pools with expected maximum concurrent instances
2. **Resource Management**: Use string keys for dynamic content, prefab references for static
3. **Lifecycle Events**: Subscribe to events for debugging and metrics collection
4. **Memory Cleanup**: Regularly clean up excess pooled objects
5. **Type Safety**: Use component-based spawning for type safety
6. **Error Handling**: Always check if objects were spawned from pools before recycling
7. **Performance Monitoring**: Use logging to identify pool usage patterns
8. **Resource Loading**: Combine with TheOne.ResourceManagement for efficient asset handling
9. **Testing**: Mock IObjectPoolManager for unit tests
10. **Scene Management**: Unload pools when switching scenes to free memory