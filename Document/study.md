# Unity DOTS 학습 노트

## 목차
1. [SystemGroup 실행 순서](#systemgroup-실행-순서)
2. [SystemBase vs ISystem](#systembase-vs-isystem)
3. [Entity Prefab과 Baking](#entity-prefab과-baking)
4. [Query vs QueryBuilder](#query-vs-querybuilder)
5. [Archetype과 Query 성능](#archetype과-query-성능)

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

**작성일**: 2025-11-26, 2025-11-27
**프로젝트**: projectc (Unity DOTS Phase 1-2)
