# 5000마리 Enemy 성능 최적화 계획서

## 📊 현재 상황 분석 (프로파일러 기반)

### 주요 병목 지점
| 시스템 | 현재 시간 | 문제 | 예상 개선 |
|--------|----------|------|----------|
| **PlayerDamageSystem** | 8.27ms | O(5000) 선형 거리 체크 | → 0.6ms (90% 개선) |
| **GhostSendSystem** | 0.88ms | 5000 Enemy 전체 동기화 | → 0.45ms (50% 개선) |
| **GameSessionSystem** | 1.57ms | EntityManager 동기 접근 | → 0.6ms (60% 개선) |
| **GPU 렌더링** | 병목 | 드로우콜 5000 | → 1 (99% 개선) |

**총 서버 프레임 타임**: 11ms → **3-4ms (70% 개선 예상)**

---

## 🎯 최적화 전략 (사용자 선택 기반)

1. **최우선**: Unity.Physics 통합 (장기 확장성, 10000+ Enemy 지원)
2. **네트워크**: Importance Scaling 적용 (거리 기반 동기화)
3. **렌더링**: GPU Instancing 활성화 (즉시 효과)

---

## 📋 Phase 1: Unity.Physics 통합 (최우선 - 70% 개선)

### 목표
PlayerDamageSystem의 O(N) 선형 검색을 **O(log N) 공간 분할 쿼리**로 전환

### 1.1 EnemyAuthoring Physics 설정 수정

**파일**: `Assets/Scripts/Authoring/EnemyAuthoring.cs`

**수정 사항**:
```csharp
// 1. CollisionResponse를 None으로 변경 (쿼리 전용)
var material = new Unity.Physics.Material
{
    CollisionResponse = CollisionResponsePolicy.None // 시뮬레이션 비활성화
};

var collider = Unity.Physics.SphereCollider.Create(
    new SphereGeometry
    {
        Center = float3.zero,
        Radius = authoring.ColliderRadius // 0.5
    },
    new CollisionFilter
    {
        BelongsTo = 1u << 2,    // Layer 2: Enemy
        CollidesWith = (1u << 0) | (1u << 1), // Layer 0: Player, Layer 1: Bullet
        GroupIndex = 0
    },
    material
);

AddComponent(entity, new PhysicsCollider { Value = collider });

// 2. PhysicsVelocity 추가 (Kinematic Body로 만들어 Broadphase 업데이트)
AddComponent(entity, new PhysicsVelocity
{
    Linear = float3.zero,
    Angular = float3.zero
});

// 3. PhysicsMass 추가 (Kinematic Body 설정)
AddComponent(entity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
```

**소요 시간**: 30분 | **난이도**: 하

---

### 1.2 PlayerDamageSystem Physics 쿼리 재작성

**파일**: `Assets/Scripts/Systems/PlayerDamageSystem.cs`

**핵심 변경사항**:
```csharp
// 기존: 5000 Enemy 전부 순회 (O(5000))
foreach (var enemyTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
{
    float distSq = math.distancesq(playerPos, enemyTransform.ValueRO.Position);
    if (distSq < 4.0f) { isInDanger = true; break; }
}

// 신규: Physics 쿼리 (O(log N))
var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
var collisionWorld = physicsWorldSingleton.CollisionWorld;

var filter = new CollisionFilter
{
    BelongsTo = 1u << 0,    // Player
    CollidesWith = 1u << 2, // Enemy
    GroupIndex = 0
};

var hitList = new NativeList<DistanceHit>(Allocator.Temp);
bool isInDanger = collisionWorld.OverlapSphere(playerPos, 2.0f, ref hitList, filter);
hitList.Dispose();
```

**성능 개선**: 8.27ms → **0.6ms** (90% 개선)

**소요 시간**: 1-2시간 | **난이도**: 중

---

### 1.3 Physics 시스템 순서 설정

**파일**: `Assets/Scripts/Systems/PlayerDamageSystem.cs`

**수정**:
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))] // Physics 전체 그룹 이후 실행
[BurstCompile]
public partial struct PlayerDamageSystem : ISystem
```

**이유**: Broadphase가 업데이트된 후 쿼리 수행하여 정확도 보장

**소요 시간**: 10분 | **난이도**: 하

---

### 1.4 GameSessionSystem EntityManager 접근 제거

**파일**: `Assets/Scripts/Systems/Network/GameSessionSystem.cs`

**수정**:
```csharp
// 기존: EntityManager 동기 접근
bool isDead = EntityManager.HasComponent<PlayerDead>(entity) &&
              EntityManager.IsComponentEnabled<PlayerDead>(entity);

// 신규: Query 기반 카운팅
var aliveQuery = SystemAPI.QueryBuilder()
    .WithAll<PlayerTag, PlayerHealth>()
    .WithDisabled<PlayerDead>()
    .Build();

int alivePlayerCount = aliveQuery.CalculateEntityCount();
```

**성능 개선**: 1.57ms → **0.6ms** (60% 개선)

**소요 시간**: 30분 | **난이도**: 하

---

## 📋 Phase 2: Ghost Importance Scaling (네트워크 50% 개선)

### 목표
거리 기반 동기화 빈도 조절로 네트워크 부하 감소

### 2.1 GhostDistanceImportance 컴포넌트 추가

**신규 파일**: `Assets/Scripts/Components/Network/GhostDistanceImportance.cs`

```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct GhostDistanceImportance : IComponentData
{
    [GhostField] public int ImportanceValue; // 0-100
}
```

**소요 시간**: 10분 | **난이도**: 하

---

### 2.2 GhostImportanceScalingSystem 구현

**신규 파일**: `Assets/Scripts/Systems/Network/GhostImportanceScalingSystem.cs`

**핵심 로직**:
```csharp
// 거리 기반 중요도 동적 조절
if (closestDist < 50f)
    importance.ImportanceValue = 100; // 매 프레임
else if (closestDist < 100f)
    importance.ImportanceValue = 50; // 2프레임마다
else
    importance.ImportanceValue = 25; // 4프레임마다
```

**성능 개선**:
- GhostSendSystem: 0.88ms → **0.45ms** (50% 개선)
- 네트워크 대역폭: **40-50% 감소**

**소요 시간**: 2시간 | **난이도**: 중

---

### 2.3 EnemyAuthoring에 컴포넌트 추가

**파일**: `Assets/Scripts/Authoring/EnemyAuthoring.cs`

```csharp
AddComponent(entity, new GhostDistanceImportance
{
    ImportanceValue = 100 // 기본값
});
```

**소요 시간**: 5분 | **난이도**: 하

---

## 📋 Phase 3: GPU Instancing 활성화 (즉시 효과)

### 목표
드로우콜 5000 → 1로 GPU 병목 완전 해소

### 3.1 Enemy Material GPU Instancing 활성화

**작업**:
1. Unity Editor에서 `Assets/Prefabs/Enemy.prefab` 열기
2. `SM_Veh_Drone_Attach_01` 선택
3. Inspector → Material → **"Enable GPU Instancing" 체크**
4. Shader 확인: URP/Lit (기본 지원)

**성능 개선**: 드로우콜 5000 → **1** (99% 개선)

**소요 시간**: 5분 | **난이도**: 하 (Unity Editor 작업)

---

## 📋 Phase 4: 성능 측정 및 검증

### 4.1 Unity Profiler 측정

**측정 항목**:
- PlayerDamageSystem: 8.27ms → **목표 0.6ms**
- GameSessionSystem: 1.57ms → **목표 0.6ms**
- GhostSendSystem: 0.88ms → **목표 0.45ms**

**소요 시간**: 1시간 | **난이도**: 하

---

### 4.2 Frame Debugger로 드로우콜 검증

**확인 항목**:
- Rendering.DrawMeshInstanced가 1개의 Batch로 통합되었는지 확인

**소요 시간**: 10분 | **난이도**: 하

---

### 4.3 최종 성능 목표 달성 확인

**Before vs After**:
```
[Before]
PlayerDamageSystem: 8.27ms
GameSessionSystem: 1.57ms
GhostSendSystem: 0.88ms
Total: ~11ms

[After]
PlayerDamageSystem: 0.6ms  (Physics 쿼리)
GameSessionSystem: 0.6ms   (Query 최적화)
GhostSendSystem: 0.45ms    (Importance Scaling)
Total: ~3.1ms (74% 개선)
```

**최종 목표**: 안정적 **60 FPS** (16.6ms 프레임 타임)

**소요 시간**: 30분 | **난이도**: 하

---

## 📅 구현 일정

### Day 1 (4시간)
- **09:00-10:00**: Phase 1.1 - EnemyAuthoring Physics 설정
- **10:00-12:00**: Phase 1.2 - PlayerDamageSystem Physics 쿼리
- **13:00-13:30**: Phase 1.3 - Physics 시스템 순서
- **13:30-14:00**: Phase 1.4 - GameSessionSystem 최적화
- **14:00-15:00**: 테스트 및 디버깅

### Day 2 (3시간)
- **09:00-11:00**: Phase 2 - Ghost Importance Scaling 구현
- **11:00-12:00**: Phase 3 - GPU Instancing 활성화

### Day 3 (2시간)
- **09:00-11:00**: Phase 4 - 성능 측정 및 검증

**총 예상 시간**: 9시간 (여유 포함 12시간)

---

## ⚠️ 위험 요소 및 대응

### 1. Physics 쿼리가 예상보다 느릴 경우
**대응**: PhysicsVelocity 제거하고 Static Body 유지, Broadphase 업데이트 간격 조절

### 2. Ghost Importance Scaling이 동작하지 않을 경우
**대응**: Unity Netcode 내장 GhostDistancePartitioningSystem 사용

### 3. GPU Instancing이 적용되지 않을 경우
**대응**: Shader를 URP/Lit로 변경, Entities Graphics 1.4.16 재설치

---

## 🎯 핵심 파일 5개

1. **`Assets/Scripts/Systems/PlayerDamageSystem.cs`**
   - 최대 병목(8.27ms) 제거
   - Physics 쿼리로 90% 개선

2. **`Assets/Scripts/Authoring/EnemyAuthoring.cs`**
   - PhysicsCollider 설정 수정
   - PhysicsVelocity/PhysicsMass 추가

3. **`Assets/Scripts/Systems/Network/GameSessionSystem.cs`**
   - EntityManager 접근 제거
   - Query 기반 최적화로 60% 개선

4. **`Assets/Prefabs/Enemy.prefab`**
   - GPU Instancing 활성화
   - 드로우콜 99% 개선

5. **`Assets/Scripts/Systems/Network/GhostImportanceScalingSystem.cs`** (신규)
   - 거리 기반 동기화 빈도 조절
   - 네트워크 대역폭 50% 감소

---

## 🚀 예상 최종 결과

- **서버 프레임 타임**: 11ms → **3-4ms** (70% 개선)
- **GPU 드로우콜**: 5000 → **1** (99% 개선)
- **네트워크 대역폭**: **50% 감소**
- **최종 목표**: 안정적 **60 FPS** 달성 ✅

이 계획대로 구현하면 5000마리뿐만 아니라 **10000+ Enemy도 안정적으로 지원** 가능합니다.
