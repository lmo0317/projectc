# Phase 2 실행 계획: 자동 발사 시스템 구현

## 프로젝트 개요

**Phase**: Phase 2 - 자동 발사 시스템 구현
**목표**: 플레이어가 자동으로 총알을 발사하는 ECS 기반 시스템 구현
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수

## Phase 2 개발 사항 요약

이 단계에서는 플레이어가 자동으로 총알을 발사하는 시스템을 ECS 구조로 구현합니다. 총알은 Entity로 생성되며, 일정 간격으로 발사되고 직선으로 이동합니다.

### 구현 범위
1. 총알 ECS 컴포넌트 및 Authoring 구현
2. 총알 Prefab 생성 및 시각적 설정
3. 플레이어 자동 발사 시스템 구현
4. 총알 이동 및 생명주기 관리 시스템 구현
5. 통합 테스트 및 검증

---

## 태스크 분해

### **TASK-005: 총알 ECS 컴포넌트 및 Authoring 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 총알 엔티티를 위한 ECS 컴포넌트와 GameObject → Entity 변환 Authoring 클래스 구현

**구현 내용**:

#### 5.1 총알 컴포넌트 정의
`Assets/Scripts/Components/` 폴더에 다음 컴포넌트 작성:

**BulletTag.cs** (총알 식별용)
```csharp
using Unity.Entities;

public struct BulletTag : IComponentData
{
}
```

**BulletSpeed.cs** (총알 이동 속도)
```csharp
using Unity.Entities;

public struct BulletSpeed : IComponentData
{
    public float Value;
}
```

**BulletLifetime.cs** (총알 생존 시간)
```csharp
using Unity.Entities;

public struct BulletLifetime : IComponentData
{
    public float RemainingTime; // 남은 생존 시간 (초)
}
```

**BulletDirection.cs** (발사 방향)
```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct BulletDirection : IComponentData
{
    public float3 Value; // 정규화된 방향 벡터
}
```

#### 5.2 Authoring 클래스 구현
`Assets/Scripts/Authoring/BulletAuthoring.cs` 작성:

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 5f;

    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletLifetime { RemainingTime = authoring.Lifetime });
            AddComponent(entity, new BulletDirection { Value = float3.zero }); // 발사 시 설정됨
        }
    }
}
```

#### 5.3 검증
- 각 스크립트 컴파일 에러 없이 작성
- SOLID 원칙: 단일 책임 원칙(SRP) - 각 컴포넌트는 하나의 데이터만 담당
- 간결한 구조 유지

**의존성**: Phase 1 완료

**완료 조건**:
- [ ] BulletTag, BulletSpeed, BulletLifetime, BulletDirection 컴포넌트 작성 완료
- [ ] BulletAuthoring 클래스 작성 완료
- [ ] 모든 스크립트 컴파일 에러 없음
- [ ] Authoring 클래스의 Baker가 정상 작동함
- [ ] Inspector에서 Speed, Lifetime 파라미터 노출 확인

**예상 작업량**: 2-3시간

---

### **TASK-006: 총알 Prefab 생성 및 시각적 설정**

**카테고리**: 환경 설정
**우선순위**: P0 (최우선)
**설명**: 총알 Entity Prefab을 생성하고 시각적 표현을 설정

**구현 내용**:

#### 6.1 총알 GameObject 생성
1. Hierarchy에서 GameObject → 3D Object → Sphere 생성
2. 이름: "Bullet"
3. Transform:
   - Position: (0, 0, 0)
   - Scale: (0.2, 0.2, 0.2) - 작은 크기
4. Material 생성 및 적용:
   - `Assets/Materials/` 폴더 생성 (없으면)
   - Material 생성: "BulletMaterial"
   - Albedo 색상: 밝은 노란색 (#FFFF00) 또는 주황색
   - Material을 Bullet에 적용

#### 6.2 BulletAuthoring 컴포넌트 추가
1. Bullet GameObject 선택
2. Add Component → BulletAuthoring
3. Inspector에서 설정:
   - Speed: 10
   - Lifetime: 5

#### 6.3 Prefab 저장
1. `Assets/Prefabs/` 폴더에 Bullet GameObject를 드래그하여 Prefab 생성
2. Hierarchy에서 Bullet GameObject 삭제 (Prefab만 유지)

#### 6.4 검증
- Prefab이 정상적으로 생성됨
- Prefab을 씬에 배치 시 시각적으로 보임
- BulletAuthoring 컴포넌트가 Prefab에 포함됨

**의존성**: TASK-005

**완료 조건**:
- [ ] Bullet GameObject 생성 및 크기 조정 완료
- [ ] BulletMaterial 생성 및 색상 설정 완료
- [ ] BulletAuthoring 컴포넌트 추가됨
- [ ] Prefab으로 저장됨 (Assets/Prefabs/Bullet.prefab)
- [ ] Hierarchy에서 임시 GameObject 제거됨
- [ ] Prefab 더블클릭 시 정상적으로 열림

**예상 작업량**: 1-2시간

---

### **TASK-007: 플레이어 자동 발사 시스템 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 플레이어가 일정 간격으로 자동으로 총알을 발사하는 시스템 구현

**구현 내용**:

#### 7.1 플레이어 발사 컴포넌트 정의
`Assets/Scripts/Components/AutoShootConfig.cs` 작성:

```csharp
using Unity.Entities;

public struct AutoShootConfig : IComponentData
{
    public float FireRate;           // 발사 간격 (초)
    public float TimeSinceLastShot;  // 마지막 발사 후 경과 시간
    public Entity BulletPrefab;      // 총알 Prefab Entity 참조
}
```

#### 7.2 PlayerAuthoring 수정
`Assets/Scripts/Authoring/PlayerAuthoring.cs`에 발사 설정 추가:

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float FireRate = 0.5f;  // 초당 2발
    public GameObject BulletPrefab; // Inspector에서 할당

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInput { Movement = float2.zero });

            // 총알 Prefab Entity 참조 가져오기
            var bulletPrefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new AutoShootConfig
            {
                FireRate = authoring.FireRate,
                TimeSinceLastShot = 0f,
                BulletPrefab = bulletPrefabEntity
            });
        }
    }
}
```

#### 7.3 자동 발사 시스템 구현
`Assets/Scripts/Systems/AutoShootSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 플레이어의 발사 처리
        foreach (var (transform, shootConfig, playerTag) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<PlayerTag>>())
        {
            var config = shootConfig.ValueRW;
            config.TimeSinceLastShot += deltaTime;

            // 발사 간격 체크
            if (config.TimeSinceLastShot >= config.FireRate)
            {
                config.TimeSinceLastShot = 0f;

                // 총알 Entity 생성
                var bulletEntity = ecb.Instantiate(config.BulletPrefab);

                // 총알 위치 설정 (플레이어 위치)
                ecb.SetComponent(bulletEntity, LocalTransform.FromPosition(transform.ValueRO.Position));

                // 총알 발사 방향 설정 (초기에는 위쪽 고정: +Z)
                ecb.SetComponent(bulletEntity, new BulletDirection { Value = new float3(0, 0, 1) });
            }
        }
    }
}
```

**설계 원칙**:
- 타이머 기반 발사 간격 관리
- EntityCommandBuffer를 사용한 안전한 Entity 생성
- 단순한 발사 방향 (위쪽 고정)
- Burst Compile 적용

#### 7.4 Player GameObject에 Prefab 참조 설정
1. Hierarchy에서 Player GameObject 선택
2. Inspector의 PlayerAuthoring 컴포넌트에서:
   - FireRate: 0.5 (초당 2발)
   - Bullet Prefab: Assets/Prefabs/Bullet 드래그 앤 드롭

#### 7.5 검증
- AutoShootSystem이 컴파일 에러 없이 작성됨
- EntityCommandBuffer 사용으로 안전한 Entity 생성 보장
- 발사 타이밍이 정확하게 관리됨

**의존성**: TASK-006

**완료 조건**:
- [ ] AutoShootConfig 컴포넌트 작성 완료
- [ ] PlayerAuthoring에 발사 설정 추가 완료
- [ ] AutoShootSystem 작성 완료
- [ ] 시스템 컴파일 에러 없음
- [ ] Burst Compile 속성 적용됨
- [ ] Player GameObject에 Bullet Prefab 참조 설정됨
- [ ] EntityCommandBuffer를 통한 안전한 생성 확인

**예상 작업량**: 3-4시간

---

### **TASK-008: 총알 이동 및 생명주기 시스템 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 총알이 지정된 방향으로 이동하고 생존 시간이 지나면 삭제되는 시스템 구현

**구현 내용**:

#### 8.1 총알 이동 시스템 구현
`Assets/Scripts/Systems/BulletMovementSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AutoShootSystem))]
[BurstCompile]
public partial struct BulletMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        new BulletMovementJob
        {
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct BulletMovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in BulletDirection direction, in BulletSpeed speed)
    {
        // 방향 * 속도 * 시간 = 이동량
        float3 movement = direction.Value * speed.Value * DeltaTime;
        transform.Position += movement;
    }
}
```

**설계 원칙**:
- IJobEntity를 통한 병렬 처리
- Burst Compile 적용
- 간단한 직선 이동
- 단일 책임: 이동만 담당

#### 8.2 총알 생명주기 시스템 구현
`Assets/Scripts/Systems/BulletLifetimeSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BulletMovementSystem))]
[BurstCompile]
public partial struct BulletLifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 총알 생명주기 감소 및 삭제
        foreach (var (lifetime, entity) in
                 SystemAPI.Query<RefRW<BulletLifetime>>()
                     .WithAll<BulletTag>()
                     .WithEntityAccess())
        {
            lifetime.ValueRW.RemainingTime -= deltaTime;

            // 생존 시간 종료 시 Entity 삭제
            if (lifetime.ValueRO.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
```

**설계 원칙**:
- EntityCommandBuffer를 통한 안전한 Entity 삭제
- EndSimulationEntityCommandBufferSystem 사용 (프레임 끝에 삭제)
- Burst Compile 적용
- 단일 책임: 생명주기 관리만 담당

#### 8.3 검증
- 두 시스템 모두 컴파일 에러 없이 작성
- 시스템 실행 순서 명확: 발사 → 이동 → 생명주기
- EntityCommandBuffer를 통한 안전한 Entity 관리

**의존성**: TASK-007

**완료 조건**:
- [ ] BulletMovementSystem 작성 완료
- [ ] BulletLifetimeSystem 작성 완료
- [ ] 두 시스템 모두 컴파일 에러 없음
- [ ] Burst Compile 속성 적용됨
- [ ] IJobEntity를 사용한 병렬 처리 구현
- [ ] EntityCommandBuffer를 통한 안전한 삭제 구현
- [ ] 시스템 실행 순서가 올바름 (발사 → 이동 → 생명주기)

**예상 작업량**: 3-4시간

---

### **TASK-009: 통합 테스트 및 Phase 2 검증**

**카테고리**: 품질 보증
**우선순위**: P0 (최우선)
**설명**: 전체 자동 발사 시스템을 통합 테스트하고 Phase 2 완료 검증

**구현 내용**:

#### 9.1 Play 모드 기본 테스트
1. Unity Editor에서 Play 버튼 클릭
2. 게임 시작 즉시 플레이어가 자동으로 총알을 발사하는지 확인
3. 총알이 위쪽(+Z 방향)으로 일정 속도로 이동하는지 확인
4. 총알이 5초 후 자동으로 사라지는지 확인
5. 발사 간격이 0.5초(초당 2발)로 정확한지 확인

#### 9.2 Entity Debugger 검증
1. Window → Entities → Hierarchy 열기
2. Play 모드에서 확인:
   - Player 엔티티에 AutoShootConfig 컴포넌트 존재
   - 총알 발사 시 새로운 Bullet 엔티티 생성
   - 총알 엔티티의 컴포넌트 확인:
     - BulletTag
     - BulletSpeed
     - BulletLifetime (시간이 감소하는지 확인)
     - BulletDirection
     - LocalTransform
   - 총알이 5초 후 Entity 목록에서 사라지는지 확인
3. TimeSinceLastShot 값이 0에서 FireRate까지 증가하는지 확인

#### 9.3 성능 검증
1. Window → Analysis → Profiler 열기
2. Play 모드에서 성능 측정:
   - 총알 100개 이상 동시 존재 시 FPS 확인 (60 FPS 이상 유지)
   - CPU Usage → Scripts 확인
   - AutoShootSystem, BulletMovementSystem, BulletLifetimeSystem이 Burst로 컴파일되었는지 확인
   - Jobs → Worker Threads에서 병렬 처리 확인
3. 장시간 플레이(5분) 후 메모리 누수 확인

#### 9.4 시각적 검증
- 총알이 화면에 노란색/주황색 구체로 표시됨
- 총알이 플레이어 위치에서 생성됨
- 총알이 부드럽게 이동함 (끊김 없음)
- 총알이 화면 밖으로 나가도 5초 후 삭제됨

#### 9.5 플레이어 이동과의 통합 테스트
1. Play 모드에서 WASD로 플레이어 이동
2. 이동 중에도 총알이 정상적으로 발사되는지 확인
3. 이동 방향과 무관하게 총알이 위쪽으로 발사되는지 확인
4. 플레이어 이동이 총알 발사에 영향을 주지 않는지 확인

#### 9.6 문제 해결 체크리스트
만약 문제 발생 시:
- [ ] 총알이 발사되지 않음 → Bullet Prefab 참조 확인, AutoShootSystem 실행 확인
- [ ] 총알이 이동하지 않음 → BulletDirection 값 확인, BulletMovementSystem 실행 확인
- [ ] 총알이 삭제되지 않음 → BulletLifetimeSystem 실행 확인, ECB 사용 확인
- [ ] 총알이 보이지 않음 → Material 적용 확인, Renderer 컴포넌트 확인
- [ ] 성능 저하 → Burst Compile 적용 확인, Job System 병렬화 확인
- [ ] Entity 생성 에러 → EntityCommandBuffer 사용 확인, Prefab 설정 확인

**의존성**: TASK-008

**완료 조건**:
- [ ] 게임 시작 시 플레이어가 자동으로 총알을 발사함
- [ ] 설정한 발사 간격(0.5초)대로 총알이 생성됨
- [ ] 총알이 위쪽으로 일정 속도로 이동함
- [ ] 총알이 5초 후 자동으로 삭제됨
- [ ] Entity Debugger에서 총알 Entity 생성/삭제 확인 가능
- [ ] BulletLifetime.RemainingTime 값이 감소하는 것을 확인
- [ ] Profiler에서 3개 시스템이 Burst로 컴파일되었는지 확인
- [ ] 총알 100개 이상 동시 존재 시 60 FPS 이상 유지
- [ ] 총알이 화면에 시각적으로 표시됨 (노란색/주황색 구체)
- [ ] 플레이어 이동 중에도 총알이 정상적으로 발사됨
- [ ] 콘솔에 에러 없음
- [ ] 5분 이상 플레이 시 메모리 누수 없음

**예상 작업량**: 3-4시간

---

## Phase 2 전체 검증 체크리스트

### 필수 검증 항목 (SPEC.md 기준)
- [ ] 게임 시작 시 플레이어가 자동으로 총알을 발사함
- [ ] 설정한 발사 간격대로 총알이 생성됨
- [ ] 총알이 지정된 방향으로 일정 속도로 이동함
- [ ] 총알이 생존 시간이 끝나면 자동으로 삭제됨
- [ ] Entity Debugger에서 총알 Entity 생성/삭제 확인 가능
- [ ] 성능 프로파일러에서 총알 시스템이 Burst로 컴파일되었는지 확인
- [ ] 다수의 총알(100개 이상)이 생성되어도 프레임 드랍 없이 작동함
- [ ] 총알이 화면에 시각적으로 표시됨
- [ ] 플레이어가 이동하면서도 총알이 정상적으로 발사됨

### 추가 검증 (안정성)
- [ ] Unity 재시작 후에도 정상 작동
- [ ] 장시간 플레이(5분 이상) 시 메모리 누수 없음
- [ ] 빌드 테스트 (Standalone Windows)
- [ ] Phase 1 기능(플레이어 이동)이 여전히 정상 작동함

### SOLID 원칙 준수 확인
- [ ] 각 컴포넌트가 단일 책임을 가짐
- [ ] 시스템이 명확하게 분리됨 (발사/이동/생명주기)
- [ ] 코드가 확장 가능한 구조임
- [ ] 불필요한 의존성이 없음

---

## 작업 순서 및 일정

### 권장 진행 순서
1. **Day 1 (2-3시간)**: TASK-005 완료
   - 총알 컴포넌트 작성
   - BulletAuthoring 작성
   - 컴파일 확인

2. **Day 1 (1-2시간)**: TASK-006 완료
   - 총알 GameObject 및 Material 생성
   - Prefab 생성
   - 시각적 확인

3. **Day 2 (3-4시간)**: TASK-007 완료
   - AutoShootConfig 작성
   - PlayerAuthoring 수정
   - AutoShootSystem 구현
   - Prefab 참조 설정

4. **Day 2-3 (3-4시간)**: TASK-008 완료
   - BulletMovementSystem 구현
   - BulletLifetimeSystem 구현
   - 시스템 순서 확인

5. **Day 3 (3-4시간)**: TASK-009 완료
   - 통합 테스트
   - 성능 검증
   - 문제 해결
   - 최종 검증

### 총 예상 소요 시간
- **필수 작업**: 12-17시간 (약 2-3일)
- **검증 및 문제 해결**: 추가 2-3시간
- **전체**: 14-20시간

---

## 의존성 다이어그램

```
Phase 1 완료
    ↓
TASK-005 (총알 컴포넌트/Authoring)
    ↓
TASK-006 (총알 Prefab 생성)
    ↓
TASK-007 (자동 발사 시스템)
    ↓
TASK-008 (이동/생명주기 시스템)
    ↓
TASK-009 (통합 테스트 및 검증)
```

**시스템 실행 순서**:
```
InitializationSystemGroup
    └─ PlayerInputSystem (Phase 1)

SimulationSystemGroup
    ├─ PlayerMovementSystem (Phase 1)
    ├─ AutoShootSystem (Phase 2)
    ├─ BulletMovementSystem (Phase 2)
    └─ BulletLifetimeSystem (Phase 2)
```

---

## SOLID 원칙 준수 확인

### Single Responsibility Principle (단일 책임)
- **BulletTag**: 총알 식별만 담당
- **BulletSpeed**: 속도 데이터만 저장
- **BulletLifetime**: 생존 시간 데이터만 저장
- **BulletDirection**: 방향 데이터만 저장
- **AutoShootConfig**: 발사 설정 데이터만 저장
- **AutoShootSystem**: 총알 생성만 담당
- **BulletMovementSystem**: 총알 이동만 담당
- **BulletLifetimeSystem**: 총알 삭제만 담당

### Open/Closed Principle (개방/폐쇄)
- IJobEntity 구조로 확장 가능
- 새로운 총알 타입 추가 시 기존 코드 수정 불필요
- 발사 방향 변경 시 AutoShootSystem만 수정

### Liskov Substitution Principle (리스코프 치환)
- IComponentData, ISystem 인터페이스를 올바르게 구현
- Entity는 어떤 컴포넌트 조합도 가능

### Interface Segregation Principle (인터페이스 분리)
- 각 컴포넌트는 필요한 데이터만 포함
- 시스템은 필요한 컴포넌트만 쿼리

### Dependency Inversion Principle (의존성 역전)
- 시스템은 구체적 구현이 아닌 컴포넌트 인터페이스에 의존
- Prefab 참조를 Entity로 관리하여 유연성 확보

---

## 문제 해결 가이드

### 일반적인 문제

**1. 총알이 발사되지 않음**
- Player GameObject에 Bullet Prefab 참조가 설정되었는지 확인
- AutoShootConfig.BulletPrefab이 Entity.Null이 아닌지 Entity Debugger에서 확인
- AutoShootSystem이 실행되는지 확인 (System 목록에서 검색)
- EntityCommandBuffer가 올바르게 사용되었는지 확인

**2. 총알이 이동하지 않음**
- BulletDirection.Value가 (0,0,0)이 아닌지 확인
- BulletSpeed.Value가 0이 아닌지 확인
- BulletMovementSystem이 실행되는지 확인
- LocalTransform 컴포넌트가 존재하는지 확인

**3. 총알이 삭제되지 않음**
- BulletLifetimeSystem이 실행되는지 확인
- EntityCommandBuffer가 EndSimulationEntityCommandBufferSystem을 사용하는지 확인
- BulletLifetime.RemainingTime이 감소하는지 Entity Debugger에서 확인

**4. 총알이 보이지 않음**
- Bullet Prefab에 MeshRenderer 컴포넌트가 있는지 확인
- Material이 할당되었는지 확인
- 총알 크기가 너무 작지 않은지 확인 (Scale 0.2 이상)
- 카메라가 총알을 볼 수 있는 위치인지 확인

**5. 성능 저하**
- Burst Compile이 적용되었는지 확인 ([BurstCompile] 속성)
- IJobEntity를 사용한 병렬 처리가 구현되었는지 확인
- Profiler에서 Burst Inspector 확인 (Jobs → Burst Compiled)
- Managed Type(string, class 등)을 사용하지 않았는지 확인

**6. Prefab 참조 에러**
- BulletAuthoring이 Prefab에 추가되었는지 확인
- Prefab이 Assets/Prefabs/ 폴더에 저장되었는지 확인
- PlayerAuthoring의 Baker에서 GetEntity를 올바르게 사용했는지 확인

---

## 코드 품질 체크리스트

### 코드 스타일
- [ ] C# Naming Convention 준수 (PascalCase for types, camelCase for fields)
- [ ] 명확한 변수명 사용 (축약어 최소화)
- [ ] 간결한 주석 (필요한 곳에만)
- [ ] 일관된 들여쓰기 (4 spaces)

### ECS 모범 사례
- [ ] IComponentData는 struct로 정의
- [ ] ISystem은 struct로 정의 (가능한 경우)
- [ ] SystemBase는 partial class로 정의
- [ ] Burst Compile 가능한 시스템에 [BurstCompile] 적용
- [ ] EntityCommandBuffer를 통한 안전한 Entity 생성/삭제
- [ ] 적절한 UpdateInGroup 지정

### 성능 최적화
- [ ] IJobEntity를 사용한 병렬 처리
- [ ] Managed Type 사용 최소화
- [ ] NativeArray 등 네이티브 컬렉션 사용
- [ ] Query 최적화 (WithAll, WithNone 등)

---

## 다음 단계 (Phase 3 준비)

Phase 2 완료 후:
1. **코드 리뷰**: 모든 스크립트 검토
2. **Git 커밋**: "Phase 2 완료: 자동 발사 시스템"
3. **문서화**: 주요 결정 사항 README에 기록
4. **Phase 3 준비**: 몬스터 스폰 시스템 설계 검토

---

## 참고 사항

### Unity 설정
- Auto Save 활성화 확인
- Version Control 활성화 (Git)
- Scene 변경 시 저장 확인
- Prefab 변경 시 Apply 확인

### 디버깅 팁
- Entity Debugger를 항상 열어두고 작업
- Profiler를 주기적으로 확인
- Console 창을 모니터링하여 에러 즉시 확인
- Play 모드에서 Inspector로 실시간 값 변화 확인

### 성능 고려사항
- 현재 단계에서는 총알 100-200개 정도 처리
- Burst Compiler와 Job System으로 충분한 성능 확보
- Phase 3(몬스터 추가) 이후 종합 성능 최적화 진행

### 확장성 고려사항
- 발사 방향은 현재 위쪽 고정이지만, Phase 4 이후 다양한 방향 지원 가능
- 총알 타입을 쉽게 추가할 수 있는 구조
- 멀티 방향 발사, 관통 등 확장 용이

---

**Phase 2 시작 준비 완료!**
TASK-005부터 순차적으로 진행하세요.
