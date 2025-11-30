# Phase 4 실행 계획: 몬스터 AI - 플레이어 추적 시스템

## 프로젝트 개요

**Phase**: Phase 4 - 몬스터 AI - 플레이어 추적 시스템
**목표**: 스폰된 몬스터가 플레이어를 향해 이동하는 AI 시스템을 ECS 기반으로 구현
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수

## Phase 4 개발 사항 요약

이 단계에서는 Phase 3에서 스폰된 몬스터들이 플레이어의 위치를 추적하며 일정 속도로 접근하는 AI 시스템을 완성합니다.

### 구현 범위
1. 몬스터 추적 시스템 (EnemyChaseSystem) 구현
2. 플레이어 위치 쿼리 및 방향 계산
3. Burst 컴파일 및 Job System 병렬 처리
4. 성능 최적화 및 검증

### Phase 3 완료 전제 조건
- ✅ EnemyTag, EnemyHealth, EnemySpeed 컴포넌트 구현됨
- ✅ Enemy.prefab 생성 및 EnemyAuthoring 설정됨
- ✅ EnemySpawnSystem으로 몬스터 스폰 작동함
- ✅ PlayerTag로 플레이어 엔티티 식별 가능

---

## 태스크 분해

### **TASK-015: 몬스터 추적 시스템 구현**

**카테고리**: 게임플레이/AI
**우선순위**: P0 (최우선)
**설명**: 몬스터가 플레이어를 향해 이동하는 EnemyChaseSystem 구현

**구현 내용**:

#### 15.1 EnemyChaseSystem 구조 설계

Phase 4의 핵심은 **간결하면서도 성능이 뛰어난 추적 AI**입니다. 다음과 같은 설계 원칙을 따릅니다:

**설계 원칙**:
1. **단일 책임**: 추적 이동만 담당 (충돌, 데미지는 Phase 5)
2. **Burst 최적화**: 모든 계산을 Burst 컴파일 가능하게 작성
3. **병렬 처리**: IJobEntity를 활용한 멀티스레드 처리
4. **최소 쿼리**: 플레이어 위치는 한 번만 쿼리

#### 15.2 EnemyChaseSystem 구현

`Assets/Scripts/Systems/EnemyChaseSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 플레이어와 적이 모두 존재할 때만 실행
        state.RequireForUpdate<PlayerTag>();
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 플레이어 위치 쿼리 (한 번만)
        float3 playerPosition = float3.zero;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPosition = transform.ValueRO.Position;
            break; // 플레이어는 1명이므로 첫 번째만
        }

        // 모든 몬스터를 병렬로 처리
        new EnemyChaseJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        // 플레이어로의 방향 벡터 계산
        float3 direction = PlayerPosition - transform.Position;

        // Y축은 무시 (XZ 평면 이동)
        direction.y = 0;

        // 거리 확인 (제곱 거리로 비교하여 sqrt 연산 절약)
        float distanceSq = math.lengthsq(direction);

        // 최소 거리 체크 (0.1 유닛 이내면 이동 안 함)
        if (distanceSq > 0.01f) // 0.1 * 0.1
        {
            // 방향 정규화
            float3 normalizedDirection = math.normalize(direction);

            // 이동 거리 = 방향 * 속도 * deltaTime
            float3 movement = normalizedDirection * speed.Value * DeltaTime;

            // Transform 위치 업데이트
            transform.Position += movement;
        }
    }
}
```

#### 15.3 핵심 구현 포인트

**1. 플레이어 위치 쿼리 최적화**
```csharp
// ✅ 올바른 방법: 한 번만 쿼리
float3 playerPosition = float3.zero;
foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
{
    playerPosition = transform.ValueRO.Position;
    break;
}

// ❌ 잘못된 방법: 각 몬스터마다 쿼리 (성능 저하)
foreach (var enemyTransform in ...)
{
    foreach (var playerTransform in ...) // 중복 쿼리!
}
```

**2. 방향 벡터 계산**
```csharp
// 3D 공간에서 플레이어로의 벡터
float3 direction = PlayerPosition - transform.Position;

// Y축 무시 (지면 위에서만 이동)
direction.y = 0;
```

**3. 거리 체크 최적화**
```csharp
// ✅ 제곱 거리 사용 (sqrt 연산 절약)
float distanceSq = math.lengthsq(direction);
if (distanceSq > 0.01f) // 0.1 * 0.1

// ❌ 일반 거리 사용 (느림)
float distance = math.length(direction);
if (distance > 0.1f)
```

**4. 정규화 및 이동**
```csharp
// 방향 정규화 (단위 벡터로 변환)
float3 normalizedDirection = math.normalize(direction);

// 속도 적용
float3 movement = normalizedDirection * speed.Value * DeltaTime;

// 위치 업데이트
transform.Position += movement;
```

#### 15.4 시스템 실행 순서

```
SimulationSystemGroup
├── PlayerMovementSystem      (플레이어 이동)
├── AutoShootSystem            (총알 발사)
├── BulletMovementSystem       (총알 이동)
├── EnemySpawnSystem           (몬스터 스폰)
├── EnemyChaseSystem           (몬스터 추적) ← 새로 추가
└── BulletLifetimeSystem       (총알 수명)
```

**UpdateAfter 설정**:
- `[UpdateAfter(typeof(EnemySpawnSystem))]` - 스폰 후 추적 시작

**의존성**: Phase 3 완료 (TASK-010 ~ TASK-014)

**완료 조건**:
- [ ] EnemyChaseSystem.cs 작성 완료
- [ ] EnemyChaseJob IJobEntity 구현 완료
- [ ] Burst 컴파일 속성 적용됨
- [ ] 플레이어 위치 쿼리 최적화됨
- [ ] 방향 벡터 계산 정확함
- [ ] Y축 무시 (XZ 평면 이동)
- [ ] 거리 체크 로직 구현됨
- [ ] 컴파일 에러 없음

**예상 작업량**: 3-4시간

---

### **TASK-016: 몬스터 회전 시스템 구현 (선택사항)**

**카테고리**: 게임플레이/시각적 개선
**우선순위**: P2 (선택)
**설명**: 몬스터가 이동 방향을 바라보도록 Rotation 업데이트

**구현 내용**:

#### 16.1 회전 시스템 필요성

**구현 여부 판단**:
- **필수 아님**: Phase 4의 핵심 기능은 "이동"이며, 회전은 선택사항
- **시각적 개선**: 몬스터가 플레이어를 바라보는 것이 더 자연스러움
- **성능 영향**: 미미함 (quaternion 계산은 Burst로 최적화됨)

**권장 사항**: **구현하지 않음** (간결함 우선)

만약 구현한다면:

#### 16.2 회전 로직 (참고용)

```csharp
[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        float3 direction = PlayerPosition - transform.Position;
        direction.y = 0;

        float distanceSq = math.lengthsq(direction);

        if (distanceSq > 0.01f)
        {
            float3 normalizedDirection = math.normalize(direction);

            // 이동
            float3 movement = normalizedDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            // 회전 (선택사항)
            quaternion targetRotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 10f * DeltaTime);
        }
    }
}
```

**의존성**: TASK-015

**완료 조건** (구현 시):
- [ ] quaternion.LookRotationSafe 사용
- [ ] Slerp를 통한 부드러운 회전
- [ ] 회전 속도 조정 가능
- [ ] 성능 저하 없음

**예상 작업량**: 1-2시간 (구현 시)

---

### **TASK-017: 성능 최적화 및 검증**

**카테고리**: 성능/품질
**우선순위**: P0 (최우선)
**설명**: Burst 컴파일, Job System 병렬화 확인 및 성능 검증

**구현 내용**:

#### 17.1 Burst 컴파일 검증

**확인 방법**:

1. **Jobs 메뉴 확인**
   - Unity Editor 상단: `Jobs → Burst → Enable Compilation`
   - ✅ 체크 되어 있어야 함

2. **Profiler에서 확인**
   - `Window → Analysis → Profiler`
   - Play 모드 실행
   - CPU Usage → `EnemyChaseSystem` 검색
   - 함수명에 `[Burst]` 표시 확인

3. **콘솔 로그 확인**
   ```
   Burst: Compiled 'EnemyChaseJob' in X ms
   ```

**문제 해결**:
- Burst 컴파일 안 되는 경우: Managed Type 사용 여부 확인
- 에러 발생 시: `[BurstCompile]` 속성 제거 후 원인 파악

#### 17.2 Job System 병렬화 검증

**확인 방법**:

1. **Profiler → Jobs 탭**
   - Play 모드에서 몬스터 50개 이상 스폰
   - Jobs 탭에서 `EnemyChaseJob` 확인
   - **Worker Threads**에서 병렬 실행 확인

2. **성능 측정**
   - 몬스터 100개 동시 추적 시 FPS 확인
   - 목표: **60 FPS 이상 유지**

#### 17.3 쿼리 최적화 검증

**체크리스트**:
- [ ] 플레이어 위치를 한 번만 쿼리함
- [ ] 불필요한 foreach 중첩 없음
- [ ] `WithAll<EnemyTag>()` 필터링 활용
- [ ] `RefRO<T>` (읽기 전용) vs `RefRW<T>` (읽기/쓰기) 올바르게 사용

**예시**:
```csharp
// ✅ 올바른 쿼리
foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())

// ❌ 잘못된 쿼리 (불필요한 쓰기 권한)
foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PlayerTag>())
```

#### 17.4 성능 프로파일링

**Unity Profiler 설정**:
1. `Window → Analysis → Profiler`
2. `CPU Usage` 모드
3. Play 모드 실행 및 기록
4. `EnemyChaseSystem` 검색

**측정 항목**:
- **프레임 시간**: 16.6ms 이하 (60 FPS)
- **EnemyChaseSystem 실행 시간**: 1ms 이하 (몬스터 100개 기준)
- **메모리 할당**: 0 (Job은 스택 메모리 사용)

**성능 목표**:
- 플레이어 1명, 몬스터 100개, 총알 200개 동시 존재 시
- **60 FPS 이상** 유지
- **Burst 적용률**: 100% (EnemyChaseJob)
- **Job 병렬화**: 활성화 확인

**의존성**: TASK-015

**완료 조건**:
- [ ] Burst 컴파일 확인됨
- [ ] Jobs 탭에서 병렬 실행 확인됨
- [ ] 몬스터 100개 동시 추적 시 60 FPS 유지
- [ ] EnemyChaseSystem 실행 시간 1ms 이하
- [ ] 메모리 할당 없음 (GC.Alloc = 0)
- [ ] Profiler 스크린샷 저장 (선택)

**예상 작업량**: 2-3시간

---

### **TASK-018: 통합 테스트 및 검증**

**카테고리**: 테스트
**우선순위**: P0 (최우선)
**설명**: Phase 4 전체 기능 통합 테스트 및 검증

**구현 내용**:

#### 18.1 Play 모드 테스트

1. **Unity Editor에서 Play 버튼 클릭** (Ctrl+P)

2. **예상 동작**:
   - ✅ 플레이어가 WASD로 이동 (Phase 1)
   - ✅ 2초마다 자동 총알 발사 (Phase 2)
   - ✅ 2초마다 빨간 큐브 스폰 (Phase 3)
   - ✅ **스폰된 몬스터가 플레이어를 향해 이동** (Phase 4) ← 새로 추가!

3. **세부 동작 확인**:
   - 몬스터가 플레이어를 직선으로 추적
   - 플레이어가 이동하면 몬스터도 방향 변경
   - 몬스터들이 서로 겹쳐도 계속 추적 (충돌은 Phase 5)
   - 플레이어에게 도달해도 멈추지 않음 (최소 거리 0.1 유닛)

#### 18.2 Entity Debugger 검증

1. **Window → Entities → Hierarchy** 열기
2. Play 모드에서 확인:
   - **EnemyTag** Entity 선택
   - `LocalTransform` 컴포넌트 확인
   - **Position 값이 실시간으로 변화**하는지 확인

3. **시스템 실행 확인**:
   - Hierarchy 창 상단 → **Systems** 탭
   - `EnemyChaseSystem` 검색
   - `Enabled: true` 확인
   - 실행 시간 확인

#### 18.3 다양한 시나리오 테스트

**시나리오 1: 정지 상태**
- 플레이어가 이동하지 않음
- 몬스터들이 플레이어 위치로 수렴함
- **예상**: 모든 몬스터가 플레이어 주변에 모임

**시나리오 2: 이동 중**
- 플레이어가 계속 이동함 (WASD)
- **예상**: 몬스터들이 플레이어를 계속 추적함

**시나리오 3: 빠른 이동**
- 플레이어가 빠르게 이동 (MovementSpeed 높임)
- **예상**: 몬스터가 따라오지만 속도 차이로 거리 벌어짐

**시나리오 4: 다수 몬스터**
- 몬스터 50개 이상 스폰 대기
- **예상**: 모든 몬스터가 독립적으로 플레이어 추적

#### 18.4 Console 확인

**정상 상태**:
- ❌ 에러 없음
- ❌ 경고 없음
- ✅ Burst 컴파일 로그만 표시

**일반적인 에러**:
- `NullReferenceException` → 플레이어 쿼리 실패
- `DivideByZeroException` → 정규화 시 방향 벡터 길이 0

#### 18.5 성능 테스트

**테스트 조건**:
- 플레이어 1명
- 몬스터 100개
- 총알 200개 (발사 중)

**측정 항목**:
- FPS: Window → Stats → FPS 확인
- **목표**: 60 FPS 이상

**Profiler 확인**:
- CPU Usage → `EnemyChaseSystem`
- 실행 시간: 1ms 이하
- GC.Alloc: 0 bytes

**의존성**: TASK-015, TASK-017

**완료 조건**:
- [ ] Play 모드에서 몬스터가 플레이어 추적함
- [ ] 플레이어 이동 시 몬스터도 방향 변경함
- [ ] Entity Debugger에서 Position 변화 확인됨
- [ ] 시스템 실행 확인됨 (Systems 탭)
- [ ] 4가지 시나리오 모두 정상 작동함
- [ ] Console 에러 없음
- [ ] 몬스터 100개 동시 추적 시 60 FPS 유지
- [ ] Profiler에서 성능 확인됨

**예상 작업량**: 2-3시간

---

## Phase 4 전체 검증 체크리스트

### 필수 검증 항목 (spec.md 기준)
- [ ] 스폰된 몬스터가 플레이어를 향해 이동함
- [ ] 플레이어가 이동하면 몬스터도 새로운 위치를 추적함
- [ ] 몬스터 이동 속도가 설정값대로 작동함 (EnemySpeed.Value)
- [ ] 다수의 몬스터(100개 이상)가 동시에 추적해도 성능 저하 없음
- [ ] Entity Debugger에서 몬스터의 Transform 변화 확인 가능
- [ ] 몬스터가 플레이어에게 겹쳐져도 계속 추적 시도함
- [ ] 성능 프로파일러에서 Chase System이 Burst로 최적화되었는지 확인

### SOLID 원칙 검증
- [ ] **단일 책임 (SRP)**: EnemyChaseSystem은 추적 이동만 담당
- [ ] **개방/폐쇄 (OCP)**: IJobEntity로 확장 가능한 구조
- [ ] **리스코프 치환 (LSP)**: ISystem, IJobEntity 인터페이스 준수
- [ ] **인터페이스 분리 (ISP)**: 필요한 컴포넌트만 쿼리 (EnemySpeed, LocalTransform)
- [ ] **의존성 역전 (DIP)**: 컴포넌트 인터페이스에 의존, 구체 클래스 아님

### Phase 1-3 통합 검증
- [ ] 플레이어 이동 (Phase 1) 정상 작동
- [ ] 자동 사격 (Phase 2) 정상 작동
- [ ] 몬스터 스폰 (Phase 3) 정상 작동
- [ ] 몬스터 추적 (Phase 4) 정상 작동
- [ ] 모든 시스템이 충돌 없이 동시 실행됨

---

## 작업 순서 및 일정

### 권장 진행 순서

1. **Day 1 (3-4시간)**: TASK-015 완료
   - EnemyChaseSystem 구현
   - EnemyChaseJob 구현
   - 기본 컴파일 확인

2. **Day 1 (선택, 1-2시간)**: TASK-016 (회전 시스템)
   - **권장: 건너뛰기** (간결함 우선)
   - 구현 시 회전 로직 추가

3. **Day 2 (2-3시간)**: TASK-017 완료
   - Burst 컴파일 검증
   - Job System 병렬화 확인
   - 성능 프로파일링

4. **Day 2 (2-3시간)**: TASK-018 완료
   - Play 모드 테스트
   - Entity Debugger 검증
   - 시나리오 테스트
   - 최종 검증

### 총 예상 소요 시간
- **필수 작업**: 7-10시간 (약 2일)
- **선택 작업** (회전 시스템): 1-2시간
- **검증 및 문제 해결**: 추가 2-3시간
- **전체**: 9-15시간 (회전 시스템 제외 시 9-13시간)

---

## 의존성 다이어그램

```
Phase 3 완료 (TASK-010 ~ TASK-014)
    ↓
TASK-015 (몬스터 추적 시스템)
    ↓
TASK-016 (회전 시스템, 선택) ← 건너뛰기 가능
    ↓
TASK-017 (성능 최적화 및 검증)
    ↓
TASK-018 (통합 테스트 및 검증)
    ↓
Phase 4 완료
```

---

## SOLID 원칙 준수 확인

### Single Responsibility Principle (단일 책임)
✅ **EnemyChaseSystem**: 몬스터 추적 이동만 담당
- 충돌 처리 없음 (Phase 5)
- 데미지 처리 없음 (Phase 5)
- 스폰 처리 없음 (Phase 3)

### Open/Closed Principle (개방/폐쇄)
✅ **IJobEntity 구조**: 확장 가능
- 새로운 AI 로직 추가 시 기존 코드 수정 불필요
- 새로운 컴포넌트 추가 가능 (예: EnemyState)

### Liskov Substitution Principle (리스코프 치환)
✅ **인터페이스 준수**:
- `ISystem` 인터페이스 올바르게 구현
- `IJobEntity` 인터페이스 올바르게 구현

### Interface Segregation Principle (인터페이스 분리)
✅ **최소 의존성**:
- EnemyChaseJob은 `EnemySpeed`, `LocalTransform`만 필요
- 불필요한 컴포넌트 쿼리 없음

### Dependency Inversion Principle (의존성 역전)
✅ **추상화에 의존**:
- 구체적인 Player/Enemy 클래스가 아닌 `PlayerTag`, `EnemyTag` 인터페이스에 의존
- 컴포넌트 기반 쿼리

---

## 문제 해결 가이드

### 문제 1: 몬스터가 이동하지 않음

**증상**:
- 몬스터가 스폰은 되지만 제자리에 멈춰 있음

**확인 사항**:
1. **EnemySpeed 컴포넌트 확인**
   - Entity Debugger → Enemy Entity 선택
   - `EnemySpeed.Value` 확인 (0보다 커야 함)
   - EnemyAuthoring에서 Speed 값 설정 확인

2. **시스템 실행 확인**
   - Entity Debugger → Systems 탭
   - `EnemyChaseSystem` 검색
   - `Enabled: true` 확인

3. **플레이어 쿼리 확인**
   - PlayerTag 컴포넌트가 플레이어에게 있는지 확인
   - Entity Debugger에서 Player Entity 확인

**해결 방법**:
```csharp
// EnemyAuthoring.cs에서 Speed 값 확인
public float Speed = 3f; // 0보다 큰 값

// 또는 Inspector에서 값 설정
// Enemy Prefab → EnemyAuthoring → Speed: 3
```

---

### 문제 2: 몬스터가 플레이어를 추적하지 않고 다른 방향으로 이동

**원인**: 플레이어 위치 쿼리 실패 또는 방향 계산 오류

**확인**:
```csharp
// OnUpdate에서 플레이어 위치 출력
UnityEngine.Debug.Log($"Player Position: {playerPosition}");

// Execute에서 방향 벡터 출력
UnityEngine.Debug.Log($"Direction: {direction}");
```

**해결**:
- PlayerTag 쿼리가 정상 작동하는지 확인
- 방향 벡터 계산 로직 확인 (PlayerPosition - EnemyPosition)

---

### 문제 3: 성능 저하 (FPS 낮음)

**원인**: Burst 컴파일 미적용 또는 중복 쿼리

**확인 방법**:
1. **Burst 컴파일 확인**
   - Jobs → Burst → Enable Compilation 체크
   - Profiler에서 [Burst] 표시 확인

2. **쿼리 최적화 확인**
   - 플레이어 위치를 한 번만 쿼리하는지 확인
   - Job 내부에서 쿼리하지 않는지 확인

**해결**:
```csharp
// ✅ 올바른 패턴
public void OnUpdate(ref SystemState state)
{
    float3 playerPosition = ...; // 한 번만 쿼리
    new EnemyChaseJob { PlayerPosition = playerPosition }.ScheduleParallel();
}

// ❌ 잘못된 패턴
void Execute(...) // Job 내부
{
    foreach (var player in ...) // 중복 쿼리!
}
```

---

### 문제 4: 몬스터가 플레이어에 도달하면 떨림 현상

**원인**: 최소 거리 체크 미구현 또는 임계값 너무 작음

**해결**:
```csharp
// 최소 거리 체크 (0.1 유닛)
if (distanceSq > 0.01f) // 0.1 * 0.1
{
    // 이동 로직
}

// 임계값 조정 (0.5 유닛으로 증가)
if (distanceSq > 0.25f) // 0.5 * 0.5
```

---

### 문제 5: Burst 컴파일 에러

**일반적인 에러**:
```
error: Managed type 'string' is not allowed
```

**원인**: Job 내부에서 Managed Type 사용

**해결**:
- `string`, `class` 등 Managed Type 제거
- Debug.Log도 Job 내부에서 사용 불가
- `Unity.Mathematics` 타입만 사용 (float3, quaternion 등)

---

## 다음 단계 (Phase 5 준비)

Phase 4 완료 후:

### Phase 5 예고: 충돌 감지 및 데미지 시스템

**주요 기능**:
1. **Unity Physics 설정**
   - 플레이어, 몬스터, 총알에 `PhysicsCollider` 추가
   - Collision Filter 설정

2. **총알-몬스터 충돌**
   - 충돌 시 몬스터 체력 감소
   - 체력 0 시 몬스터 삭제
   - 총알 삭제

3. **몬스터-플레이어 충돌**
   - 충돌 시 플레이어 체력 감소
   - 체력 0 시 게임 오버

### 준비 사항
- Unity Physics 패키지 설치 확인
- `PhysicsCollider` 컴포넌트 학습
- ITriggerEventsJob 이해

---

## 참고 사항

### 코드 스타일
- C# Naming Convention 준수
- 명확한 변수명 사용 (direction, normalizedDirection, movement)
- 간결한 주석 (필요시에만)

### Unity 설정
- Auto Save 활성화 권장
- Scene 변경 시 저장 확인
- Burst Compilation 활성화 확인

### 성능 고려사항
- 플레이어 위치는 한 번만 쿼리
- 제곱 거리 사용 (sqrt 연산 절약)
- Burst 컴파일 적용
- IJobEntity로 병렬 처리

### Git 커밋 메시지
```
Phase 4 완료: 몬스터 AI - 플레이어 추적 시스템

- EnemyChaseSystem 구현
- IJobEntity 병렬 처리
- Burst 컴파일 최적화
- 몬스터 100개 동시 추적 성능 검증 (60 FPS 유지)

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## 성능 목표 (Phase 4)

| 항목 | 목표 | 측정 방법 |
|------|------|-----------|
| FPS | 60 이상 | Stats 윈도우 |
| EnemyChaseSystem 실행 시간 | 1ms 이하 | Profiler (몬스터 100개) |
| GC.Alloc | 0 bytes | Profiler → Memory |
| Burst 적용률 | 100% | EnemyChaseJob |
| Job 병렬화 | 활성화 | Profiler → Jobs |

---

**Phase 4 시작 준비 완료!**

Phase 3가 완료되었는지 확인한 후, TASK-015부터 순차적으로 진행하세요.

**핵심 체크포인트**:
1. ✅ Phase 3 완료 확인 (몬스터 스폰 작동)
2. ✅ EnemySpeed 컴포넌트 존재 확인
3. ✅ PlayerTag 컴포넌트 존재 확인
4. ▶️ TASK-015 시작: EnemyChaseSystem 구현

---

**문서 버전**: 1.0
**작성일**: 2025-11-30
**대상 Phase**: Phase 4 - 몬스터 AI
