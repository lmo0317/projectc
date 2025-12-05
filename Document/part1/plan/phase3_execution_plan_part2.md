# Phase 3 실행 계획 (Part 2): 스폰 시스템 및 통합 테스트

## TASK-013: 몬스터 스폰 시스템 구현

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 타이머 기반으로 플레이어 주변 랜덤 위치에 몬스터를 스폰하는 시스템 구현

### 13.1 스폰 시스템 구현

`Assets/Scripts/Systems/EnemySpawnSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AutoShootSystem))]
[BurstCompile]
public partial struct EnemySpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawnConfig>();
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // EntityCommandBuffer 생성
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 플레이어 위치 가져오기
        float3 playerPosition = float3.zero;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPosition = transform.ValueRO.Position;
            break;
        }

        // 스폰 매니저 처리
        foreach (var spawnConfig in SystemAPI.Query<RefRW<EnemySpawnConfig>>())
        {
            // ValueRW를 통해 직접 수정 (로컬 변수 복사 금지!)
            spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;

            // 스폰 간격 체크
            if (spawnConfig.ValueRW.TimeSinceLastSpawn >= spawnConfig.ValueRW.SpawnInterval)
            {
                spawnConfig.ValueRW.TimeSinceLastSpawn = 0f;

                // 최대 몬스터 수 체크
                if (spawnConfig.ValueRW.MaxEnemies > 0)
                {
                    int enemyCount = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount();
                    if (enemyCount >= spawnConfig.ValueRW.MaxEnemies)
                    {
                        continue;
                    }
                }

                // 랜덤 위치 계산 (원형 분포)
                float angle = spawnConfig.ValueRW.RandomGenerator.NextFloat(0f, math.PI * 2f);
                float distance = spawnConfig.ValueRW.RandomGenerator.NextFloat(
                    spawnConfig.ValueRW.SpawnRadius * 0.8f,
                    spawnConfig.ValueRW.SpawnRadius
                );

                // 원형 좌표 → 직교 좌표 변환
                float3 offset = new float3(
                    math.cos(angle) * distance,
                    0f,
                    math.sin(angle) * distance
                );

                float3 spawnPosition = playerPosition + offset;

                // 몬스터 Entity 생성
                var enemyEntity = ecb.Instantiate(spawnConfig.ValueRW.EnemyPrefab);
                ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(spawnPosition));
            }
        }
    }
}
```

### 13.2 핵심 구현 포인트

1. **타이머 관리**: `TimeSinceLastSpawn`에 deltaTime 누적
2. **플레이어 위치 쿼리**: `SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>()`
3. **최대 개체 수 제한**: `QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount()`
4. **랜덤 원형 분포**:
   - 각도: 0 ~ 2π 랜덤
   - 거리: SpawnRadius의 80% ~ 100%
5. **RefRW 직접 접근**: `spawnConfig.ValueRW.필드명` 형태로 직접 수정
6. **EntityCommandBuffer**: 안전한 Entity 생성

**완료 조건**:
- [ ] EnemySpawnSystem.cs 작성 완료
- [ ] 타이머 로직 구현됨
- [ ] 랜덤 원형 스폰 구현됨
- [ ] 최대 개수 제한 구현됨
- [ ] 컴파일 에러 없음

**예상 작업량**: 2-3시간

---

## TASK-014: 통합 테스트 및 검증

**카테고리**: 테스트
**우선순위**: P0 (최우선)
**설명**: Phase 3 전체 기능 통합 테스트

### 14.1 Play 모드 테스트

1. Unity Editor에서 Play 버튼 클릭 (Ctrl+P)
2. **예상 동작**:
   - 플레이어가 WASD로 이동 (Phase 1)
   - 2초마다 자동 총알 발사 (Phase 2)
   - **2초마다 플레이어 주변에 빨간 큐브 스폰** (Phase 3)
   - 몬스터는 플레이어로부터 8-10 유닛 거리에 원형 스폰
   - 최대 50개까지만 스폰됨

### 14.2 검증 항목

- [ ] 몬스터가 2초 간격으로 스폰되는가?
- [ ] 몬스터가 플레이어 주변 원형으로 스폰되는가?
- [ ] 스폰 위치가 매번 랜덤하게 변경되는가?
- [ ] 빨간색 큐브로 렌더링되는가?
- [ ] 최대 50개 제한이 작동하는가?

### 14.3 Entity Debugger 검증

1. Window → Entities → Hierarchy 열기
2. Play 모드에서 확인:
   - **Enemy Entities**: EnemyTag, EnemyHealth, EnemySpeed 보유
   - **EnemySpawnManager Entity**: EnemySpawnConfig 보유
3. Enemy Entity 선택 후:
   - LocalTransform에 위치 값 확인
   - EnemyHealth.Value = 100
   - EnemySpeed.Value = 3

### 14.4 Console 확인

- [ ] Play 모드 실행 중 에러 없음
- [ ] 경고 메시지 없음

**완료 조건**:
- [ ] Play 모드에서 몬스터 정상 스폰
- [ ] 스폰 간격 준수
- [ ] 랜덤 원형 위치 스폰
- [ ] 빨간색 렌더링
- [ ] 최대 50개 제한 작동
- [ ] Console 에러 없음

**예상 작업량**: 1-2시간

---

## Phase 3 전체 검증 체크리스트

### 컴포넌트 검증
- [ ] EnemyTag.cs 컴파일 성공
- [ ] EnemyHealth.cs 컴파일 성공
- [ ] EnemySpeed.cs 컴파일 성공
- [ ] EnemySpawnConfig.cs 컴파일 성공

### Authoring 검증
- [ ] EnemyAuthoring.cs Baker 정상 동작
- [ ] EnemySpawnAuthoring.cs Baker 정상 동작
- [ ] TransformUsageFlags 올바르게 설정됨

### Prefab 검증
- [ ] Enemy.prefab 생성됨
- [ ] EnemyMaterial 빨간색 설정됨
- [ ] EnemyAuthoring 컴포넌트 추가됨

### 시스템 검증
- [ ] EnemySpawnSystem.cs 컴파일 성공
- [ ] 타이머 스폰 작동
- [ ] 플레이어 위치 쿼리 성공
- [ ] 랜덤 원형 위치 계산 정확
- [ ] 최대 개수 제한 작동
- [ ] RefRW 직접 접근 패턴 사용

### 통합 테스트
- [ ] Play 모드 실행 성공
- [ ] 몬스터 스폰 확인
- [ ] 스폰 간격 정확
- [ ] 렌더링 정상
- [ ] Entity Debugger 확인
- [ ] Console 에러 없음

---

## 작업 순서

1. **Day 1**: TASK-010 (컴포넌트)
2. **Day 2**: TASK-011 (Prefab)
3. **Day 3**: TASK-012 (스폰 매니저)
4. **Day 4-5**: TASK-013 (스폰 시스템)
5. **Day 6**: TASK-014 (통합 테스트)

---

## 의존성 다이어그램

```
TASK-010 (컴포넌트)
    ↓
TASK-011 (Prefab)
    ↓
TASK-012 (스폰 매니저)
    ↓
TASK-013 (스폰 시스템)
    ↓
TASK-014 (통합 테스트)
    ↓
Phase 3 완료
```

---

## 문제 해결 가이드

### 문제 1: 몬스터가 스폰되지 않음

**확인 사항**:
- Console 에러 확인
- EnemySpawnManager GameObject 존재 확인
- Enemy Prefab 할당 확인
- Enemy.prefab에 EnemyAuthoring 컴포넌트 존재 확인

**해결 방법**:
- Enemy Prefab을 EnemySpawnManager Inspector에 할당
- Play 모드 재시작

### 문제 2: 몬스터가 렌더링되지 않음

**원인**: `TransformUsageFlags.Renderable` 누락

**확인**:
```csharp
// ❌ 잘못됨
var entity = GetEntity(TransformUsageFlags.Dynamic);

// ✅ 올바름
var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
```

**해결**: EnemyAuthoring.cs 수정 후 Unity Editor 재시작

### 문제 3: 스폰 타이머가 작동하지 않음

**원인**: `RefRW<T>` 로컬 변수 복사

**확인**:
```csharp
// ❌ 잘못됨
var config = spawnConfig.ValueRW;
config.TimeSinceLastSpawn += deltaTime;

// ✅ 올바름
spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;
```

**해결**: EnemySpawnSystem.cs 수정

### 문제 4: 최대 개수 제한 미작동

**확인**:
- EnemySpawnConfig.MaxEnemies 값 확인
- 개수 체크 로직 확인

**해결**: Inspector에서 MaxEnemies 재설정

### 문제 5: 스폰 위치가 항상 같음

**원인**: Random 시드 문제

**해결**: Unity Editor 재시작

---

## SOLID 원칙 준수

### Single Responsibility
✅ 각 컴포넌트/시스템이 단일 책임만 가짐

### Open/Closed
✅ 컴포넌트 구조로 확장 가능

### Liskov Substitution
✅ IComponentData 인터페이스 준수

### Interface Segregation
✅ 필요한 데이터만 포함

### Dependency Inversion
✅ 컴포넌트 인터페이스에 의존

---

## 다음 단계 (Phase 4)

Phase 3 완료 후:

### Phase 4: 몬스터 AI
- 몬스터가 플레이어를 향해 이동
- EnemySpeed 컴포넌트 활용
- 간단한 직선 추적 AI

### Phase 5: 충돌 및 데미지
- 총알-몬스터 충돌 감지
- EnemyHealth 감소
- 체력 0 시 몬스터 제거

---

**문서 버전**: 1.0
**작성일**: 2025-11-28
