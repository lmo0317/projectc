# Unity DOTS + Netcode for Entities 2인 멀티플레이 구현 계획

## 프로젝트 개요

**목표**: 현재 싱글플레이 Survival Shooter 게임을 Unity Netcode for Entities를 활용하여 2인 협동 멀티플레이 게임으로 전환

**핵심 원칙**:
- 기존 DOTS 아키텍처 유지
- 클라이언트-서버 모델 사용
- 최소한의 코드 변경으로 멀티플레이 구현
- 단계별 검증 필수

**현재 프로젝트 상태**:
- Unity 6 (6000.1.7f1)
- Unity Entities 1.4.2
- 완전한 ECS 기반 Survival Shooter 게임
- 플레이어, 총알, 몬스터, UI, 웨이브 시스템 구현 완료

---

## 1. Netcode for Entities 개요

### 1.1 핵심 개념

**Ghost (네트워크 오브젝트)**:
- 서버에서 클라이언트로 동기화되는 Entity
- 플레이어, 총알, 몬스터가 Ghost가 됨
- `[GhostComponent]` 어트리뷰트로 동기화할 컴포넌트 지정

**RPC (Remote Procedure Call)**:
- 클라이언트-서버 간 메시지 전송
- 플레이어 입력, 게임 이벤트 전달에 사용
- `IRpcCommand` 인터페이스 구현

**Client Prediction**:
- 클라이언트에서 로컬 플레이어 움직임 예측
- 서버 검증 후 보정
- 부드러운 플레이 경험 제공

**Server Authority**:
- 모든 게임 로직은 서버에서 실행
- 클라이언트는 입력만 전송
- 치팅 방지

### 1.2 패키지 정보

**패키지 이름**: `com.unity.netcode`

**권장 버전**: 1.4.x (Unity Entities 1.4.2 호환)
- 현재 프로젝트는 Unity Entities 1.4.2 사용 중
- Netcode for Entities 1.4.1 사용 권장 (2025-04-30 릴리즈)
- Transport 패키지 2.1.0 필요

**추가 필요 패키지**:
- `com.unity.transport` 2.1.0 (자동 설치됨)
- `com.unity.collections` 2.6.3 (이미 설치됨)
- `com.unity.multiplayer.tools` (선택, 디버깅용)

### 1.3 참고 자료

**공식 문서**:
- [Unity Netcode for Entities Documentation](https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/index.html)
- [Build a session with Netcode for Entities](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/build-with-netcode-for-entities)

**튜토리얼**:
- [Code Monkey - Getting Started with Netcode for Entities (Unity 6)](https://unitycodemonkey.com/video.php?v=HpUmpw_N8BA)
- [DOTS NetCode Tutorial](https://dots-tutorial.moetsi.com/dots-netcode/intro-to-dots-netcode)

---

## 2. 아키텍처 설계

### 2.1 네트워크 모델

```
┌─────────────┐         ┌─────────────┐
│  Client 1   │         │  Client 2   │
│ (Player 1)  │         │ (Player 2)  │
└──────┬──────┘         └──────┬──────┘
       │ Input                 │ Input
       │                       │
       └───────────┬───────────┘
                   │
              ┌────▼────┐
              │  Server │
              │ (권위)  │
              └────┬────┘
                   │ Game State
       ┌───────────┴───────────┐
       │                       │
       ▼                       ▼
┌─────────────┐         ┌─────────────┐
│  Client 1   │         │  Client 2   │
│  (렌더링)   │         │  (렌더링)   │
└─────────────┘         └─────────────┘
```

**서버 역할**:
- 모든 게임 로직 실행
- 몬스터 AI, 충돌 체크, 총알 발사
- 게임 상태를 모든 클라이언트에 동기화

**클라이언트 역할**:
- 입력 수집 및 서버 전송
- 로컬 플레이어 움직임 예측
- 서버로부터 받은 상태 렌더링

### 2.2 Entity 동기화 전략

| Entity 타입 | 동기화 방식 | Owner | 예측 |
|------------|-----------|-------|------|
| **Player** | Ghost | Owner (각 클라이언트) | 예측 O |
| **Bullet** | Ghost | Server | 예측 X |
| **Enemy** | Ghost | Server | 예측 X |
| **UI Data** | Ghost (싱글톤) | Server | 예측 X |

**Ghost Component 설정**:
```csharp
// Player: 움직임 예측
[GhostComponent(PrefabType=GhostPrefabType.AllPredicted)]
public struct PlayerTag : IComponentData { }

// Bullet, Enemy: 서버 권위
[GhostComponent(PrefabType=GhostPrefabType.Server)]
public struct BulletTag : IComponentData { }

[GhostComponent(PrefabType=GhostPrefabType.Server)]
public struct EnemyTag : IComponentData { }
```

### 2.3 System 실행 위치

| System | 실행 위치 | 이유 |
|--------|----------|------|
| **PlayerInputSystem** | Client | 입력 수집 |
| **PlayerMovementSystem** | Client + Server | 클라이언트 예측 |
| **AutoShootSystem** | Server | 서버 권위 |
| **BulletMovementSystem** | Server | 서버 권위 |
| **EnemySpawnSystem** | Server | 서버 권위 |
| **EnemyChaseSystem** | Server | 서버 권위 |
| **BulletHitSystem** | Server | 서버 권위 |
| **PlayerDamageSystem** | Server | 서버 권위 |
| **GameStatsSystem** | Server | 서버 권위 |
| **UIUpdateSystem** | Client | UI 렌더링 |

**시스템 실행 조건 지정**:
```csharp
// Server에서만 실행
[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct AutoShootSystem : ISystem { }

// Client에서만 실행
[UpdateInGroup(typeof(PresentationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct UIUpdateSystem : ISystem { }

// Client + Server 모두 실행 (예측)
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct PlayerMovementSystem : ISystem { }
```

---

## 3. 구현 계획 (단계별)

### **PHASE 1: 환경 설정 및 패키지 설치**

**예상 소요 시간**: 1-2시간

#### 1.1 Netcode 패키지 설치

**Packages/manifest.json 수정**:
```json
{
  "dependencies": {
    "com.unity.netcode": "1.4.1",
    "com.unity.transport": "2.1.0",
    // ... 기존 패키지들
  }
}
```

**검증**:
- Package Manager에서 Netcode for Entities 1.4.1 설치 확인
- Unity Transport 2.1.0 자동 설치 확인
- 콘솔에 에러 없음

#### 1.2 Multiplayer Play Mode 설치 (선택)

**디버깅 편의성 향상**:
- Window → Package Manager → Unity Registry
- "Multiplayer Play Mode" 검색
- Install
- 하나의 Unity Editor에서 Server + Client 2개 동시 실행 가능

#### 1.3 네트워크 씬 구조 생성

**Bootstrap 씬 생성**:
1. 새 씬 생성: `Assets/Scenes/NetworkBootstrap.unity`
2. 빈 GameObject 생성: "NetworkManager"
3. 나중에 Bootstrap 스크립트 추가 예정

**완료 조건**:
- [ ] Netcode for Entities 1.4.1 설치 완료
- [ ] Transport 2.1.0 설치 확인
- [ ] Multiplayer Play Mode 설치 (선택)
- [ ] NetworkBootstrap.unity 씬 생성
- [ ] 컴파일 에러 없음

---

### **PHASE 2: Component Ghost 변환**

**예상 소요 시간**: 2-3시간

#### 2.1 GhostComponent 어트리뷰트 추가

**Player 컴포넌트**:

`Assets/Scripts/Components/PlayerTag.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerTag : IComponentData { }
```

`Assets/Scripts/Components/PlayerInput.cs`:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInput : IComponentData
{
    [GhostField] public float2 Movement;
}
```

`Assets/Scripts/Components/PlayerHealth.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerHealth : IComponentData
{
    [GhostField] public float CurrentHealth;
    [GhostField] public float MaxHealth;
}
```

`Assets/Scripts/Components/MovementSpeed.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct MovementSpeed : IComponentData
{
    [GhostField] public float Value;
}
```

**Bullet 컴포넌트**:

`Assets/Scripts/Components/BulletTag.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletTag : IComponentData { }
```

`Assets/Scripts/Components/BulletDirection.cs`:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletDirection : IComponentData
{
    [GhostField] public float3 Value;
}
```

`Assets/Scripts/Components/BulletSpeed.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletSpeed : IComponentData
{
    [GhostField] public float Value;
}
```

**Enemy 컴포넌트**:

`Assets/Scripts/Components/EnemyTag.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyTag : IComponentData { }
```

`Assets/Scripts/Components/EnemyHealth.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyHealth : IComponentData
{
    [GhostField] public float Value;
}
```

`Assets/Scripts/Components/EnemySpeed.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemySpeed : IComponentData
{
    [GhostField] public float Value;
}
```

**Game State 컴포넌트**:

`Assets/Scripts/Components/GameStats.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct GameStats : IComponentData
{
    [GhostField] public float SurvivalTime;
    [GhostField] public int KillCount;
}
```

#### 2.2 NetworkId 컴포넌트 추가

**플레이어 소유권 식별**:

`Assets/Scripts/Components/PlayerNetworkId.cs` 생성:
```csharp
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 소유권 식별 (어느 클라이언트가 조종하는지)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerNetworkId : IComponentData
{
    [GhostField] public int Value;
}
```

#### 2.3 PlayerAuthoring에 NetworkId 추가

`Assets/Scripts/Authoring/PlayerAuthoring.cs` 수정:
```csharp
class Baker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

        AddComponent(entity, new PlayerTag());
        AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
        AddComponent(entity, new PlayerInput());
        AddComponent(entity, new PlayerHealth
        {
            CurrentHealth = authoring.MaxHealth,
            MaxHealth = authoring.MaxHealth
        });
        AddComponent(entity, new PlayerNetworkId { Value = -1 }); // 추가!

        // ... 기존 코드
    }
}
```

**완료 조건**:
- [ ] 모든 Player 컴포넌트에 `[GhostComponent]` 추가
- [ ] 모든 Bullet 컴포넌트에 `[GhostComponent]` 추가
- [ ] 모든 Enemy 컴포넌트에 `[GhostComponent]` 추가
- [ ] GameStats에 `[GhostComponent]` 추가
- [ ] PlayerNetworkId 컴포넌트 생성
- [ ] PlayerAuthoring에 PlayerNetworkId 추가
- [ ] 컴파일 에러 없음

---

### **PHASE 3: Network Bootstrap 구현**

**예상 소요 시간**: 3-4시간

#### 3.1 NetworkBootstrap 스크립트 작성

`Assets/Scripts/Network/NetworkBootstrap.cs` 생성:
```csharp
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 네트워크 초기화 및 서버/클라이언트 월드 생성
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public class NetworkBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // 기본 초기화 (Server + Client Worlds 생성)
        AutoConnectPort = 7979; // 포트 설정
        return base.Initialize(defaultWorldName);
    }
}
```

#### 3.2 GameBootstrap MonoBehaviour 작성

`Assets/Scripts/Network/GameBootstrap.cs` 생성:
```csharp
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// Unity Editor에서 Server/Client 시작 제어
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Network Settings")]
    public ushort Port = 7979;
    public string ServerIP = "127.0.0.1";

    [Header("Launch Mode")]
    public LaunchMode Mode = LaunchMode.ClientAndServer;

    public enum LaunchMode
    {
        ClientAndServer, // 로컬 테스트 (Server + Client 동시)
        ClientOnly,      // Client만
        ServerOnly       // Dedicated Server
    }

    void Start()
    {
        // Server World 생성
        if (Mode == LaunchMode.ClientAndServer || Mode == LaunchMode.ServerOnly)
        {
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var serverEp = NetworkEndpoint.AnyIpv4.WithPort(Port);

            // Server 리스닝 시작
            server.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
            world.EntityManager.SetComponentData(server.GetExistingSystemManaged<NetworkStreamReceiveSystem>().ListenEndpoint,
                new NetworkStreamRequestListen { Endpoint = serverEp });

            Debug.Log($"[Server] Listening on port {Port}");
        }

        // Client World 생성
        if (Mode == LaunchMode.ClientAndServer || Mode == LaunchMode.ClientOnly)
        {
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            var clientEp = NetworkEndpoint.Parse(ServerIP, Port);

            // Server에 연결
            var connectRequest = client.EntityManager.CreateEntity();
            client.EntityManager.AddComponentData(connectRequest, new NetworkStreamRequestConnect { Endpoint = clientEp });

            Debug.Log($"[Client] Connecting to {ServerIP}:{Port}");
        }
    }
}
```

#### 3.3 NetworkBootstrap 씬 설정

**NetworkBootstrap.unity 씬 편집**:
1. "NetworkManager" GameObject 선택
2. Add Component → GameBootstrap
3. Inspector 설정:
   - Port: 7979
   - Server IP: 127.0.0.1
   - Mode: Client And Server

**Build Settings 업데이트**:
1. File → Build Settings
2. NetworkBootstrap.unity를 씬 리스트 **첫 번째**로 추가
3. SampleScene.unity는 두 번째 (자동 로드됨)

#### 3.4 PlayerSpawn 시스템 작성

`Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs` 생성:
```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 클라이언트 연결 시 플레이어 스폰
/// Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerAuthoring>(); // PlayerPrefab 필요
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 연결된 클라이언트 중 플레이어가 없는 경우
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithNone<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            // InGame 태그 추가 (스폰 완료 표시)
            ecb.AddComponent<NetworkStreamInGame>(entity);

            // PlayerPrefab 가져오기 (씬에 있는 PlayerAuthoring에서)
            Entity playerPrefab = Entity.Null;
            foreach (var prefab in SystemAPI.Query<RefRO<PlayerAuthoring>>())
            {
                // TODO: Prefab Entity로 변경 필요
                break;
            }

            // 플레이어 스폰
            var player = ecb.Instantiate(playerPrefab);

            // 스폰 위치 설정 (2명이므로 좌/우 배치)
            float3 spawnPosition = new float3(id.ValueRO.Value * 3f, 0f, 0f);
            ecb.SetComponent(player, LocalTransform.FromPosition(spawnPosition));

            // 소유권 설정
            ecb.SetComponent(player, new GhostOwner { NetworkId = id.ValueRO.Value });
            ecb.SetComponent(player, new PlayerNetworkId { Value = id.ValueRO.Value });

            Debug.Log($"[Server] Player spawned for client {id.ValueRO.Value} at {spawnPosition}");
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**완료 조건**:
- [ ] NetworkBootstrap.cs 작성 완료
- [ ] GameBootstrap.cs 작성 완료
- [ ] NetworkBootstrap.unity 씬에 GameBootstrap 컴포넌트 추가
- [ ] Build Settings에 NetworkBootstrap.unity 첫 번째로 추가
- [ ] PlayerSpawnSystem.cs 작성 완료
- [ ] 컴파일 에러 없음

---

### **PHASE 4: Input 시스템 네트워크 변환**

**예상 소요 시간**: 2-3시간

#### 4.1 InputCommandData 정의

`Assets/Scripts/Network/PlayerInputCommand.cs` 생성:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 클라이언트 → 서버 입력 전송
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInputCommand : IInputComponentData
{
    public NetworkTick Tick { get; set; }
    public float2 Movement;
}
```

#### 4.2 PlayerInputSystem 수정

`Assets/Scripts/Systems/PlayerInputSystem.cs` 수정:
```csharp
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 클라이언트에서 입력 수집 및 Command 생성
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 로컬 플레이어 찾기
        foreach (var (inputCommand, id) in SystemAPI.Query<RefRW<PlayerInputCommand>, RefRO<PlayerNetworkId>>())
        {
            // 내 플레이어인지 확인 (GhostOwner로 판단)
            if (!SystemAPI.HasComponent<GhostOwner>(entity))
                continue;

            var ghostOwner = SystemAPI.GetComponent<GhostOwner>(entity);
            if (ghostOwner.NetworkId != SystemAPI.GetSingleton<NetworkId>().Value)
                continue;

            // 입력 수집
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            inputCommand.ValueRW.Movement = new float2(horizontal, vertical);
            inputCommand.ValueRW.Tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        }
    }
}
```

#### 4.3 PlayerMovementSystem 수정 (예측 지원)

`Assets/Scripts/Systems/PlayerMovementSystem.cs` 수정:
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 플레이어 이동 처리 (클라이언트 예측 지원)
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 예측 가능한 Entity만 처리
        foreach (var (transform, speed, inputCommand) in SystemAPI.Query<
                     RefRW<LocalTransform>,
                     RefRO<MovementSpeed>,
                     RefRO<PlayerInputCommand>>()
                 .WithAll<Simulate>()) // Netcode 예측 플래그
        {
            // 입력 기반 이동
            float3 movement = new float3(inputCommand.ValueRO.Movement.x, 0f, inputCommand.ValueRO.Movement.y);
            float3 moveDirection = math.normalize(movement);

            if (math.lengthsq(movement) > 0.01f)
            {
                transform.ValueRW.Position += moveDirection * speed.ValueRO.Value * deltaTime;
                transform.ValueRW.Position.y = 0f;

                // 회전
                quaternion targetRotation = quaternion.LookRotationSafe(moveDirection, math.up());
                transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, targetRotation, 10f * deltaTime);
            }
        }
    }
}
```

**완료 조건**:
- [ ] PlayerInputCommand 작성 완료
- [ ] PlayerInputSystem 네트워크 버전으로 수정
- [ ] PlayerMovementSystem 예측 지원 추가
- [ ] 컴파일 에러 없음

---

### **PHASE 5: 게임플레이 System 네트워크 변환**

**예상 소요 시간**: 4-5시간

#### 5.1 AutoShootSystem 서버 전용 변환

`Assets/Scripts/Systems/AutoShootSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    // 기존 코드 유지
    // Server에서만 실행되므로 변경 불필요
}
```

#### 5.2 EnemySpawnSystem 서버 전용 변환

`Assets/Scripts/Systems/EnemySpawnSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct EnemySpawnSystem : ISystem
{
    // 기존 코드 유지
}
```

#### 5.3 EnemyChaseSystem 서버 전용 변환

`Assets/Scripts/Systems/EnemyChaseSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 가장 가까운 플레이어 추적 (2명 중 선택)
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 모든 플레이어 위치 수집
        var playerPositions = new NativeList<float3>(Allocator.Temp);
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPositions.Add(transform.ValueRO.Position);
        }

        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            return;
        }

        // 각 적이 가장 가까운 플레이어 추적
        foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemySpeed>>()
                     .WithAll<EnemyTag>())
        {
            float3 enemyPos = transform.ValueRO.Position;

            // 가장 가까운 플레이어 찾기
            float closestDistSq = float.MaxValue;
            float3 targetPlayerPos = float3.zero;

            for (int i = 0; i < playerPositions.Length; i++)
            {
                float distSq = math.distancesq(enemyPos, playerPositions[i]);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    targetPlayerPos = playerPositions[i];
                }
            }

            // 추적
            float3 direction = math.normalize(targetPlayerPos - enemyPos);
            float3 newPosition = enemyPos + direction * speed.ValueRO.Value * deltaTime;
            newPosition.y = 0f;

            transform.ValueRW.Position = newPosition;

            // 회전
            quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
            transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, targetRotation, 10f * deltaTime);
        }

        playerPositions.Dispose();
    }
}
```

#### 5.4 BulletMovementSystem 서버 전용 변환

`Assets/Scripts/Systems/BulletMovementSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct BulletMovementSystem : ISystem
{
    // 기존 코드 유지
}
```

#### 5.5 충돌 시스템 서버 전용 변환

`Assets/Scripts/Systems/BulletHitSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct BulletHitSystem : ISystem
{
    // 기존 코드 유지
}
```

`Assets/Scripts/Systems/PlayerDamageSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerDamageSystem : SystemBase
{
    // 기존 코드 유지
}
```

#### 5.6 게임 상태 시스템 서버 전용 변환

`Assets/Scripts/Systems/GameStatsSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct GameStatsSystem : ISystem
{
    // 기존 코드 유지
}
```

`Assets/Scripts/Systems/GameOverSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // 추가!
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class GameOverSystem : SystemBase
{
    // 기존 코드 유지
}
```

**완료 조건**:
- [ ] AutoShootSystem에 `[WorldSystemFilter]` 추가
- [ ] EnemySpawnSystem에 `[WorldSystemFilter]` 추가
- [ ] EnemyChaseSystem에 `[WorldSystemFilter]` 추가 및 다중 플레이어 지원
- [ ] BulletMovementSystem에 `[WorldSystemFilter]` 추가
- [ ] BulletHitSystem에 `[WorldSystemFilter]` 추가
- [ ] PlayerDamageSystem에 `[WorldSystemFilter]` 추가
- [ ] GameStatsSystem에 `[WorldSystemFilter]` 추가
- [ ] GameOverSystem에 `[WorldSystemFilter]` 추가
- [ ] 컴파일 에러 없음

---

### **PHASE 6: UI 시스템 클라이언트 변환**

**예상 소요 시간**: 2-3시간

#### 6.1 UIUpdateSystem 클라이언트 전용 변환

`Assets/Scripts/Systems/UIUpdateSystem.cs` 수정:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // 추가!
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    private UIManager uiManager;

    protected override void OnCreate()
    {
        RequireForUpdate<PlayerTag>();
    }

    protected override void OnStartRunning()
    {
        uiManager = Object.FindFirstObjectByType<UIManager>();
    }

    protected override void OnUpdate()
    {
        if (uiManager == null) return;

        // 로컬 플레이어 체력만 표시
        foreach (var (health, ghostOwner) in SystemAPI.Query<RefRO<PlayerHealth>, RefRO<GhostOwner>>()
                     .WithAll<PlayerTag>())
        {
            // 내 플레이어인지 확인
            if (!SystemAPI.HasSingleton<NetworkId>())
                continue;

            var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;
            if (ghostOwner.ValueRO.NetworkId != myNetworkId)
                continue;

            uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            break;
        }

        // 게임 통계 (서버에서 동기화됨)
        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            uiManager.UpdateSurvivalTime(stats.ValueRO.SurvivalTime);
            uiManager.UpdateKillCount(stats.ValueRO.KillCount);
            break;
        }

        // 웨이브 정보 (구현된 경우)
        if (SystemAPI.HasSingleton<WaveConfig>())
        {
            var waveConfig = SystemAPI.GetSingleton<WaveConfig>();
            uiManager.UpdateWave(waveConfig.CurrentWave);
        }
    }
}
```

#### 6.2 UIManager 다중 플레이어 지원

`Assets/Scripts/UI/UIManager.cs` 수정 (선택):
```csharp
[Header("Multiplayer HUD")]
public TextMeshProUGUI Player1HealthText;
public TextMeshProUGUI Player2HealthText;

public void UpdatePlayerHealth(int playerId, float current, float max)
{
    if (playerId == 0 && Player1HealthText != null)
    {
        Player1HealthText.text = $"P1 Health: {current:F0}/{max:F0}";
    }
    else if (playerId == 1 && Player2HealthText != null)
    {
        Player2HealthText.text = $"P2 Health: {current:F0}/{max:F0}";
    }
}
```

**완료 조건**:
- [ ] UIUpdateSystem에 `[WorldSystemFilter]` 추가
- [ ] UIUpdateSystem에서 로컬 플레이어만 표시
- [ ] UIManager 다중 플레이어 지원 (선택)
- [ ] 컴파일 에러 없음

---

### **PHASE 7: Ghost Prefab 설정**

**예상 소요 시간**: 2-3시간

#### 7.1 Player Ghost Prefab

**Unity Editor 작업**:
1. Hierarchy에서 Player GameObject 선택
2. Add Component → Ghost Authoring Component
3. Inspector 설정:
   - Ghost Mode: Owner Predicted
   - Supported Ghost Mode: All
   - Optimization Mode: Dynamic
   - Default Ghost Mode: Owner Predicted

4. Player를 Prefab으로 변환:
   - Assets/Prefabs/ 폴더 생성
   - Player를 Prefabs 폴더로 드래그 (Create Original Prefab)

#### 7.2 Bullet Ghost Prefab

**Unity Editor 작업**:
1. Hierarchy에서 Bullet GameObject 선택
2. Add Component → Ghost Authoring Component
3. Inspector 설정:
   - Ghost Mode: Interpolated
   - Supported Ghost Mode: Interpolated
   - Optimization Mode: Dynamic

4. Bullet을 Prefab으로 변환:
   - Assets/Prefabs/Bullet.prefab 생성

#### 7.3 Enemy Ghost Prefab

**Unity Editor 작업**:
1. Hierarchy에서 Enemy GameObject 선택
2. Add Component → Ghost Authoring Component
3. Inspector 설정:
   - Ghost Mode: Interpolated
   - Supported Ghost Mode: Interpolated
   - Optimization Mode: Dynamic

4. Enemy를 Prefab으로 변환:
   - Assets/Prefabs/Enemy.prefab 생성

#### 7.4 Authoring 참조 업데이트

**PlayerAuthoring.cs**:
- BulletPrefab 필드를 Prefab 에셋으로 참조

**EnemySpawnAuthoring.cs**:
- EnemyPrefab 필드를 Prefab 에셋으로 참조

**완료 조건**:
- [ ] Player에 Ghost Authoring Component 추가 (Owner Predicted)
- [ ] Bullet에 Ghost Authoring Component 추가 (Interpolated)
- [ ] Enemy에 Ghost Authoring Component 추가 (Interpolated)
- [ ] 모든 GameObject를 Prefab으로 변환
- [ ] Authoring 클래스의 Prefab 참조 업데이트
- [ ] 컴파일 에러 없음

---

### **PHASE 8: 테스트 및 디버깅**

**예상 소요 시간**: 4-6시간

#### 8.1 로컬 테스트 (Editor)

**Multiplayer Play Mode 사용**:
1. Window → Multiplayer Play Mode
2. Virtual Players: 2 설정
3. Play 버튼 클릭
4. 한 Editor에서 Server + Client 2개 동시 실행

**검증 항목**:
- [ ] Server 시작 확인 (콘솔 로그)
- [ ] Client 2개 연결 확인
- [ ] Player 2명 스폰 확인
- [ ] 각 플레이어 독립적으로 움직임
- [ ] 서로 상대방 플레이어 보임
- [ ] 총알 발사 및 동기화
- [ ] 몬스터 스폰 및 추격 (가장 가까운 플레이어)
- [ ] 체력 감소 동기화
- [ ] UI 업데이트 (각 클라이언트)

#### 8.2 빌드 테스트

**Standalone 빌드**:
1. File → Build Settings
2. PC, Mac & Linux Standalone
3. Create Folder: Builds/Server
4. Build (Server)

5. Build Settings 다시 열기
6. Create Folder: Builds/Client1
7. Build (Client)

8. Builds/Client1을 복사하여 Builds/Client2 생성

**테스트 절차**:
1. Server 실행 (GameBootstrap Mode: Server Only)
2. Client1 실행 (GameBootstrap Mode: Client Only)
3. Client2 실행 (GameBootstrap Mode: Client Only)
4. 3개 창에서 동시 플레이 확인

#### 8.3 네트워크 디버깅

**NetDbg 툴 사용**:
1. Window → Multiplayer → NetDbg
2. Server/Client 연결 상태 확인
3. Ghost 동기화 확인
4. Ping/RTT 확인

**Entity Debugger**:
1. Window → Entities → Hierarchy
2. Server World 선택
3. Player, Bullet, Enemy Entity 확인
4. Client World 선택
5. Ghost Entity 확인

**완료 조건**:
- [ ] Multiplayer Play Mode에서 2인 플레이 정상 작동
- [ ] Standalone 빌드에서 Server + Client 2개 정상 작동
- [ ] 모든 게임 기능 동기화 확인
- [ ] 네트워크 지연(100ms 이상) 테스트
- [ ] 재연결 테스트
- [ ] 성능 확인 (60 FPS 유지)

---

## 4. 예상 문제 및 해결 방안

### 4.1 일반적인 문제

| 문제 | 원인 | 해결 방법 |
|------|------|----------|
| **Player가 스폰 안됨** | PlayerSpawnSystem 미작동 | PlayerPrefab 참조 확인, GhostAuthoring 확인 |
| **입력이 안먹힘** | Input Command 동기화 실패 | PlayerInputCommand 컴포넌트 확인, GhostField 확인 |
| **플레이어가 떨림** | 예측/보정 충돌 | PredictedSimulationSystemGroup 사용 확인 |
| **총알/몬스터 안보임** | Ghost 동기화 실패 | GhostComponent 어트리뷰트 확인 |
| **연결 실패** | 포트/IP 문제 | 방화벽 확인, 포트 7979 개방 |
| **성능 저하** | 과다한 Ghost 동기화 | Ghost Optimization Mode 조정 |

### 4.2 디버깅 팁

**콘솔 로그 활용**:
```csharp
#if UNITY_SERVER
Debug.Log("[Server] ...");
#elif UNITY_CLIENT
Debug.Log("[Client] ...");
#endif
```

**NetworkId 확인**:
```csharp
if (SystemAPI.HasSingleton<NetworkId>())
{
    var id = SystemAPI.GetSingleton<NetworkId>().Value;
    Debug.Log($"My NetworkId: {id}");
}
```

**Ghost 상태 확인**:
```csharp
foreach (var (ghostOwner, entity) in SystemAPI.Query<RefRO<GhostOwner>>()
             .WithEntityAccess())
{
    Debug.Log($"Ghost Entity: {entity.Index}, Owner: {ghostOwner.ValueRO.NetworkId}");
}
```

---

## 5. 전체 작업 일정

### 총 예상 소요 시간: 20-30시간 (4-6일)

| Phase | 작업 내용 | 예상 시간 |
|-------|----------|-----------|
| **Phase 1** | 환경 설정 및 패키지 설치 | 1-2시간 |
| **Phase 2** | Component Ghost 변환 | 2-3시간 |
| **Phase 3** | Network Bootstrap 구현 | 3-4시간 |
| **Phase 4** | Input 시스템 네트워크 변환 | 2-3시간 |
| **Phase 5** | 게임플레이 System 네트워크 변환 | 4-5시간 |
| **Phase 6** | UI 시스템 클라이언트 변환 | 2-3시간 |
| **Phase 7** | Ghost Prefab 설정 | 2-3시간 |
| **Phase 8** | 테스트 및 디버깅 | 4-6시간 |

**버퍼 시간**: +5-7시간 (문제 해결)

**전체**: 25-37시간 (약 5-7일)

---

## 6. SOLID 원칙 준수

### Single Responsibility (단일 책임)
- **NetworkBootstrap**: 네트워크 초기화만
- **PlayerSpawnSystem**: 플레이어 스폰만
- **각 System**: 기존 책임 유지

### Open/Closed (개방/폐쇄)
- Ghost 컴포넌트 추가로 네트워크 기능 확장
- 기존 시스템 로직 수정 최소화

### Liskov Substitution (리스코프 치환)
- ISystem, IComponentData 인터페이스 준수
- Netcode 인터페이스 (IInputComponentData) 올바르게 구현

### Interface Segregation (인터페이스 분리)
- 각 System은 필요한 컴포넌트만 쿼리
- WorldSystemFilter로 실행 위치 명확히 분리

### Dependency Inversion (의존성 역전)
- System은 구체적 구현이 아닌 컴포넌트에 의존
- NetworkId, GhostOwner 싱글톤 활용

---

## 7. 대안 접근법 비교

### 7.1 Netcode for Entities vs Netcode for GameObjects

| 항목 | Netcode for Entities | Netcode for GameObjects |
|------|---------------------|------------------------|
| **호환성** | ECS/DOTS 전용 | MonoBehaviour 전용 |
| **성능** | 매우 높음 (Burst) | 보통 |
| **러닝 커브** | 높음 | 낮음 |
| **현재 프로젝트** | ✅ **최적** (이미 ECS) | ❌ 전체 재작성 필요 |

**결론**: Netcode for Entities 사용이 최적

### 7.2 MCP (Model Context Protocol) 사용

**MCP란?**:
- AI Assistant가 외부 도구/서비스에 접근하는 프로토콜
- Unity 프로젝트 관리에는 직접적으로 부적합

**Unity 멀티플레이 구현에 MCP 활용 가능 여부**:
- ❌ 직접 사용 불가 (Unity Netcode가 표준)
- ✅ 간접 활용: AI로 코드 생성/리뷰 시 MCP를 통해 문서 접근

**결론**: MCP는 개발 보조 도구로만 활용, 실제 네트워크는 Netcode for Entities 사용

### 7.3 Claude Code Skills 활용

**현재 프로젝트에 유용한 Skills**:
- **코드 생성**: Netcode 보일러플레이트 자동 생성
- **디버깅 지원**: 네트워크 이슈 분석
- **문서 참조**: Unity 공식 문서 자동 검색

**활용 방안**:
1. Skill을 통해 Netcode 예제 코드 생성
2. 기존 시스템과 통합
3. AI가 코드 리뷰 및 최적화 제안

**결론**: Skills는 개발 속도 향상에 도움, 하지만 핵심 구현은 직접 수행 필요

---

## 8. 최종 검증 체크리스트

### 필수 기능
- [ ] 2명의 플레이어가 동시에 접속
- [ ] 각 플레이어 독립적으로 조작 가능
- [ ] 상대방 플레이어가 보임
- [ ] 총알 발사 동기화
- [ ] 몬스터 스폰 및 추격 (가장 가까운 플레이어)
- [ ] 충돌 감지 및 체력 감소
- [ ] 게임 오버 (한 명이라도 죽으면?)
- [ ] UI 업데이트 (각 클라이언트)

### 성능
- [ ] 60 FPS 유지 (Server + Client 2개)
- [ ] 네트워크 지연 < 100ms (로컬)
- [ ] 패킷 손실 대응

### 안정성
- [ ] 재연결 지원
- [ ] 클라이언트 연결 해제 처리
- [ ] 에러 핸들링

---

## 9. 참고 자료

### 공식 문서
- [Unity Netcode for Entities 1.4.1](https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/index.html)
- [Netcode for Entities Changelog](https://docs.unity3d.com/Packages/com.unity.netcode@1.4/changelog/CHANGELOG.html)
- [Build a session with Netcode for Entities](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual/build-with-netcode-for-entities)

### 튜토리얼
- [Code Monkey - Getting Started with Netcode for Entities (Unity 6)](https://unitycodemonkey.com/video.php?v=HpUmpw_N8BA)
- [DOTS NetCode Tutorial](https://dots-tutorial.moetsi.com/dots-netcode/intro-to-dots-netcode)
- [Multiplayer NetCode Entities GitHub](https://github.com/guokunlin/multiplayer-netcode-entities)

### 커뮤니티
- Unity Discussions: Netcode for Entities 섹션
- Unity DOTS Discord

---

## 10. 다음 단계 (멀티플레이 완료 후)

### 추가 기능
1. **팀 시스템**: 2명이 협동하여 몬스터 처치
2. **공유 점수**: 합산 킬 카운트
3. **부활 시스템**: 한 명이 죽으면 동료가 부활
4. **채팅 시스템**: RPC로 간단한 메시지 전송
5. **매치메이킹**: Lobby 시스템 추가

### 최적화
1. **Ghost Snapshot Compression**: 대역폭 절약
2. **Lag Compensation**: 서버 측 히트 검증
3. **Relevancy System**: 거리 기반 동기화

### 배포
1. **Dedicated Server 빌드**: Linux 서버 빌드
2. **클라우드 호스팅**: AWS, Azure, Unity Cloud
3. **NAT Punchthrough**: 릴레이 서버 없이 P2P

---

**계획 작성 완료!**

이 계획서는 기존 DOTS 아키텍처를 최대한 유지하면서 Netcode for Entities를 통합하는 단계별 가이드입니다. 각 Phase를 순차적으로 진행하면서 검증하는 것이 중요합니다.
