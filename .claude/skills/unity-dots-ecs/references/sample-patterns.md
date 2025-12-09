# Unity DOTS Sample Patterns

이 문서는 Unity의 EntityComponentSystemSamples에서 가져온 실전 패턴들입니다.

## Spawning Pattern

프리팹 인스턴스화 및 스폰 시스템:

```csharp
[BurstCompile]
public partial struct SpawnSystem : ISystem
{
    float timer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PrefabLoadResult>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0) return;

        var config = SystemAPI.GetSingleton<Config>();
        timer = config.SpawnInterval;

        var configEntity = SystemAPI.GetSingletonEntity<Config>();
        if (!SystemAPI.HasComponent<PrefabLoadResult>(configEntity))
            return;

        var prefabResult = SystemAPI.GetComponent<PrefabLoadResult>(configEntity);
        var instance = state.EntityManager.Instantiate(prefabResult.PrefabRoot);
        
        var randomPos = new float3(
            Random.CreateFromIndex((uint)instance.Index).NextFloat(-10, 10),
            0,
            Random.CreateFromIndex((uint)instance.Index + 1).NextFloat(-10, 10)
        );
        
        state.EntityManager.SetComponentData(instance, LocalTransform.FromPosition(randomPos));
    }
}
```

## Shooting/Projectile Pattern

```csharp
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[BurstCompile]
public partial struct TurretShootingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Execute.TurretShooting>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (turret, localToWorld) in 
                 SystemAPI.Query<TurretAspect, RefRO<LocalToWorld>>()
                     .WithAll<Shooting>())
        {
            Entity instance = state.EntityManager.Instantiate(turret.CannonBallPrefab);
            
            state.EntityManager.SetComponentData(instance, new LocalTransform
            {
                Position = localToWorld.ValueRO.Position,
                Rotation = localToWorld.ValueRO.Rotation,
                Scale = 1f
            });
            
            state.EntityManager.SetComponentData(instance, new Velocity
            {
                Value = localToWorld.ValueRO.Forward * turret.ShootSpeed
            });
        }
        
        ecb.Playback(state.EntityManager);
    }
}
```

## Boids/Flocking Pattern

복잡한 집단 행동 시스템:

```csharp
[BurstCompile]
public partial struct BoidSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<BoidConfig>();
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        var job = new BoidJob
        {
            DeltaTime = deltaTime,
            SeparationWeight = config.SeparationWeight,
            AlignmentWeight = config.AlignmentWeight,
            CohesionWeight = config.CohesionWeight,
            PerceptionRadius = config.PerceptionRadius
        };
        
        job.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct BoidJob : IJobEntity
{
    public float DeltaTime;
    public float SeparationWeight;
    public float AlignmentWeight;
    public float CohesionWeight;
    public float PerceptionRadius;
    
    void Execute(ref LocalTransform transform, ref Velocity velocity, in Boid boid)
    {
        // Flocking behavior logic
        float3 separation = float3.zero;
        float3 alignment = float3.zero;
        float3 cohesion = float3.zero;
        int neighbors = 0;
        
        // Calculate vectors based on nearby boids
        // (실제 구현에서는 spatial queries 사용)
        
        // Apply forces
        velocity.Value += (separation * SeparationWeight +
                          alignment * AlignmentWeight +
                          cohesion * CohesionWeight) * DeltaTime;
        
        // Clamp velocity
        velocity.Value = math.normalizesafe(velocity.Value) * boid.Speed;
        
        // Update position and rotation
        transform.Position += velocity.Value * DeltaTime;
        transform.Rotation = quaternion.LookRotationSafe(velocity.Value, math.up());
    }
}
```

## Prefab Reference Pattern

Scene에서 프리팹 로드 및 참조:

```csharp
// Authoring Component
public class ConfigAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnInterval = 1f;
}

// Baker
public class ConfigBaker : Baker<ConfigAuthoring>
{
    public override void Bake(ConfigAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        
        AddComponent(entity, new Config
        {
            SpawnInterval = authoring.SpawnInterval
        });
        
        // Prefab reference는 별도 처리 필요
        AddComponent(entity, new PrefabReference
        {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
        });
    }
}

// Runtime Component
public struct Config : IComponentData
{
    public float SpawnInterval;
}

public struct PrefabReference : IComponentData
{
    public Entity Prefab;
}
```

## Physics Interaction Pattern

```csharp
[BurstCompile]
public partial struct PhysicsCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        
        var job = new CollisionJob
        {
            // Setup collision data
        };
        
        state.Dependency = job.Schedule(simulation, state.Dependency);
    }
}

[BurstCompile]
struct CollisionJob : ICollisionEventsJob
{
    public void Execute(CollisionEvent collisionEvent)
    {
        // Handle collision
        var entityA = collisionEvent.EntityA;
        var entityB = collisionEvent.EntityB;
        
        // Apply damage, effects, etc.
    }
}
```

## Dynamic Buffer Pattern

가변 길이 배열이 필요할 때:

```csharp
// Buffer element
public struct PathPoint : IBufferElementData
{
    public float3 Position;
}

// Authoring
public class PathAuthoring : MonoBehaviour
{
    public List<Vector3> Points;
}

public class PathBaker : Baker<PathAuthoring>
{
    public override void Bake(PathAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        
        var buffer = AddBuffer<PathPoint>(entity);
        foreach (var point in authoring.Points)
        {
            buffer.Add(new PathPoint { Position = point });
        }
    }
}

// Usage in system
public void OnUpdate(ref SystemState state)
{
    foreach (var (transform, entity) in 
             SystemAPI.Query<RefRW<LocalTransform>>()
                 .WithEntityAccess())
    {
        if (state.EntityManager.HasBuffer<PathPoint>(entity))
        {
            var buffer = state.EntityManager.GetBuffer<PathPoint>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                // Use buffer[i].Position
            }
        }
    }
}
```

## Netcode Prediction Pattern

```csharp
// Predicted component
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct CharacterController : IComponentData
{
    public float Speed;
    public float JumpForce;
}

// Input component
public struct PlayerInput : IInputComponentData
{
    public float2 Move;
    public InputEvent Jump;
}

// Prediction system (runs on both client and server)
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct CharacterControllerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, controller, input) in
                 SystemAPI.Query<RefRW<LocalTransform>, 
                               RefRO<CharacterController>,
                               RefRO<PlayerInput>>())
        {
            var movement = new float3(input.ValueRO.Move.x, 0, input.ValueRO.Move.y);
            movement = math.normalizesafe(movement) * controller.ValueRO.Speed;
            
            transform.ValueRW.Position += movement * SystemAPI.Time.DeltaTime;
            
            if (input.ValueRO.Jump.IsSet)
            {
                // Apply jump
            }
        }
    }
}
```

## Enable/Disable Components Pattern

동적으로 컴포넌트 활성화/비활성화:

```csharp
public struct IsActive : IComponentData, IEnableableComponent { }

public void OnUpdate(ref SystemState state)
{
    foreach (var (transform, entity) in 
             SystemAPI.Query<RefRO<LocalTransform>>()
                 .WithEntityAccess())
    {
        // Disable component
        state.EntityManager.SetComponentEnabled<IsActive>(entity, false);
        
        // Enable component
        state.EntityManager.SetComponentEnabled<IsActive>(entity, true);
    }
}

// Query only enabled
foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>()
                              .WithAll<IsActive>())
{
    // Only processes entities where IsActive is enabled
}
```

## System State Component Pattern

시스템이 entity를 추적하기 위한 마커:

```csharp
public struct Initialized : ISystemStateComponentData { }

public void OnUpdate(ref SystemState state)
{
    // Newly created entities (has Target, no Initialized)
    foreach (var (target, entity) in 
             SystemAPI.Query<RefRO<Target>>()
                 .WithNone<Initialized>()
                 .WithEntityAccess())
    {
        // Initialize
        state.EntityManager.AddComponent<Initialized>(entity);
    }
    
    // Destroyed entities (no Target, has Initialized)
    foreach (var entity in 
             SystemAPI.Query<Entity>()
                 .WithAll<Initialized>()
                 .WithNone<Target>())
    {
        // Cleanup
        state.EntityManager.RemoveComponent<Initialized>(entity);
    }
}
```

## Netcode Ghost Spawning

네트워크 객체 생성:

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct ServerSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (spawner, entity) in 
                 SystemAPI.Query<RefRW<PlayerSpawner>>()
                     .WithEntityAccess())
        {
            if (spawner.ValueRO.ShouldSpawn)
            {
                var ghost = ecb.Instantiate(spawner.ValueRO.GhostPrefab);
                ecb.SetComponent(ghost, new GhostOwner { NetworkId = spawner.ValueRO.NetworkId });
                
                spawner.ValueRW.ShouldSpawn = false;
            }
        }
        
        ecb.Playback(state.EntityManager);
    }
}
```

## Cleanup Component Pattern

Entity 제거 시 정리 작업:

```csharp
public struct NeedsCleanup : ICleanupComponentData { }

public void OnUpdate(ref SystemState state)
{
    // When entity with Target is destroyed
    foreach (var entity in 
             SystemAPI.Query<Entity>()
                 .WithAll<NeedsCleanup>()
                 .WithNone<Target>())
    {
        // Perform cleanup
        Debug.Log("Cleaning up destroyed entity");
        
        // Remove cleanup marker
        state.EntityManager.RemoveComponent<NeedsCleanup>(entity);
    }
}
```
