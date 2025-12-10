# 🎯 NetCode 멀티플레이 구현 계획 (NetcodeSamples 패턴 기반)

> **기반**: NetcodeSamples 05_SpawnPlayer 검증된 패턴
> **작성일**: 2025-12-10
> **목표**: 현재 싱글플레이 게임을 검증된 멀티플레이어 패턴으로 점진적 전환
> **참고 플랜**: `netcode_multiplayer_plan_incremental.md`, `netcode_multiplayer_plan_incremental_v2.md`

---

## 📋 핵심 원칙

### 1. NetcodeSamples 05_SpawnPlayer 패턴 준수
- ✅ **Spawner 싱글톤**: Prefab을 매 프레임 쿼리하지 않고 한 번에 참조
- ✅ **IInputComponentData**: 자동 네트워크 직렬화
- ✅ **CommandTarget**: 입력 라우팅 명확화
- ✅ **LinkedEntityGroup**: 생명주기 자동 관리
- ✅ **GhostOwnerIsLocal**: 로컬 플레이어 필터링
- ✅ **시스템 그룹**: GhostInputSystemGroup, PredictedSimulationSystemGroup 사용

### 2. 점진적 통합 전략
- ✅ **한 번에 하나씩**: 각 Phase 완료 후 반드시 테스트
- ✅ **검증 가능**: 각 단계마다 눈으로 확인 가능한 결과
- ✅ **롤백 가능**: 문제 발생 시 이전 단계로 복구

### 3. 현재 프로젝트 문제점 (v2 플랜 분석 결과)
- ❌ **Spawner 없음** - Prefab을 매 프레임 쿼리로 찾고 있음
- ❌ **IInputComponentData 미사용** - 일반 `IComponentData` 사용 중
- ❌ **CommandTarget 미설정** - 입력이 어느 플레이어로 갈지 지정 안 됨
- ❌ **LinkedEntityGroup 미사용** - 연결 끊김 시 자동 정리 안 됨
- ❌ **GhostOwnerIsLocal 미활용** - 로컬 플레이어 필터링 제대로 안 됨
- ❌ **잘못된 입력 시스템** - `InitializationSystemGroup`에서 모든 플레이어에 입력 적용
- ❌ **GhostInputSystemGroup 미사용** - 입력 수집을 잘못된 그룹에서 실행

---

## 🚀 전체 진행 단계 (Phase 1-10)

| Phase | 작업 | 예상 시간 | 검증 방법 |
|-------|------|-----------|-----------|
| **1** | 환경 설정 + 기본 연결 | 1-2h | ✅ 콘솔에 "Connected" 로그 |
| **2** | Spawner 싱글톤 생성 | 15분 | ✅ Prefab 참조 확인 |
| **3** | IInputComponentData로 변경 | 10분 | ✅ 컴파일 성공 |
| **4** | 입력 수집 시스템 재작성 | 20분 | ✅ 입력 로그 확인 |
| **5** | PlayerSpawnSystem 재작성 | 25분 | ✅ GhostOwner 설정 확인 |
| **6** | 입력 처리 시스템 생성 | 20분 | ✅ **플레이어 움직임!** ⭐ |
| **7** | AutoShoot Network 대응 | 20분 | ✅ 발사 동기화 |
| **8** | Bullet 동기화 | 15분 | ✅ 총알 보임 |
| **9** | Enemy 스폰 및 추격 | 25분 | ✅ Enemy 동작 |
| **10** | 충돌 처리 + UI 동기화 | 30분 | ✅ 완전 동작 |

**총 예상 시간**: 약 3-4시간

---

## 📂 디렉토리 구조 (NetcodeSamples 패턴 준수)

```
Assets/Scripts/
├── Components/
│   ├── Network/
│   │   ├── Spawner.cs                    ← Phase 2 (싱글톤)
│   │   ├── PlayerSpawned.cs              ← Phase 5 (마커)
│   │   ├── ConnectionOwner.cs            ← Phase 5 (역참조)
│   │   └── EnableMultiplayer.cs          ← Phase 1 (기능 플래그)
│   ├── PlayerInput.cs                     ← Phase 3 (IInputComponentData로 변경)
│   └── (기존 컴포넌트들...)
│
├── Authoring/
│   ├── Network/
│   │   ├── SpawnerAuthoring.cs           ← Phase 2
│   │   └── EnableMultiplayerAuthoring.cs ← Phase 1
│   └── (기존 Authoring들...)
│
└── Systems/
    ├── Network/
    │   ├── SimpleNetworkBootstrap.cs     ← Phase 1 (연결 테스트)
    │   ├── ConnectionDebugSystem.cs      ← Phase 1 (디버깅)
    │   ├── GatherPlayerInputSystem.cs    ← Phase 4 (입력 수집)
    │   ├── ProcessPlayerInputSystem.cs   ← Phase 6 (입력 처리)
    │   ├── PlayerSpawnSystem.cs          ← Phase 5 (스폰 로직)
    │   └── (이후 Phase 시스템들...)
    └── (기존 Systems...)
```

---

## 🚀 Phase 1: 환경 설정 및 기본 연결 (1-2h)

### 목표
- Netcode 패키지 설치
- Server/Client 연결 확인
- Multiplayer Play Mode 설치

### 작업 내용

#### 1.1 Netcode 패키지 설치

**`Packages/manifest.json` 수정**:
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
- Unity Editor 재시작
- Window → Package Manager에서 Netcode for Entities 1.4.1 확인
- Console에 에러 없음

---

#### 1.2 Multiplayer Play Mode 설치

**설치 방법**:
1. Window → Package Manager
2. Unity Registry
3. "Multiplayer Play Mode" 검색
4. Install

**장점**: 하나의 Editor에서 Server + Client 2개 동시 실행 가능

---

#### 1.3 테스트 씬 생성

**새 씬**: `Assets/Scenes/NetworkTest.unity`
- Main Camera (Position: 0, 10, -10 / Rotation: 45, 0, 0)
- Directional Light

---

#### 1.4 EnableMultiplayer 컴포넌트 생성

**파일**: `Assets/Scripts/Components/Network/EnableMultiplayer.cs`

```csharp
using Unity.Entities;

/// <summary>
/// 멀티플레이 기능 활성화 마커 (NetcodeSamples의 EnableSpawnPlayer 패턴)
/// </summary>
public struct EnableMultiplayer : IComponentData { }
```

**파일**: `Assets/Scripts/Authoring/Network/EnableMultiplayerAuthoring.cs`

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 멀티플레이 기능 활성화
/// </summary>
public class EnableMultiplayerAuthoring : MonoBehaviour
{
    class Baker : Baker<EnableMultiplayerAuthoring>
    {
        public override void Bake(EnableMultiplayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<EnableMultiplayer>(entity);
        }
    }
}
```

---

#### 1.5 Bootstrap 스크립트 작성

**파일**: `Assets/Scripts/Systems/Network/SimpleNetworkBootstrap.cs`

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

---

#### 1.6 연결 확인 시스템

**파일**: `Assets/Scripts/Systems/Network/ConnectionDebugSystem.cs`

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

---

#### 1.7 씬 설정

**NetworkTest.unity 씬 설정**:
1. GameObject 생성: "NetworkManager"
2. Add Component → Simple Network Bootstrap
3. Port: 7979
4. GameObject 생성: "EnableMultiplayer"
5. Add Component → Enable Multiplayer Authoring
6. 씬 저장

---

### ✅ Phase 1 테스트

#### 실행 방법
1. NetworkTest.unity 씬 열기
2. Play 버튼 클릭

#### 기대 결과 (Console 로그)
```
[Server] Listening on port 7979
[Client] Connecting to 127.0.0.1:7979
[Server] Client connected: NetworkId = 1
[Client] Connected to server: My NetworkId = 1
```

#### 검증 포인트
- [ ] Netcode for Entities 1.4.1 설치 완료
- [ ] Multiplayer Play Mode 설치
- [ ] NetworkTest.unity 씬 생성
- [ ] Console에 연결 로그 4줄 출력
- [ ] 컴파일 에러 없음

---

## 🚀 Phase 2: Spawner 싱글톤 생성 (15분)

### 목표
- Prefab을 매 프레임 쿼리하지 않고 Spawner로 한 번에 참조
- NetcodeSamples 05_SpawnPlayer의 Spawner 패턴 정확히 구현

### 작업 내용

#### 2.1 Spawner 컴포넌트 생성

**파일**: `Assets/Scripts/Components/Network/Spawner.cs`

```csharp
using Unity.Entities;

/// <summary>
/// 플레이어 Prefab을 참조하는 싱글톤 (NetcodeSamples 패턴)
/// </summary>
public struct Spawner : IComponentData
{
    public Entity Player;
}
```

---

#### 2.2 SpawnerAuthoring 생성

**파일**: `Assets/Scripts/Authoring/Network/SpawnerAuthoring.cs`

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Spawner 싱글톤 Authoring (NetcodeSamples 패턴)
/// </summary>
public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject PlayerPrefab;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Spawner
            {
                Player = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}
```

---

#### 2.3 씬에 Spawner 추가

**Unity Editor 작업**:
1. NetworkTest.unity 씬 열기
2. GameObject 생성: "Spawner"
3. Add Component → Spawner Authoring
4. Player Prefab 필드에 `Assets/Prefabs/Player.prefab` 드래그
5. Ctrl+S로 저장

---

### ✅ Phase 2 테스트

#### 실행 방법
1. NetworkTest.unity 씬 열기
2. Play 버튼 클릭

#### 기대 결과
- [ ] Spawner GameObject가 씬에 존재
- [ ] PlayerPrefab 필드가 할당됨
- [ ] Play 시 "Spawner not found!" 경고 없음

---

## 🚀 Phase 3: IInputComponentData로 변경 (10분)

### 목표
- 네트워크 Input 버퍼 자동 생성
- `IComponentData` → `IInputComponentData`
- NetcodeSamples의 PlayerInput 패턴 정확히 구현

### 작업 내용

#### 3.1 PlayerInput.cs 수정

**파일**: `Assets/Scripts/Components/PlayerInput.cs`

**전체 내용 교체**:

```csharp
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 입력 - IInputComponentData로 자동 네트워크 전송
/// (NetcodeSamples 05_SpawnPlayer 패턴)
/// </summary>
public struct PlayerInput : IInputComponentData
{
    public int Horizontal;
    public int Vertical;
    public InputEvent Fire;  // 발사 버튼 (Phase 7에서 사용)
}
```

**중요 변경점**:
- `IComponentData` → `IInputComponentData`
- `float2 Movement` → `int Horizontal, int Vertical` (NetcodeSamples 패턴)
- `InputEvent Fire` 추가 (일회성 입력 이벤트)

---

#### 3.2 PlayerAuthoring.cs의 Baker 수정

**파일**: `Assets/Scripts/Authoring/PlayerAuthoring.cs`

**Baker 부분 수정**:

```csharp
class Baker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

        // 기존 컴포넌트들...

        // PlayerInput 추가 (기본값으로 초기화)
        AddComponent<PlayerInput>(entity);
    }
}
```

---

### ✅ Phase 3 테스트

#### 실행 방법
1. 컴파일 에러 없는지 확인 (Console 창)
2. Play 버튼 클릭

#### 기대 결과
- [ ] ✅ 컴파일 에러 없음
- [ ] ✅ 플레이어 정상 스폰
- [ ] ⚠️ 입력은 아직 작동 안 함 (정상)

---

## 🚀 Phase 4: 입력 수집 시스템 재작성 (20분)

### 목표
- GhostInputSystemGroup에서 입력 수집 (NetcodeSamples 패턴)
- GhostOwnerIsLocal 태그로 로컬 플레이어만 입력
- 05_SpawnPlayer의 GatherAutoCommandsSystem 패턴 준수

### 작업 내용

#### 4.1 기존 PlayerInputSystem.cs 삭제 (있다면)

**파일 삭제**: `Assets/Scripts/Systems/PlayerInputSystem.cs`

⚠️ **주의**: Unity Editor에서 파일 삭제 시 .meta 파일도 자동 삭제됨

---

#### 4.2 새로운 GatherPlayerInputSystem.cs 생성

**파일**: `Assets/Scripts/Systems/Network/GatherPlayerInputSystem.cs`

```csharp
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 클라이언트에서 입력 수집 (GhostInputSystemGroup)
/// NetcodeSamples 05_SpawnPlayer의 GatherAutoCommandsSystem 패턴
/// </summary>
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GatherPlayerInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
        RequireForUpdate<EnableMultiplayer>();
    }

    protected override void OnUpdate()
    {
        // 입력 읽기 (UnityEngine.Input 사용)
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);
        bool up = Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.S);
        bool fire = Input.GetKeyDown(KeyCode.Space);

        // GhostOwnerIsLocal 태그로 로컬 플레이어만 필터링
        foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            input.ValueRW = default;  // 초기화

            if (fire) input.ValueRW.Fire.Set();
            if (left) input.ValueRW.Horizontal -= 1;
            if (right) input.ValueRW.Horizontal += 1;
            if (down) input.ValueRW.Vertical -= 1;
            if (up) input.ValueRW.Vertical += 1;

            Debug.Log($"[Client Input] H:{input.ValueRW.Horizontal}, V:{input.ValueRW.Vertical}");
        }
    }
}
```

**핵심 포인트**:
- `[UpdateInGroup(typeof(GhostInputSystemGroup))]`: 입력 수집 그룹
- `WithAll<GhostOwnerIsLocal>()`: 로컬 플레이어만 입력 (Netcode 자동 추가)
- `input.ValueRW = default`: 매 프레임 초기화 (NetcodeSamples 패턴)

---

### ✅ Phase 4 테스트

#### 실행 방법
1. Play 버튼 클릭
2. **WASD 키** 눌러보기

#### 기대 결과 (Console 로그)
```
[Client Input] H:0, V:1   // W 키
[Client Input] H:-1, V:0  // A 키
[Client Input] H:1, V:0   // D 키
[Client Input] H:0, V:-1  // S 키
```

#### 검증 포인트
- [ ] ✅ WASD 키 입력 시 콘솔에 로그 출력
- [ ] ⚠️ 플레이어는 아직 안 움직임 (정상)
- [ ] ✅ "GhostOwnerIsLocal" 필터링 작동

---

## 🚀 Phase 5: PlayerSpawnSystem 완전 재작성 (25분)

### 목표
- NetcodeSamples 05_SpawnPlayer 패턴과 완전히 동일하게 구현
- CommandTarget, LinkedEntityGroup 설정
- 호스트 마이그레이션 지원

### 작업 내용

#### 5.1 필요한 컴포넌트 생성

**파일 1**: `Assets/Scripts/Components/Network/PlayerSpawned.cs`

```csharp
using Unity.Entities;

/// <summary>
/// 플레이어 스폰 완료 마커 (연결 Entity에 부착)
/// </summary>
public struct PlayerSpawned : IComponentData { }
```

**파일 2**: `Assets/Scripts/Components/Network/ConnectionOwner.cs`

```csharp
using Unity.Entities;

/// <summary>
/// 플레이어 Entity에서 연결 Entity 역참조
/// </summary>
public struct ConnectionOwner : IComponentData
{
    public Entity Entity;
}
```

---

#### 5.2 PlayerSpawnSystem.cs 완전 교체

**파일**: `Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs`

**전체 내용 교체**:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 클라이언트 연결 시 플레이어 스폰 (NetcodeSamples 05_SpawnPlayer 패턴)
/// Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct PlayerSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Spawner>();
        state.RequireForUpdate<EnableMultiplayer>();
        UnityEngine.Debug.Log("[PlayerSpawnSystem] OnCreate - Waiting for Spawner");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var prefab = SystemAPI.GetSingleton<Spawner>().Player;

        // 새로 연결된 플레이어 찾기 (PlayerSpawned 태그 없음)
        foreach (var (networkId, entity) in
                 SystemAPI.Query<RefRO<NetworkId>>()
                     .WithNone<PlayerSpawned>()
                     .WithEntityAccess())
        {
            UnityEngine.Debug.Log($"[PlayerSpawnSystem] Spawning player for NetworkId {networkId.ValueRO.Value}");

            // 1. 플레이어 Entity 생성
            var player = state.EntityManager.Instantiate(prefab);

            // 2. 스폰 위치 설정 (NetcodeSamples 패턴)
            var spawnPos = new float3(networkId.ValueRO.Value * 4f - 2f, 0.5f, 0f);
            state.EntityManager.SetComponentData(player, LocalTransform.FromPosition(spawnPos));

            // 3. GhostOwner 설정 (누구의 Ghost인지)
            state.EntityManager.SetComponentData(player, new GhostOwner
            {
                NetworkId = networkId.ValueRO.Value
            });

            // 4. CommandTarget 설정 (Input이 이 Entity로 감)
            state.EntityManager.SetComponentData(entity, new CommandTarget
            {
                targetEntity = player
            });

            // 5. LinkedEntityGroup 설정 (연결 끊김 시 자동 삭제)
            state.EntityManager.GetBuffer<LinkedEntityGroup>(entity)
                .Add(new LinkedEntityGroup { Value = player });

            // 6. 역참조 설정 (플레이어 → 연결)
            state.EntityManager.AddComponentData(player, new ConnectionOwner
            {
                Entity = entity
            });

            // 7. 마커 추가 (중복 스폰 방지)
            state.EntityManager.AddComponent<PlayerSpawned>(entity);

            UnityEngine.Debug.Log($"[Server] Player spawned at {spawnPos}, GhostOwner={networkId.ValueRO.Value}");
        }
    }
}
```

**7단계 스폰 프로세스 (NetcodeSamples 패턴)**:
1. **Entity 생성**: Prefab에서 인스턴스화
2. **위치 설정**: NetworkId 기반으로 겹치지 않게 배치
3. **GhostOwner**: 소유권 설정 → `GhostOwnerIsLocal` 자동 추가
4. **CommandTarget**: 입력 라우팅 설정
5. **LinkedEntityGroup**: 생명주기 관리
6. **ConnectionOwner**: 역참조 (플레이어 → 연결)
7. **PlayerSpawned**: 중복 스폰 방지 마커

---

### ✅ Phase 5 테스트

#### 실행 방법
1. Play 버튼 클릭
2. WASD 키 눌러보기

#### 기대 결과 (Console 로그)
```
[PlayerSpawnSystem] OnCreate - Waiting for Spawner
[PlayerSpawnSystem] Spawning player for NetworkId 1
[Server] Player spawned at (-2, 0.5, 0), GhostOwner=1
[Client Input] H:1, V:0  // 입력은 수집되지만 아직 안 움직임
```

#### 검증 포인트
- [ ] ✅ 플레이어 스폰 로그 출력
- [ ] ✅ GhostOwner 설정 로그
- [ ] ✅ 입력 수집 로그 (WASD)
- [ ] ⚠️ 플레이어는 아직 안 움직임 (정상)

#### Entities Hierarchy 검증
**Window → Entities → Hierarchy**

**ServerWorld 확인**:
1. Player Entity 선택
2. Inspector에서 확인:
   - `GhostOwner` → NetworkId = 1
   - `ConnectionOwner` → Entity = (Connection Entity)

**ClientWorld 확인**:
1. NetworkConnection Entity 선택
2. Inspector에서 확인:
   - `CommandTarget` → targetEntity = (Player Entity)
   - `LinkedEntityGroup` → Player 포함
   - `GhostOwnerIsLocal` 태그 존재 ⭐

---

## 🚀 Phase 6: 입력 처리 시스템 생성 (20분) ⭐

### 목표
- 예측 시뮬레이션에서 입력을 실제 움직임으로 변환
- **플레이어가 드디어 움직임!** 🎉
- NetcodeSamples의 ProcessAutoCommandsSystem 패턴 구현

### 작업 내용

#### 6.1 ProcessPlayerInputSystem.cs 생성

**파일**: `Assets/Scripts/Systems/Network/ProcessPlayerInputSystem.cs`

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 입력을 실제 움직임으로 변환 (예측 시뮬레이션)
/// NetcodeSamples 05_SpawnPlayer의 ProcessAutoCommandsSystem 패턴
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProcessPlayerInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInput>();
        state.RequireForUpdate<EnableMultiplayer>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (input, transform, speed) in
                 SystemAPI.Query<RefRO<PlayerInput>, RefRW<LocalTransform>, RefRO<MovementSpeed>>()
                     .WithAll<Simulate>())  // Netcode 예측 플래그
        {
            // 입력 기반 이동
            if (input.ValueRO.Horizontal != 0 || input.ValueRO.Vertical != 0)
            {
                float3 movement = new float3(
                    input.ValueRO.Horizontal,
                    0,
                    input.ValueRO.Vertical
                );
                movement = math.normalizesafe(movement) * speed.ValueRO.Value * deltaTime;
                transform.ValueRW.Position += movement;
                transform.ValueRW.Position.y = 0.5f;  // 높이 고정

                // 이동 방향으로 회전
                if (math.lengthsq(movement) > 0.01f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(movement, math.up());
                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRW.Rotation,
                        targetRotation,
                        10f * deltaTime
                    );
                }
            }

            // 발사 처리 (Phase 7에서 구현)
            if (input.ValueRO.Fire.IsSet)
            {
                // TODO: 발사 로직
            }
        }
    }
}
```

**핵심 포인트**:
- `[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]`: 예측 시뮬레이션 그룹
- `WithAll<Simulate>()`: Netcode가 자동으로 예측 가능한 Entity에만 추가
- `ref var transform = ref transformRef.ValueRW`: 참조로 접근 (복사 방지)

---

### ✅ Phase 6 테스트 - 🎉 첫 움직임!

#### 실행 방법
1. Play 버튼 클릭
2. **WASD 키로 플레이어 이동**

#### 기대 결과
- [ ] ✅ **플레이어가 움직임!** 🎉
- [ ] ✅ WASD 키 입력에 반응
- [ ] ✅ 이동 방향으로 회전
- [ ] ✅ 부드러운 움직임

#### Multiplayer Play Mode 테스트 (선택사항)
1. **Window → Multiplayer Play Mode**
2. Virtual Players: 2
3. Play
4. 각 클라이언트 창에서 WASD로 이동
5. **상대방 플레이어 움직임도 보임!** ⭐

---

## 🎯 Phase 1-6 완료 체크리스트

### 필수 검증 항목
- [ ] **Phase 1**: Server/Client 연결 확인
- [ ] **Phase 2**: Spawner 로그 확인, 플레이어 스폰
- [ ] **Phase 3**: 컴파일 성공
- [ ] **Phase 4**: 입력 수집 로그 출력
- [ ] **Phase 5**: GhostOwner, CommandTarget 설정 확인
- [ ] **Phase 6**: **플레이어 움직임 확인** ⭐

### 코어 기능 검증
- [ ] 플레이어 스폰 (서버)
- [ ] 입력 수집 (클라이언트)
- [ ] 입력 → 움직임 변환 (예측 시뮬레이션)
- [ ] GhostOwnerIsLocal 필터링
- [ ] CommandTarget 라우팅

### 파일 체크리스트

**새로 생성한 파일** (10개):
1. ✅ `Components/Network/EnableMultiplayer.cs`
2. ✅ `Components/Network/Spawner.cs`
3. ✅ `Components/Network/PlayerSpawned.cs`
4. ✅ `Components/Network/ConnectionOwner.cs`
5. ✅ `Authoring/Network/EnableMultiplayerAuthoring.cs`
6. ✅ `Authoring/Network/SpawnerAuthoring.cs`
7. ✅ `Systems/Network/SimpleNetworkBootstrap.cs`
8. ✅ `Systems/Network/ConnectionDebugSystem.cs`
9. ✅ `Systems/Network/GatherPlayerInputSystem.cs`
10. ✅ `Systems/Network/ProcessPlayerInputSystem.cs`

**수정한 파일** (3개):
1. ✅ `Components/PlayerInput.cs` (IInputComponentData)
2. ✅ `Authoring/PlayerAuthoring.cs` (Baker 수정)
3. ✅ `Systems/Network/PlayerSpawnSystem.cs` (완전 재작성)

---

## 🚀 Phase 7: AutoShoot 시스템 Network 대응 (20분)

### 목표
- 기존 AutoShoot 시스템을 네트워크 환경에 맞게 수정
- `InputEvent.Fire` 사용하여 발사 트리거
- 서버에서만 총알 생성 (Server Authoritative)

### 작업 내용

#### 7.1 AutoShootConfig 수정 (GhostComponent 추가)

**파일**: `Assets/Scripts/Components/AutoShootConfig.cs`

```csharp
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 자동 발사 설정 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AutoShootConfig : IComponentData
{
    [GhostField] public float Interval;      // 발사 간격
    [GhostField] public float Timer;         // 현재 타이머
    public Entity BulletPrefab;               // 총알 프리팹 (서버만 필요)
}
```

**변경점**:
- `[GhostComponent]`: 네트워크 동기화 지정
- `[GhostField]`: Interval, Timer만 동기화 (Prefab은 서버만)

---

#### 7.2 AutoShootSystem 수정 (Server 전용)

**파일**: `Assets/Scripts/Systems/AutoShootSystem.cs`

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
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AutoShootConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (shootConfig, transform) in
                 SystemAPI.Query<RefRW<AutoShootConfig>, RefRO<LocalTransform>>()
                     .WithAll<Simulate>())
        {
            // 타이머 증가 (직접 접근 - 복사 방지!)
            shootConfig.ValueRW.Timer += deltaTime;

            // 발사 시간 체크
            if (shootConfig.ValueRW.Timer >= shootConfig.ValueRW.Interval)
            {
                shootConfig.ValueRW.Timer = 0f;

                // 총알 생성
                var bullet = ecb.Instantiate(shootConfig.ValueRO.BulletPrefab);

                // 플레이어 앞에서 생성
                float3 spawnPos = transform.ValueRO.Position +
                    math.mul(transform.ValueRO.Rotation, new float3(0, 0.5f, 1f));

                ecb.SetComponent(bullet, LocalTransform.FromPositionRotation(
                    spawnPos, transform.ValueRO.Rotation));

                // 발사 방향 설정 (Bullet 컴포넌트에 따라)
                var direction = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));
                // BulletDirection 컴포넌트가 있다면 설정
            }
        }
    }
}
```

**핵심 변경점**:
- `[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]`: 서버 전용
- `shootConfig.ValueRW.Timer += deltaTime`: 직접 접근 (복사 방지)
- `WithAll<Simulate>()`: 예측 가능한 Entity만

---

### ✅ Phase 7 테스트

#### 실행 방법
1. Play 버튼 클릭
2. 플레이어가 자동으로 총알 발사

#### 기대 결과
- [ ] ✅ 총알이 주기적으로 발사됨
- [ ] ✅ **양쪽 클라이언트에서 총알 보임**
- [ ] ✅ 총알이 플레이어 방향으로 발사

---

## 🚀 Phase 8: Bullet 동기화 (15분)

### 목표
- 총알 Prefab에 Ghost 설정
- 서버에서 생성한 총알이 클라이언트에 동기화

### 작업 내용

#### 8.1 Bullet 컴포넌트에 GhostComponent 추가

**파일**: 기존 Bullet 관련 컴포넌트 파일들

**예시 (BulletSpeed 등)**:
```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletSpeed : IComponentData
{
    [GhostField] public float Value;
}
```

---

#### 8.2 Bullet Prefab 설정

**Unity Editor 작업**:
1. `Assets/Prefabs/Bullet.prefab` 선택
2. Add Component → Ghost Authoring Component (없다면)
3. Ghost Authoring 설정:
   - Ghost Mode: **Interpolated** (총알은 예측 불필요)
   - Supported Ghost Mode: Interpolated Only
   - Default Ghost Mode: Interpolated

---

### ✅ Phase 8 테스트

#### 실행 방법
1. Multiplayer Play Mode (Virtual Players: 2)
2. Play
3. 각 클라이언트 화면 확인

#### 기대 결과
- [ ] ✅ **Client 1에서 발사한 총알이 Client 2 화면에서도 보임**
- [ ] ✅ **Client 2에서 발사한 총알이 Client 1 화면에서도 보임**
- [ ] ✅ 총알 움직임 동기화

---

## 🚀 Phase 9: Enemy 스폰 및 추격 동기화 (25분)

### 목표
- Enemy를 서버에서만 스폰
- 가장 가까운 플레이어 추격 (다중 플레이어 지원)

### 작업 내용

#### 9.1 Enemy 컴포넌트 수정

**기존 Enemy 컴포넌트에 GhostComponent 추가**:

```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyTag : IComponentData { }

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemySpeed : IComponentData
{
    [GhostField] public float Value;
}
```

---

#### 9.2 EnemySpawnSystem 수정 (Server 전용)

**파일**: 기존 Enemy 스폰 시스템 수정

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct EnemySpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnableMultiplayer>();
        // ... 기존 코드
    }

    // ... 나머지 코드
}
```

---

#### 9.3 EnemyChaseSystem 수정 (다중 플레이어 지원)

**파일**: 기존 Enemy 추격 시스템 수정

```csharp
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
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>()
            .WithAll<GhostOwner>())  // 플레이어만
        {
            playerPositions.Add(transform.ValueRO.Position);
        }

        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            return;
        }

        // 각 적이 가장 가까운 플레이어 추적
        foreach (var (transform, speed) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemySpeed>>()
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
            transform.ValueRW.Position += direction * speed.ValueRO.Value * deltaTime;
        }

        playerPositions.Dispose();
    }
}
```

---

### ✅ Phase 9 테스트

#### 실행 방법
1. Multiplayer Play Mode (Virtual Players: 2)
2. Play
3. 플레이어 2명이 서로 멀리 이동

#### 기대 결과
- [ ] ✅ Enemy가 양쪽 클라이언트에서 보임
- [ ] ✅ Enemy가 가장 가까운 플레이어 추격
- [ ] ✅ 플레이어가 멀어지면 다른 플레이어 추격

---

## 🚀 Phase 10: 충돌 처리 + UI 동기화 (30분)

### 목표
- 서버에서만 충돌 검사
- 체력 동기화 (Ghost 컴포넌트)
- UI 표시 (각 클라이언트 자기 체력만)

### 작업 내용

#### 10.1 체력 컴포넌트 수정

**파일**: 기존 체력 컴포넌트 수정

```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerHealth : IComponentData
{
    [GhostField] public float CurrentHealth;
    [GhostField] public float MaxHealth;
}

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyHealth : IComponentData
{
    [GhostField] public float Value;
}
```

---

#### 10.2 충돌 시스템 수정 (Server 전용)

**기존 충돌 시스템에 WorldSystemFilter 추가**:

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class BulletHitSystem : SystemBase
{
    // ... 기존 코드
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerDamageSystem : SystemBase
{
    // ... 기존 코드
}
```

---

#### 10.3 UI 업데이트 시스템 (Client 전용)

**새 파일**: `Assets/Scripts/Systems/Network/UIUpdateSystem.cs`

```csharp
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// UI 업데이트 (Client에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }

    protected override void OnUpdate()
    {
        // 내 NetworkId
        if (!SystemAPI.HasSingleton<NetworkId>())
            return;

        var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;

        // 내 플레이어 체력 찾기
        foreach (var (health, ghostOwner) in
                 SystemAPI.Query<RefRO<PlayerHealth>, RefRO<GhostOwner>>())
        {
            if (ghostOwner.ValueRO.NetworkId != myNetworkId)
                continue;

            // UI 업데이트
            // UIManager.Instance?.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            break;
        }
    }
}
```

---

### ✅ Phase 10 테스트

#### 실행 방법
1. Multiplayer Play Mode (Virtual Players: 2)
2. Play
3. Enemy와 충돌하여 데미지 확인

#### 기대 결과
- [ ] ✅ 총알이 Enemy 처치
- [ ] ✅ Enemy가 플레이어에게 데미지
- [ ] ✅ **각 클라이언트가 자기 체력만 표시**
- [ ] ✅ 체력 변화가 동기화

---

## 🎉 전체 완료 체크리스트

### Phase별 완료 상태
- [ ] **Phase 1**: 환경 설정 + 연결 (1-2h)
- [ ] **Phase 2**: Spawner 싱글톤 (15분)
- [ ] **Phase 3**: IInputComponentData (10분)
- [ ] **Phase 4**: 입력 수집 시스템 (20분)
- [ ] **Phase 5**: PlayerSpawnSystem (25분)
- [ ] **Phase 6**: 입력 처리 시스템 (20분) ⭐
- [ ] **Phase 7**: AutoShoot Network (20분)
- [ ] **Phase 8**: Bullet 동기화 (15분)
- [ ] **Phase 9**: Enemy 동기화 (25분)
- [ ] **Phase 10**: 충돌 + UI (30분)

### 핵심 기능 검증
- [ ] ✅ Server/Client 연결
- [ ] ✅ 플레이어 스폰 (서버)
- [ ] ✅ 입력 수집 (클라이언트)
- [ ] ✅ 플레이어 움직임 동기화
- [ ] ✅ 총알 발사 동기화
- [ ] ✅ Enemy 스폰 및 추격
- [ ] ✅ 충돌 처리
- [ ] ✅ UI 동기화

---

## 🚨 문제 해결 가이드

### 플레이어가 안 보이는 경우
1. **Spawner 확인**
   - SubScene에 Spawner GameObject 있는지
   - PlayerPrefab 필드 할당 확인
2. **Prefab 확인**
   - Player.prefab에 GhostAuthoringComponent 있는지
   - MeshRenderer 있는지

### 입력이 안 먹히는 경우
1. **Phase 4 확인**
   - GatherPlayerInputSystem 로그 확인
   - Console에 "[Client Input]" 로그 있는지
2. **Phase 5 확인**
   - Entities Hierarchy에서 GhostOwnerIsLocal 태그 확인
   - CommandTarget.targetEntity 확인

### 플레이어가 안 움직이는 경우
1. **Input 값 확인**
   - Entities Hierarchy에서 PlayerInput 값 확인
   - Horizontal, Vertical이 변하는지
2. **Transform 확인**
   - LocalTransform.Position이 변하는지

### Multiplayer Play Mode에서 문제
1. **Ghost 동기화 안 됨**
   - GhostAuthoringComponent 확인
   - DefaultGhostMode = Predicted
2. **상대방 안 보임**
   - 두 클라이언트 모두 연결되었는지
   - ServerWorld에 두 플레이어 Entity 있는지

---

## 📝 NetcodeSamples 05_SpawnPlayer 핵심 패턴 요약

### 1. Singleton 패턴 (Spawner)
```csharp
state.RequireForUpdate<Spawner>();
var prefab = SystemAPI.GetSingleton<Spawner>().Player;
```

### 2. 입력 수집 (GhostInputSystemGroup)
```csharp
[UpdateInGroup(typeof(GhostInputSystemGroup))]
foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>()
    .WithAll<GhostOwnerIsLocal>())
{
    input.ValueRW = default;  // 매 프레임 리셋
    if (fire) input.ValueRW.Fire.Set();
}
```

### 3. 입력 처리 (PredictedSimulationSystemGroup)
```csharp
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
foreach (var (input, transform) in SystemAPI.Query<...>().WithAll<Simulate>())
{
    // 움직임 처리
}
```

### 4. 7단계 스폰 프로세스
```csharp
1. var player = state.EntityManager.Instantiate(prefab);
2. state.EntityManager.SetComponentData(player, LocalTransform...);
3. state.EntityManager.SetComponentData(player, new GhostOwner {...});
4. state.EntityManager.SetComponentData(entity, new CommandTarget {...});
5. state.EntityManager.GetBuffer<LinkedEntityGroup>(entity).Add(...);
6. state.EntityManager.AddComponentData(player, new ConnectionOwner {...});
7. state.EntityManager.AddComponent<PlayerSpawned>(entity);
```

### 5. 핵심 컴포넌트
- **IInputComponentData**: 자동 네트워크 직렬화
- **[GhostComponent]**: Ghost 타입 지정
- **[GhostField]**: 동기화 필드
- **GhostOwnerIsLocal**: 로컬 플레이어 자동 태그
- **CommandTarget**: 입력 라우팅
- **LinkedEntityGroup**: 생명주기 관리

---

## 🎯 다음 단계 (Phase 10 이후)

### 선택적 개선 사항
1. **Snapshot Interpolation**: 부드러운 원격 플레이어 움직임
2. **Lag Compensation**: 지연 보상
3. **Host Migration**: 호스트 마이그레이션 지원
4. **Reconnection**: 재연결 처리
5. **Dedicated Server**: 전용 서버 빌드

---

**문서 버전**: v3.0 (NetcodeSamples 패턴 기반 통합)
**최종 수정**: 2025-12-10
**작성자**: Claude + unity-dots-ecs Skill
**참고**: NetcodeSamples 05_SpawnPlayer, netcode_multiplayer_plan_incremental.md, netcode_multiplayer_plan_incremental_v2.md
