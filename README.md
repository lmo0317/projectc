# ProjectC - 프로젝트 기술 문서

## 개요

Unity 6 (6000.1.7f1) 기반의 **멀티플레이어 우주 슈팅 게임**입니다. 사이버펑크 레트로 스타일의 시각적 테마와 함께, Unity DOTS (Data-Oriented Technology Stack)와 Netcode for Entities를 활용한 고성능 네트워크 게임입니다.

**기술 스택:**
- Unity Entities 패키지 (ECS 아키텍처)
- Netcode for Entities (서버-클라이언트 동기화)
- Burst Compiler (고성능 연산)
- Unity Physics (물리 충돌)
- URP (Universal Render Pipeline)

---

## 1. DOTS (Data-Oriented Technology Stack) 아키텍처

### 1.1 ECS 개념

ECS는 세 가지 핵심 요소로 구성됩니다:

| 요소 | 설명 | 예시 |
|------|------|------|
| **Entity** | 고유 식별자 (경량화된 GameObject) | 플레이어, 적, 총알 |
| **Component** | 데이터만 포함 (로직 없음) | `PlayerHealth`, `MovementSpeed` |
| **System** | 로직 처리 (데이터 변환) | `PlayerMovementSystem`, `BulletHitSystem` |

### 1.2 Component 목록

#### 플레이어 관련 Component

| Component | 파일 위치 | 설명 |
|-----------|-----------|------|
| `PlayerTag` | Components/PlayerTag.cs | 플레이어 Entity 식별 태그 |
| `PlayerInput` | Components/PlayerInput.cs | 플레이어 입력 (IInputComponentData) |
| `PlayerHealth` | Components/PlayerHealth.cs | 체력 (CurrentHealth, MaxHealth) |
| `PlayerDead` | Components/PlayerDead.cs | 사망 상태 (비활성화 가능) |
| `PlayerStarPoints` | Components/PlayerStarPoints.cs | 별 포인트 (버프 언락용) |
| `MovementSpeed` | Components/MovementSpeed.cs | 이동 속도 |
| `AutoShootConfig` | Components/AutoShootConfig.cs | 자동 발사 설정 |

```csharp
// PlayerHealth - 네트워크 동기화 컴포넌트 예시
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerHealth : IComponentData
{
    [GhostField] public float CurrentHealth;
    [GhostField] public float MaxHealth;
}
```

#### 적(Enemy) 관련 Component

| Component | 파일 위치 | 설명 |
|-----------|-----------|------|
| `EnemyTag` | Components/EnemyTag.cs | 적 Entity 식별 태그 |
| `EnemyHealth` | Components/EnemyHealth.cs | 적 체력 |
| `EnemySpeed` | Components/EnemySpeed.cs | 적 이동 속도 |
| `EnemySpawnConfig` | Components/EnemySpawnConfig.cs | 적 스폰 설정 |

#### 투사체 관련 Component

| Component | 파일 위치 | 설명 |
|-----------|-----------|------|
| `BulletTag` | Components/BulletTag.cs | 총알 식별 태그 |
| `BulletDirection` | Components/BulletDirection.cs | 총알 이동 방향 |
| `BulletSpeed` | Components/BulletSpeed.cs | 총알 속도 |
| `BulletLifetime` | Components/BulletLifetime.cs | 총알 수명 |
| `DamageValue` | Components/DamageValue.cs | 데미지 값 |
| `MissileTag` | Components/MissileTag.cs | 미사일 식별 태그 |
| `MissileTarget` | Components/MissileTarget.cs | 미사일 타겟 Entity |
| `MissileTurnSpeed` | Components/MissileTurnSpeed.cs | 미사일 선회 속도 |

#### 아이템 관련 Component

| Component | 파일 위치 | 설명 |
|-----------|-----------|------|
| `StarTag` | Components/Items/StarTag.cs | 별 아이템 태그 |
| `StarId` | Components/Items/StarId.cs | 별 고유 ID |
| `StarValue` | Components/Items/StarValue.cs | 별 포인트 값 |
| `StarSpawnConfig` | Components/Items/StarSpawnConfig.cs | 별 스폰 설정 |

### 1.3 System 목록

#### 게임플레이 System (서버 실행)

| System | 파일 위치 | 설명 |
|--------|-----------|------|
| `AutoShootSystem` | Systems/AutoShootSystem.cs | 자동 발사 처리 |
| `BulletMovementSystem` | Systems/BulletMovementSystem.cs | 총알 이동 |
| `BulletHitSystem` | Systems/BulletHitSystem.cs | 총알-적 충돌 처리 |
| `BulletLifetimeSystem` | Systems/BulletLifetimeSystem.cs | 총알 수명 관리 |
| `MissileGuidanceSystem` | Systems/MissileGuidanceSystem.cs | 미사일 유도 |
| `EnemySpawnSystem` | Systems/EnemySpawnSystem.cs | 적 스폰 |
| `EnemyChaseSystem` | Systems/EnemyChaseSystem.cs | 적 추적 + 분산 AI |
| `PlayerDamageSystem` | Systems/PlayerDamageSystem.cs | 플레이어 피격 처리 |
| `GameOverSystem` | Systems/GameOverSystem.cs | 게임오버 처리 |
| `StarCollectSystem` | Systems/Items/StarCollectSystem.cs | 별 수집 처리 |

```csharp
// System 구조 예시 - EnemyChaseSystem
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]  // 서버에서만 실행
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 가장 가까운 플레이어 추적 + Enemy간 충돌 회피 로직
    }
}
```

#### 클라이언트 전용 System

| System | 파일 위치 | 설명 |
|--------|-----------|------|
| `GatherPlayerInputSystem` | Systems/Network/GatherPlayerInputSystem.cs | 입력 수집 |
| `ClientStarVisualSystem` | Systems/Items/ClientStarVisualSystem.cs | 별 시각화 |
| `ClientMagnetVisualSystem` | Systems/Items/ClientMagnetVisualSystem.cs | 자석 범위 시각화 |
| `HitEffectClientSystem` | Systems/Network/HitEffectClientSystem.cs | 피격 이펙트 |
| `BuffSelectionClientSystem` | Systems/Buffs/BuffSelectionClientSystem.cs | 버프 선택 UI |

### 1.4 Authoring (Baker)

Authoring 클래스는 Unity Editor의 GameObject를 ECS Entity로 변환합니다.

| Authoring | 파일 위치 | 설명 |
|-----------|-----------|------|
| `PlayerAuthoring` | Authoring/PlayerAuthoring.cs | 플레이어 Entity 베이킹 |
| `EnemyAuthoring` | Authoring/EnemyAuthoring.cs | 적 Entity 베이킹 |
| `BulletAuthoring` | Authoring/BulletAuthoring.cs | 총알 Entity 베이킹 |
| `EnemySpawnAuthoring` | Authoring/EnemySpawnAuthoring.cs | 적 스폰 설정 베이킹 |
| `StarAuthoring` | Authoring/StarAuthoring.cs | 별 Entity 베이킹 |
| `StarSpawnAuthoring` | Authoring/StarSpawnAuthoring.cs | 별 스폰 설정 베이킹 |

```csharp
// PlayerAuthoring 예시
public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float FireRate = 0.25f;
    public GameObject BulletPrefab;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent<PlayerInput>(entity);
            // ... 추가 컴포넌트들
        }
    }
}
```

---

## 2. Netcode for Entities 아키텍처

### 2.1 서버-클라이언트 구조

```
┌─────────────────────────────────────────────────────────────┐
│                         서버 (Server World)                  │
│  - 게임 로직 실행 (적 스폰, 충돌 처리, 데미지 계산)          │
│  - 권위적 상태 관리                                          │
│  - Ghost 엔티티 동기화                                       │
└─────────────────────────────────────────────────────────────┘
                              ↕ Ghost Sync / RPC
┌─────────────────────────────────────────────────────────────┐
│                       클라이언트 (Client World)              │
│  - 입력 수집 및 전송                                         │
│  - 예측 시뮬레이션 (Prediction)                              │
│  - UI 및 이펙트 표시                                         │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 서버 전용 System

`WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)` 어트리뷰트로 서버에서만 실행되는 System:

| System | 역할 |
|--------|------|
| `PlayerSpawnSystem` | 클라이언트 연결 시 플레이어 스폰 |
| `PlayerRespawnSystem` | 플레이어 부활 요청 처리 |
| `PlayerDisconnectCleanupSystem` | 연결 해제 시 정리 |
| `EnemySpawnSystem` | 적 생성 |
| `EnemyChaseSystem` | 적 AI |
| `BulletHitSystem` | 총알 충돌 판정 |
| `PlayerDamageSystem` | 플레이어 피격 |
| `BuffSelectionSystem` | 버프 선택 로직 |
| `BuffApplySystem` | 버프 적용 |
| `StatCalculationSystem` | 스탯 계산 |
| `MagnetSystem` | 자석 효과 |
| `HealthRegenSystem` | 체력 재생 |
| `StarCollectSystem` | 별 수집 |
| `GameSessionSystem` | 게임 세션 관리 |

```csharp
// 서버 전용 System 예시
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 새로 연결된 클라이언트에 플레이어 Entity 생성
        foreach (var connectionEntity in newConnectionQuery)
        {
            var player = state.EntityManager.Instantiate(prefab);
            state.EntityManager.SetComponentData(player, new GhostOwner { NetworkId = networkId });
        }
    }
}
```

### 2.3 클라이언트 전용 System

`WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)` 어트리뷰트로 클라이언트에서만 실행되는 System:

| System | 역할 |
|--------|------|
| `GatherPlayerInputSystem` | 키보드/게임패드 입력 수집 |
| `HitEffectClientSystem` | 피격 이펙트 표시 |
| `ClientStarVisualSystem` | 별 아이템 비주얼 |
| `ClientMagnetVisualSystem` | 자석 범위 인디케이터 |
| `BuffSelectionClientSystem` | 버프 선택 UI 처리 |
| `ReceiveKillCountRpcSystem` | 킬 카운트 수신 |

```csharp
// 클라이언트 전용 System 예시
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GatherPlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);

        foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            if (left) input.ValueRW.Horizontal -= 1;
            if (right) input.ValueRW.Horizontal += 1;
        }
    }
}
```

### 2.4 양쪽에서 실행되는 System

| System | 역할 |
|--------|------|
| `GoInGameSystem` | 네트워크 연결을 InGame 상태로 전환 |
| `ProcessPlayerInputSystem` | 입력을 이동으로 변환 (예측 시뮬레이션) |

```csharp
// 양쪽 실행 System 예시
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial class GoInGameSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // NetworkId가 설정되면 InGame 상태로 전환
        foreach (var (id, ent) in SystemAPI.Query<NetworkId>()
            .WithNone<NetworkStreamInGame>()
            .WithEntityAccess())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(ent);
        }
    }
}
```

### 2.5 Ghost 동기화

Ghost 컴포넌트는 서버에서 클라이언트로 자동 동기화됩니다:

```csharp
// Ghost 동기화 예시
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerHealth : IComponentData
{
    [GhostField] public float CurrentHealth;  // 자동 동기화
    [GhostField] public float MaxHealth;      // 자동 동기화
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerBuffs : IComponentData
{
    [GhostField] public int DamageLevel;      // 버프 레벨 동기화
    [GhostField] public int SpeedLevel;
    // ...
}
```

---

## 3. RPC (Remote Procedure Call) 시스템

### 3.1 RPC 개요

RPC는 서버와 클라이언트 간 비동기 통신에 사용됩니다.

| 방향 | 용도 |
|------|------|
| Server → Client | 이벤트 알림 (킬, 이펙트, UI 표시) |
| Client → Server | 요청 (버프 선택, 부활 요청) |

### 3.2 RPC 목록

#### Server → Client RPC

| RPC | 파일 위치 | 설명 |
|-----|-----------|------|
| `HitEffectRpc` | Components/Network/HitEffectRpc.cs | 피격 이펙트 위치, 데미지, 치명타 여부 |
| `KillCountRpc` | Components/Network/KillCountRpc.cs | 킬 카운트 증가 |
| `ShowBuffSelectionRpc` | Components/Network/ShowBuffSelectionRpc.cs | 버프 선택 UI 표시 요청 |
| `BuffAppliedRpc` | Components/Network/BuffAppliedRpc.cs | 버프 적용 알림 |
| `GamePauseRpc` | Components/Network/GamePauseRpc.cs | 게임 일시정지/재개 |
| `StarSpawnRpc` | Components/Network/StarSpawnRpc.cs | 별 스폰 알림 |
| `StarDestroyRpc` | Components/Network/StarDestroyRpc.cs | 별 삭제 알림 |
| `StarCollectRpc` | Components/Network/StarCollectRpc.cs | 별 수집 이펙트 |

#### Client → Server RPC

| RPC | 파일 위치 | 설명 |
|-----|-----------|------|
| `BuffSelectedRpc` | Components/Network/BuffSelectedRpc.cs | 버프 선택 결과 |
| `RespawnRequestRpc` | Components/Network/RespawnRequestRpc.cs | 부활 요청 |
| `DebugKillRequestRpc` | Components/Network/DebugKillRequestRpc.cs | 디버그 킬 요청 |
| `DebugAddPointsRpc` | Components/Network/DebugAddPointsRpc.cs | 디버그 포인트 추가 |

### 3.3 RPC 구현 예시

```csharp
// RPC 정의
[BurstCompile]
public struct HitEffectRpc : IRpcCommand
{
    public float3 Position;   // 피격 위치
    public float Damage;      // 데미지 양
    public bool IsCritical;   // 치명타 여부
}

// RPC 전송 (Server → Client)
var hitEffectRpcEntity = ecb.CreateEntity();
ecb.AddComponent(hitEffectRpcEntity, new HitEffectRpc
{
    Position = enemyPos,
    Damage = finalDamage,
    IsCritical = isCritical
});
ecb.AddComponent(hitEffectRpcEntity, new SendRpcCommandRequest
{
    TargetConnection = connectionEntity  // 특정 클라이언트에게
});

// RPC 수신 (Client)
foreach (var (rpc, receiveRpc, entity) in
    SystemAPI.Query<RefRO<HitEffectRpc>, RefRO<ReceiveRpcCommandRequest>>()
        .WithEntityAccess())
{
    // 이펙트 표시
    HitEffectPool.Instance?.SpawnHitEffect(rpc.ValueRO.Position, rpc.ValueRO.Damage, rpc.ValueRO.IsCritical);
    ecb.DestroyEntity(entity);  // RPC 엔티티 정리
}
```

### 3.4 RPC 흐름도

#### 버프 선택 플로우
```
[Server]                                    [Client]
    │                                           │
    │  ← Star 수집, 포인트 임계값 도달 →        │
    │                                           │
    ├──── ShowBuffSelectionRpc ────────────────>│
    │     (옵션 3개 + 현재 레벨)                │
    │                                           │
    ├──── GamePauseRpc (IsPaused=true) ────────>│
    │                                           │
    │      (유저가 버프 선택)                   │
    │                                           │
    │<─── BuffSelectedRpc ─────────────────────┤
    │     (선택한 버프 타입)                    │
    │                                           │
    ├──── BuffAppliedRpc ──────────────────────>│
    │     (적용된 버프 알림)                    │
    │                                           │
    ├──── GamePauseRpc (IsPaused=false) ───────>│
    │                                           │
```

#### 피격 이펙트 플로우
```
[Server]                                    [Client]
    │                                           │
    │  총알이 적에게 명중                       │
    │                                           │
    ├──── HitEffectRpc ────────────────────────>│
    │     (Position, Damage, IsCritical)        │
    │                                           │
    │            (파티클 이펙트 + 데미지 팝업)  │
    │                                           │
```

---

## 4. 버프 시스템

### 4.1 버프 종류

게임에는 8가지 버프가 있으며, 각 버프는 최대 5레벨까지 성장합니다.

| 버프 | 열거형 | 색상 | 효과 |
|------|--------|------|------|
| Damage | `BuffType.Damage` | 빨강 | 데미지 증가 |
| Speed | `BuffType.Speed` | 하늘색 | 이동 속도 증가 |
| FireRate | `BuffType.FireRate` | 주황 | 공격 속도 증가 |
| MissileCount | `BuffType.MissileCount` | 보라 | 미사일 개수 증가 |
| Magnet | `BuffType.Magnet` | 초록 | 아이템 흡수 범위 |
| HealthRegen | `BuffType.HealthRegen` | 분홍 | 초당 체력 재생 |
| MaxHealth | `BuffType.MaxHealth` | 핫핑크 | 최대 체력 증가 |
| Critical | `BuffType.Critical` | 노랑 | 치명타 확률 |

### 4.2 버프 레벨별 효과

#### Damage (데미지 증가)
| 레벨 | 보너스 |
|------|--------|
| Lv1 | +10% |
| Lv2 | +20% |
| Lv3 | +35% |
| Lv4 | +50% |
| Lv5 | +75% |

#### Speed (이동 속도)
| 레벨 | 보너스 |
|------|--------|
| Lv1 | +10% |
| Lv2 | +20% |
| Lv3 | +30% |
| Lv4 | +40% |
| Lv5 | +50% |

#### FireRate (공격 속도)
| 레벨 | 쿨다운 감소 |
|------|-------------|
| Lv1 | -15% |
| Lv2 | -30% |
| Lv3 | -45% |
| Lv4 | -60% |
| Lv5 | -80% |

#### MissileCount (미사일 개수)
| 레벨 | 추가 개수 |
|------|-----------|
| Lv1 | +1 |
| Lv2 | +2 |
| Lv3 | +3 |
| Lv4 | +4 |
| Lv5 | +6 |

#### Magnet (자석 범위)
| 레벨 | 범위 |
|------|------|
| Lv1 | 3 |
| Lv2 | 5 |
| Lv3 | 7 |
| Lv4 | 10 |
| Lv5 | 15 |

#### HealthRegen (체력 재생)
| 레벨 | 초당 회복 |
|------|-----------|
| Lv1 | 1 HP/s |
| Lv2 | 2 HP/s |
| Lv3 | 3 HP/s |
| Lv4 | 5 HP/s |
| Lv5 | 8 HP/s |

#### MaxHealth (최대 체력)
| 레벨 | 추가 HP |
|------|---------|
| Lv1 | +20 |
| Lv2 | +40 |
| Lv3 | +70 |
| Lv4 | +100 |
| Lv5 | +150 |

#### Critical (치명타)
| 레벨 | 확률 | 배율 |
|------|------|------|
| Lv1 | 5% | 2.0x |
| Lv2 | 10% | 2.0x |
| Lv3 | 15% | 2.5x |
| Lv4 | 20% | 2.5x |
| Lv5 | 30% | 3.0x |

### 4.3 버프 선택 트리거

버프 선택은 포인트 임계값에 도달하면 발동됩니다:

| 선택 횟수 | 필요 포인트 |
|-----------|-------------|
| 1차 | 10 |
| 2차 | 15 |
| 3차 | 20 |
| 4차 | 30 |
| 5차 | 40 |
| 6차+ | +15씩 증가 |

### 4.4 버프 관련 Component

```csharp
// 버프 레벨 저장 (네트워크 동기화)
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerBuffs : IComponentData
{
    [GhostField] public int DamageLevel;
    [GhostField] public int SpeedLevel;
    [GhostField] public int FireRateLevel;
    [GhostField] public int MissileCountLevel;
    [GhostField] public int MagnetLevel;
    [GhostField] public int HealthRegenLevel;
    [GhostField] public int MaxHealthLevel;
    [GhostField] public int CriticalLevel;

    public const int MaxLevel = 5;
}

// 계산된 스탯 수정치
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct StatModifiers : IComponentData
{
    [GhostField] public float DamageMultiplier;      // 1.0 = 100%
    [GhostField] public float FireRateMultiplier;    // 낮을수록 빠름
    [GhostField] public int BonusMissileCount;
    [GhostField] public float SpeedMultiplier;
    [GhostField] public float BonusMaxHealth;
    [GhostField] public float HealthRegenPerSecond;
    [GhostField] public float CriticalChance;
    [GhostField] public float CriticalMultiplier;
    [GhostField] public float MagnetRange;
}
```

### 4.5 버프 관련 System

| System | 역할 |
|--------|------|
| `StatCalculationSystem` | PlayerBuffs → StatModifiers 변환 |
| `BuffSelectionSystem` | 포인트 임계값 도달 시 버프 선택 트리거 |
| `BuffApplySystem` | 클라이언트 선택 결과 적용 |
| `BuffSelectionClientSystem` | 버프 선택 UI 표시/처리 |
| `HealthRegenSystem` | 체력 재생 효과 적용 |
| `MagnetSystem` | 자석 효과로 아이템 끌어당김 |

---

## 5. 게임 컨텐츠

### 5.1 게임 흐름

1. **로비 (LobbyScene)**
   - 서버 시작 또는 클라이언트 접속
   - `NetworkConnectionManager`로 연결 관리

2. **게임 (SampleScene)**
   - 플레이어 스폰 (`PlayerSpawnSystem`)
   - 적 스폰 시작 (`EnemySpawnSystem`)
   - 자동 발사 (`AutoShootSystem`)

3. **게임플레이 루프**
   - 적 처치 → 별 드롭 (`BulletHitSystem`)
   - 별 수집 → 포인트 획득 (`StarCollectSystem`)
   - 포인트 도달 → 버프 선택 (`BuffSelectionSystem`)
   - 게임 일시정지 중 버프 선택 UI
   - 버프 적용 후 게임 재개

4. **게임오버**
   - 플레이어 체력 0 → 사망 (`PlayerDamageSystem`)
   - 부활 요청 가능 (`RespawnRequestRpc`)

### 5.2 전투 시스템

#### 자동 발사
- 가장 가까운 적을 자동 타겟팅
- 좌우 교대 발사 (좌우 날개)
- 다중 미사일 부채꼴 패턴

```csharp
// 미사일 부채꼴 발사 로직
for (int i = 0; i < missileCount; i++)
{
    float spreadAngle = 30f;  // 총 펼침 각도
    float angleStep = spreadAngle / (missileCount - 1);
    float currentAngle = -spreadAngle / 2f + angleStep * i;
    quaternion rotation = quaternion.AxisAngle(math.up(), math.radians(currentAngle));
    shootDirection = math.mul(rotation, playerForward);
}
```

#### 미사일 유도
- `MissileTarget` 컴포넌트로 타겟 추적
- `MissileTurnSpeed`로 선회 속도 제어

#### 적 AI
- 가장 가까운 플레이어 추적
- Enemy 간 분산 (Separation) 알고리즘
- 충돌 회피 반경 5.0 유닛

### 5.3 아이템 시스템

#### 별 (Star)
- 적 처치 시 드롭
- 수집 시 포인트 획득
- 자석 버프로 자동 수집 범위 증가

### 5.4 UI 시스템

| UI | 파일 위치 | 설명 |
|----|-----------|------|
| `PlayerStatsUI` | UI/PlayerStatsUI.cs | 체력바, 포인트 표시 |
| `BuffSelectionUI` | UI/BuffSelectionUI.cs | 버프 선택 화면 |
| `BuffIconsUI` | UI/BuffIconsUI.cs | 획득한 버프 아이콘 |
| `BuffOptionCard` | UI/BuffOptionCard.cs | 버프 선택 카드 |
| `DamagePopupManager` | UI/DamagePopupManager.cs | 데미지 숫자 표시 |
| `MagnetRangeIndicator` | UI/MagnetRangeIndicator.cs | 자석 범위 시각화 |
| `FPSDisplay` | UI/FPSDisplay.cs | FPS 표시 |
| `LobbyUI` | UI/LobbyUI.cs | 로비 UI |
| `UIManager` | UI/UIManager.cs | UI 총괄 관리 |

---

## 6. 네트워크 부트스트랩

### 6.1 SimpleNetworkBootstrap

```csharp
[UnityEngine.Scripting.Preserve]
public class SimpleNetworkBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;

        var activeScene = SceneManager.GetActiveScene().name;

        // 로비 씬: 자동 연결 비활성화
        if (activeScene == "LobbyScene")
        {
            AutoConnectPort = 0;
            CreateLocalWorld(defaultWorldName);
            return true;
        }

        // 게임 씬: 자동 연결 (테스트용)
        AutoConnectPort = 7979;
        CreateDefaultClientServerWorlds();

        // Tick Rate 설정
        SetTickRate(world);  // 20Hz

        return true;
    }
}
```

### 6.2 틱 레이트 설정

| 설정 | 값 |
|------|-----|
| SimulationTickRate | 20 Hz |
| NetworkTickRate | 20 Hz |
| MaxSimulationStepsPerFrame | 8 |

---

## 7. 디렉토리 구조

```
Assets/Scripts/
├── Authoring/                    # Baker 클래스
│   ├── Network/                  # 네트워크 관련 Authoring
│   ├── PlayerAuthoring.cs
│   ├── EnemyAuthoring.cs
│   ├── BulletAuthoring.cs
│   └── ...
├── Buffs/
│   └── Core/                     # 버프 열거형
│       ├── BuffType.cs
│       └── StatType.cs
├── Camera/
│   └── PlayerFollowCamera.cs
├── Components/                   # ECS 컴포넌트
│   ├── Buffs/                    # 버프 관련 컴포넌트
│   ├── Items/                    # 아이템 관련 컴포넌트
│   ├── Network/                  # RPC 정의
│   ├── PlayerTag.cs
│   ├── PlayerHealth.cs
│   └── ...
├── Editor/                       # 에디터 도구
├── Effects/                      # 이펙트 풀
│   ├── HitEffectPool.cs
│   ├── StarCollectEffectPool.cs
│   └── BuffEffectPool.cs
├── Network/                      # 네트워크 관리
│   ├── NetworkConnectionManager.cs
│   ├── SimpleNetworkBootstrap.cs
│   └── ConnectionDebugSystem.cs
├── Sound/
│   └── GameSoundManager.cs
├── Systems/                      # ECS 시스템
│   ├── Buffs/                    # 버프 시스템
│   ├── Items/                    # 아이템 시스템
│   ├── Network/                  # 네트워크 시스템
│   ├── UI/                       # UI 시스템
│   ├── AutoShootSystem.cs
│   ├── EnemySpawnSystem.cs
│   ├── EnemyChaseSystem.cs
│   └── ...
└── UI/                           # UI 스크립트
    ├── BuffSelectionUI.cs
    ├── PlayerStatsUI.cs
    └── ...
```

---

## 8. 주의사항 및 Best Practices

### 8.1 ECS 주의사항

1. **RefRW 직접 수정**
```csharp
// ❌ 잘못됨: 복사본 생성
var config = shootConfig.ValueRW;
config.Timer += deltaTime;  // 원본 변경 안 됨!

// ✅ 올바름: 직접 접근
shootConfig.ValueRW.Timer += deltaTime;
```

2. **TransformUsageFlags 설정**
```csharp
// ❌ 잘못됨: 렌더링 안 됨
var entity = GetEntity(TransformUsageFlags.Dynamic);

// ✅ 올바름: 렌더링 + 이동
var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
```

### 8.2 네트워크 주의사항

1. **서버 권위적 상태**
   - 게임 로직은 서버에서만 실행
   - 클라이언트는 입력 전송 + 비주얼만 처리

2. **Ghost 동기화**
   - `[GhostField]`로 자동 동기화되는 필드 지정
   - 예측이 필요한 컴포넌트는 `GhostPrefabType.AllPredicted`

3. **RPC 사용**
   - 즉시 반응이 필요한 이벤트에 사용
   - 대역폭 고려하여 최소한의 데이터만 전송

---

## 9. ECS 핵심 개념 (Quick Reference)

### 9.1 SystemGroup 실행 순서

```
InitializationSystemGroup → SimulationSystemGroup → PresentationSystemGroup
```

| SystemGroup | 역할 | 예시 |
|-------------|------|------|
| **Initialization** | 입력 수집, 네트워크 메시지 수신 | `GatherPlayerInputSystem` |
| **Simulation** | 게임 로직, 물리, AI | `EnemyChaseSystem`, `BulletHitSystem` |
| **Presentation** | 렌더링 준비, VFX | 카메라, 애니메이션 |

### 9.2 SystemBase vs ISystem

| 특징 | SystemBase (Class) | ISystem (Struct) |
|------|-------------------|------------------|
| **Burst 컴파일** | ❌ 불가능 | ✅ 가능 |
| **Unity API** | ✅ 가능 (`Input.GetKey`) | ❌ 불가능 |
| **성능** | 보통 | 높음 (권장) |
| **사용 시기** | Unity API 필요 시 | 순수 계산 로직 |

### 9.3 EntityCommandBuffer (ECB)

OnUpdate 중 Entity 구조 변경 시 반드시 ECB 사용:

```csharp
// ❌ 에러 발생
state.EntityManager.DestroyEntity(entity);

// ✅ ECB 사용
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                  .CreateCommandBuffer(state.WorldUnmanaged);
ecb.DestroyEntity(entity);
```

### 9.4 IJobEntity의 Execute

Execute 함수는 Unity ECS가 **자동으로** 조건에 맞는 모든 Entity에 대해 호출합니다:

```csharp
[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;

    // Unity ECS가 LocalTransform + EnemySpeed를 가진 모든 Entity에 대해 호출
    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        float3 direction = math.normalizesafe(PlayerPosition - transform.Position);
        transform.Position += direction * speed.Value;
    }
}

// 사용
new EnemyChaseJob { PlayerPosition = pos }.ScheduleParallel();
```

---

## 10. Netcode 핵심 개념 (Quick Reference)

### 10.1 네트워크 연결 흐름

```
[Client 연결]
    ↓
NetworkId 컴포넌트 생성
    ↓
GoInGameSystem 실행
    ↓
NetworkStreamInGame 태그 추가 ← 핵심!
    ↓
Ghost 스냅샷 동기화 시작
    ↓
PlayerSpawnSystem 실행 (Server)
    ↓
플레이어 Entity 생성 및 동기화
```

### 10.2 IInputComponentData

네트워크 입력 전용 인터페이스:

```csharp
public struct PlayerInput : IInputComponentData
{
    public int Horizontal;  // -1, 0, 1 (대역폭 절약)
    public int Vertical;
}
```

**핵심 규칙:**
1. 매 프레임 `input.ValueRW = default;` 초기화 필수
2. `GhostOwnerIsLocal`로 내 플레이어만 필터링
3. 입력 수집: `GhostInputSystemGroup`
4. 입력 처리: `PredictedSimulationSystemGroup`

### 10.3 RPC vs Ghost

| | RPC | Ghost 컴포넌트 |
|---|---|---|
| **용도** | 일회성 이벤트 | 지속적 상태 |
| **전송** | 이벤트 발생 시 1회 | 매 프레임 자동 |
| **예시** | 채팅, 킬 알림, 아이템 획득 | HP, Position, Score |
| **대역폭** | 낮음 (필요 시만) | 높음 |

**RPC 사용 시 필수:**
```csharp
// RPC Entity는 처리 후 반드시 삭제!
ecb.DestroyEntity(entity);
```

### 10.4 플레이어 스폰 7단계

```csharp
// 1. 스폰 위치 설정
localTransform.Position.x += networkId.Value * 2;

// 2. GhostOwner 설정 (네트워크 소유권)
new GhostOwner { NetworkId = networkId.Value }

// 3. CommandTarget 설정 (입력 라우팅)
new CommandTarget { targetEntity = player }

// 4. LinkedEntityGroup에 추가 (자동 정리)
LinkedEntityGroup에 player 추가

// 5. ConnectionOwner 추가 (역참조)
new ConnectionOwner { Entity = connectionEntity }

// 6. PlayerSpawned 마커 (중복 방지)
AddComponent<PlayerSpawned>(connectionEntity)
```

### 10.5 RequireForUpdate vs WithAll

```csharp
public void OnCreate(ref SystemState state)
{
    // RequireForUpdate: "최소 1개 있어야 시스템 실행"
    state.RequireForUpdate<NetworkStreamInGame>();

    // Query의 WithAll: "이 컴포넌트를 가진 Entity만 필터링"
    m_Query = SystemAPI.QueryBuilder()
        .WithAll<NetworkId>()
        .WithNone<PlayerSpawned>()
        .Build();
}
```

---

## 11. 디버깅 체크리스트

### 네트워크 동기화 안 될 때

1. [ ] GoInGameSystem 존재 확인
2. [ ] EnableGoInGameAuthoring 씬에 배치됨
3. [ ] NetworkStreamInGame 태그 확인 (Entity Hierarchy)
4. [ ] GhostCollection의 Num Loaded Prefabs > 0
5. [ ] Network Debugger에서 Snapshot Ack 확인

### 플레이어 스폰 안 될 때

1. [ ] Spawner 컴포넌트 존재 확인
2. [ ] SpawnerAuthoring에 Player Prefab 할당됨
3. [ ] Player.prefab에 GhostAuthoringComponent 있음
4. [ ] PlayerSpawnSystem 로그 확인

### RPC 전송/수신 안 될 때

1. [ ] `RequireForUpdate<NetworkId>()` 추가했는지
2. [ ] `SendRpcCommandRequest` 컴포넌트 추가했는지
3. [ ] 수신 후 `ecb.DestroyEntity(entity)` 했는지
4. [ ] WorldSystemFilter 확인 (Server/Client)

---

## 12. 참고 자료

- [Unity Entities 공식 문서](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Netcode for Entities 공식 문서](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
- [NetcodeSamples 저장소](https://github.com/Unity-Technologies/EntityComponentSystemSamples)

### 프로젝트 내 문서
- `Document/knowledge/knowledge_ecs.md` - ECS 상세 학습 노트
- `Document/knowledge/knowledge_netcode.md` - Netcode 상세 학습 노트
- `Document/spec.md` - 프로젝트 요구사항 명세
- `CLAUDE.md` - Claude Code 가이드라인
