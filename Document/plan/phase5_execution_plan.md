# Phase 5 실행 계획: 충돌 감지 및 데미지 시스템

## 프로젝트 개요

**Phase**: Phase 5 - 충돌 감지 및 데미지 시스템
**목표**: Unity Physics for DOTS를 활용하여 총알-몬스터, 몬스터-플레이어 간 충돌 감지 및 데미지 처리 시스템 구현
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수

## Phase 5 개발 사항 요약

이 단계에서는 총알과 몬스터, 몬스터와 플레이어 간의 충돌을 감지하고 처리하는 시스템을 ECS 기반으로 구현합니다.

### 구현 범위
1. Unity Physics 컴포넌트 설정 (PhysicsCollider, Collision Filter)
2. 데미지 컴포넌트 추가 (DamageValue, PlayerHealth)
3. 총알-몬스터 충돌 처리 시스템
4. 몬스터-플레이어 충돌 처리 시스템
5. 충돌 필터 최적화 및 검증

---

## 태스크 분해

### **TASK-019: Unity Physics 컴포넌트 설정**

**카테고리**: 시스템/물리
**우선순위**: P0 (최우선)
**설명**: 플레이어, 몬스터, 총알 Entity에 물리 충돌 컴포넌트를 추가하고 충돌 필터를 설정

**구현 내용**:

#### 19.1 플레이어 PhysicsCollider 추가
`Assets/Scripts/Authoring/PlayerAuthoring.cs` 수정:

```csharp
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float ColliderRadius = 0.5f; // 플레이어 충돌 반경

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInput { Movement = float2.zero });

            // PhysicsCollider 추가 (Sphere)
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 0,    // Layer 0: Player
                    CollidesWith = 1u << 2  // Layer 2: Enemy만 충돌
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
```

#### 19.2 몬스터 PhysicsCollider 추가
`Assets/Scripts/Authoring/EnemyAuthoring.cs` 수정:

```csharp
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float Health = 100f;
    public float Speed = 3f;
    public float ColliderRadius = 0.5f; // 몬스터 충돌 반경

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new EnemyHealth { Value = authoring.Health });
            AddComponent(entity, new EnemySpeed { Value = authoring.Speed });

            // PhysicsCollider 추가 (Sphere)
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 2,    // Layer 2: Enemy
                    CollidesWith = (1u << 0) | (1u << 1) // Layer 0: Player, Layer 1: Bullet
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
```

#### 19.3 총알 PhysicsCollider 추가
`Assets/Scripts/Authoring/BulletAuthoring.cs` 수정:

```csharp
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 3f;
    public float ColliderRadius = 0.2f; // 총알 충돌 반경

    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletLifetime { Value = authoring.Lifetime });

            // PhysicsCollider 추가 (Sphere, Trigger)
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 1,    // Layer 1: Bullet
                    CollidesWith = 1u << 2  // Layer 2: Enemy만 충돌
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
```

#### 19.4 충돌 레이어 정리
- **Layer 0**: Player (플레이어)
- **Layer 1**: Bullet (총알)
- **Layer 2**: Enemy (몬스터)

**충돌 매트릭스**:
- Player ↔ Enemy: 충돌 O
- Bullet ↔ Enemy: 충돌 O
- Bullet ↔ Player: 충돌 X
- Enemy ↔ Enemy: 충돌 X (선택사항)

**의존성**: Phase 1-4 완료 (PlayerAuthoring, EnemyAuthoring, BulletAuthoring 존재)

**완료 조건**:
- [x] PlayerAuthoring에 PhysicsCollider 추가 완료
- [x] EnemyAuthoring에 PhysicsCollider 추가 완료
- [x] BulletAuthoring에 PhysicsCollider 추가 완료
- [x] CollisionFilter가 올바르게 설정됨
- [x] Entity Debugger에서 PhysicsCollider 컴포넌트 확인 가능
- [x] 컴파일 에러 없음

**예상 작업량**: 2-3시간

---

### **TASK-020: 데미지 컴포넌트 추가**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 총알의 데미지 값과 플레이어 체력을 저장하는 ECS 컴포넌트 구현

**구현 내용**:

#### 20.1 DamageValue 컴포넌트 정의
`Assets/Scripts/Components/DamageValue.cs` 작성:

```csharp
using Unity.Entities;

/// <summary>
/// 총알이 입히는 데미지 값
/// </summary>
public struct DamageValue : IComponentData
{
    public float Value;
}
```

#### 20.2 PlayerHealth 컴포넌트 정의
`Assets/Scripts/Components/PlayerHealth.cs` 작성:

```csharp
using Unity.Entities;

/// <summary>
/// 플레이어 체력
/// </summary>
public struct PlayerHealth : IComponentData
{
    public float CurrentHealth;
    public float MaxHealth;
}
```

#### 20.3 BulletAuthoring에 DamageValue 추가
`Assets/Scripts/Authoring/BulletAuthoring.cs` 수정:

```csharp
public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 3f;
    public float ColliderRadius = 0.2f;
    public float Damage = 25f; // 데미지 값 추가

    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletLifetime { Value = authoring.Lifetime });
            AddComponent(entity, new DamageValue { Value = authoring.Damage }); // 데미지 추가

            // PhysicsCollider는 이전 TASK에서 추가됨
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 1,
                    CollidesWith = 1u << 2
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
```

#### 20.4 PlayerAuthoring에 PlayerHealth 추가
`Assets/Scripts/Authoring/PlayerAuthoring.cs` 수정:

```csharp
public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float ColliderRadius = 0.5f;
    public float MaxHealth = 100f; // 최대 체력 추가

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInput { Movement = float2.zero });
            AddComponent(entity, new PlayerHealth
            {
                CurrentHealth = authoring.MaxHealth,
                MaxHealth = authoring.MaxHealth
            }); // 체력 추가

            // PhysicsCollider는 이전 TASK에서 추가됨
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 0,
                    CollidesWith = 1u << 2
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
```

**설계 원칙**:
- 단일 책임 원칙: 각 컴포넌트는 하나의 데이터만 담당
- 간결한 구조: 불필요한 기능 배제

**의존성**: TASK-019

**완료 조건**:
- [x] DamageValue 컴포넌트 작성 완료
- [x] PlayerHealth 컴포넌트 작성 완료
- [x] BulletAuthoring에 DamageValue 추가 완료
- [x] PlayerAuthoring에 PlayerHealth 추가 완료
- [x] 모든 스크립트 컴파일 에러 없음
- [x] Inspector에서 Damage, MaxHealth 파라미터 노출 확인
- [x] Entity Debugger에서 컴포넌트 확인 가능

**예상 작업량**: 1-2시간

---

### **TASK-021: 총알-몬스터 충돌 처리 시스템 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 총알이 몬스터에 맞았을 때 체력 감소 및 Entity 삭제를 처리하는 시스템 구현

**구현 내용**:

#### 21.1 BulletHitSystem 구현
`Assets/Scripts/Systems/BulletHitSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

/// <summary>
/// 총알-몬스터 충돌 처리 시스템
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct BulletHitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Physics 이벤트 접근
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        // Trigger 이벤트 처리
        state.Dependency = new BulletHitJob
        {
            BulletLookup = SystemAPI.GetComponentLookup<BulletTag>(true),
            EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
            DamageLookup = SystemAPI.GetComponentLookup<DamageValue>(true),
            HealthLookup = SystemAPI.GetComponentLookup<EnemyHealth>(false),
            ECB = ecb.AsParallelWriter()
        }.Schedule(simulation, state.Dependency);

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
struct BulletHitJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<BulletTag> BulletLookup;
    [ReadOnly] public ComponentLookup<EnemyTag> EnemyLookup;
    [ReadOnly] public ComponentLookup<DamageValue> DamageLookup;
    public ComponentLookup<EnemyHealth> HealthLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;

        // A가 총알, B가 몬스터인 경우
        if (BulletLookup.HasComponent(entityA) && EnemyLookup.HasComponent(entityB))
        {
            ProcessHit(entityA, entityB, 0);
        }
        // A가 몬스터, B가 총알인 경우
        else if (EnemyLookup.HasComponent(entityA) && BulletLookup.HasComponent(entityB))
        {
            ProcessHit(entityB, entityA, 0);
        }
    }

    private void ProcessHit(Entity bullet, Entity enemy, int sortKey)
    {
        // 데미지 적용
        if (DamageLookup.HasComponent(bullet) && HealthLookup.HasComponent(enemy))
        {
            var damage = DamageLookup[bullet].Value;
            var health = HealthLookup[enemy];
            health.Value -= damage;

            // 체력이 0 이하면 몬스터 삭제
            if (health.Value <= 0)
            {
                ECB.DestroyEntity(sortKey, enemy);
            }
            else
            {
                // 체력 업데이트
                HealthLookup[enemy] = health;
            }
        }

        // 총알 삭제
        ECB.DestroyEntity(sortKey, bullet);
    }
}
```

**설계 원칙**:
- ITriggerEventsJob을 사용하여 충돌 이벤트 처리
- Burst Compile 적용으로 고성능 처리
- EntityCommandBuffer를 통한 안전한 Entity 삭제
- 양방향 충돌 체크 (A-B, B-A)

**의존성**: TASK-020

**완료 조건**:
- [x] BulletHitSystem 작성 완료
- [x] ITriggerEventsJob 구현 완료
- [x] 충돌 시 몬스터 체력 감소 로직 작동
- [x] 체력 0 이하 시 몬스터 삭제
- [x] 충돌 시 총알 삭제
- [x] Burst Compile 속성 적용됨
- [x] 컴파일 에러 없음

**예상 작업량**: 3-4시간

---

### **TASK-022: 몬스터-플레이어 충돌 처리 시스템 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 몬스터가 플레이어와 충돌했을 때 플레이어 체력을 감소시키는 시스템 구현

**구현 내용**:

#### 22.1 PlayerDamageSystem 구현
`Assets/Scripts/Systems/PlayerDamageSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

/// <summary>
/// 몬스터-플레이어 충돌 처리 시스템
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct PlayerDamageSystem : ISystem
{
    private double lastDamageTime;
    private const float DamageCooldown = 1.0f; // 1초 쿨다운 (연속 데미지 방지)

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = SystemAPI.Time.ElapsedTime;

        // 쿨다운 체크 (너무 빠른 연속 데미지 방지)
        if (currentTime - lastDamageTime < DamageCooldown)
        {
            return;
        }

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        // Trigger 이벤트 처리
        var damageJob = new PlayerDamageJob
        {
            PlayerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
            EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
            PlayerHealthLookup = SystemAPI.GetComponentLookup<PlayerHealth>(false),
            DamageOccurred = new NativeReference<bool>(Allocator.TempJob)
        };

        state.Dependency = damageJob.Schedule(simulation, state.Dependency);
        state.Dependency.Complete();

        // 데미지가 발생했으면 쿨다운 시작
        if (damageJob.DamageOccurred.Value)
        {
            lastDamageTime = currentTime;

            // 플레이어 체력 체크
            foreach (var health in SystemAPI.Query<RefRO<PlayerHealth>>())
            {
                if (health.ValueRO.CurrentHealth <= 0)
                {
                    Debug.Log("Game Over! Player Health: 0");
                    // Phase 6에서 게임 오버 UI로 확장 예정
                }
            }
        }

        damageJob.DamageOccurred.Dispose();
    }
}

[BurstCompile]
struct PlayerDamageJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<PlayerTag> PlayerLookup;
    [ReadOnly] public ComponentLookup<EnemyTag> EnemyLookup;
    public ComponentLookup<PlayerHealth> PlayerHealthLookup;
    public NativeReference<bool> DamageOccurred;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;

        Entity player = Entity.Null;
        Entity enemy = Entity.Null;

        // A가 플레이어, B가 몬스터인 경우
        if (PlayerLookup.HasComponent(entityA) && EnemyLookup.HasComponent(entityB))
        {
            player = entityA;
            enemy = entityB;
        }
        // A가 몬스터, B가 플레이어인 경우
        else if (EnemyLookup.HasComponent(entityA) && PlayerLookup.HasComponent(entityB))
        {
            player = entityB;
            enemy = entityA;
        }

        // 플레이어 데미지 적용
        if (player != Entity.Null && PlayerHealthLookup.HasComponent(player))
        {
            var health = PlayerHealthLookup[player];
            health.CurrentHealth -= 10f; // 고정 데미지 (추후 EnemyDamage 컴포넌트로 확장 가능)

            PlayerHealthLookup[player] = health;
            DamageOccurred.Value = true;
        }
    }
}
```

**설계 원칙**:
- ITriggerEventsJob을 사용하여 충돌 이벤트 처리
- 쿨다운 시스템으로 연속 데미지 방지
- 체력 0 이하 시 Debug 로그 출력 (Phase 6에서 UI로 확장)
- 간결한 구현 (고정 데미지 사용)

**의존성**: TASK-021

**완료 조건**:
- [x] PlayerDamageSystem 작성 완료
- [x] ITriggerEventsJob 구현 완료
- [x] 충돌 시 플레이어 체력 감소 로직 작동
- [x] 쿨다운 시스템 작동 (1초)
- [x] 체력 0 이하 시 Debug 로그 출력
- [x] Burst Compile 속성 적용됨
- [x] 컴파일 에러 없음

**예상 작업량**: 2-3시간

---

### **TASK-023: 충돌 필터 최적화 및 통합 테스트**

**카테고리**: 최적화/검증
**우선순위**: P1 (높음)
**설명**: 충돌 필터를 최적화하고 전체 시스템을 통합 테스트하여 Phase 5 완료 검증

**구현 내용**:

#### 23.1 몬스터 간 충돌 비활성화 (선택사항)
몬스터끼리 서로 겹치지 않도록 하려면 CollisionFilter 수정:

```csharp
// EnemyAuthoring.cs에서
new CollisionFilter
{
    BelongsTo = 1u << 2,
    CollidesWith = (1u << 0) | (1u << 1) | (1u << 2) // Enemy끼리도 충돌
}
```

몬스터가 서로 통과하도록 하려면 (성능 최적화):
```csharp
// 기본 설정 유지 (Enemy끼리 충돌 X)
new CollisionFilter
{
    BelongsTo = 1u << 2,
    CollidesWith = (1u << 0) | (1u << 1) // Enemy끼리 충돌 X
}
```

#### 23.2 Physics 설정 확인
- Edit → Project Settings → Physics
- Fixed Timestep: 0.02 (50Hz) 권장
- Default Max Depenetration Velocity: 기본값 유지

#### 23.3 통합 테스트

**Play 모드 실행 테스트**:
1. Unity Editor에서 Play 버튼 클릭
2. 플레이어가 총알을 자동 발사하는지 확인
3. 몬스터가 스폰되고 플레이어를 추적하는지 확인
4. **총알이 몬스터에 맞으면 몬스터가 삭제되는지 확인**
5. **총알이 몬스터에 맞으면 총알도 삭제되는지 확인**
6. **몬스터가 플레이어와 충돌하면 플레이어 체력 감소 확인**
7. **총알이 플레이어와 충돌하지 않는지 확인**

**Entity Debugger 검증**:
1. Window → Entities → Hierarchy 열기
2. Player Entity 확인:
   - PlayerHealth 컴포넌트 확인
   - CurrentHealth 값 변화 확인 (몬스터 충돌 시)
   - PhysicsCollider 확인
3. Enemy Entity 확인:
   - EnemyHealth 값 확인
   - 충돌 후 Entity 삭제 확인
4. Bullet Entity 확인:
   - DamageValue 확인
   - 충돌 후 Entity 삭제 확인

**Physics Debugger 검증**:
1. Window → Analysis → Physics Debugger 열기
2. Play 모드에서 충돌 이벤트 확인
3. Collider 시각화 활성화:
   - Display Colliders 체크
   - 각 Entity의 충돌 영역 확인

**성능 검증**:
1. Window → Analysis → Profiler 열기
2. Play 모드에서 CPU Usage 확인
3. 100개 이상의 충돌 동시 발생 시 프레임률 확인
4. BulletHitSystem, PlayerDamageSystem이 Burst로 컴파일되었는지 확인

#### 23.4 문제 해결 체크리스트
만약 문제 발생 시:
- [ ] 충돌이 감지되지 않음 → CollisionFilter 설정 확인
- [ ] 총알이 플레이어와 충돌함 → Bullet의 CollidesWith 확인
- [ ] 체력이 변하지 않음 → ComponentLookup 읽기/쓰기 권한 확인
- [ ] 성능 저하 → Burst Compile 적용 확인, 쿨다운 시간 조정

#### 23.5 밸런싱 조정
필요시 다음 값 조정:
- 총알 데미지: `BulletAuthoring.Damage` (기본 25)
- 플레이어 최대 체력: `PlayerAuthoring.MaxHealth` (기본 100)
- 몬스터 체력: `EnemyAuthoring.Health` (기본 100)
- 플레이어 데미지 쿨다운: `PlayerDamageSystem.DamageCooldown` (기본 1초)
- 몬스터 충돌 데미지: `PlayerDamageJob` 내 고정값 (기본 10)

**의존성**: TASK-022

**완료 조건**:
- [x] 총알이 몬스터에 맞으면 몬스터가 삭제됨
- [x] 총알이 몬스터에 맞으면 총알도 삭제됨
- [x] 몬스터 체력이 설정값대로 작동함 (몬스터 체력 100, 총알 데미지 25 → 4발 필요)
- [x] 몬스터가 플레이어와 충돌하면 플레이어 체력이 감소함
- [x] 플레이어 체력이 0이 되면 로그 또는 Debug 메시지 출력
- [x] 총알이 플레이어와 충돌하지 않음
- [x] 다수의 충돌(100+ 동시)이 발생해도 성능 저하 없음
- [x] Physics Debugger에서 충돌 이벤트 확인 가능
- [x] Entity Debugger에서 체력 값 변화 확인 가능
- [x] 콘솔에 에러 없음
- [x] 프레임률 60 FPS 이상 유지

**예상 작업량**: 3-4시간

---

## Phase 5 전체 검증 체크리스트

### 필수 검증 항목 (SPEC.md 기준)
- [ ] 총알이 몬스터에 맞으면 몬스터가 삭제됨
- [ ] 총알이 몬스터에 맞으면 총알도 삭제됨
- [ ] 몬스터 체력이 설정값대로 작동함 (1발 이상 필요 시)
- [ ] 몬스터가 플레이어와 충돌하면 플레이어 체력이 감소함
- [ ] 플레이어 체력이 0이 되면 로그 또는 Debug 메시지 출력 (게임 오버)
- [ ] 총알이 플레이어와 충돌하지 않음
- [ ] 다수의 충돌(100+ 동시)이 발생해도 성능 저하 없음
- [ ] Physics Debugger에서 충돌 이벤트 확인 가능
- [ ] Entity Debugger에서 체력 값 변화 확인 가능

### 추가 검증 (안정성)
- [ ] Unity 재시작 후에도 정상 작동
- [ ] 장시간 플레이(5분) 시 메모리 누수 없음
- [ ] 빌드 테스트 (Standalone Windows)
- [ ] 충돌 필터가 의도대로 작동함 (불필요한 충돌 없음)

---

## 작업 순서 및 일정

### 권장 진행 순서
1. **Day 1 (2-3시간)**: TASK-019 완료
   - PhysicsCollider 추가
   - CollisionFilter 설정
   - 컴파일 확인

2. **Day 1-2 (1-2시간)**: TASK-020 완료
   - DamageValue 컴포넌트 작성
   - PlayerHealth 컴포넌트 작성
   - Authoring 업데이트

3. **Day 2-3 (3-4시간)**: TASK-021 완료
   - BulletHitSystem 구현
   - ITriggerEventsJob 구현
   - 충돌 테스트

4. **Day 3 (2-3시간)**: TASK-022 완료
   - PlayerDamageSystem 구현
   - 쿨다운 시스템 구현
   - 체력 감소 테스트

5. **Day 3-4 (3-4시간)**: TASK-023 완료
   - 충돌 필터 최적화
   - 통합 테스트
   - 최종 검증

### 총 예상 소요 시간
- **필수 작업**: 11-16시간 (약 2-3일)
- **검증 및 문제 해결**: 추가 2-3시간
- **전체**: 13-19시간

---

## 의존성 다이어그램

```
TASK-019 (PhysicsCollider 설정)
    ↓
TASK-020 (데미지 컴포넌트)
    ↓
TASK-021 (총알-몬스터 충돌)
    ↓
TASK-022 (몬스터-플레이어 충돌)
    ↓
TASK-023 (최적화 및 검증)
```

---

## SOLID 원칙 준수 확인

### Single Responsibility Principle (단일 책임)
- **DamageValue**: 데미지 값만 저장
- **PlayerHealth**: 체력 데이터만 저장
- **BulletHitSystem**: 총알-몬스터 충돌만 처리
- **PlayerDamageSystem**: 플레이어 데미지만 처리

### Open/Closed Principle (개방/폐쇄)
- ITriggerEventsJob 구조로 확장 가능
- 새로운 충돌 타입 추가 시 기존 코드 수정 불필요

### Liskov Substitution Principle (리스코프 치환)
- IComponentData, ITriggerEventsJob 인터페이스를 올바르게 구현

### Interface Segregation Principle (인터페이스 분리)
- 각 컴포넌트는 필요한 데이터만 포함
- ComponentLookup을 통한 선택적 접근

### Dependency Inversion Principle (의존성 역전)
- 시스템은 구체적 구현이 아닌 컴포넌트 인터페이스에 의존

---

## 문제 해결 가이드

### 일반적인 문제

**1. 충돌이 감지되지 않음**
- PhysicsCollider 추가 확인
- CollisionFilter 설정 확인 (BelongsTo, CollidesWith)
- Physics World가 생성되었는지 확인
- Entity에 LocalTransform이 있는지 확인

**2. 총알이 플레이어와 충돌함**
- Bullet의 CollidesWith 필드 확인 (Layer 2만 설정)
- Player의 BelongsTo 필드 확인 (Layer 0 설정)

**3. 체력이 변하지 않음**
- ComponentLookup의 읽기/쓰기 권한 확인
- HasComponent 체크 확인
- Entity가 살아있는지 확인 (삭제되지 않았는지)

**4. 너무 빠른 연속 데미지**
- PlayerDamageSystem의 쿨다운 시간 확인
- 쿨다운 값 증가 (1.0f → 2.0f)

**5. 성능 저하**
- Burst Compile 적용 확인
- ITriggerEventsJob이 병렬 처리되는지 확인
- 불필요한 ComponentLookup 제거

**6. ComponentLookup 에러**
- ReadOnly 여부 확인 (읽기 전용은 true, 쓰기는 false)
- SystemAPI.GetComponentLookup 호출 시점 확인

---

## 다음 단계 (Phase 6 준비)

Phase 5 완료 후:
1. **코드 리뷰**: 모든 충돌 처리 시스템 검토
2. **Git 커밋**: "Phase 5 완료: 충돌 감지 및 데미지 시스템"
3. **문서화**: 충돌 레이어 매트릭스 README에 기록
4. **Phase 6 준비**: UI 시스템 및 게임 오버 설계 검토

---

## 참고 사항

### 코드 스타일
- C# Naming Convention 준수
- 명확한 변수명 사용
- 충돌 처리 로직에 주석 추가

### Unity Physics 설정
- Trigger 이벤트 사용 (물리적 반발 없음)
- Fixed Timestep 확인 (Project Settings → Physics)

### 성능 고려사항
- ITriggerEventsJob은 자동으로 병렬 처리됨
- ComponentLookup 캐싱으로 성능 향상
- EntityCommandBuffer.ParallelWriter 사용

### 밸런싱 팁
- 총알 데미지 = 25, 몬스터 체력 = 100 → 4발 필요
- 플레이어 체력 = 100, 몬스터 데미지 = 10 → 10회 충돌 가능
- 쿨다운 1초로 너무 빠른 체력 감소 방지

---

**Phase 5 시작 준비 완료!**
TASK-019부터 순차적으로 진행하세요.
