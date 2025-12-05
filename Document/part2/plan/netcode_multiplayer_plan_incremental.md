# Unity DOTS + Netcode for Entities 2인 멀티플레이 구현 계획 (점진적 통합)

## 프로젝트 개요

**목표**: 현재 싱글플레이 Survival Shooter 게임을 Unity Netcode for Entities를 활용하여 2인 협동 멀티플레이 게임으로 전환

**핵심 원칙**:
- ✅ **점진적 통합**: 작은 단위로 구현 → 테스트 → 확장
- ✅ **최소 기능 우선**: 플레이어 접속/이동부터 시작
- ✅ **단계별 검증**: 각 단계마다 반드시 동작 확인
- ✅ **기존 코드 보존**: 싱글플레이 기능은 유지

**현재 프로젝트 상태**:
- Unity 6 (6000.1.7f1)
- Unity Entities 1.4.2
- 완전한 ECS 기반 Survival Shooter 게임
- 플레이어, 총알, 몬스터, UI, 웨이브 시스템 구현 완료

---

## 점진적 통합 전략

```
┌─────────────────────────────────────────────────────────┐
│ STEP 1: 패키지 설치 + 기본 연결                          │
│ 목표: Server/Client 연결 확인                            │
│ 검증: 콘솔에 "Connected" 로그                            │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 2: 플레이어 2명 스폰                                │
│ 목표: 각 클라이언트에 플레이어 생성                        │
│ 검증: 게임 화면에 큐브 2개 보임                           │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 3: 플레이어 이동 동기화 ⭐ (핵심 마일스톤)          │
│ 목표: WASD 입력으로 움직임, 상대방에게 보임               │
│ 검증: Client1이 움직이면 Client2 화면에서도 움직임        │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 4: 총알 발사 동기화                                 │
│ 목표: 자동 발사 총알이 모든 클라이언트에 보임             │
│ 검증: 총알이 양쪽 화면에서 동기화                         │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 5: 몬스터 스폰 및 추격 동기화                        │
│ 목표: 몬스터가 가장 가까운 플레이어 추격                  │
│ 검증: 몬스터가 2명 중 가까운 쪽으로 이동                  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 6: 충돌 및 체력 동기화                              │
│ 목표: 총알/몬스터 충돌 시 체력 감소                       │
│ 검증: 체력 감소가 모든 클라이언트에 동기화                │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 7: UI 및 게임 상태 동기화                           │
│ 목표: 생존 시간, 킬 카운트, 게임 오버 동기화              │
│ 검증: 모든 UI가 정상 작동                                │
└─────────────────────────────────────────────────────────┘
```

---

## STEP 1: 환경 설정 및 기본 연결

**목표**: Netcode 패키지 설치 및 Server/Client 연결 확인

**예상 소요 시간**: 1-2시간

### 1.1 Netcode 패키지 설치

`Packages/manifest.json` 수정:
```json
{
  "dependencies": {
    "com.unity.netcode": "1.4.1",
    "com.unity.transport": "2.1.0",
    "com.unity.ai.navigation": "2.0.9",
    "com.unity.burst": "1.8.25",
    "com.unity.collections": "2.6.3",
    "com.unity.entities": "1.4.2",
    // ... 기존 패키지들
  }
}
```

**검증**:
- Unity Editor 재시작
- Window → Package Manager에서 Netcode for Entities 1.4.1 확인
- Console에 에러 없음

### 1.2 Multiplayer Play Mode 설치 (강력 권장)

**디버깅 필수 도구**:
1. Window → Package Manager
2. Unity Registry
3. "Multiplayer Play Mode" 검색
4. Install

**장점**: 하나의 Editor에서 Server + Client 2개 동시 실행 가능

### 1.3 간단한 연결 테스트 씬 생성

**새 씬 생성**: `Assets/Scenes/NetworkTest.unity`

**빈 GameObject 생성**: "NetworkManager"

### 1.4 최소 Bootstrap 스크립트 작성

`Assets/Scripts/Network/SimpleNetworkBootstrap.cs` 생성:
```csharp
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// 간단한 네트워크 연결 테스트용 Bootstrap
/// </summary>
public class SimpleNetworkBootstrap : MonoBehaviour
{
    public ushort Port = 7979;

    void Start()
    {
        // Server World 생성
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        var serverEp = NetworkEndpoint.AnyIpv4.WithPort(Port);

        // Server 리스닝 시작
        var listenEntity = server.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
        server.EntityManager.SetComponentData(listenEntity, new NetworkStreamRequestListen { Endpoint = serverEp });

        Debug.Log($"[Server] Listening on port {Port}");

        // Client World 생성
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        var clientEp = NetworkEndpoint.LoopbackIpv4.WithPort(Port);

        // Server에 연결
        var connectEntity = client.EntityManager.CreateEntity(typeof(NetworkStreamRequestConnect));
        client.EntityManager.SetComponentData(connectEntity, new NetworkStreamRequestConnect { Endpoint = clientEp });

        Debug.Log($"[Client] Connecting to 127.0.0.1:{Port}");
    }
}
```

### 1.5 씬 설정

1. NetworkTest.unity 열기
2. NetworkManager GameObject 선택
3. Add Component → Simple Network Bootstrap
4. Port: 7979
5. 씬 저장

### 1.6 연결 확인 시스템 작성

`Assets/Scripts/Network/ConnectionDebugSystem.cs` 생성:
```csharp
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 연결 상태를 콘솔에 출력하는 디버그 시스템
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ConnectionDebugSystem : SystemBase
{
    private bool hasLoggedConnection = false;

    protected override void OnUpdate()
    {
        if (hasLoggedConnection) return;

        // Server에서 연결 확인
        if (World.IsServer())
        {
            foreach (var id in SystemAPI.Query<RefRO<NetworkId>>())
            {
                Debug.Log($"[Server] Client connected: NetworkId = {id.ValueRO.Value}");
                hasLoggedConnection = true;
            }
        }

        // Client에서 연결 확인
        if (World.IsClient())
        {
            if (SystemAPI.HasSingleton<NetworkId>())
            {
                var myId = SystemAPI.GetSingleton<NetworkId>();
                Debug.Log($"[Client] Connected to server: My NetworkId = {myId.Value}");
                hasLoggedConnection = true;
            }
        }
    }
}
```

### 1.7 테스트

1. NetworkTest.unity 씬 열기
2. Play 버튼 클릭
3. **기대 결과**:
   ```
   [Server] Listening on port 7979
   [Client] Connecting to 127.0.0.1:7979
   [Server] Client connected: NetworkId = 1
   [Client] Connected to server: My NetworkId = 1
   ```

### ✅ STEP 1 완료 조건

- [ ] Netcode for Entities 1.4.1 설치 완료
- [ ] Multiplayer Play Mode 설치
- [ ] NetworkTest.unity 씬 생성
- [ ] SimpleNetworkBootstrap 작성 및 테스트
- [ ] ConnectionDebugSystem 작성
- [ ] Play 모드에서 Server/Client 연결 확인 (콘솔 로그)
- [ ] 컴파일 에러 없음

---

## STEP 2: 플레이어 2명 스폰

**목표**: 각 클라이언트 연결 시 플레이어 Entity 생성

**예상 소요 시간**: 2-3시간

### 2.1 최소 플레이어 컴포넌트 정의

`Assets/Scripts/Components/Network/PlayerTag.cs` 생성:
```csharp
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 식별 태그 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerTag : IComponentData { }
```

### 2.2 간단한 플레이어 Authoring 작성

`Assets/Scripts/Authoring/Network/SimplePlayerAuthoring.cs` 생성:
```csharp
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/// <summary>
/// 테스트용 간단한 플레이어 Authoring
/// </summary>
public class SimplePlayerAuthoring : MonoBehaviour
{
    class Baker : Baker<SimplePlayerAuthoring>
    {
        public override void Bake(SimplePlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerTag());
        }
    }
}
```

### 2.3 플레이어 프리팹 생성

**Unity Editor 작업**:
1. Hierarchy에서 3D Object → Cube 생성
2. 이름: "PlayerPrefab"
3. Scale: (0.5, 1.0, 0.5) - 사람 크기
4. Add Component → Simple Player Authoring
5. Add Component → Ghost Authoring Component
6. Ghost Authoring Component 설정:
   - Ghost Mode: **Owner Predicted**
   - Supported Ghost Mode: All
   - Optimization Mode: Dynamic
   - Default Ghost Mode: Owner Predicted

7. Assets/Prefabs/Network/ 폴더 생성
8. PlayerPrefab을 Prefabs/Network/ 폴더로 드래그 (Create Original Prefab)
9. Hierarchy에서 PlayerPrefab 삭제 (Prefab만 유지)

### 2.4 플레이어 스폰 시스템 작성

`Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs` 생성:
```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 클라이언트 연결 시 플레이어 스폰
/// Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerSpawnSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    protected override void OnUpdate()
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);

        // PlayerPrefab 가져오기
        Entity playerPrefab = Entity.Null;
        foreach (var prefab in SystemAPI.Query<RefRO<Prefab>>().WithAll<PlayerTag>())
        {
            playerPrefab = SystemAPI.GetComponent<Prefab>(prefab).Value;
            break;
        }

        if (playerPrefab == Entity.Null)
        {
            Debug.LogWarning("[PlayerSpawnSystem] PlayerPrefab not found!");
            return;
        }

        // 연결된 클라이언트 중 플레이어가 없는 경우
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithNone<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            // InGame 태그 추가 (스폰 완료 표시)
            ecb.AddComponent<NetworkStreamInGame>(entity);

            // 플레이어 스폰
            var player = ecb.Instantiate(playerPrefab);

            // 스폰 위치 설정 (2명이므로 좌/우 배치)
            // NetworkId는 1부터 시작하므로 -1 해서 0, 1로 만듦
            int playerIndex = id.ValueRO.Value - 1;
            float3 spawnPosition = new float3(playerIndex * 4f - 2f, 0.5f, 0f);
            // Player 0: x=-2, Player 1: x=2

            ecb.SetComponent(player, LocalTransform.FromPosition(spawnPosition));

            // 소유권 설정
            ecb.SetComponent(player, new GhostOwner { NetworkId = id.ValueRO.Value });

            Debug.Log($"[Server] Player spawned for NetworkId {id.ValueRO.Value} at {spawnPosition}");
        }
    }
}
```

### 2.5 씬 설정 업데이트

**NetworkTest.unity 씬 수정**:
1. Hierarchy에서 GameObject 생성
2. 이름: "PlayerPrefabReference"
3. Add Component → Entity Prefab Reference (또는 Prefab을 서브씬에 배치)

**또는 간단하게**:
- Project 창에서 PlayerPrefab을 Hierarchy로 드래그
- 위치: (0, 0, 0)
- **중요**: Disabled 상태로 설정 (스폰 시스템이 인스턴스화)

### 2.6 Multiplayer Play Mode로 테스트

1. Window → Multiplayer Play Mode
2. Virtual Players: **2**로 설정
3. Play 버튼 클릭
4. **기대 결과**:
   - Server World에 Player Entity 2개 생성
   - Client 1 화면: 큐브 2개 보임 (x=-2, x=2)
   - Client 2 화면: 큐브 2개 보임 (x=-2, x=2)
   - 콘솔: "[Server] Player spawned..." 메시지 2번

### ✅ STEP 2 완료 조건

- [ ] PlayerTag 컴포넌트 작성
- [ ] SimplePlayerAuthoring 작성
- [ ] PlayerPrefab 생성 (Ghost Authoring 설정)
- [ ] PlayerSpawnSystem 작성
- [ ] Multiplayer Play Mode에서 큐브 2개 스폰 확인
- [ ] 콘솔에 스폰 로그 2번 출력
- [ ] 컴파일 에러 없음

---

## STEP 3: 플레이어 이동 동기화 ⭐ (핵심 마일스톤)

**목표**: WASD 입력으로 플레이어 이동, 상대방 화면에서 동기화 확인

**예상 소요 시간**: 3-4시간

### 3.1 이동 관련 컴포넌트 추가

`Assets/Scripts/Components/Network/MovementSpeed.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct MovementSpeed : IComponentData
{
    [GhostField] public float Value;
}
```

### 3.2 Input Command 정의

`Assets/Scripts/Components/Network/PlayerInputCommand.cs` 생성:
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
    [GhostField] public float2 Movement;
}
```

### 3.3 SimplePlayerAuthoring에 컴포넌트 추가

`Assets/Scripts/Authoring/Network/SimplePlayerAuthoring.cs` 수정:
```csharp
using Unity.Entities;
using UnityEngine;

public class SimplePlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;

    class Baker : Baker<SimplePlayerAuthoring>
    {
        public override void Bake(SimplePlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInputCommand()); // Input 컴포넌트 추가
        }
    }
}
```

### 3.4 Input 수집 시스템 작성

`Assets/Scripts/Systems/Network/PlayerInputSystem.cs` 생성:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 클라이언트에서 입력 수집 및 Command 생성
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }

    protected override void OnUpdate()
    {
        // 내 NetworkId 가져오기
        if (!SystemAPI.HasSingleton<NetworkId>())
            return;

        var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;
        var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        // 입력 수집
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float2 inputMovement = new float2(horizontal, vertical);

        // 내 플레이어에게만 입력 적용
        foreach (var (inputCommand, ghostOwner, entity) in SystemAPI.Query<
                     RefRW<PlayerInputCommand>,
                     RefRO<GhostOwner>>()
                 .WithAll<PlayerTag>()
                 .WithEntityAccess())
        {
            // 내 플레이어인지 확인
            if (ghostOwner.ValueRO.NetworkId != myNetworkId)
                continue;

            // Input 설정
            inputCommand.ValueRW.Movement = inputMovement;
            inputCommand.ValueRW.Tick = currentTick;

            break; // 내 플레이어만
        }
    }
}
```

### 3.5 Movement 시스템 작성 (예측 지원)

`Assets/Scripts/Systems/Network/PlayerMovementSystem.cs` 생성:
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
            if (math.lengthsq(inputCommand.ValueRO.Movement) > 0.01f)
            {
                float3 movement = new float3(
                    inputCommand.ValueRO.Movement.x,
                    0f,
                    inputCommand.ValueRO.Movement.y
                );

                float3 moveDirection = math.normalize(movement);

                // 위치 업데이트
                transform.ValueRW.Position += moveDirection * speed.ValueRO.Value * deltaTime;
                transform.ValueRW.Position.y = 0.5f; // 바닥에서 0.5 높이 유지

                // 회전 업데이트 (이동 방향)
                quaternion targetRotation = quaternion.LookRotationSafe(moveDirection, math.up());
                transform.ValueRW.Rotation = math.slerp(
                    transform.ValueRW.Rotation,
                    targetRotation,
                    10f * deltaTime
                );
            }
        }
    }
}
```

### 3.6 PlayerPrefab 업데이트

**Unity Editor 작업**:
1. Assets/Prefabs/Network/PlayerPrefab 선택
2. Inspector에서 Simple Player Authoring 확인
3. Move Speed: **5** 설정
4. 저장

### 3.7 카메라 설정 (테스트용)

**NetworkTest.unity 씬**:
1. Main Camera 선택
2. Position: (0, 10, -10)
3. Rotation: (45, 0, 0)
4. 또는 Top View: Position (0, 20, 0), Rotation (90, 0, 0)

### 3.8 Multiplayer Play Mode로 테스트

1. Window → Multiplayer Play Mode
2. Virtual Players: **2**
3. Play 버튼 클릭
4. **테스트 시나리오**:
   - **Client 1**: WASD 키로 플레이어 이동
   - **Client 2 화면 확인**: Client 1의 플레이어가 움직이는지 확인
   - **Client 2**: WASD 키로 플레이어 이동
   - **Client 1 화면 확인**: Client 2의 플레이어가 움직이는지 확인

5. **기대 결과**:
   - ✅ 각 클라이언트에서 자기 플레이어만 조종 가능
   - ✅ 상대방 플레이어 움직임이 실시간 동기화
   - ✅ 부드러운 이동 (클라이언트 예측)
   - ✅ 큐브가 이동 방향으로 회전

### 3.9 디버깅 팁

**움직임이 안 보이는 경우**:
1. Window → Entities → Hierarchy
2. Client World 선택
3. Player Entity 선택
4. LocalTransform.Position 값이 변하는지 확인

**입력이 안 먹히는 경우**:
```csharp
// PlayerInputSystem에 로그 추가
Debug.Log($"[Client {myNetworkId}] Input: {inputMovement}");
```

**상대방 플레이어가 안 보이는 경우**:
- Ghost Authoring Component 설정 확인
- Netcode Settings → Ghost Snapshot Buffer Size 증가

### ✅ STEP 3 완료 조건 (핵심 마일스톤!)

- [ ] MovementSpeed, PlayerInputCommand 컴포넌트 작성
- [ ] PlayerInputSystem 작성 (Client 전용)
- [ ] PlayerMovementSystem 작성 (Predicted)
- [ ] SimplePlayerAuthoring에 컴포넌트 추가
- [ ] **Client 1에서 이동 시 Client 2 화면에서 보임**
- [ ] **Client 2에서 이동 시 Client 1 화면에서 보임**
- [ ] 부드러운 움직임 (끊김 없음)
- [ ] 컴파일 에러 없음

**🎉 이 단계 완료 시 멀티플레이 핵심 동작 확인!**

---

## STEP 4: 총알 발사 동기화

**목표**: 자동 발사 총알이 모든 클라이언트에 보임

**예상 소요 시간**: 2-3시간

### 4.1 총알 컴포넌트 정의

`Assets/Scripts/Components/Network/BulletTag.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletTag : IComponentData { }
```

`Assets/Scripts/Components/Network/BulletSpeed.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletSpeed : IComponentData
{
    [GhostField] public float Value;
}
```

`Assets/Scripts/Components/Network/BulletDirection.cs`:
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

`Assets/Scripts/Components/Network/BulletLifetime.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletLifetime : IComponentData
{
    [GhostField] public float RemainingTime;
}
```

### 4.2 총알 Authoring 작성

`Assets/Scripts/Authoring/Network/SimpleBulletAuthoring.cs`:
```csharp
using Unity.Entities;
using UnityEngine;

public class SimpleBulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 5f;

    class Baker : Baker<SimpleBulletAuthoring>
    {
        public override void Bake(SimpleBulletAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletDirection());
            AddComponent(entity, new BulletLifetime { RemainingTime = authoring.Lifetime });
        }
    }
}
```

### 4.3 총알 프리팹 생성

**Unity Editor 작업**:
1. 3D Object → Sphere 생성
2. 이름: "BulletPrefab"
3. Scale: (0.2, 0.2, 0.2)
4. Material: 밝은 색 (Yellow)
5. Add Component → Simple Bullet Authoring
6. Add Component → Ghost Authoring Component
7. Ghost Authoring 설정:
   - Ghost Mode: **Interpolated**
   - Optimization Mode: Dynamic
8. Assets/Prefabs/Network/BulletPrefab.prefab 생성
9. Hierarchy에서 삭제

### 4.4 AutoShoot 컴포넌트 추가

`Assets/Scripts/Components/Network/AutoShootConfig.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AutoShootConfig : IComponentData
{
    [GhostField] public float FireRate;
    [GhostField] public float TimeSinceLastShot;
    public Entity BulletPrefab; // Prefab은 동기화 안됨 (Server에만 필요)
}
```

### 4.5 SimplePlayerAuthoring에 AutoShoot 추가

```csharp
public class SimplePlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float FireRate = 0.5f; // 0.5초마다 발사
    public GameObject BulletPrefab;

    class Baker : Baker<SimplePlayerAuthoring>
    {
        public override void Bake(SimplePlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInputCommand());
            AddComponent(entity, new AutoShootConfig
            {
                FireRate = authoring.FireRate,
                TimeSinceLastShot = 0f,
                BulletPrefab = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}
```

### 4.6 AutoShoot 시스템 작성 (Server 전용)

`Assets/Scripts/Systems/Network/AutoShootSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 자동 발사 시스템 (Server에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (shootConfig, transform) in SystemAPI.Query<
                     RefRW<AutoShootConfig>,
                     RefRO<LocalTransform>>()
                 .WithAll<PlayerTag>())
        {
            // 타이머 증가
            shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

            // 발사 시간 체크
            if (shootConfig.ValueRW.TimeSinceLastShot >= shootConfig.ValueRO.FireRate)
            {
                shootConfig.ValueRW.TimeSinceLastShot = 0f;

                // 총알 생성
                var bullet = ecb.Instantiate(shootConfig.ValueRO.BulletPrefab);

                // 플레이어 위치에서 생성
                float3 spawnPos = transform.ValueRO.Position + new float3(0f, 0.5f, 0f);
                ecb.SetComponent(bullet, LocalTransform.FromPosition(spawnPos));

                // 위쪽으로 발사 (임시)
                ecb.SetComponent(bullet, new BulletDirection { Value = new float3(0f, 0f, 1f) });
            }
        }
    }
}
```

### 4.7 Bullet Movement 시스템 작성

`Assets/Scripts/Systems/Network/BulletMovementSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct BulletMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, speed, direction) in SystemAPI.Query<
                     RefRW<LocalTransform>,
                     RefRO<BulletSpeed>,
                     RefRO<BulletDirection>>()
                 .WithAll<BulletTag>())
        {
            // 이동
            transform.ValueRW.Position += direction.ValueRO.Value * speed.ValueRO.Value * deltaTime;
        }
    }
}
```

### 4.8 Bullet Lifetime 시스템 작성

`Assets/Scripts/Systems/Network/BulletLifetimeSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct BulletLifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (lifetime, entity) in SystemAPI.Query<RefRW<BulletLifetime>>()
                     .WithAll<BulletTag>()
                     .WithEntityAccess())
        {
            lifetime.ValueRW.RemainingTime -= deltaTime;

            if (lifetime.ValueRW.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
```

### 4.9 PlayerPrefab 업데이트

1. PlayerPrefab 선택
2. Bullet Prefab 필드에 BulletPrefab 드래그
3. Fire Rate: 0.5
4. 저장

### 4.10 씬에 BulletPrefab 참조 추가

NetworkTest.unity 씬에 BulletPrefab을 Disabled 상태로 배치 (또는 서브씬 사용)

### 4.11 테스트

1. Multiplayer Play Mode
2. Play
3. **기대 결과**:
   - 각 플레이어에서 0.5초마다 총알 발사
   - 총알이 위쪽(Z+)으로 이동
   - **양쪽 클라이언트 화면에 총알 보임**
   - 5초 후 총알 자동 소멸

### ✅ STEP 4 완료 조건

- [ ] Bullet 컴포넌트 4개 작성
- [ ] SimpleBulletAuthoring 작성
- [ ] BulletPrefab 생성 (Ghost Authoring)
- [ ] AutoShootConfig 추가
- [ ] AutoShootSystem 작성 (Server)
- [ ] BulletMovementSystem 작성 (Server)
- [ ] BulletLifetimeSystem 작성 (Server)
- [ ] **양쪽 클라이언트에서 총알 보임**
- [ ] 총알 움직임 동기화
- [ ] 컴파일 에러 없음

---

## STEP 5: 몬스터 스폰 및 추격 동기화

**목표**: 몬스터가 가장 가까운 플레이어를 추격

**예상 소요 시간**: 2-3시간

### 5.1 Enemy 컴포넌트 정의

`Assets/Scripts/Components/Network/EnemyTag.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyTag : IComponentData { }
```

`Assets/Scripts/Components/Network/EnemySpeed.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemySpeed : IComponentData
{
    [GhostField] public float Value;
}
```

### 5.2 Enemy Authoring 작성

`Assets/Scripts/Authoring/Network/SimpleEnemyAuthoring.cs`:
```csharp
using Unity.Entities;
using UnityEngine;

public class SimpleEnemyAuthoring : MonoBehaviour
{
    public float Speed = 3f;

    class Baker : Baker<SimpleEnemyAuthoring>
    {
        public override void Bake(SimpleEnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new EnemySpeed { Value = authoring.Speed });
        }
    }
}
```

### 5.3 Enemy 프리팹 생성

1. 3D Object → Capsule 생성
2. 이름: "EnemyPrefab"
3. Scale: (0.5, 1.0, 0.5)
4. Material: Red
5. Add Component → Simple Enemy Authoring
6. Add Component → Ghost Authoring Component (Interpolated)
7. Prefabs/Network/EnemyPrefab.prefab 생성

### 5.4 EnemySpawn 컴포넌트 정의

`Assets/Scripts/Components/Network/EnemySpawnConfig.cs`:
```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct EnemySpawnConfig : IComponentData
{
    public float SpawnInterval;
    public float TimeSinceLastSpawn;
    public Entity EnemyPrefab;
    public float SpawnRadius;
    public int MaxEnemies;
    public Random RandomGenerator;
}
```

### 5.5 EnemySpawn Authoring 작성

`Assets/Scripts/Authoring/Network/EnemySpawnAuthoring.cs`:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemySpawnAuthoring : MonoBehaviour
{
    public float SpawnInterval = 2f;
    public GameObject EnemyPrefab;
    public float SpawnRadius = 10f;
    public int MaxEnemies = 20;

    class Baker : Baker<EnemySpawnAuthoring>
    {
        public override void Bake(EnemySpawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new EnemySpawnConfig
            {
                SpawnInterval = authoring.SpawnInterval,
                TimeSinceLastSpawn = 0f,
                EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                SpawnRadius = authoring.SpawnRadius,
                MaxEnemies = authoring.MaxEnemies,
                RandomGenerator = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks)
            });
        }
    }
}
```

### 5.6 EnemySpawn 시스템 작성

`Assets/Scripts/Systems/Network/EnemySpawnSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct EnemySpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        // 플레이어 중심 위치 (평균)
        float3 centerPosition = float3.zero;
        int playerCount = 0;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            centerPosition += transform.ValueRO.Position;
            playerCount++;
        }

        if (playerCount == 0) return;
        centerPosition /= playerCount;

        foreach (var spawnConfig in SystemAPI.Query<RefRW<EnemySpawnConfig>>())
        {
            spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;

            if (spawnConfig.ValueRW.TimeSinceLastSpawn >= spawnConfig.ValueRO.SpawnInterval)
            {
                spawnConfig.ValueRW.TimeSinceLastSpawn = 0f;

                // 현재 적 수 체크
                int enemyCount = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount();
                if (enemyCount >= spawnConfig.ValueRO.MaxEnemies)
                    continue;

                // 원형 분포로 스폰
                float angle = spawnConfig.ValueRW.RandomGenerator.NextFloat(0f, math.PI * 2f);
                float distance = spawnConfig.ValueRW.RandomGenerator.NextFloat(
                    spawnConfig.ValueRO.SpawnRadius * 0.8f,
                    spawnConfig.ValueRO.SpawnRadius
                );

                float3 offset = new float3(
                    math.cos(angle) * distance,
                    0f,
                    math.sin(angle) * distance
                );

                float3 spawnPosition = centerPosition + offset;
                spawnPosition.y = 0.5f;

                // 적 생성
                var enemy = ecb.Instantiate(spawnConfig.ValueRO.EnemyPrefab);
                ecb.SetComponent(enemy, LocalTransform.FromPosition(spawnPosition));
            }
        }
    }
}
```

### 5.7 EnemyChase 시스템 작성

`Assets/Scripts/Systems/Network/EnemyChaseSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
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
            newPosition.y = 0.5f;

            transform.ValueRW.Position = newPosition;

            // 회전
            quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
            transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, targetRotation, 10f * deltaTime);
        }

        playerPositions.Dispose();
    }
}
```

### 5.8 씬에 EnemySpawner 추가

NetworkTest.unity:
1. GameObject 생성: "EnemySpawner"
2. Add Component → Enemy Spawn Authoring
3. 설정:
   - Spawn Interval: 2
   - Enemy Prefab: EnemyPrefab 드래그
   - Spawn Radius: 10
   - Max Enemies: 20

### 5.9 테스트

1. Multiplayer Play Mode
2. Play
3. **기대 결과**:
   - 2초마다 몬스터 스폰
   - 몬스터가 가장 가까운 플레이어 추격
   - **양쪽 클라이언트에서 몬스터 보임**
   - 플레이어가 멀어지면 다른 플레이어 추격

### ✅ STEP 5 완료 조건

- [ ] Enemy 컴포넌트 작성
- [ ] SimpleEnemyAuthoring 작성
- [ ] EnemyPrefab 생성
- [ ] EnemySpawnConfig, Authoring 작성
- [ ] EnemySpawnSystem 작성
- [ ] EnemyChaseSystem 작성 (다중 플레이어 지원)
- [ ] **몬스터가 양쪽 클라이언트에서 보임**
- [ ] **몬스터가 가까운 플레이어 추격**
- [ ] 컴파일 에러 없음

---

## STEP 6: 충돌 및 체력 동기화

**목표**: 총알-몬스터, 몬스터-플레이어 충돌 처리

**예상 소요 시간**: 2-3시간

### 6.1 체력 및 데미지 컴포넌트

`Assets/Scripts/Components/Network/PlayerHealth.cs`:
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

`Assets/Scripts/Components/Network/EnemyHealth.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyHealth : IComponentData
{
    [GhostField] public float Value;
}
```

`Assets/Scripts/Components/Network/DamageValue.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct DamageValue : IComponentData
{
    [GhostField] public float Value;
}
```

### 6.2 Authoring 업데이트

**SimplePlayerAuthoring**에 체력 추가:
```csharp
public float MaxHealth = 100f;

// Baker에서
AddComponent(entity, new PlayerHealth
{
    CurrentHealth = authoring.MaxHealth,
    MaxHealth = authoring.MaxHealth
});
```

**SimpleBulletAuthoring**에 데미지 추가:
```csharp
public float Damage = 25f;

// Baker에서
AddComponent(entity, new DamageValue { Value = authoring.Damage });
```

**SimpleEnemyAuthoring**에 체력 추가:
```csharp
public float Health = 100f;

// Baker에서
AddComponent(entity, new EnemyHealth { Value = authoring.Health });
```

### 6.3 BulletHit 시스템 작성

`Assets/Scripts/Systems/Network/BulletHitSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct BulletHitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        // 총알 위치 수집
        var bullets = new NativeList<BulletData>(Allocator.Temp);
        foreach (var (transform, damage, entity) in SystemAPI.Query<
                     RefRO<LocalTransform>,
                     RefRO<DamageValue>>()
                 .WithAll<BulletTag>()
                 .WithEntityAccess())
        {
            bullets.Add(new BulletData
            {
                Entity = entity,
                Position = transform.ValueRO.Position,
                Damage = damage.ValueRO.Value
            });
        }

        // 적과 충돌 체크
        foreach (var (transform, health, entity) in SystemAPI.Query<
                     RefRO<LocalTransform>,
                     RefRW<EnemyHealth>>()
                 .WithAll<EnemyTag>()
                 .WithEntityAccess())
        {
            float3 enemyPos = transform.ValueRO.Position;

            for (int i = 0; i < bullets.Length; i++)
            {
                float distSq = math.distancesq(enemyPos, bullets[i].Position);
                float hitRadius = 0.7f; // 총알(0.2) + 적(0.5)

                if (distSq < hitRadius * hitRadius)
                {
                    // 데미지 적용
                    health.ValueRW.Value -= bullets[i].Damage;

                    // 총알 삭제
                    ecb.DestroyEntity(bullets[i].Entity);

                    // 적 체력 0 이하면 삭제
                    if (health.ValueRO.Value <= 0f)
                    {
                        ecb.DestroyEntity(entity);
                    }

                    break;
                }
            }
        }

        bullets.Dispose();
    }

    private struct BulletData
    {
        public Entity Entity;
        public float3 Position;
        public float Damage;
    }
}
```

### 6.4 PlayerDamage 시스템 작성

`Assets/Scripts/Systems/Network/PlayerDamageSystem.cs`:
```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerDamageSystem : SystemBase
{
    private float damageCooldown = 0f;
    private const float COOLDOWN_TIME = 1f;
    private const float COLLISION_DAMAGE = 10f;

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        damageCooldown -= deltaTime;

        if (damageCooldown > 0f) return;

        // 플레이어-적 충돌 체크
        foreach (var (playerTransform, playerHealth) in SystemAPI.Query<
                     RefRO<LocalTransform>,
                     RefRW<PlayerHealth>>()
                 .WithAll<PlayerTag>())
        {
            float3 playerPos = playerTransform.ValueRO.Position;

            foreach (var enemyTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
            {
                float3 enemyPos = enemyTransform.ValueRO.Position;
                float distSq = math.distancesq(playerPos, enemyPos);
                float hitRadius = 1.0f; // 플레이어(0.5) + 적(0.5)

                if (distSq < hitRadius * hitRadius)
                {
                    // 데미지 적용
                    playerHealth.ValueRW.CurrentHealth -= COLLISION_DAMAGE;
                    damageCooldown = COOLDOWN_TIME;
                    return;
                }
            }
        }
    }
}
```

### 6.5 테스트

1. Multiplayer Play Mode
2. Play
3. **테스트 시나리오**:
   - 총알이 몬스터에 맞으면 몬스터 삭제
   - 몬스터가 플레이어에 닿으면 체력 감소 (Window → Entities → Hierarchy에서 확인)

### ✅ STEP 6 완료 조건

- [ ] PlayerHealth, EnemyHealth, DamageValue 작성
- [ ] Authoring 클래스에 체력/데미지 추가
- [ ] BulletHitSystem 작성 (총알-몬스터 충돌)
- [ ] PlayerDamageSystem 작성 (몬스터-플레이어 충돌)
- [ ] **총알이 몬스터 처치**
- [ ] **몬스터가 플레이어 데미지**
- [ ] **체력 변화가 동기화** (Entity Debugger 확인)
- [ ] 컴파일 에러 없음

---

## STEP 7: UI 및 게임 상태 동기화

**목표**: 생존 시간, 킬 카운트, 게임 오버 UI

**예상 소요 시간**: 2-3시간

### 7.1 GameStats 컴포넌트

`Assets/Scripts/Components/Network/GameStats.cs`:
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

### 7.2 GameStats Authoring

`Assets/Scripts/Authoring/Network/GameStatsAuthoring.cs`:
```csharp
using Unity.Entities;
using UnityEngine;

public class GameStatsAuthoring : MonoBehaviour
{
    class Baker : Baker<GameStatsAuthoring>
    {
        public override void Bake(GameStatsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GameStats
            {
                SurvivalTime = 0f,
                KillCount = 0
            });
        }
    }
}
```

### 7.3 GameStats 시스템

`Assets/Scripts/Systems/Network/GameStatsSystem.cs`:
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct GameStatsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime += deltaTime;
        }
    }
}
```

### 7.4 BulletHitSystem에 킬 카운트 추가

```csharp
// 적 체력 0 이하면 삭제
if (health.ValueRO.Value <= 0f)
{
    ecb.DestroyEntity(entity);

    // 킬 카운트 증가
    foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
    {
        stats.ValueRW.KillCount++;
        break;
    }
}
```

### 7.5 UIManager MonoBehaviour

`Assets/Scripts/UI/SimpleUIManager.cs`:
```csharp
using TMPro;
using UnityEngine;

public class SimpleUIManager : MonoBehaviour
{
    [Header("HUD")]
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI SurvivalTimeText;
    public TextMeshProUGUI KillCountText;

    public void UpdateHealth(float current, float max)
    {
        if (HealthText != null)
            HealthText.text = $"Health: {current:F0}/{max:F0}";
    }

    public void UpdateSurvivalTime(float time)
    {
        if (SurvivalTimeText != null)
            SurvivalTimeText.text = $"Time: {time:F1}s";
    }

    public void UpdateKillCount(int count)
    {
        if (KillCountText != null)
            KillCountText.text = $"Kills: {count}";
    }
}
```

### 7.6 UIUpdate 시스템

`Assets/Scripts/Systems/Network/UIUpdateSystem.cs`:
```csharp
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    private SimpleUIManager uiManager;

    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }

    protected override void OnStartRunning()
    {
        uiManager = Object.FindFirstObjectByType<SimpleUIManager>();
    }

    protected override void OnUpdate()
    {
        if (uiManager == null) return;

        // 내 NetworkId
        if (!SystemAPI.HasSingleton<NetworkId>())
            return;

        var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;

        // 내 플레이어 체력
        foreach (var (health, ghostOwner) in SystemAPI.Query<RefRO<PlayerHealth>, RefRO<GhostOwner>>()
                     .WithAll<PlayerTag>())
        {
            if (ghostOwner.ValueRO.NetworkId != myNetworkId)
                continue;

            uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            break;
        }

        // 게임 통계
        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            uiManager.UpdateSurvivalTime(stats.ValueRO.SurvivalTime);
            uiManager.UpdateKillCount(stats.ValueRO.KillCount);
            break;
        }
    }
}
```

### 7.7 UI 캔버스 생성

NetworkTest.unity:
1. UI → Canvas 생성
2. Canvas 설정: Screen Space - Overlay
3. TextMeshPro 3개 생성:
   - HealthText: 좌상단
   - SurvivalTimeText: 중앙 상단
   - KillCountText: 우상단
4. Canvas에 Add Component → Simple UI Manager
5. 참조 연결

### 7.8 씬에 GameStats 추가

GameObject 생성: "GameStats"
Add Component → Game Stats Authoring

### 7.9 테스트

1. Multiplayer Play Mode
2. Play
3. **기대 결과**:
   - 체력 표시 (자신의 플레이어)
   - 생존 시간 증가
   - 몬스터 처치 시 킬 카운트 증가
   - **양쪽 클라이언트 UI 정상 작동**

### ✅ STEP 7 완료 조건

- [ ] GameStats 컴포넌트 작성
- [ ] GameStatsSystem 작성
- [ ] BulletHitSystem에 킬 카운트 추가
- [ ] SimpleUIManager 작성
- [ ] UIUpdateSystem 작성 (Client 전용)
- [ ] UI 캔버스 생성 및 설정
- [ ] **UI가 실시간 업데이트**
- [ ] **각 클라이언트 자기 체력만 표시**
- [ ] 컴파일 에러 없음

---

## 전체 테스트 및 검증

### 최종 테스트 시나리오

**Multiplayer Play Mode** (Virtual Players: 2):

1. **접속 테스트**:
   - ✅ Server/Client 연결 확인
   - ✅ 플레이어 2명 스폰

2. **이동 테스트**:
   - ✅ Client 1 이동 → Client 2 화면에서 보임
   - ✅ Client 2 이동 → Client 1 화면에서 보임
   - ✅ 부드러운 움직임

3. **전투 테스트**:
   - ✅ 총알 발사 동기화
   - ✅ 몬스터 스폰 및 추격
   - ✅ 총알-몬스터 충돌
   - ✅ 몬스터-플레이어 충돌

4. **UI 테스트**:
   - ✅ 체력 표시
   - ✅ 생존 시간 증가
   - ✅ 킬 카운트 증가

### 성능 확인

- FPS: 60 이상 유지
- Ping: < 50ms (로컬)
- Ghost 동기화 정상

---

## 다음 단계: 기존 게임과 통합

### STEP 8: 기존 시스템 통합

**지금까지 만든 것**: 간단한 프로토타입

**해야 할 일**: 기존 Survival Shooter 코드와 통합

1. **기존 컴포넌트에 Ghost 어트리뷰트 추가**
2. **기존 시스템에 WorldSystemFilter 추가**
3. **Authoring 클래스 통합**
4. **UI 통합**
5. **웨이브 시스템 통합**

**예상 소요 시간**: 4-6시간

---

## 총 예상 소요 시간

| Step | 내용 | 시간 | 누적 |
|------|------|------|------|
| **STEP 1** | 환경 설정 및 연결 | 1-2h | 1-2h |
| **STEP 2** | 플레이어 스폰 | 2-3h | 3-5h |
| **STEP 3** | 이동 동기화 ⭐ | 3-4h | 6-9h |
| **STEP 4** | 총알 동기화 | 2-3h | 8-12h |
| **STEP 5** | 몬스터 동기화 | 2-3h | 10-15h |
| **STEP 6** | 충돌 동기화 | 2-3h | 12-18h |
| **STEP 7** | UI 동기화 | 2-3h | 14-21h |
| **STEP 8** | 기존 코드 통합 | 4-6h | 18-27h |

**총합**: 18-27시간 (3-5일)

**버퍼**: +4-6시간 (문제 해결)

**최종**: 22-33시간 (4-6일)

---

## 장점: 점진적 통합 방식

✅ **각 단계마다 동작 확인** → 문제 조기 발견
✅ **작은 단위로 구현** → 디버깅 쉬움
✅ **마일스톤 명확** → 진행 상황 체감
✅ **롤백 가능** → 이전 단계로 복구 쉬움
✅ **학습 효과** → Netcode 이해도 향상

---

**🎯 STEP 3 완료 시 멀티플레이 핵심 동작 확인 가능!**

이후 단계는 기능 추가일 뿐, 네트워크 동기화의 핵심은 STEP 3에서 완성됩니다.
