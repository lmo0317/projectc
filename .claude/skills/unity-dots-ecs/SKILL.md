---
name: unity-dots-ecs
description: Unity DOTS (Data-Oriented Technology Stack) and ECS (Entity Component System) best practices, patterns, and code generation. Use when working with Unity Entities package, writing ISystem implementations, creating IComponentData structs, using Burst compilation, implementing IJobEntity jobs, working with SystemAPI, EntityManager, or Netcode for Entities multiplayer. Applies to Unity 6+ with Entities 1.3+.
---

# Unity DOTS/ECS Development

## Overview

Provide Unity DOTS/ECS best practices based on Unity's official samples, focusing on modern patterns using ISystem, SystemAPI, and Burst-compiled code. Follows Unity 6.0 with Entities 1.3+ conventions.

## Core Principles

### 1. System Structure (ISystem Pattern)

Use partial struct ISystem for all systems. Always Burst-compile when possible.

```csharp
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Require necessary components/singletons
        state.RequireForUpdate<MyComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Main system logic
        foreach (var (transform, entity) in 
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithEntityAccess())
        {
            // Process entities
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}
```

### 2. Component Data (IComponentData)

Keep components simple value types. No managed references.

```csharp
using Unity.Entities;

public struct Velocity : IComponentData
{
    public float3 Value;
}

// Tag components (zero-size markers)
public struct IsPlayer : IComponentData { }
```

### 3. Query Patterns with SystemAPI

Modern query syntax using SystemAPI.Query<>:

```csharp
// Basic query with RefRW for write access
foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>())
{
    transform.ValueRW.Position += float3(0, 1, 0);
}

// Multiple components with entity access
foreach (var (transform, velocity, entity) in 
         SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>()
             .WithEntityAccess())
{
    transform.ValueRW.Position += velocity.ValueRO.Value * SystemAPI.Time.DeltaTime;
}

// With filtering
foreach (var player in SystemAPI.Query<PlayerAspect>()
                           .WithAll<IsAlive>()
                           .WithNone<IsDead>())
{
    // Process only alive players
}
```

### 4. Aspects (Recommended Pattern)

Group related components into aspects for cleaner code:

```csharp
using Unity.Entities;
using Unity.Transforms;

public readonly partial struct PlayerAspect : IAspect
{
    public readonly Entity Entity;
    
    readonly RefRW<LocalTransform> Transform;
    readonly RefRO<Velocity> Velocity;
    readonly RefRW<Health> Health;
    
    public float3 Position
    {
        get => Transform.ValueRO.Position;
        set => Transform.ValueRW.Position = value;
    }
    
    public void Move(float deltaTime)
    {
        Position += Velocity.ValueRO.Value * deltaTime;
    }
}

// Usage in system
foreach (var player in SystemAPI.Query<PlayerAspect>())
{
    player.Move(SystemAPI.Time.DeltaTime);
}
```

### 5. Singleton Access

Access singleton components efficiently:

```csharp
// Get singleton
var config = SystemAPI.GetSingleton<GameConfig>();

// Get singleton entity
var configEntity = SystemAPI.GetSingletonEntity<GameConfig>();

// Modify singleton
SystemAPI.SetSingleton(new GameConfig { Value = 10 });

// Check existence
if (SystemAPI.TryGetSingleton<GameConfig>(out var config))
{
    // Use config
}
```

### 6. Entity Creation Patterns

```csharp
// Create entity with EntityManager
var entity = state.EntityManager.CreateEntity();
state.EntityManager.AddComponent<MyComponent>(entity);

// Instantiate prefab (most common)
var instance = state.EntityManager.Instantiate(prefabEntity);
state.EntityManager.SetComponentData(instance, new LocalTransform 
{ 
    Position = spawnPos,
    Rotation = quaternion.identity,
    Scale = 1f
});

// Create with archetype
var archetype = state.EntityManager.CreateArchetype(
    typeof(LocalTransform),
    typeof(Velocity),
    typeof(Health)
);
var entity = state.EntityManager.CreateEntity(archetype);
```

### 7. Jobs with IJobEntity

For parallel processing:

```csharp
[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    public float DeltaTime;
    
    void Execute(ref LocalTransform transform, in Velocity velocity)
    {
        transform.Position += velocity.Value * DeltaTime;
    }
}

// Schedule in system
public void OnUpdate(ref SystemState state)
{
    new MovementJob 
    { 
        DeltaTime = SystemAPI.Time.DeltaTime 
    }.ScheduleParallel();
}
```

### 8. Baking (GameObject to Entity Conversion)

```csharp
using Unity.Entities;
using UnityEngine;

public class VelocityAuthoring : MonoBehaviour
{
    public Vector3 Velocity;
}

public class VelocityBaker : Baker<VelocityAuthoring>
{
    public override void Bake(VelocityAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Velocity 
        { 
            Value = authoring.Velocity 
        });
    }
}
```

### 9. System Ordering

```csharp
// Update in specific group
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct InitSystem : ISystem { }

// Order within group
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(OtherSystem))]
public partial struct FirstSystem : ISystem { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FirstSystem))]
public partial struct SecondSystem : ISystem { }
```

## Common Patterns

### State Management

```csharp
public struct GameState : IComponentData
{
    public enum State { Menu, Playing, Paused, GameOver }
    public State CurrentState;
}

// State transition system
public void OnUpdate(ref SystemState state)
{
    var gameState = SystemAPI.GetSingletonRW<GameState>();
    
    if (gameState.ValueRO.CurrentState == GameState.State.Playing)
    {
        // Playing logic
    }
}
```

### Timers

```csharp
public struct SpawnTimer : IComponentData
{
    public float Value;
    public float Interval;
}

public void OnUpdate(ref SystemState state)
{
    var timer = SystemAPI.GetSingletonRW<SpawnTimer>();
    timer.ValueRW.Value -= SystemAPI.Time.DeltaTime;
    
    if (timer.ValueRO.Value <= 0)
    {
        // Spawn logic
        timer.ValueRW.Value = timer.ValueRO.Interval;
    }
}
```

### Entity Commands (Deferred Operations)

```csharp
public void OnUpdate(ref SystemState state)
{
    var ecb = new EntityCommandBuffer(Allocator.TempJob);
    
    foreach (var (health, entity) in 
             SystemAPI.Query<RefRO<Health>>()
                 .WithEntityAccess())
    {
        if (health.ValueRO.Value <= 0)
        {
            ecb.DestroyEntity(entity);
        }
    }
    
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
}
```

## Netcode for Entities Patterns

### Ghost Component

```csharp
[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct PlayerInput : IInputComponentData
{
    public float2 Movement;
    public InputEvent Jump;
}
```

### Client/Server Separation

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerOnlySystem : ISystem { }

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientOnlySystem : ISystem { }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | 
                   WorldSystemFilterFlags.ClientSimulation)]
public partial struct SharedSystem : ISystem { }
```

### RPC (Remote Procedure Call)

RPC를 사용하여 Server와 Client 간 이벤트 기반 메시지를 전송합니다.

#### RPC 정의

```csharp
using Unity.NetCode;
using Unity.Collections;

// RPC 커맨드 구조체 정의
public struct MyRpcCommand : IRpcCommand
{
    public int Value;
    public FixedString128Bytes Message; // 문자열은 FixedString 사용
}
```

#### Server에서 Client로 RPC 전송

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerRpcSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1. 모든 클라이언트에게 브로드캐스트
        var broadcastEntity = ecb.CreateEntity();
        ecb.AddComponent(broadcastEntity, new MyRpcCommand
        {
            Value = 42,
            Message = "Hello All Clients"
        });
        ecb.AddComponent<SendRpcCommandRequest>(broadcastEntity);

        // 2. 특정 클라이언트에게만 전송
        foreach (var (networkId, connectionEntity) in
                 SystemAPI.Query<RefRO<NetworkId>>()
                     .WithEntityAccess()
                     .WithNone<MyTag>()) // 특정 조건
        {
            var targetEntity = ecb.CreateEntity();
            ecb.AddComponent(targetEntity, new MyRpcCommand
            {
                Value = networkId.ValueRO.Value,
                Message = "Hello Specific Client"
            });
            ecb.AddComponent(targetEntity, new SendRpcCommandRequest
            {
                TargetConnection = connectionEntity
            });
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
```

#### Client에서 Server로 RPC 전송

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientRpcSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>(); // 연결된 후에만 실행
    }

    protected override void OnUpdate()
    {
        // RPC 전송
        var rpcEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(rpcEntity, new MyRpcCommand
        {
            Value = 100,
            Message = "Hello Server"
        });
        EntityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
        // TargetConnection을 설정하지 않으면 자동으로 Server로 전송
    }
}
```

#### RPC 수신 처리

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ReceiveRpcServerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var connections = GetComponentLookup<NetworkId>(true);

        // 수신된 RPC 처리
        foreach (var (receiveRequest, rpcData, entity) in
                 SystemAPI.Query<ReceiveRpcCommandRequest, MyRpcCommand>()
                     .WithEntityAccess())
        {
            // RPC를 보낸 클라이언트의 NetworkId
            var senderNetworkId = connections[receiveRequest.SourceConnection].Value;

            UnityEngine.Debug.Log(
                $"[Server] Received RPC from client {senderNetworkId}: " +
                $"Value={rpcData.Value}, Message={rpcData.Message}");

            // RPC 처리 로직
            // ...

            // RPC Entity 삭제 (필수!)
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ReceiveRpcClientSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (receiveRequest, rpcData, entity) in
                 SystemAPI.Query<ReceiveRpcCommandRequest, MyRpcCommand>()
                     .WithEntityAccess())
        {
            UnityEngine.Debug.Log(
                $"[Client] Received RPC from server: " +
                $"Value={rpcData.Value}, Message={rpcData.Message}");

            // RPC 처리 로직
            // ...

            // RPC Entity 삭제 (필수!)
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
```

#### RPC 사용 시 주의사항

1. **RPC Entity는 반드시 삭제해야 함** - 수신 처리 후 `ecb.DestroyEntity(entity)` 호출
2. **문자열은 FixedString 사용** - `FixedString32Bytes`, `FixedString128Bytes` 등
3. **Unmanaged 타입만 가능** - class, string, List 등 사용 불가
4. **TargetConnection 생략 시**:
   - Client → Server: 자동으로 Server로 전송
   - Server → Client: 모든 Client에게 브로드캐스트
5. **연결 확인** - `RequireForUpdate<NetworkId>()`로 연결된 후에만 RPC 전송

## Performance Tips

1. **Use Burst everywhere possible** - Mark all ISystem methods with [BurstCompile]
2. **Prefer SystemAPI.Query** over manual EntityQuery creation
3. **Use aspects** to group related component access
4. **Schedule parallel jobs** with IJobEntity for expensive operations
5. **Minimize structural changes** - batch CreateEntity/DestroyEntity operations
6. **Use EntityCommandBuffer** for deferred structural changes in jobs
7. **Avoid managed components** - use unmanaged types only
8. **Keep components small** - split large data into multiple components
9. **Use tag components** for filtering instead of bool flags

## Common Mistakes to Avoid

1. ❌ Using class for ISystem → ✅ Use partial struct ISystem
2. ❌ Forgetting [BurstCompile] → ✅ Always mark Burst-compatible methods
3. ❌ state.EntityManager in jobs → ✅ Use EntityCommandBuffer
4. ❌ RefRO when writing needed → ✅ Use RefRW for write access
5. ❌ Manual EntityQuery.ToEntityArray → ✅ Use SystemAPI.Query foreach
6. ❌ Managed fields in IComponentData → ✅ Only unmanaged types
7. ❌ Not calling RequireForUpdate → ✅ Explicitly require dependencies

## References

Unity's EntityComponentSystemSamples repository provides authoritative patterns:
- EntitiesSamples: Core ECS patterns and best practices
- NetcodeSamples: Multiplayer with Netcode for Entities
- PhysicsSamples: Unity Physics integration patterns
- See references/sample-patterns.md for detailed code examples

## Additional Resources

### Sample Patterns Reference

For detailed code examples from Unity's official samples, see `references/sample-patterns.md`:

- Spawning patterns with prefab instantiation
- Shooting/projectile systems
- Boids/flocking algorithms  
- Physics collision handling
- Dynamic buffers for variable-length data
- Netcode prediction and ghost spawning
- Enable/Disable components
- System state components for lifecycle tracking
- Cleanup patterns

Load this reference when implementing complex gameplay systems or need concrete examples of proven patterns.
