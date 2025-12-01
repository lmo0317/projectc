# Unity DOTS 학습 노트

## 목차
1. [SystemGroup 실행 순서](#systemgroup-실행-순서)
2. [SystemBase vs ISystem](#systembase-vs-isystem)
3. [Entity Prefab과 Baking](#entity-prefab과-baking)
4. [Query vs QueryBuilder](#query-vs-querybuilder)
5. [Archetype과 Query 성능](#archetype과-query-성능)
6. [IJobEntity의 Execute 자동 호출 메커니즘](#ijoventity의-execute-자동-호출-메커니즘)
7. [Unity DOTS Physics와 충돌 처리](#unity-dots-physics와-충돌-처리)

---

## SystemGroup 실행 순서

Unity DOTS는 매 프레임마다 세 가지 주요 SystemGroup을 순서대로 실행합니다.

### 실행 순서

```
InitializationSystemGroup → SimulationSystemGroup → PresentationSystemGroup
```

### 1. InitializationSystemGroup
**역할**: 프레임 시작 시 데이터 수집 및 초기화

**주요 용도**:
- 입력 수집 (키보드, 마우스, 게임패드)
- 네트워크 메시지 수신
- 외부 데이터 읽기
- 프레임 초기 상태 설정

**예시**:
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 입력 수집만 담당
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        // PlayerInput 컴포넌트에 저장
    }
}
```

### 2. SimulationSystemGroup
**역할**: 게임 로직 및 물리 시뮬레이션 처리

**주요 용도**:
- 이동, 회전 등 Transform 변경
- 물리 시뮬레이션 (충돌, 중력)
- AI 로직
- 게임 규칙 적용
- 상태 업데이트

**예시**:
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 입력을 기반으로 이동 처리
        new PlayerMovementJob { DeltaTime = SystemAPI.Time.DeltaTime }
            .ScheduleParallel();
    }
}
```

### 3. PresentationSystemGroup
**역할**: 렌더링 준비 및 시각적 표현

**주요 용도**:
- 카메라 업데이트
- 애니메이션 적용
- VFX/파티클 업데이트
- UI 갱신
- 렌더링 데이터 준비

### 데이터 흐름

```
InitializationSystemGroup:  입력 수집
         ↓
SimulationSystemGroup:      입력 → 이동 계산
         ↓
PresentationSystemGroup:    이동 결과 → 렌더링
```

---

## SystemBase vs ISystem

Unity DOTS에서 시스템을 작성하는 두 가지 방식의 차이점입니다.

### 비교표

| 특징 | SystemBase | ISystem |
|------|-----------|---------|
| **타입** | Class (참조 타입) | Struct (값 타입) |
| **시대** | Legacy (레거시) | Modern (최신 권장) |
| **Burst 컴파일** | ❌ 불가능 | ✅ 가능 |
| **성능** | 보통 (GC 오버헤드) | 높음 (GC 없음) |
| **Unity API 접근** | ✅ 가능 | ❌ 불가능 |
| **Managed 타입** | ✅ 사용 가능 | ❌ 사용 불가 |
| **병렬 처리** | Entities.ForEach | IJobEntity |

### SystemBase (Class 기반)

**장점**:
- Unity API 사용 가능 (`Input.GetAxis`, `Debug.Log` 등)
- Managed 타입 사용 가능 (string, class)
- 작성이 간단함

**단점**:
- Burst 컴파일 불가 → 성능 낮음
- GC 오버헤드 발생
- 레거시 방식

**사용 시기**:
- Unity API가 필수인 경우 (Input.GetAxis 등)
- Managed 타입이 필요한 경우
- 성능이 크리티컬하지 않은 경우

### ISystem (Struct 기반)

**장점**:
- Burst 컴파일 가능 → 고성능
- GC 오버헤드 없음
- 병렬 처리 최적화
- Unity 권장 방식

**단점**:
- Unity API 사용 불가
- Managed 타입 사용 불가
- 순수 계산 로직만 가능

**사용 시기**:
- 순수 계산 로직 (이동, 회전, 충돌 등)
- 대량의 엔티티 처리
- 고성능이 요구되는 경우
- **대부분의 게임 로직 시스템**

### 선택 가이드

```
Unity API 필요? (Input, Debug, GameObject 등)
    ↓ YES
SystemBase 사용

    ↓ NO

순수 계산/로직만 수행?
    ↓ YES
ISystem 사용 (권장)
```

### 프로젝트 적용 예시

**PlayerInputSystem → SystemBase 선택 이유**:
- `Input.GetAxis()`는 Unity API이므로 SystemBase 필요

**PlayerMovementSystem → ISystem 선택 이유**:
- 순수 계산만 수행 → Burst 최적화 가능
- 1000개 엔티티 처리 시 약 8배 빠름

---

## 핵심 정리

### SystemGroup 순서
1. **InitializationSystemGroup**: 데이터 수집 (입력, 네트워크)
2. **SimulationSystemGroup**: 게임 로직 (이동, 물리)
3. **PresentationSystemGroup**: 렌더링 준비 (카메라, 애니메이션)

### System 타입 선택
- **SystemBase**: Unity API가 필요한 경우만 (입력, 디버그)
- **ISystem**: 나머지 모든 경우 (권장, 고성능)

### 프로젝트 적용
```
PlayerInputSystem (SystemBase)          → InitializationSystemGroup
    ↓ (PlayerInput 데이터)
PlayerMovementSystem (ISystem + Burst)  → SimulationSystemGroup
```

---

## Entity Prefab과 Baking

Unity ECS에서 GameObject Prefab을 Entity로 변환하여 사용하는 시스템입니다.

### Entity Prefab이란?

Entity를 복사하기 위한 **템플릿 Entity**입니다.
- GameObject Prefab → Baking → Entity Prefab
- 런타임에 `Instantiate`로 빠르게 복사 생성
- `Prefab` 태그로 일반 Entity와 구분

### 변환 과정 (Baking)

```
[Unity Editor]
GameObject Prefab (Bullet.prefab)
  - Transform, MeshRenderer
  - BulletAuthoring (MonoBehaviour)

    ↓ [Play 버튼 클릭]

[Baking 단계]
Baker 실행
  - MonoBehaviour → IComponentData 변환
  - GameObject → Entity 변환

    ↓

[런타임]
Entity Prefab (메모리)
  - BulletTag, BulletSpeed, BulletLifetime
  - LocalTransform, RenderMesh
  - Prefab 태그 포함
```

### Baker 클래스

```csharp
public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 5f;

    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // MonoBehaviour 데이터를 IComponentData로 변환
            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletLifetime { RemainingTime = authoring.Lifetime });
        }
    }
}
```

### Prefab 참조 변환

```csharp
public class PlayerAuthoring : MonoBehaviour
{
    public GameObject BulletPrefab;  // Inspector에서 할당

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // GameObject Prefab을 Entity Prefab으로 변환
            var bulletPrefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

            // Entity 참조를 컴포넌트에 저장
            AddComponent(entity, new AutoShootConfig
            {
                BulletPrefab = bulletPrefabEntity  // Entity 참조
            });
        }
    }
}
```

**GetEntity가 하는 일**:
1. BulletPrefab의 BulletAuthoring 찾기
2. BulletAuthoring의 Baker 실행
3. GameObject → Entity 변환
4. 변환된 Entity 참조 반환

### 런타임 사용 (Instantiate)

```csharp
public partial struct AutoShootSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = ...;

        foreach (var (config, transform) in
                 SystemAPI.Query<RefRO<AutoShootConfig>, RefRO<LocalTransform>>())
        {
            // Entity Prefab 복사 (매우 빠름!)
            Entity bullet = ecb.Instantiate(config.ValueRO.BulletPrefab);

            // 복사된 Entity는 Prefab의 모든 컴포넌트 보유
            // Prefab 태그는 자동 제거됨

            // 필요한 부분만 수정
            ecb.SetComponent(bullet, LocalTransform.FromPosition(...));
            ecb.SetComponent(bullet, new BulletDirection { Value = ... });
        }
    }
}
```

### Prefab 태그의 역할

```csharp
struct Prefab : IComponentData { }
```

**기능**:
- 시스템 쿼리에서 자동 제외 (렌더링, 업데이트 안 됨)
- Instantiate 시 자동 제거 (복사본은 일반 Entity가 됨)
- 원본 Prefab은 변경되지 않음

**쿼리 예시**:
```csharp
// Prefab은 제외하고 실제 총알만 처리
SystemAPI.Query<RefRW<BulletLifetime>>()
    .WithAll<BulletTag>()
    .WithNone<Prefab>()  // Prefab 제외
```

### TransformUsageFlags

```csharp
TransformUsageFlags.Dynamic   // Entity가 움직임 (총알, 플레이어)
TransformUsageFlags.Renderable // 화면에 렌더링됨
TransformUsageFlags.None      // Transform 불필요 (순수 데이터)
```

### 성능 비교

| 방식 | 10,000개 생성 시간 |
|------|------------------|
| GameObject.Instantiate | ~50ms |
| Entity Instantiate | ~2ms (25배 빠름) |

**빠른 이유**:
- 연속된 메모리 복사 (CPU 캐시 친화적)
- 단순 memcpy (객체 초기화 없음)
- GC 오버헤드 없음

### 핵심 정리

**Entity Prefab = GameObject Prefab의 ECS 버전**
- Baking: GameObject → Entity 변환 (Play 시 자동)
- Baker: 변환 로직을 정의하는 클래스
- Prefab 태그: 템플릿임을 표시 (시스템에서 제외)
- Instantiate: Prefab 복사하여 새 Entity 생성

**데이터 흐름**:
```
GameObject Prefab (에디터)
    ↓ [Baking]
Entity Prefab (템플릿)
    ↓ [Instantiate]
Entity Instance (게임 오브젝트)
```

---

## Query vs QueryBuilder

Entity를 검색하는 두 가지 방식의 차이점입니다.

### 비교표

| 특징 | `Query<>()` | `QueryBuilder()` |
|------|-------------|------------------|
| **용도** | 간단한 쿼리 | 복잡한 쿼리 |
| **문법** | 제네릭 매개변수 | 메서드 체이닝 |
| **간결성** | ✅ 매우 간결 | 약간 장황 |
| **필터링** | 제한적 | ✅ 강력함 |
| **사용 빈도** | ✅ 90% 이상 | 10% 미만 |

### SystemAPI.Query<>() - 일반적인 방법

**대부분의 경우 사용**하는 간단한 쿼리 방식입니다.

```csharp
// 기본 사용
foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>())
{
    // LocalTransform을 가진 모든 Entity 처리
}

// 여러 컴포넌트
foreach (var (transform, speed, input) in
         SystemAPI.Query<RefRO<LocalTransform>,
                       RefRW<MovementSpeed>,
                       RefRO<PlayerInput>>())
{
    // 3개 컴포넌트를 모두 가진 Entity만
}

// Entity 접근 필요 시
foreach (var (transform, entity) in
         SystemAPI.Query<RefRO<LocalTransform>>()
                   .WithEntityAccess())
{
    // Entity 자체에도 접근 가능
}
```

**Ref 타입**:
- `RefRO<T>`: Read-Only (읽기만, 성능 최적화)
- `RefRW<T>`: Read-Write (읽기/쓰기)

### SystemAPI.QueryBuilder() - 복잡한 조건

**특수한 경우**에만 사용하는 강력한 쿼리 방식입니다.

```csharp
// WithAll: 반드시 포함
var query = SystemAPI.QueryBuilder()
    .WithAll<BulletTag>()        // BulletTag 필수
    .WithAll<MovementSpeed>()    // MovementSpeed도 필수
    .Build();

// WithNone: 포함하지 않음
var query = SystemAPI.QueryBuilder()
    .WithAll<BulletTag>()        // 총알이지만
    .WithNone<Prefab>()          // Prefab은 아닌 것
    .Build();

// WithAny: 하나라도 포함
var query = SystemAPI.QueryBuilder()
    .WithAny<PlayerTag, EnemyTag>()  // Player 또는 Enemy
    .Build();

// 복합 조건
var query = SystemAPI.QueryBuilder()
    .WithAll<HealthComponent>()           // 체력이 있고
    .WithAny<PlayerTag, EnemyTag>()       // 플레이어거나 적이고
    .WithNone<DeadTag, InvincibleTag>()   // 죽지도 무적도 아님
    .Build();
```

### 실전 사용 예시

#### Query<>() - 일반적인 경우 (90%)
```csharp
public partial struct PlayerMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 간단한 컴포넌트 조합
        foreach (var (transform, speed, input) in
                 SystemAPI.Query<RefRW<LocalTransform>,
                               RefRO<MovementSpeed>,
                               RefRO<PlayerInput>>())
        {
            // 대부분의 경우 이것만으로 충분
        }
    }
}
```

#### QueryBuilder() - 특수한 경우 (10%)
```csharp
public partial struct BulletCountSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Prefab 제외하고 실제 총알만 세기
        var bulletQuery = SystemAPI.QueryBuilder()
            .WithAll<BulletTag>()
            .WithNone<Prefab>()    // 중요!
            .Build();

        int activeBullets = bulletQuery.CalculateEntityCount();
    }
}
```

#### Query 재사용 패턴
```csharp
public partial struct CombatSystem : ISystem
{
    private EntityQuery _damagableQuery;

    public void OnCreate(ref SystemState state)
    {
        // 시스템 생성 시 한 번만 빌드 (성능 최적화)
        _damagableQuery = SystemAPI.QueryBuilder()
            .WithAll<HealthComponent>()
            .WithNone<DeadTag>()
            .Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 매 프레임 재사용
        int aliveCount = _damagableQuery.CalculateEntityCount();
    }
}
```

### 선택 가이드

```
단순 컴포넌트 조합?
    ↓ YES
Query<>() 사용 (대부분의 경우)

    ↓ NO

Prefab 제외 필요? 복잡한 필터 조건?
    ↓ YES
QueryBuilder() 사용
```

### 핵심 정리

**Query<>() = 일상적 사용**
- foreach 루프에서 직접 사용
- 컴포넌트 타입만 지정
- 90% 이상의 경우에 사용

**QueryBuilder() = 특수 상황**
- Prefab 제외 필요 시
- Entity 개수 계산 시
- 복잡한 필터링 필요 시
- Query 재사용 시 (OnCreate에서 생성)

**프로젝트 예시**:
```csharp
// AutoShootSystem - Query<>() 사용
foreach (var (transform, config) in
         SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>>())
{
    // 간단하므로 Query<> 충분
}

// BulletLifetimeSystem - QueryBuilder() 사용 (가정)
var activeBullets = SystemAPI.QueryBuilder()
    .WithAll<BulletTag>()
    .WithNone<Prefab>()  // Prefab 제외 필요
    .Build();
```

---

## Archetype과 Query 성능

Unity ECS는 Archetype 시스템으로 Entity를 자동 분류하여 Query 성능을 최적화합니다.

### Archetype이란?

**동일한 컴포넌트 조합을 가진 Entity들의 그룹**입니다.

```
Archetype 1: [PlayerTag, LocalTransform, MovementSpeed, PlayerInput]
    ├─ Player Entity 1
    └─ Player Entity 2

Archetype 2: [BulletTag, LocalTransform, BulletSpeed, BulletLifetime]
    ├─ Bullet Entity 1
    ├─ Bullet Entity 2
    └─ ... (1000개)

Archetype 3: [EnemyTag, LocalTransform, Health, AI]
    └─ Enemy Entity 1...
```

### Query의 실제 동작

사용자는 간단하게 쿼리를 작성하지만, ECS는 내부적으로 Archetype 필터링을 수행합니다.

```csharp
// 사용자 코드 - 간단해 보임
foreach (var (transform, speed) in
         SystemAPI.Query<RefRO<LocalTransform>, RefRO<BulletSpeed>>())
{
    // 모든 Entity를 검색할 것 같지만...
}

// ECS 내부 동작 - 자동 최적화
// 1. Query 분석: "LocalTransform + BulletSpeed 필요"
// 2. Archetype 필터링:
//    - Player Archetype: BulletSpeed 없음 → 제외
//    - Bullet Archetype: 둘 다 있음 → 선택!
//    - Enemy Archetype: BulletSpeed 없음 → 제외
// 3. Bullet Archetype의 Chunk만 순회
```

### Chunk 기반 메모리 구조

Archetype의 Entity들은 Chunk라는 연속된 메모리 블록에 저장됩니다.

```
Bullet Archetype Chunk (연속 메모리):
┌─────────────────────────────────────┐
│ LocalTransform[100개] (연속)        │
│ BulletSpeed[100개] (연속)           │
│ BulletLifetime[100개] (연속)        │
└─────────────────────────────────────┘

Player Archetype Chunk (별도 메모리):
┌─────────────────────────────────────┐
│ LocalTransform[10개]                │
│ MovementSpeed[10개]                 │
└─────────────────────────────────────┘
```

**장점:**
- Query 실행 시 필요한 Chunk만 읽음
- CPU 캐시에 데이터가 연속으로 로드 → 매우 빠름
- 불필요한 Entity는 아예 접근하지 않음

### 성능 비교

#### 시나리오: 10,000개 Entity
- Player: 10개
- Bullet: 1,000개
- Enemy: 9,000개

#### GameObject 방식 (느림)
```csharp
var allObjects = FindObjectsOfType<GameObject>();
foreach (var obj in allObjects)  // 10,000개 전부 확인
{
    var bullet = obj.GetComponent<Bullet>();
    if (bullet != null) { /* 처리 */ }
}
// 시간: ~10ms
```

#### ECS 방식 (빠름)
```csharp
// Bullet Archetype만 자동 선택
foreach (var bullet in
         SystemAPI.Query<RefRO<BulletTag>, RefRO<BulletSpeed>>())
{
    // 1,000개 Bullet만 순회
}
// 시간: ~0.1ms (100배 빠름!)
```

### 실전 예시

#### 총알만 검색
```csharp
public partial struct BulletMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // BulletDirection을 요구 → Bullet Archetype만 선택
        foreach (var (transform, direction, speed) in
                 SystemAPI.Query<RefRW<LocalTransform>,
                               RefRO<BulletDirection>,
                               RefRO<BulletSpeed>>())
        {
            // Player, Enemy는 이 쿼리에 포함 안 됨!
            // BulletDirection 컴포넌트가 없기 때문
        }
    }
}
```

#### 플레이어만 검색
```csharp
public partial struct PlayerMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // PlayerInput을 요구 → Player Archetype만 선택
        foreach (var (transform, input, speed) in
                 SystemAPI.Query<RefRW<LocalTransform>,
                               RefRO<PlayerInput>,
                               RefRO<MovementSpeed>>())
        {
            // Bullet, Enemy는 제외됨
        }
    }
}
```

### 성능 측정 결과

| 작업 | GameObject | ECS | 배속 |
|------|-----------|-----|------|
| 1,000개 Bullet 업데이트 | ~5ms | ~0.05ms | 100배 |
| 검색 방식 | 모든 오브젝트 순회 | Archetype 필터링 | - |
| 메모리 접근 | 랜덤 (캐시 미스) | 연속 (캐시 히트) | - |

**ECS가 빠른 이유:**
1. **자동 Archetype 분류**: Entity가 자동으로 분류됨
2. **필요한 것만 순회**: Bullet 찾을 때 Player는 아예 안 봄
3. **연속 메모리**: CPU 캐시 효율 극대화
4. **Burst 컴파일**: 네이티브 코드 수준 최적화
5. **Job System**: 자동 병렬 처리

### Archetype 변경 시 주의사항

컴포넌트 추가/제거 시 Entity가 다른 Archetype으로 이동합니다.

```csharp
// Archetype 변경 (비용이 큼)
entityManager.AddComponent<NewComponent>(entity);  // 느림!
// Entity가 새 Archetype의 Chunk로 복사됨

// 해결책 1: Enable/Disable 사용
entityManager.SetEnabled(entity, false);  // 빠름!
// Archetype 변경 없이 비활성화

// 해결책 2: 상태를 값으로 관리
public struct BulletState : IComponentData
{
    public bool IsActive;  // Archetype 변경 없음
}
```

### 핵심 정리

**Query는 모든 Entity를 검색하지 않습니다!**

**동작 방식:**
```
사용자: SystemAPI.Query<BulletTag, BulletSpeed>()
    ↓
ECS: "이 컴포넌트를 가진 Archetype은?"
    ↓
ECS: "Bullet Archetype만 해당!"
    ↓
결과: Bullet Archetype의 Chunk만 순회
```

**성능 요약:**
- 10,000개 Entity 중 1,000개만 필요 → 1,000개만 검색
- 연속 메모리 접근 → CPU 캐시 효율 극대화
- GameObject 대비 100배 빠름

**결론:** Query 성능 걱정 없이 편하게 사용하세요! ECS가 알아서 최적화합니다. 👍

---

## IJobEntity의 Execute 자동 호출 메커니즘

`IJobEntity`의 `Execute` 함수가 어떻게 자동으로 호출되는지 상세히 설명합니다.

### 기본 개념

`IJobEntity`는 Unity ECS에서 **자동으로 엔티티를 순회**하며 `Execute` 함수를 호출하는 특별한 인터페이스입니다.

개발자는 로직만 작성하면, Unity ECS가 나머지를 자동 처리합니다:
- 컴포넌트를 가진 엔티티 검색
- 각 엔티티마다 Execute 호출
- 병렬 처리 (멀티스레드)

### 호출 흐름 상세 분석

```csharp
[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;

    // ⚙️ 이 함수는 자동으로 각 엔티티마다 호출됩니다!
    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        // 로직...
    }
}
```

#### 호출되는 시점

```csharp
public void OnUpdate(ref SystemState state)
{
    // 1. Job 인스턴스 생성
    var job = new EnemyChaseJob
    {
        PlayerPosition = playerPosition,
        DeltaTime = deltaTime
    };

    // 2. ScheduleParallel() 호출 시점에 Unity ECS가 자동으로:
    //    - EnemySpeed + LocalTransform을 가진 모든 엔티티를 찾음
    //    - 각 엔티티마다 Execute()를 호출함
    job.ScheduleParallel();
}
```

### 단계별 상세 설명

#### Step 1: 쿼리 자동 생성

Unity ECS는 `Execute` 함수의 **파라미터**를 보고 자동으로 쿼리를 생성합니다:

```csharp
void Execute(ref LocalTransform transform, in EnemySpeed speed)
//           ^^^                          ^^
//           필요한 컴포넌트들
```

**자동 생성되는 쿼리 (의사 코드)**:
```csharp
// Unity가 내부적으로 이렇게 쿼리를 생성합니다
var query = SystemAPI.QueryBuilder()
    .WithAll<LocalTransform>()   // Execute 파라미터에 있음
    .WithAll<EnemySpeed>()       // Execute 파라미터에 있음
    .Build();
```

#### Step 2: 엔티티 순회 및 Execute 호출

```csharp
// Unity가 내부적으로 수행하는 작업 (의사 코드)
foreach (var entity in matchingEntities)
{
    // 각 엔티티에서 컴포넌트 가져오기
    ref LocalTransform transform = entity.GetComponent<LocalTransform>();
    ref EnemySpeed speed = entity.GetComponent<EnemySpeed>();

    // Execute 호출!
    job.Execute(ref transform, in speed);
}
```

#### Step 3: 병렬 처리

`ScheduleParallel()`을 호출하면 Unity Job System이 **여러 스레드에서 동시 실행**합니다:

```
엔티티 100개가 있다면:

스레드 1: Execute(entity 1~25)
스레드 2: Execute(entity 26~50)
스레드 3: Execute(entity 51~75)
스레드 4: Execute(entity 76~100)

⚡ 병렬로 실행되어 4배 빠름!
```

### 실제 예제로 이해하기

현재 씬 상태를 가정:
```
PlayerSubScene:
- Player (PlayerTag, LocalTransform, MovementSpeed)
- Enemy_1 (EnemyTag, LocalTransform, EnemySpeed, EnemyHealth)
- Enemy_2 (EnemyTag, LocalTransform, EnemySpeed, EnemyHealth)
- Enemy_3 (EnemyTag, LocalTransform, EnemySpeed, EnemyHealth)
```

#### OnUpdate 실행 흐름:

```csharp
public void OnUpdate(ref SystemState state)
{
    // 1️⃣ 플레이어 위치 쿼리 (한 번만)
    float3 playerPosition = float3.zero;
    foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
    {
        playerPosition = transform.ValueRO.Position; // (0, 1, 0)
        break;
    }

    // 2️⃣ Job 생성 및 스케줄링
    new EnemyChaseJob
    {
        PlayerPosition = playerPosition,  // (0, 1, 0)
        DeltaTime = 0.016f               // 60 FPS 기준
    }.ScheduleParallel();
    // ⚙️ 여기서 Unity가 자동으로 Execute를 호출합니다!
}
```

#### Execute 호출 과정 (내부 동작):

```csharp
// Unity가 자동으로 수행:

// Enemy_1에 대해 Execute 호출
Execute(
    ref Enemy_1.LocalTransform,  // Position: (5, 0, 5)
    in Enemy_1.EnemySpeed        // Value: 3
);

// Enemy_2에 대해 Execute 호출
Execute(
    ref Enemy_2.LocalTransform,  // Position: (-5, 0, 5)
    in Enemy_2.EnemySpeed        // Value: 3
);

// Enemy_3에 대해 Execute 호출
Execute(
    ref Enemy_3.LocalTransform,  // Position: (0, 0, 10)
    in Enemy_3.EnemySpeed        // Value: 3
);
```

**Player는 제외됨**: EnemySpeed 컴포넌트가 없기 때문!

### Execute 파라미터의 의미

```csharp
void Execute(ref LocalTransform transform, in EnemySpeed speed)
//           ^^^                           ^^
//           |                             |
//           |                             +-- 읽기 전용 (성능 최적화)
//           +-- 읽기/쓰기 가능 (위치 업데이트 필요)
```

#### `ref` (읽기/쓰기)
- 컴포넌트를 **수정**할 수 있음
- `transform.Position += movement;` ✅ 가능
- 쓰기 권한이 필요한 경우 사용

#### `in` (읽기 전용)
- 컴포넌트를 **읽기만** 가능
- `speed.Value = 10;` ❌ 불가능
- 성능 최적화 (복사 안 함)
- 읽기만 필요한 경우 사용

### 쿼리 필터링 추가

**EnemyTag**를 가진 엔티티만 처리하고 싶다면:

```csharp
[BurstCompile]
[WithAll(typeof(EnemyTag))]  // ✅ 필터 추가!
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        // EnemyTag를 가진 엔티티만 여기 들어옴!
    }
}
```

**효과**:
- Player는 제외됨 (PlayerTag만 있음)
- Enemy만 처리됨 (EnemyTag가 있음)

### Schedule vs ScheduleParallel

#### `Schedule()` - 순차 실행
```csharp
job.Schedule();
// 단일 스레드에서 순차 실행
// Enemy_1 → Enemy_2 → Enemy_3
```

#### `ScheduleParallel()` - 병렬 실행 ⚡
```csharp
job.ScheduleParallel();
// 여러 스레드에서 동시 실행
// Enemy_1, Enemy_2, Enemy_3 동시 처리
```

**성능 비교**:
- 순차: 1ms × 100개 = 100ms
- 병렬 (4코어): 1ms × 100개 ÷ 4 = 25ms (4배 빠름)

### 실행 순서 보장

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
public partial struct EnemyChaseSystem : ISystem
```

**실행 순서**:
1. EnemySpawnSystem 실행 (몬스터 스폰)
2. **EnemyChaseSystem 실행** (추적 시작)
3. Job 스케줄링
4. **Execute 자동 호출** (각 Enemy마다)

### 비교: Entities.ForEach vs IJobEntity

#### SystemBase + Entities.ForEach (레거시)
```csharp
protected override void OnUpdate()
{
    float deltaTime = Time.DeltaTime;

    Entities
        .WithAll<EnemyTag>()
        .ForEach((ref LocalTransform transform, in EnemySpeed speed) =>
        {
            // 로직...
        })
        .ScheduleParallel();
}
```

#### ISystem + IJobEntity (최신, 권장) ✅
```csharp
public void OnUpdate(ref SystemState state)
{
    new EnemyChaseJob { ... }.ScheduleParallel();
}

[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        // 로직...
    }
}
```

**IJobEntity의 장점**:
- Burst 컴파일 최적화
- 더 빠른 성능
- 명확한 구조 분리

### Archetype과의 관계

Execute는 Archetype 필터링을 자동으로 활용합니다:

```
모든 Entity:
├─ Player Archetype: [PlayerTag, LocalTransform, MovementSpeed]
│   └─ Player (제외됨 - EnemySpeed 없음)
│
├─ Enemy Archetype: [EnemyTag, LocalTransform, EnemySpeed, EnemyHealth]
│   ├─ Enemy_1 (Execute 호출 ✅)
│   ├─ Enemy_2 (Execute 호출 ✅)
│   └─ Enemy_3 (Execute 호출 ✅)
│
└─ Bullet Archetype: [BulletTag, LocalTransform, BulletSpeed]
    └─ Bullet (제외됨 - EnemySpeed 없음)
```

**성능 최적화**:
- Enemy Archetype의 Chunk만 순회
- Player, Bullet은 아예 접근하지 않음
- 연속 메모리 접근으로 CPU 캐시 효율 극대화

### 디버깅 팁

Execute가 호출되는지 확인하려면:

```csharp
void Execute(ref LocalTransform transform, in EnemySpeed speed)
{
    // ⚠️ 주의: Burst 컴파일 시 Debug.Log 사용 불가!
    // [BurstCompile]를 잠시 제거하고 테스트:

    // UnityEngine.Debug.Log($"Execute called for enemy at {transform.Position}");

    float3 direction = PlayerPosition - transform.Position;
    // ...
}
```

### 요약 비교표

| 항목 | 설명 |
|------|------|
| **누가 호출?** | Unity ECS가 자동으로 |
| **언제 호출?** | `ScheduleParallel()` 실행 시 |
| **몇 번 호출?** | 조건에 맞는 엔티티 개수만큼 |
| **어떤 엔티티?** | Execute 파라미터의 컴포넌트를 모두 보유한 엔티티 |
| **어떻게 실행?** | 병렬 (멀티스레드) |
| **성능** | Burst 컴파일로 최적화 |
| **메모리 접근** | 연속 메모리 (Archetype Chunk) |

### 핵심 정리

**IJobEntity의 Execute는 자동 호출 함수입니다!**

**동작 원리**:
```
개발자: Execute 파라미터 정의
    ↓
Unity ECS: 파라미터 분석 → 쿼리 자동 생성
    ↓
Unity ECS: Archetype 필터링 → 조건 맞는 엔티티만 선택
    ↓
Unity Job System: 병렬 처리 → 각 엔티티마다 Execute 호출
    ↓
결과: 모든 Enemy가 플레이어 추적
```

**개발자가 할 일**:
1. Execute 파라미터 정의 (필요한 컴포넌트)
2. Execute 로직 작성 (추적 이동)
3. ScheduleParallel() 호출

**Unity ECS가 알아서 하는 일**:
1. 쿼리 생성
2. 엔티티 검색
3. Execute 호출
4. 병렬 처리
5. 성능 최적화

**결론**: 개발자는 로직만 작성하면 됩니다! ECS가 나머지를 자동으로 처리합니다. 🚀

---

## Unity DOTS Physics와 충돌 처리

Unity Physics 패키지를 사용한 충돌 감지 및 처리 메커니즘입니다.

### ITriggerEventsJob - 트리거 이벤트 처리

Physics 시뮬레이션에서 발생한 트리거 이벤트를 처리하는 Job입니다.

#### 기본 구조

```csharp
[BurstCompile]
struct BulletHitJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<BulletTag> BulletLookup;
    [ReadOnly] public ComponentLookup<EnemyTag> EnemyLookup;
    public ComponentLookup<EnemyHealth> HealthLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;

        // 충돌 처리...
    }
}
```

#### 핵심 개념

**1. Execute 호출 패턴**
- `Schedule()` 1번 호출 → `Execute()` N번 호출
- N = 트리거 이벤트 발생 횟수
- 각 충돌마다 Execute가 한 번씩 호출됨

```
프레임 1:
- 총알 3개가 적과 충돌
- Schedule() 1번 호출
- Execute() 3번 호출 (각 충돌마다)

프레임 2:
- 총알 1개가 적과 충돌
- Schedule() 1번 호출
- Execute() 1번 호출
```

**2. ComponentLookup<T>**

Entity의 컴포넌트에 접근하는 방법:

```csharp
// 읽기 전용 - [ReadOnly] 속성 필수
[ReadOnly] public ComponentLookup<BulletTag> BulletLookup;

// 읽기/쓰기
public ComponentLookup<EnemyHealth> HealthLookup;

// 사용
if (BulletLookup.HasComponent(entity))  // 컴포넌트 보유 확인
{
    var health = HealthLookup[entity];  // 컴포넌트 읽기
    health.Value -= 10;
    HealthLookup[entity] = health;       // 컴포넌트 쓰기
}
```

**3. EntityCommandBuffer.ParallelWriter**

병렬 처리에서 안전하게 Entity 명령을 기록:

```csharp
public EntityCommandBuffer.ParallelWriter ECB;

// sortKey: 병렬 처리 시 순서 보장용 (보통 0 사용)
ECB.DestroyEntity(sortKey, entity);
ECB.SetComponent(sortKey, entity, component);
```

#### ITriggerEventsJob 스케줄링

```csharp
public void OnUpdate(ref SystemState state)
{
    var ecb = new EntityCommandBuffer(Allocator.TempJob);
    var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

    // Job 스케줄
    state.Dependency = new BulletHitJob
    {
        BulletLookup = SystemAPI.GetComponentLookup<BulletTag>(true),
        ECB = ecb.AsParallelWriter()
    }.Schedule(simulation, state.Dependency);

    // 완료 대기 및 명령 실행
    state.Dependency.Complete();
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
}
```

### state.Dependency - Job 체이닝

여러 Job을 순서대로 실행하기 위한 의존성 관리 시스템입니다.

#### 핵심 패턴: 읽기 → 쓰기

```csharp
// ⚙️ 기존 의존성 읽기
state.Dependency = new MyJob { ... }.Schedule(state.Dependency);
//                                              ^^^^^^^^^^^^^^^
//                                              이전 Job이 끝나야 시작

// 다음 Job도 체이닝
state.Dependency = new NextJob { ... }.Schedule(state.Dependency);
```

**왜 읽고 쓰는가?**

```
프레임 1:
1. state.Dependency 읽기 → JobHandle_A (이전 프레임 Job)
2. MyJob.Schedule(JobHandle_A) → "JobHandle_A가 끝나면 실행해"
3. 반환값 JobHandle_B를 state.Dependency에 쓰기
4. 다음 프레임에서 JobHandle_B를 읽음

프레임 2:
1. state.Dependency 읽기 → JobHandle_B (프레임 1의 MyJob)
2. NextJob.Schedule(JobHandle_B) → "JobHandle_B가 끝나면 실행해"
3. 반환값 JobHandle_C를 state.Dependency에 쓰기
```

#### Job 의존성 체인 예시

```csharp
// Job A: 적 이동
state.Dependency = new EnemyMoveJob().ScheduleParallel(state.Dependency);

// Job B: 충돌 체크 (적 이동 후 실행)
state.Dependency = new BulletHitJob().Schedule(simulation, state.Dependency);

// Job C: 데미지 처리 (충돌 체크 후 실행)
state.Dependency = new DamageJob().ScheduleParallel(state.Dependency);
```

**실행 순서**:
```
Job A 시작 → Job A 끝 → Job B 시작 → Job B 끝 → Job C 시작 → Job C 끝
```

#### Complete()의 역할

```csharp
state.Dependency.Complete();  // 현재 프레임 내에서 Job 완료 대기
ecb.Playback(state.EntityManager);  // Job 결과를 즉시 적용
```

**중요**: Complete()는 다음 프레임으로 넘어가지 않습니다!
- 같은 프레임 안에서 Job이 끝날 때까지 대기
- Job 완료 후 다음 코드 실행
- 프레임은 OnUpdate 전체가 끝나야 진행

### state.RequireForUpdate<T>

시스템 실행 조건을 설정합니다.

```csharp
public void OnCreate(ref SystemState state)
{
    state.RequireForUpdate<SimulationSingleton>();
}
```

**효과**:
- `SimulationSingleton` 컴포넌트가 없으면 `OnUpdate` 실행 안 됨
- Physics World가 준비되지 않았으면 시스템 비활성화
- 불필요한 실행 방지 (성능 최적화)

**사용 시기**:
- Physics 시스템: `SimulationSingleton` 필요
- Player 관련 시스템: `PlayerTag` 필요
- 특정 상태에서만 동작: 상태 컴포넌트 필요

### 프레임 내 실행 흐름

```
[프레임 N 시작]

OnUpdate() 시작
    ↓
state.Dependency 읽기 (프레임 N-1의 JobHandle)
    ↓
new Job().Schedule(이전 JobHandle)
    ↓
state.Dependency 쓰기 (새 JobHandle 저장)
    ↓
state.Dependency.Complete() ← 여기서 대기 (프레임 안에서)
    ↓
Job 실행 완료
    ↓
ecb.Playback() (결과 적용)
    ↓
OnUpdate() 끝

[프레임 N 끝]
[프레임 N+1 시작] ← 여기서 다음 프레임
```

### 실전 예제: BulletHitSystem

총알-몬스터 충돌 처리 시스템의 전체 흐름:

```csharp
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct BulletHitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Physics World 준비될 때까지 대기
        state.RequireForUpdate<SimulationSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 1. EntityCommandBuffer 생성
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        // 2. Job 스케줄 (이전 Physics Job 후 실행)
        state.Dependency = new BulletHitJob
        {
            BulletLookup = SystemAPI.GetComponentLookup<BulletTag>(true),
            EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
            DamageLookup = SystemAPI.GetComponentLookup<DamageValue>(true),
            HealthLookup = SystemAPI.GetComponentLookup<EnemyHealth>(false),
            ECB = ecb.AsParallelWriter()
        }.Schedule(simulation, state.Dependency);

        // 3. Job 완료 대기 (같은 프레임 안에서)
        state.Dependency.Complete();

        // 4. 명령 실행 (Entity 삭제, 컴포넌트 변경)
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**실행 흐름**:
```
PhysicsSystemGroup 실행 (충돌 감지)
    ↓
BulletHitSystem.OnUpdate 시작
    ↓
BulletHitJob.Schedule() - 충돌 이벤트 처리 Job 등록
    ↓
Execute() N번 호출 (각 충돌마다)
    - 총알 A ↔ 몬스터 1
    - 총알 B ↔ 몬스터 2
    - 총알 C ↔ 몬스터 1
    ↓
Complete() - Job 완료 대기
    ↓
Playback() - ECB 명령 실행
    - 총알 A, B, C 삭제
    - 몬스터 1 체력 -50
    - 몬스터 2 체력 -25
```

### 핵심 정리

**ITriggerEventsJob**:
- 충돌 이벤트 1개당 Execute() 1번 호출
- ComponentLookup으로 Entity 데이터 접근
- EntityCommandBuffer로 안전한 Entity 수정

**state.Dependency**:
- 읽기: 이전 Job 의존성 가져오기
- 쓰기: 새 Job 의존성 저장하기
- Job 순서 보장을 위한 체인 구조

**Complete()**:
- 현재 프레임 안에서 Job 완료 대기
- 다음 프레임으로 넘어가지 않음
- ECB 명령 실행 전 필수

**RequireForUpdate**:
- 필요한 컴포넌트가 없으면 OnUpdate 실행 안 됨
- Physics, Player 등 전제 조건 체크
- 불필요한 시스템 실행 방지

---

**작성일**: 2025-11-26, 2025-11-27, 2025-11-30, 2025-12-01
**프로젝트**: projectc (Unity DOTS Phase 1-5)
