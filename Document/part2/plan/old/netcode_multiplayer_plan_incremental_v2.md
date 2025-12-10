# 🎯 NetCode 재구현 계획 - 단계별 검증 방식

> **기반**: NetcodeSamples 05_SpawnPlayer 패턴
> **작성일**: 2025-12-09
> **목표**: 기존 싱글플레이 게임을 검증된 멀티플레이어 패턴으로 전환

## 📋 전체 개요

### 핵심 원칙
1. ✅ **한 번에 하나씩** - 각 Phase 완료 후 반드시 테스트
2. ✅ **검증 가능** - 각 단계마다 눈으로 확인 가능한 결과
3. ✅ **롤백 가능** - 문제 발생 시 이전 단계로 복구
4. ✅ **NetcodeSamples 패턴 준수** - 검증된 방식 사용

### 현재 프로젝트 문제점
- ❌ **Spawner 없음** - Prefab을 매 프레임 쿼리로 찾고 있음
- ❌ **IInputComponentData 미사용** - 일반 `IComponentData` 사용 중
- ❌ **CommandTarget 미설정** - 입력이 어느 플레이어로 갈지 지정 안 됨
- ❌ **LinkedEntityGroup 미사용** - 연결 끊김 시 자동 정리 안 됨
- ❌ **GhostOwnerIsLocal 미활용** - 로컬 플레이어 필터링 제대로 안 됨
- ❌ **잘못된 입력 시스템** - `InitializationSystemGroup`에서 모든 플레이어에 입력 적용
- ❌ **GhostInputSystemGroup 미사용** - 입력 수집을 잘못된 그룹에서 실행

### 전체 진행 단계
| Phase | 작업 | 예상 시간 | 테스트 가능 여부 |
|-------|------|-----------|------------------|
| 1 | Spawner 싱글톤 생성 | 15분 | ✅ Prefab 참조 확인 |
| 2 | IInputComponentData로 변경 | 10분 | ✅ 컴파일 성공 |
| 3 | 입력 수집 시스템 재작성 | 20분 | ✅ 입력 로그 확인 |
| 4 | PlayerSpawnSystem 재작성 | 25분 | ✅ GhostOwner 설정 확인 |
| 5 | 입력 처리 시스템 생성 | 20분 | ✅ **플레이어 움직임!** |
| 6 | AutoShoot Network 대응 | 20분 | ✅ 발사 동기화 |
| 7 | Bullet 동기화 | 15분 | ✅ 총알 보임 |
| 8 | Enemy 스폰 및 추격 | 25분 | ✅ Enemy 동작 |
| 9 | 충돌 처리 | 20분 | ✅ 데미지 동기화 |
| 10 | UI 동기화 | 15분 | ✅ 체력바 표시 |

**총 예상 시간**: 약 3시간

---

## 🚀 Phase 1: Spawner 싱글톤 생성 (15분)

### 🎯 목표
- Prefab을 매 프레임 쿼리하지 않고 Spawner로 한 번에 참조
- SubScene 로딩 대기 불필요

### 📝 작업 내용

#### 1.1 Spawner 컴포넌트 생성
**파일**: `Assets/Scripts/Components/Network/Spawner.cs`

```csharp
using Unity.Entities;

/// <summary>
/// 플레이어 Prefab을 참조하는 싱글톤
/// </summary>
public struct Spawner : IComponentData
{
    public Entity Player;
}
```

#### 1.2 SpawnerAuthoring 생성
**파일**: `Assets/Scripts/Authoring/Network/SpawnerAuthoring.cs`

```csharp
using Unity.Entities;
using UnityEngine;

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

#### 1.3 씬에 Spawner 추가
**Unity Editor 작업**:
1. `Assets/Scenes/NetworkTest/NetworkTestSubscene` 열기
2. 우클릭 → Create Empty → 이름: "Spawner"
3. Add Component → `Spawner Authoring`
4. Player Prefab 필드에 `Assets/Prefabs/Player.prefab` 드래그
5. Ctrl+S로 저장

#### 1.4 PlayerSpawnSystem 임시 수정
**파일**: `Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs`

**기존 OnCreate, OnUpdate 메서드 전체 교체**:

```csharp
protected override void OnCreate()
{
    RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    RequireForUpdate<Spawner>();  // Spawner 필수
    Debug.Log("[PlayerSpawnSystem] OnCreate - System initialized in ServerWorld");
}

protected override void OnUpdate()
{
    // Spawner에서 Prefab 가져오기
    if (!SystemAPI.TryGetSingleton<Spawner>(out var spawner))
    {
        Debug.LogWarning("[PlayerSpawnSystem] Spawner not found!");
        return;
    }

    var m_PlayerPrefab = spawner.Player;

    var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                       .CreateCommandBuffer(World.Unmanaged);

    // 연결된 클라이언트 중 플레이어가 없는 경우
    foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                 .WithNone<NetworkStreamInGame>()
                 .WithEntityAccess())
    {
        Debug.Log($"[PlayerSpawnSystem] Client connected: NetworkId = {id.ValueRO.Value}");

        // InGame 태그 추가 (스폰 완료 표시)
        ecb.AddComponent<NetworkStreamInGame>(entity);

        // 플레이어 스폰
        var player = ecb.Instantiate(m_PlayerPrefab);

        // 스폰 위치 설정
        int playerIndex = id.ValueRO.Value - 1;
        float3 spawnPosition = new float3(playerIndex * 4f - 2f, 0.5f, 0f);

        ecb.SetComponent(player, LocalTransform.FromPosition(spawnPosition));

        // 소유권 설정
        ecb.SetComponent(player, new GhostOwner { NetworkId = id.ValueRO.Value });

        Debug.Log($"[Server] Player spawned for NetworkId {id.ValueRO.Value} at {spawnPosition}");
    }
}
```

### ✅ Phase 1 테스트

#### 실행 방법
1. `Assets/Scenes/NetworkTest/NetworkTestSubscene.unity` 씬 열기
2. Play 버튼 클릭

#### 기대 결과 (Console 로그)
```
[PlayerSpawnSystem] OnCreate - System initialized in ServerWorld
[PlayerSpawnSystem] Client connected: NetworkId = 1
[Server] Player spawned for NetworkId 1 at (-2, 0.5, 0)
```

#### 검증 포인트
- [ ] ❌ "PlayerPrefab not found!" 메시지 없음
- [ ] ✅ "Spawner not found!" 경고도 없음
- [ ] ✅ 플레이어 스폰 로그 출력
- [ ] ✅ 화면에 플레이어 1개 보임

#### 문제 발생 시
| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| "Spawner not found!" | Spawner GameObject 없음 | SubScene에 Spawner 추가 |
| 플레이어 안 보임 | Prefab 미할당 | Inspector에서 Player Prefab 할당 |
| 아무 로그도 없음 | SubScene 미빌드 | Play 시 자동 빌드되므로 재실행 |

---

## 🚀 Phase 2: IInputComponentData로 변경 (10분)

### 🎯 목표
- 네트워크 Input 버퍼 자동 생성
- `IComponentData` → `IInputComponentData`

### 📝 작업 내용

#### 2.1 PlayerInput.cs 수정
**파일**: `Assets/Scripts/Components/PlayerInput.cs`

**전체 내용 교체**:

```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 플레이어 입력 - IInputComponentData로 자동 네트워크 전송
/// </summary>
public struct PlayerInput : IInputComponentData
{
    public int Horizontal;
    public int Vertical;
    public InputEvent Fire;  // 발사 버튼 (Phase 6에서 사용)
}
```

#### 2.2 PlayerAuthoring.cs의 Baker 수정
**파일**: `Assets/Scripts/Authoring/PlayerAuthoring.cs`

**28번 줄 수정**:

```csharp
// 기존
AddComponent(entity, new PlayerInput { Movement = float2.zero });

// 변경 후
AddComponent<PlayerInput>(entity);  // 기본값으로 초기화
```

### ✅ Phase 2 테스트

#### 실행 방법
1. 컴파일 에러 없는지 확인 (Console 창)
2. Play 버튼 클릭

#### 기대 결과
- [ ] ✅ 컴파일 에러 없음
- [ ] ✅ 플레이어 정상 스폰
- [ ] ⚠️ 입력은 아직 작동 안 함 (정상)

#### 검증 포인트
- PlayerInput이 IInputComponentData로 변경됨
- 컴파일 성공
- 게임 정상 실행

#### 문제 발생 시
| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| "Movement does not exist" | PlayerInput 구조 변경 | 다른 파일에서 .Movement 참조 제거 |
| 컴파일 에러 | using 누락 | `using Unity.NetCode;` 추가 |

---

## 🚀 Phase 3: 입력 수집 시스템 재작성 (20분)

### 🎯 목표
- GhostInputSystemGroup에서 입력 수집
- GhostOwnerIsLocal 태그로 로컬 플레이어만 입력

### 📝 작업 내용

#### 3.1 기존 PlayerInputSystem.cs 삭제
**파일 삭제**: `Assets/Scripts/Systems/PlayerInputSystem.cs`

⚠️ **주의**: Unity Editor에서 파일 삭제 시 .meta 파일도 자동 삭제됨

#### 3.2 새로운 GatherPlayerInputSystem.cs 생성
**파일**: `Assets/Scripts/Systems/Network/GatherPlayerInputSystem.cs`

```csharp
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 클라이언트에서 입력 수집 (GhostInputSystemGroup)
/// </summary>
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GatherPlayerInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        // 입력 읽기
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

### ✅ Phase 3 테스트

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
- [ ] ✅ "GhostOwnerIsLocal" 필터링 작동 (로컬 플레이어만 입력)

#### 입력 값 확인 (Window → Entities → Hierarchy)
1. ClientWorld 펼치기
2. Player Entity 선택
3. Inspector에서 `PlayerInput` 컴포넌트 확인
4. WASD 키 누를 때 Horizontal/Vertical 값 변화 확인

#### 문제 발생 시
| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| 로그 안 나옴 | GhostOwnerIsLocal 없음 | Phase 4에서 CommandTarget 설정 필요 |
| foreach 진입 안 함 | NetworkStreamInGame 없음 | Phase 4에서 해결됨 |
| "Query is empty" | 플레이어 스폰 안 됨 | Phase 1 재확인 |

---

## 🚀 Phase 4: PlayerSpawnSystem 완전 재작성 (25분)

### 🎯 목표
- NetcodeSamples 패턴과 완전히 동일하게 구현
- CommandTarget, LinkedEntityGroup 설정

### 📝 작업 내용

#### 4.1 필요한 컴포넌트 생성

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

#### 4.2 PlayerSpawnSystem.cs 완전 교체
**파일**: `Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs`

**전체 내용 교체**:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 클라이언트 연결 시 플레이어 스폰 (NetcodeSamples 패턴)
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
        state.RequireForUpdate<NetworkStreamInGame>();
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

            // 2. 스폰 위치 설정
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

### ✅ Phase 4 테스트

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

#### 문제 발생 시
| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| GhostOwnerIsLocal 없음 | CommandTarget 미설정 | 4.2 코드 재확인 |
| 입력 로그 안 나옴 | Phase 3 미완료 | Phase 3 재확인 |
| 플레이어 중복 스폰 | PlayerSpawned 안 붙음 | 7번 단계 코드 확인 |

---

## 🚀 Phase 5: 입력 처리 시스템 생성 (20분)

### 🎯 목표
- 예측 시뮬레이션에서 입력을 실제 움직임으로 변환
- **플레이어가 드디어 움직임!** 🎉

### 📝 작업 내용

#### 5.1 ProcessPlayerInputSystem.cs 생성
**파일**: `Assets/Scripts/Systems/Network/ProcessPlayerInputSystem.cs`

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 입력을 실제 움직임으로 변환 (예측 시뮬레이션)
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProcessPlayerInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (input, transform, speed) in
                 SystemAPI.Query<RefRO<PlayerInput>, RefRW<LocalTransform>, RefRO<MovementSpeed>>())
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

            // 발사 처리 (Phase 6에서 구현)
            if (input.ValueRO.Fire.IsSet)
            {
                // TODO: 발사 로직
            }
        }
    }
}
```

### ✅ Phase 5 테스트 - 🎉 첫 움직임!

#### 실행 방법
1. Play 버튼 클릭
2. **WASD 키로 플레이어 이동**

#### 기대 결과
- [ ] ✅ **플레이어가 움직임!** 🎉
- [ ] ✅ WASD 키 입력에 반응
- [ ] ✅ 이동 방향으로 회전
- [ ] ✅ 부드러운 움직임

#### 검증 포인트 (Entities Hierarchy)
**Window → Entities → Hierarchy → ClientWorld → Player Entity**
- `LocalTransform` → Position 값이 실시간으로 변함
- Scene 뷰에서 플레이어 이동 확인

#### Multiplayer Play Mode 테스트 (선택사항)
1. **Window → Multiplayer Play Mode**
2. Virtual Players: 2
3. Play
4. 각 클라이언트 창에서 WASD로 이동
5. **상대방 플레이어 움직임도 보임!** ⭐

#### 문제 발생 시
| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| 플레이어 안 움직임 | MovementSpeed 없음 | PlayerAuthoring.cs 확인 |
| 입력 안 먹힘 | Phase 3,4 미완료 | 이전 Phase 재확인 |
| 멀티플레이 안 보임 | Ghost 동기화 문제 | GhostAuthoringComponent 확인 |

---

## 🎯 Phase 1-5 완료 체크리스트

### 필수 검증 항목
- [ ] **Phase 1**: Spawner 로그 확인, 플레이어 스폰
- [ ] **Phase 2**: 컴파일 성공
- [ ] **Phase 3**: 입력 수집 로그 출력
- [ ] **Phase 4**: GhostOwner, CommandTarget 설정 확인
- [ ] **Phase 5**: **플레이어 움직임 확인** ⭐

### 코어 기능 검증
- [ ] 플레이어 스폰 (서버)
- [ ] 입력 수집 (클라이언트)
- [ ] 입력 → 움직임 변환 (예측 시뮬레이션)
- [ ] GhostOwnerIsLocal 필터링
- [ ] CommandTarget 라우팅

### 파일 체크리스트

**새로 생성한 파일** (6개):
1. ✅ `Assets/Scripts/Components/Network/Spawner.cs`
2. ✅ `Assets/Scripts/Authoring/Network/SpawnerAuthoring.cs`
3. ✅ `Assets/Scripts/Components/Network/PlayerSpawned.cs`
4. ✅ `Assets/Scripts/Components/Network/ConnectionOwner.cs`
5. ✅ `Assets/Scripts/Systems/Network/GatherPlayerInputSystem.cs`
6. ✅ `Assets/Scripts/Systems/Network/ProcessPlayerInputSystem.cs`

**수정한 파일** (3개):
1. ✅ `Assets/Scripts/Components/PlayerInput.cs` (IInputComponentData)
2. ✅ `Assets/Scripts/Authoring/PlayerAuthoring.cs` (Baker 수정)
3. ✅ `Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs` (완전 재작성)

**삭제한 파일** (1개):
1. ✅ `Assets/Scripts/Systems/PlayerInputSystem.cs` (기존)

---

## 📊 Phase 6-10 미리보기

Phase 1-5 완료 후 다음 단계:

### Phase 6: AutoShoot 시스템 Network 대응 (20분)
- `AutoShootConfig`를 `[GhostComponent]`로 설정
- `InputEvent.Fire` 사용하여 발사
- 예측 시뮬레이션 그룹에서 실행

### Phase 7: Bullet 동기화 (15분)
- Bullet Prefab에 GhostAuthoringComponent 추가
- GhostOwner 복사 (누가 쏜 총알인지)
- 서버에서 충돌 검사

### Phase 8: Enemy 스폰 및 추격 (25분)
- EnemySpawner 싱글톤 생성
- 서버에서만 Enemy 스폰
- NavMesh 대신 직선 추격

### Phase 9: 충돌 처리 (20분)
- 서버에서만 충돌 검사
- RPC로 데미지 전송
- 체력 동기화

### Phase 10: UI 동기화 (15분)
- Ghost 데이터를 UI로 표시
- 체력바, 점수 표시
- GameOver 처리

---

## 🚨 전체 문제 해결 가이드

### 플레이어가 안 보이는 경우
1. **Spawner 확인**
   - SubScene에 Spawner GameObject 있는지
   - PlayerPrefab 필드 할당 확인
2. **Prefab 확인**
   - `Assets/Prefabs/Player.prefab`에 GhostAuthoringComponent 있는지
   - MeshRenderer 있는지
3. **Camera 위치**
   - Main Camera가 플레이어를 보고 있는지

### 입력이 안 먹히는 경우
1. **Phase 3 확인**
   - GatherPlayerInputSystem 로그 확인
   - Console에 "[Client Input]" 로그 있는지
2. **Phase 4 확인**
   - Entities Hierarchy에서 GhostOwnerIsLocal 태그 확인
   - CommandTarget.targetEntity 확인
3. **Phase 5 확인**
   - ProcessPlayerInputSystem이 실행되는지
   - MovementSpeed 컴포넌트 확인

### 플레이어가 안 움직이는 경우
1. **Input 값 확인**
   - Entities Hierarchy에서 PlayerInput 값 확인
   - Horizontal, Vertical이 변하는지
2. **Transform 확인**
   - LocalTransform.Position이 변하는지
3. **System 실행 확인**
   - Console에서 ProcessPlayerInputSystem 에러 없는지

### Multiplayer Play Mode에서 문제
1. **Ghost 동기화 안 됨**
   - GhostAuthoringComponent 확인
   - DefaultGhostMode = Predicted
2. **상대방 안 보임**
   - 두 클라이언트 모두 연결되었는지
   - ServerWorld에 두 플레이어 Entity 있는지

### 컴파일 에러
1. **using 구문 확인**
   ```csharp
   using Unity.Entities;
   using Unity.NetCode;
   using Unity.Mathematics;
   using Unity.Transforms;
   ```
2. **네임스페이스 확인**
   - 모든 스크립트가 global namespace
3. **Unity 재시작**
   - Domain Reload 문제 시 Unity 재시작

---

## 📝 추가 참고 사항

### NetcodeSamples 05_SpawnPlayer와의 차이점
| 항목 | 05_SpawnPlayer | 현재 프로젝트 |
|------|----------------|---------------|
| Spawner | SubScene의 GameObject | ✅ 동일 |
| Input | IInputComponentData | ✅ 동일 |
| Spawn 로직 | ISystem (Burst) | ✅ 동일 |
| 추가 기능 | 없음 | AutoShoot, Enemy 등 |

### 성능 최적화 팁
1. **Burst 컴파일 활용**
   - 모든 ISystem에 `[BurstCompile]` 추가
2. **쿼리 최적화**
   - WithAll, WithNone 적극 활용
3. **EntityCommandBuffer**
   - 구조적 변경은 ECB 사용

### 다음 단계 추천 순서
1. ✅ Phase 1-5 완료 (플레이어 이동)
2. 🎯 Phase 6-7 완료 (발사 시스템)
3. 🎯 Phase 8-9 완료 (Enemy 및 충돌)
4. 🎯 Phase 10 완료 (UI)
5. 최적화 및 버그 수정

---

## 🎉 현재 상태

**완료 시**: 멀티플레이어 이동 동기화 완료!

**다음 목표**: Phase 6 - AutoShoot 시스템 Network 대응

---

**문서 버전**: v2.0
**최종 수정**: 2025-12-09
**작성자**: Claude + unity-dots-ecs Skill
