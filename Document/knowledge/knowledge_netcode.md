# Unity Netcode for Entities 학습 노트

## 목차
1. [NetworkStreamInGame의 핵심 역할](#networkstreamingame의-핵심-역할)
2. [GoInGameSystem - 네트워크 동기화의 시작점](#goingamesystem---네트워크-동기화의-시작점)
3. [IInputComponentData - 네트워크 입력 시스템](#iinputcomponentdata---네트워크-입력-시스템)
4. [Spawner Singleton 패턴](#spawner-singleton-패턴)
5. [플레이어 스폰의 7단계](#플레이어-스폰의-7단계)
6. [네트워크 연결 상태 흐름도](#네트워크-연결-상태-흐름도)
7. [GhostAuthoringComponent와 prefabId](#ghostauthoringcomponent와-prefabid)
8. [RequireForUpdate vs WithAll 구분](#requireforupdate-vs-withall-구분)
9. [문제 해결 사례](#문제-해결-사례)
10. [디버깅 체크리스트](#디버깅-체크리스트)

---

## NetworkStreamInGame의 핵심 역할

### 핵심 개념

**NetworkStreamInGame은 단순한 태그 컴포넌트이지만, 네트워크 동기화의 핵심입니다!**

```csharp
// Unity.NetCode 패키지에서 제공하는 태그
public struct NetworkStreamInGame : IComponentData { }
```

### 역할

이 태그가 **connection entity**에 추가되면:
- ✅ Ghost 스냅샷 동기화 시작
- ✅ GhostCollection이 prefab 로드 시작
- ✅ ServerWorld와 ClientWorld 간 패킷 교환 시작
- ✅ 네트워크 동기화 전체 활성화

이 태그가 **없으면**:
- ❌ GhostCollection: Num Loaded Prefabs = 0
- ❌ Network Debugger: Snapshot Ack = Invalid
- ❌ 패킷 교환 없음
- ❌ ClientWorld에 아무것도 동기화 안 됨

### 실전 예시

```csharp
// GoInGameSystem이 이 태그를 추가합니다
foreach (var (id, ent) in SystemAPI.Query<NetworkId>()
             .WithNone<NetworkStreamInGame>()
             .WithEntityAccess())
{
    Debug.Log($"[{worldName}] Go in game connection {id.Value}");
    commandBuffer.AddComponent<NetworkStreamInGame>(ent);  // ← 핵심!
}
```

**효과**:
```
태그 추가 전:
- GhostCollection: 비어있음
- ClientWorld: 동기화 안 됨

태그 추가 후:
- GhostCollection: Prefab 로드 시작
- ClientWorld: ServerWorld의 Ghost 수신 시작
- 플레이어 엔티티 동기화 ✅
```

---

## GoInGameSystem - 네트워크 동기화의 시작점

### NetcodeSamples 04_GoInGame 패턴

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial class GoInGameSystem : SystemBase
{
    private EntityQuery m_NewConnections;

    protected override void OnCreate()
    {
        RequireForUpdate<EnableGoInGame>();
        m_NewConnections = SystemAPI.QueryBuilder()
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>()
            .Build();
        RequireForUpdate(m_NewConnections);
    }

    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        FixedString32Bytes worldName = World.Name;

        // NetworkId가 설정되면 즉시 InGame 상태로 전환
        foreach (var (id, ent) in SystemAPI.Query<NetworkId>()
                     .WithNone<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            Debug.Log($"[{worldName}] Go in game connection {id.Value}");
            commandBuffer.AddComponent<NetworkStreamInGame>(ent);
        }

        commandBuffer.Playback(EntityManager);
    }
}
```

### 핵심 포인트

1. **Client와 Server 모두에서 실행**
   ```csharp
   [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
   ```
   - ClientWorld: 자신의 connection에 태그 추가
   - ServerWorld: 각 client connection에 태그 추가

2. **EnableGoInGame 마커로 활성화**
   ```csharp
   RequireForUpdate<EnableGoInGame>();
   ```
   - 씬에 EnableGoInGameAuthoring이 있어야 동작
   - 없으면 시스템이 실행되지 않음

3. **새로운 연결만 처리**
   ```csharp
   .WithAll<NetworkId>()          // NetworkId가 있고
   .WithNone<NetworkStreamInGame>() // NetworkStreamInGame은 없는
   ```
   - 이미 InGame 상태인 연결은 건너뜀
   - 중복 처리 방지

---

## IInputComponentData - 네트워크 입력 시스템

### 핵심 개념

**IInputComponentData는 네트워크 입력 전용 인터페이스로, 자동 직렬화 및 입력 버퍼를 제공합니다.**

```csharp
// ❌ 잘못됨
public struct PlayerInput : IComponentData
{
    public float2 Movement;  // 수동 동기화 필요
}

// ✅ 올바름 (NetcodeSamples 패턴)
public struct PlayerInput : IInputComponentData
{
    public int Horizontal;   // -1, 0, 1
    public int Vertical;     // -1, 0, 1
    public InputEvent Fire;  // 일회성 이벤트
}
```

**왜 int를 사용하나?**
- 네트워크 대역폭 절약 (int < float)
- 입력은 방향만 필요 (-1, 0, 1)
- NetcodeSamples 05_SpawnPlayer 패턴

### InputEvent 사용법

```csharp
// 입력 설정
if (Input.GetKeyDown(KeyCode.Space))
    input.Fire.Set();

// 입력 확인
if (input.Fire.IsSet)
    // 발사 처리
```

### 입력 시스템 패턴

#### 1. 입력 수집 (GhostInputSystemGroup)

```csharp
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GatherPlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>()
            .WithAll<GhostOwnerIsLocal>())  // 내 플레이어만
        {
            input.ValueRW = default;  // 매 프레임 초기화 필수!
            if (Input.GetKey(KeyCode.A)) input.ValueRW.Horizontal -= 1;
            if (Input.GetKey(KeyCode.D)) input.ValueRW.Horizontal += 1;
            // ...
        }
    }
}
```

#### 2. 입력 처리 (PredictedSimulationSystemGroup)

```csharp
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ProcessPlayerInputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (input, transform) in SystemAPI.Query<RefRO<PlayerInput>, RefRW<LocalTransform>>()
            .WithAll<Simulate>())
        {
            // int → float3 변환
            float3 dir = new float3(input.ValueRO.Horizontal, 0, input.ValueRO.Vertical);
            dir = math.normalizesafe(dir);  // 대각선 보정
            transform.ValueRW.Position += dir * speed * deltaTime;
        }
    }
}
```

### 핵심 규칙

1. **매 프레임 초기화 필수**: `input.ValueRW = default;`
2. **GhostOwnerIsLocal 필터링**: 내 플레이어만 입력 적용
3. **Client에서만 수집**: `[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]`
4. **올바른 시스템 그룹**:
   - 입력 수집: `GhostInputSystemGroup`
   - 입력 처리: `PredictedSimulationSystemGroup`

---

## Spawner Singleton 패턴

### 잘못된 패턴 (사용하지 말 것!)

```csharp
// ❌ Query 기반 prefab 찾기 (비효율적)
var prefab = Entity.Null;
foreach (var (tag, ent) in SystemAPI.Query<RefRO<PlayerPrefabTag>>().WithEntityAccess())
{
    prefab = ent;
    break;
}

if (prefab == Entity.Null)
    return;  // Prefab 없으면 종료
```

**문제점**:
- 매 프레임 쿼리 실행 (비효율)
- Prefab과 Instance 구분 어려움
- NetcodeSamples 패턴과 다름

### 올바른 패턴: Spawner Singleton

#### 1. Spawner 컴포넌트
```csharp
public struct Spawner : IComponentData
{
    public Entity Player;  // Player Prefab Entity 참조
}
```

#### 2. SpawnerAuthoring
```csharp
[DisallowMultipleComponent]
public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Player;  // Inspector에서 Player.prefab 할당

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            Spawner component = default(Spawner);
            // GameObject Prefab을 Entity Prefab으로 변환
            component.Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
```

#### 3. PlayerSpawnSystem에서 사용
```csharp
public void OnCreate(ref SystemState state)
{
    state.RequireForUpdate<Spawner>();  // Spawner가 있을 때만 실행
    // ...
}

public void OnUpdate(ref SystemState state)
{
    // Singleton으로 즉시 접근 (쿼리 필요 없음)
    var prefab = SystemAPI.GetSingleton<Spawner>().Player;

    // Prefab으로 Entity 생성
    var player = state.EntityManager.Instantiate(prefab);
}
```

**장점**:
- ✅ 매 프레임 쿼리 불필요 (성능 향상)
- ✅ Prefab 참조가 명확함
- ✅ NetcodeSamples 05_SpawnPlayer 패턴과 동일
- ✅ RequireForUpdate로 안전하게 대기

---

## 플레이어 스폰의 7단계

NetcodeSamples의 `SpawnPlayerSystem.cs`에서 사용하는 완벽한 패턴입니다.

```csharp
for (var i = 0; i < connectionEntities.Length; i++)
{
    var networkId = networkIds[i];
    var connectionEntity = connectionEntities[i];
    var player = state.EntityManager.Instantiate(prefab);

    // 1️⃣ 스폰 위치 설정 (겹치지 않게)
    var localTransform = state.EntityManager.GetComponentData<LocalTransform>(prefab);
    localTransform.Position.x += networkId.Value * 2;
    state.EntityManager.SetComponentData(player, localTransform);

    // 2️⃣ GhostOwner 설정 (네트워크 소유권)
    state.EntityManager.SetComponentData(player, new GhostOwner { NetworkId = networkId.Value });

    // 3️⃣ CommandTarget 설정 (입력 라우팅)
    state.EntityManager.SetComponentData(connectionEntity, new CommandTarget { targetEntity = player });

    // 4️⃣ LinkedEntityGroup에 추가 (연결 끊김 시 자동 삭제)
    state.EntityManager.GetBuffer<LinkedEntityGroup>(connectionEntity)
        .Add(new LinkedEntityGroup { Value = player });

    // 5️⃣ ConnectionOwner 추가 (역참조)
    state.EntityManager.AddComponentData(player, new ConnectionOwner { Entity = connectionEntity });

    // 6️⃣ PlayerSpawned 마커 추가 (중복 스폰 방지)
    state.EntityManager.AddComponent<PlayerSpawned>(connectionEntity);
}
```

### 각 단계 상세 설명

#### 1️⃣ 스폰 위치 오프셋
```csharp
localTransform.Position.x += networkId.Value * 2;
```
- NetworkId가 1, 2, 3이면 x 위치가 2, 4, 6으로 분산
- 플레이어가 겹치지 않게 배치

#### 2️⃣ GhostOwner (네트워크 소유권)
```csharp
new GhostOwner { NetworkId = networkId.Value }
```
- **소유권 표시**: 이 Ghost가 누구의 것인지
- ClientWorld에서 자동으로 `GhostOwnerIsLocal` 태그 추가됨
- 입력 처리 시 "내 플레이어인지" 판별 가능

#### 3️⃣ CommandTarget (입력 라우팅)
```csharp
new CommandTarget { targetEntity = player }
```
- **connection → player 연결**: 이 connection의 입력은 이 player로
- 입력 시스템이 이 정보를 보고 올바른 엔티티에 입력 전달

#### 4️⃣ LinkedEntityGroup (자동 정리)
```csharp
connectionEntity의 LinkedEntityGroup에 player 추가
```
- **자동 삭제**: connection이 끊기면 player도 자동 삭제
- Unity Netcode가 자동 처리

#### 5️⃣ ConnectionOwner (역참조)
```csharp
new ConnectionOwner { Entity = connectionEntity }
```
- **player → connection 참조**: 플레이어에서 connection 찾기
- 필요 시 connection 정보에 접근 가능

#### 6️⃣ PlayerSpawned 마커 (중복 방지)
```csharp
AddComponent<PlayerSpawned>(connectionEntity)
```
- **한 번만 스폰**: 이미 스폰된 connection은 쿼리에서 제외
- 쿼리: `.WithNone<PlayerSpawned>()`

---

## 네트워크 연결 상태 흐름도

```
[Client 연결]
    ↓
ServerWorld: NetworkId 컴포넌트 생성
ClientWorld: NetworkId 컴포넌트 생성
    ↓
GoInGameSystem 실행 (양쪽)
    ↓
NetworkStreamInGame 태그 추가 ← ⚡ 핵심!
    ↓
Ghost 스냅샷 동기화 시작
    ↓
GhostCollection: Prefab 로드 시작
    ↓
PlayerSpawnSystem 실행 (ServerWorld만)
    ↓
Query: NetworkId 있고 PlayerSpawned 없는 connection 찾기
    ↓
플레이어 Entity 생성 (ServerWorld)
    ↓
7단계 설정 (GhostOwner, CommandTarget 등)
    ↓
Ghost 동기화
    ↓
ClientWorld: 플레이어 Entity 수신 ✅
    ↓
화면에 표시됨! 🎮
```

### 중요한 순서

1. **GoInGameSystem이 먼저 실행되어야 함**
   - NetworkStreamInGame 태그가 없으면 Ghost 동기화 안 됨

2. **PlayerSpawnSystem은 NetworkStreamInGame 필요**
   ```csharp
   state.RequireForUpdate<NetworkStreamInGame>();
   ```
   - 최소 하나의 InGame 연결이 있어야 실행

3. **Query에서는 NetworkStreamInGame 확인 안 함**
   ```csharp
   // ✅ 올바름
   m_NewPlayersQuery = SystemAPI.QueryBuilder()
       .WithAll<NetworkId>()
       .WithNone<PlayerSpawned>()
       .Build();
   ```
   - RequireForUpdate로만 확인 (최소 1개 존재)
   - Query에 넣으면 안 됨 (connection entity에는 없을 수 있음)

---

## GhostAuthoringComponent와 prefabId

### GhostAuthoringComponent란?

Ghost로 동기화할 Prefab에 반드시 필요한 컴포넌트입니다.

```
Player.prefab
├─ Transform
├─ MeshRenderer
├─ GhostAuthoringComponent ← 필수!
│  ├─ Name: "Player"
│  ├─ Importance: 1
│  ├─ SupportedGhostModes: All
│  ├─ OptimizationMode: Dynamic
│  └─ prefabId: 자동 생성됨 (Baking 시)
└─ PlayerAuthoring
```

### prefabId의 역할

- **고유 식별자**: 각 Ghost Prefab을 구분
- **자동 생성**: Unity가 Baking 시 GUID 기반으로 생성
- **동기화**: ServerWorld와 ClientWorld가 같은 Prefab 식별

### 트러블슈팅 사례

**문제**: GhostCollection이 비어있음 (Num Loaded Prefabs = 0)

**잘못된 진단**: prefabId가 비어있어서 문제라고 생각
- Inspector에서 prefabId 필드가 비어있어 보임
- 수동으로 값 입력 시도

**실제 원인**: NetworkStreamInGame 태그가 없어서!
- prefabId는 Baking 시 자동 생성됨
- Inspector에서 비어보여도 실제로는 설정되어 있음
- 문제는 네트워크 연결 상태였음

**교훈**:
> Prefab이나 SubScene Baking 문제가 아니라면, 네트워크 연결 위주로 확인하세요!

---

## RequireForUpdate vs WithAll 구분

### 핵심 차이

| | RequireForUpdate | WithAll |
|---|---|---|
| **용도** | 시스템 실행 조건 | Entity 필터링 |
| **위치** | OnCreate | Query |
| **의미** | "최소 1개 존재해야 시스템 실행" | "이 컴포넌트를 가진 Entity만" |
| **실패 시** | OnUpdate 자체가 실행 안 됨 | 해당 Entity만 제외 |

### 실전 예제: PlayerSpawnSystem

#### ✅ 올바른 패턴 (NetcodeSamples)

```csharp
public void OnCreate(ref SystemState state)
{
    state.RequireForUpdate<Spawner>();
    state.RequireForUpdate<NetworkStreamInGame>();  // 최소 1개 InGame 연결 필요

    m_NewPlayersQuery = SystemAPI.QueryBuilder()
        .WithAll<NetworkId>()         // NetworkId가 있는 Entity
        .WithNone<PlayerSpawned>()    // PlayerSpawned는 없는 Entity
        .Build();
}
```

**의미**:
- `RequireForUpdate<NetworkStreamInGame>()`: "InGame 연결이 하나라도 있어야 시스템 실행"
- `.WithAll<NetworkId>()`: "쿼리는 NetworkId를 가진 connection만 찾기"
- NetworkStreamInGame은 쿼리 조건에 없음 ← **핵심!**

#### ❌ 잘못된 패턴

```csharp
m_NewPlayersQuery = SystemAPI.QueryBuilder()
    .WithAll<NetworkId, NetworkStreamInGame>()  // ❌ 잘못됨!
    .WithNone<PlayerSpawned>()
    .Build();
```

**문제**:
- ServerWorld에서 스폰이 안 됨
- connection entity에 NetworkStreamInGame이 없을 수 있음
- 과도하게 제한적인 조건

### 언제 RequireForUpdate를 쓰나?

**시스템 실행 전제 조건**:
```csharp
state.RequireForUpdate<SimulationSingleton>();  // Physics World 필요
state.RequireForUpdate<Spawner>();              // Spawner 필요
state.RequireForUpdate<PlayerTag>();            // 플레이어 존재 필요
```

**의미**: "이게 없으면 시스템을 실행할 이유가 없다"

### 언제 WithAll을 쓰나?

**Entity 필터링**:
```csharp
.WithAll<PlayerTag>()     // 플레이어만
.WithAll<EnemyTag>()      // 적만
.WithAll<BulletTag>()     // 총알만
```

**의미**: "이 컴포넌트를 가진 Entity만 처리하고 싶다"

---

## 문제 해결 사례

### 사례 1: ServerWorld에 생성되지만 화면에 안 보임

**증상**:
- Entity Hierarchy: Player 생성 확인
- 하지만 Game View에 아무것도 안 보임

**가능한 원인**:
1. ~~TransformUsageFlags 누락~~
   - `Renderable | Dynamic` 필요
   - 현재 프로젝트는 이미 설정되어 있음

2. ~~카메라 위치~~
   - 카메라가 플레이어를 안 보고 있을 수 있음

3. **실제 원인**: ClientWorld 동기화 안 됨
   - ServerWorld에만 있고 ClientWorld에 없음
   - → GoInGameSystem 누락

### 사례 2: GhostCollection 비어있음

**증상**:
```
GhostCollection (ServerWorld)
- Num Loaded Prefabs: 0

Network Debugger
- Snapshot Ack: Invalid
- 패킷 교환 없음
```

**잘못된 접근**:
- Prefab의 prefabId 확인
- SubScene Baking 확인
- GhostAuthoringComponent 재설정

**올바른 접근**:
- **네트워크 연결 상태 확인**
- GoInGameSystem 존재 여부
- NetworkStreamInGame 태그 확인

**해결책**:
```csharp
// GoInGameSystem 추가
// EnableGoInGame 컴포넌트 추가
// EnableGoInGameAuthoring 씬에 배치
```

### 사례 3: GoInGameSystem 추가 후 ServerWorld도 스폰 안 됨

**증상**:
- 이전: ServerWorld만 생성, ClientWorld 안 됨
- 현재: 둘 다 안 됨 (더 망가짐)

**원인**:
```csharp
// ❌ 잘못된 쿼리
m_NewPlayersQuery = SystemAPI.QueryBuilder()
    .WithAll<NetworkId, NetworkStreamInGame>()  // 너무 제한적!
    .WithNone<PlayerSpawned>()
    .Build();
```

**해결**:
```csharp
// ✅ 올바른 쿼리
m_NewPlayersQuery = SystemAPI.QueryBuilder()
    .WithAll<NetworkId>()         // NetworkStreamInGame 제거
    .WithNone<PlayerSpawned>()
    .Build();
```

---

## 디버깅 체크리스트

### 네트워크 동기화 문제 시

#### 1단계: GoInGameSystem 확인
- [ ] GoInGameSystem.cs 파일 존재
- [ ] EnableGoInGame.cs 컴포넌트 존재
- [ ] EnableGoInGameAuthoring.cs 존재
- [ ] 씬에 EnableGoInGameAuthoring 배치됨

#### 2단계: NetworkStreamInGame 태그 확인
```csharp
// Entity Hierarchy에서 connection entity 확인
// NetworkStreamInGame 컴포넌트 있는지 확인
```

- [ ] ServerWorld connection에 태그 있음
- [ ] ClientWorld connection에 태그 있음

#### 3단계: GhostCollection 확인
- [ ] Num Loaded Prefabs > 0
- [ ] Player Prefab이 리스트에 있음

#### 4단계: Network Debugger 확인
- [ ] Snapshot Ack: 숫자 (Invalid 아님)
- [ ] 패킷 송수신 확인
- [ ] RTT (Round Trip Time) 측정됨

#### 5단계: PlayerSpawnSystem 확인
```csharp
// 로그 확인
[PlayerSpawnSystem] OnCreate - Waiting for Spawner
[SpawnPlayerSystem][ServerWorld] Spawning player for NetworkId 1
```

- [ ] OnCreate 로그 출력됨
- [ ] Spawning 로그 출력됨
- [ ] NetworkId 값이 올바름

### Prefab 문제 시

#### GhostAuthoringComponent 확인
- [ ] Player.prefab에 GhostAuthoringComponent 있음
- [ ] Name 설정됨
- [ ] SupportedGhostModes 설정됨
- [ ] ~~prefabId 확인~~ (자동 생성되므로 걱정 안 해도 됨)

#### Spawner 확인
- [ ] Spawner.cs 컴포넌트 존재
- [ ] SpawnerAuthoring.cs 존재
- [ ] 씬에 SpawnerAuthoring 배치됨
- [ ] Inspector에서 Player 필드에 Player.prefab 할당됨

### Query 문제 시

#### PlayerSpawnSystem 쿼리 확인
```csharp
// ✅ 올바른 패턴
m_NewPlayersQuery = SystemAPI.QueryBuilder()
    .WithAll<NetworkId>()
    .WithNone<PlayerSpawned>()
    .Build();

// ❌ 잘못된 패턴
m_NewPlayersQuery = SystemAPI.QueryBuilder()
    .WithAll<NetworkId, NetworkStreamInGame>()  // NetworkStreamInGame 제거!
    .WithNone<PlayerSpawned>()
    .Build();
```

---

## 핵심 정리

### 네트워크 동기화의 3요소

1. **GoInGameSystem**: NetworkStreamInGame 태그 추가
2. **NetworkStreamInGame**: Ghost 동기화 활성화
3. **PlayerSpawnSystem**: 플레이어 Entity 생성

**빠진 것 하나라도 있으면 동기화 안 됨!**

### 스폰 시스템의 3요소

1. **Spawner Singleton**: Prefab 참조
2. **7단계 설정**: GhostOwner, CommandTarget 등
3. **PlayerSpawned 마커**: 중복 스폰 방지

### 문제 해결 접근법

```
증상: ClientWorld 동기화 안 됨
    ↓
Prefab/Baking 문제? → NO
    ↓
네트워크 연결 문제! → YES
    ↓
GoInGameSystem 확인
    ↓
NetworkStreamInGame 태그 확인
    ↓
해결! ✅
```

**교훈**:
> 표면적인 증상(Prefab, Baking)에 집중하지 말고, 근본 원인(네트워크 연결)을 찾으세요!

---

---

**작성일**: 2025-12-10
**최종 수정**: 2025-12-10 (Phase 3: IInputComponentData 추가)
**프로젝트**: projectc (Unity Netcode for Entities)
**참고**: NetcodeSamples 04_GoInGame, 05_SpawnPlayer

### 버전 히스토리
- **v1.0** (2025-12-10): 초기 작성 (NetworkStreamInGame, GoInGameSystem, Spawner 패턴)
- **v1.1** (2025-12-10): IInputComponentData 섹션 추가 (Phase 3 완료)
