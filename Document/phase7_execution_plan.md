# Phase 7 실행 계획: 게임 밸런싱 및 웨이브 시스템

## 프로젝트 개요

**Phase**: Phase 7 - 게임 밸런싱 및 웨이브 시스템
**목표**: 게임 난이도를 조정하고 웨이브 시스템을 구현하여 게임 완성도를 높이며, 코드 최적화 및 최종 검증 수행
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수
- 성능 최적화 중시

## Phase 7 개발 사항 요약

이 단계에서는 게임의 최종 완성도를 높이기 위한 밸런싱, 웨이브 시스템, 코드 최적화를 수행합니다.

### 구현 범위
1. **난이도 밸런싱** (필수)
   - 플레이어/몬스터/총알 수치 조정
   - 플레이 테스트 기반 튜닝

2. **웨이브 시스템** (권장)
   - 시간 기반 웨이브 증가
   - 웨이브별 난이도 상승
   - UI 연동

3. **코드 리팩토링 및 최적화** (필수)
   - 중복 코드 제거
   - 시스템 실행 순서 최적화
   - Burst/Job 최적화 검증

4. **최종 통합 테스트** (필수)
   - 장시간 플레이 테스트
   - 성능 프로파일링
   - 빌드 검증

---

## 태스크 분해

### **TASK-029: 난이도 밸런싱**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 현재 게임 플레이를 분석하고 적절한 난이도로 밸런싱 수행

**구현 내용**:

#### 29.1 현재 게임 플레이 분석

**테스트 플레이 수행**:
1. Unity Editor에서 Play 모드로 3분 이상 플레이
2. 다음 지표 기록:
   - 생존 시간 (플레이어 사망까지)
   - 처치한 몬스터 수
   - 플레이어가 받은 피해 횟수
   - 체감 난이도 (너무 쉬운지/어려운지)

**현재 수치 확인**:
```
플레이어:
- MaxHealth: 100
- MoveSpeed: 5
- 충돌 시 받는 데미지: 10

몬스터:
- Health: 100
- MoveSpeed: 3
- SpawnInterval: 2초
- SpawnRadius: 15
- MaxEnemies: 50

총알:
- Damage: 25
- Speed: 10
- Lifetime: 5초
- FireRate: 0.5초 (초당 2발)
```

#### 29.2 밸런싱 파라미터 정의

**목표 난이도 곡선**:
- 0-30초: 쉬움 (튜토리얼 느낌)
- 30-60초: 보통 (적응 단계)
- 60-120초: 어려움 (도전 단계)
- 120초+: 매우 어려움 (생존 도전)

**조정 대상 파라미터**:

| 대상 | 파라미터 | 현재 값 | 권장 범위 | 비고 |
|------|---------|---------|-----------|------|
| 플레이어 | MaxHealth | 100 | 100-150 | 초반 생존성 |
| 플레이어 | MoveSpeed | 5 | 4-6 | 회피 능력 |
| 플레이어 | 충돌 데미지 | 10 | 5-15 | 한 번 실수의 대가 |
| 몬스터 | Health | 100 | 75-100 | 처치 난이도 |
| 몬스터 | MoveSpeed | 3 | 2.5-4 | 추격 압박감 |
| 몬스터 | SpawnInterval | 2초 | 1.5-3초 | 몬스터 밀도 |
| 총알 | Damage | 25 | 25-35 | 처치 속도 |
| 총알 | FireRate | 0.5초 | 0.3-0.6초 | DPS |

#### 29.3 밸런싱 수행

**PlayerAuthoring 수정**:
```csharp
public class PlayerAuthoring : MonoBehaviour
{
    [Header("Balance Settings")]
    public float MaxHealth = 120f;      // 100 → 120 (생존성 향상)
    public float MoveSpeed = 5.5f;      // 5 → 5.5 (회피 능력 향상)
}
```

**PlayerDamageSystem 수정**:
```csharp
// 충돌 시 데미지
const float COLLISION_DAMAGE = 8f;  // 10 → 8 (한 번 실수 허용)
playerHealth.ValueRW.CurrentHealth -= COLLISION_DAMAGE;
```

**EnemyAuthoring 수정**:
```csharp
public class EnemyAuthoring : MonoBehaviour
{
    [Header("Balance Settings")]
    public float Health = 80f;          // 100 → 80 (처치 쉽게)
    public float MoveSpeed = 3.2f;      // 3 → 3.2 (약간 빠르게)
}
```

**EnemySpawnAuthoring 수정**:
```csharp
public class EnemySpawnAuthoring : MonoBehaviour
{
    [Header("Balance Settings")]
    public float SpawnInterval = 2.2f;  // 2 → 2.2 (초반 여유)
    public float SpawnRadius = 15f;
    public int MaxEnemies = 40;         // 50 → 40 (밀도 감소)
}
```

**BulletAuthoring 수정**:
```csharp
public class BulletAuthoring : MonoBehaviour
{
    [Header("Balance Settings")]
    public float Damage = 30f;          // 25 → 30 (총 4발로 처치)
}
```

**AutoShootConfig (PlayerAuthoring) 수정**:
```csharp
public float FireRate = 0.4f;           // 0.5 → 0.4 (DPS 향상)
```

#### 29.4 반복 테스트 및 튜닝

**테스트 절차**:
1. 수정된 값으로 플레이 테스트 (3회 이상)
2. 생존 시간 평균 기록
3. 너무 쉬우면 → 몬스터 스폰 빠르게, 이동 속도 증가
4. 너무 어려우면 → 플레이어 체력 증가, 총알 데미지 증가
5. 목표: 평균 생존 시간 60-90초

**설계 원칙**:
- 단일 책임: 각 Authoring은 자신의 밸런스만 담당
- 개방/폐쇄: 수치 조정만으로 난이도 변경 가능
- 간결함: 복잡한 공식 없이 직관적인 수치

**의존성**: Phase 1-6 완료

**완료 조건**:
- [ ] 현재 게임 플레이 분석 완료
- [ ] 밸런싱 파라미터 시트 작성
- [ ] 모든 Authoring 클래스 수치 조정
- [ ] 3회 이상 플레이 테스트 수행
- [ ] 평균 생존 시간 60-90초 달성
- [ ] 초반(0-30초)은 쉽고, 후반(60초+)은 어려움 확인
- [ ] 컴파일 에러 없음

**예상 작업량**: 4-6시간

---

### **TASK-030: 웨이브 시스템 구현**

**카테고리**: 게임플레이
**우선순위**: P1 (권장)
**설명**: 시간 기반 웨이브 시스템을 구현하여 게임 진행에 따라 난이도가 점진적으로 상승하도록 함

**구현 내용**:

#### 30.1 WaveConfig 컴포넌트 정의

`Assets/Scripts/Components/WaveConfig.cs` 작성:

```csharp
using Unity.Entities;

/// <summary>
/// 웨이브 시스템 설정 (싱글톤)
/// </summary>
public struct WaveConfig : IComponentData
{
    public int CurrentWave;           // 현재 웨이브 번호 (1부터 시작)
    public float TimeInCurrentWave;   // 현재 웨이브 경과 시간
    public float WaveDuration;        // 각 웨이브 지속 시간 (초)

    // 웨이브별 난이도 증가 배율
    public float SpawnIntervalMultiplier;  // 스폰 간격 감소율 (0.9 = 10% 빠르게)
    public float EnemySpeedMultiplier;     // 적 속도 증가율 (1.1 = 10% 빠르게)
}
```

#### 30.2 WaveAuthoring 구현

`Assets/Scripts/Authoring/WaveAuthoring.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 웨이브 설정 싱글톤 Entity 생성
/// </summary>
public class WaveAuthoring : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("각 웨이브 지속 시간 (초)")]
    public float WaveDuration = 30f;

    [Tooltip("웨이브당 스폰 간격 감소율 (0.9 = 10% 빠르게)")]
    [Range(0.8f, 1.0f)]
    public float SpawnIntervalMultiplier = 0.92f;

    [Tooltip("웨이브당 적 속도 증가율 (1.1 = 10% 빠르게)")]
    [Range(1.0f, 1.2f)]
    public float EnemySpeedMultiplier = 1.08f;

    class Baker : Baker<WaveAuthoring>
    {
        public override void Bake(WaveAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new WaveConfig
            {
                CurrentWave = 1,
                TimeInCurrentWave = 0f,
                WaveDuration = authoring.WaveDuration,
                SpawnIntervalMultiplier = authoring.SpawnIntervalMultiplier,
                EnemySpeedMultiplier = authoring.EnemySpeedMultiplier
            });
        }
    }
}
```

#### 30.3 WaveSystem 구현

`Assets/Scripts/Systems/WaveSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 웨이브 진행 및 난이도 조정 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct WaveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaveConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 게임 오버 시 중지
        if (SystemAPI.HasSingleton<GameOverTag>())
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var waveConfig in SystemAPI.Query<RefRW<WaveConfig>>())
        {
            // 시간 증가
            waveConfig.ValueRW.TimeInCurrentWave += deltaTime;

            // 웨이브 종료 체크
            if (waveConfig.ValueRW.TimeInCurrentWave >= waveConfig.ValueRW.WaveDuration)
            {
                // 다음 웨이브로 전환
                waveConfig.ValueRW.CurrentWave++;
                waveConfig.ValueRW.TimeInCurrentWave = 0f;

                Debug.Log($"Wave {waveConfig.ValueRO.CurrentWave} started!");
            }

            break; // 싱글톤
        }
    }
}
```

#### 30.4 EnemySpawnSystem에 웨이브 난이도 적용

`Assets/Scripts/Systems/EnemySpawnSystem.cs` 수정:

```csharp
public void OnUpdate(ref SystemState state)
{
    // 게임 오버 시 스폰 중지
    if (SystemAPI.HasSingleton<GameOverTag>())
        return;

    float deltaTime = SystemAPI.Time.DeltaTime;

    // 웨이브 배율 가져오기
    float spawnIntervalMultiplier = 1f;
    if (SystemAPI.HasSingleton<WaveConfig>())
    {
        var waveConfig = SystemAPI.GetSingleton<WaveConfig>();
        int wavePower = waveConfig.CurrentWave - 1; // Wave 1 = 배율 없음

        // 웨이브마다 스폰 간격 감소 (더 빠르게 스폰)
        for (int i = 0; i < wavePower; i++)
        {
            spawnIntervalMultiplier *= waveConfig.SpawnIntervalMultiplier;
        }
    }

    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    float3 playerPosition = float3.zero;
    foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
    {
        playerPosition = transform.ValueRO.Position;
        break;
    }

    foreach (var spawnConfig in SystemAPI.Query<RefRW<EnemySpawnConfig>>())
    {
        spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;

        // 웨이브 배율 적용된 스폰 간격
        float adjustedInterval = spawnConfig.ValueRW.SpawnInterval * spawnIntervalMultiplier;

        if (spawnConfig.ValueRW.TimeSinceLastSpawn >= adjustedInterval)
        {
            spawnConfig.ValueRW.TimeSinceLastSpawn = 0f;

            if (spawnConfig.ValueRW.MaxEnemies > 0)
            {
                int enemyCount = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount();
                if (enemyCount >= spawnConfig.ValueRW.MaxEnemies)
                {
                    continue;
                }
            }

            float angle = spawnConfig.ValueRW.RandomGenerator.NextFloat(0f, math.PI * 2f);
            float distance = spawnConfig.ValueRW.RandomGenerator.NextFloat(
                spawnConfig.ValueRW.SpawnRadius * 0.8f,
                spawnConfig.ValueRW.SpawnRadius
            );

            float3 offset = new float3(
                math.cos(angle) * distance,
                0f,
                math.sin(angle) * distance
            );

            float3 spawnPosition = playerPosition + offset;

            var enemyEntity = ecb.Instantiate(spawnConfig.ValueRW.EnemyPrefab);
            ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(spawnPosition));
        }
    }
}
```

#### 30.5 EnemyChaseSystem에 웨이브 속도 적용

`Assets/Scripts/Systems/EnemyChaseSystem.cs` 수정:

```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    float deltaTime = SystemAPI.Time.DeltaTime;

    // 웨이브 배율 가져오기
    float enemySpeedMultiplier = 1f;
    if (SystemAPI.HasSingleton<WaveConfig>())
    {
        var waveConfig = SystemAPI.GetSingleton<WaveConfig>();
        int wavePower = waveConfig.CurrentWave - 1;

        // 웨이브마다 적 속도 증가
        for (int i = 0; i < wavePower; i++)
        {
            enemySpeedMultiplier *= waveConfig.EnemySpeedMultiplier;
        }
    }

    // 플레이어 위치 가져오기
    float3 playerPosition = float3.zero;
    bool playerExists = false;

    foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
    {
        playerPosition = playerTransform.ValueRO.Position;
        playerExists = true;
        break;
    }

    if (!playerExists) return;

    // 적 이동
    foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemySpeed>>())
    {
        float3 direction = math.normalize(playerPosition - transform.ValueRO.Position);

        // 웨이브 배율 적용된 속도
        float adjustedSpeed = speed.ValueRO.Value * enemySpeedMultiplier;

        float3 newPosition = transform.ValueRO.Position + direction * adjustedSpeed * deltaTime;
        newPosition.y = 0f;

        transform.ValueRW.Position = newPosition;
    }
}
```

#### 30.6 UI에 웨이브 표시

**UIManager.cs 수정**:

```csharp
[Header("HUD Text References")]
public TextMeshProUGUI HealthText;
public TextMeshProUGUI SurvivalTimeText;
public TextMeshProUGUI KillCountText;
public TextMeshProUGUI WaveText;  // 추가!

public void UpdateWave(int wave)
{
    if (WaveText != null)
    {
        WaveText.text = $"Wave: {wave}";
    }
}
```

**UIUpdateSystem.cs 수정**:

```csharp
protected override void OnUpdate()
{
    if (uiManager == null) return;

    // 1. 플레이어 체력 업데이트
    foreach (var health in SystemAPI.Query<RefRO<PlayerHealth>>().WithAll<PlayerTag>())
    {
        uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
        break;
    }

    // 2. 게임 통계 업데이트
    foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
    {
        uiManager.UpdateSurvivalTime(stats.ValueRO.SurvivalTime);
        uiManager.UpdateKillCount(stats.ValueRO.KillCount);
        break;
    }

    // 3. 웨이브 정보 업데이트 (추가!)
    if (SystemAPI.HasSingleton<WaveConfig>())
    {
        var waveConfig = SystemAPI.GetSingleton<WaveConfig>();
        uiManager.UpdateWave(waveConfig.CurrentWave);
    }
}
```

**Unity Editor 작업**:
1. HUDPanel에 TextMeshPro 추가
2. 이름: "WaveText"
3. Text: "Wave: 1"
4. Font Size: 32
5. Color: Cyan
6. Position: KillCountText 아래
7. UIManager의 WaveText 참조 연결

#### 30.7 씬에 WaveManager 추가

1. Hierarchy에서 빈 GameObject 생성
2. 이름: "WaveManager"
3. Add Component → WaveAuthoring
4. Inspector 설정:
   - Wave Duration: 30
   - Spawn Interval Multiplier: 0.92
   - Enemy Speed Multiplier: 1.08

**설계 원칙**:
- 단일 책임: WaveSystem은 웨이브 진행만 담당
- 개방/폐쇄: 배율 조정으로 난이도 곡선 변경 가능
- 의존성 역전: 시스템들은 WaveConfig 싱글톤에 의존

**의존성**: TASK-029

**완료 조건**:
- [ ] WaveConfig 컴포넌트 작성 완료
- [ ] WaveAuthoring 작성 완료
- [ ] WaveSystem 작성 완료
- [ ] EnemySpawnSystem에 웨이브 배율 적용
- [ ] EnemyChaseSystem에 웨이브 배율 적용
- [ ] UIManager에 웨이브 표시 추가
- [ ] UIUpdateSystem에 웨이브 업데이트 추가
- [ ] 씬에 WaveManager GameObject 추가
- [ ] Play 모드에서 30초마다 웨이브 증가 확인
- [ ] 웨이브가 올라갈수록 적 스폰 빠르고 이동 속도 증가 확인
- [ ] UI에 현재 웨이브 표시 확인
- [ ] 컴파일 에러 없음

**예상 작업량**: 6-8시간

---

### **TASK-031: 코드 리팩토링 및 최적화**

**카테고리**: 코드 품질
**우선순위**: P0 (최우선)
**설명**: 중복 코드 제거, 시스템 실행 순서 최적화, Burst/Job 최적화 검증

**구현 내용**:

#### 31.1 중복 코드 제거

**ECB 패턴 통일**:
모든 시스템에서 동일한 ECB 생성 패턴 사용 확인:

```csharp
// ✅ 표준 패턴
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                  .CreateCommandBuffer(state.WorldUnmanaged);
```

**게임 오버 체크 통일**:
모든 게임플레이 시스템에서 동일한 패턴 사용:

```csharp
// ✅ 표준 패턴
if (SystemAPI.HasSingleton<GameOverTag>())
    return;
```

**적용 대상 시스템**:
- EnemySpawnSystem
- AutoShootSystem
- WaveSystem (웨이브 구현 시)
- GameStatsSystem

#### 31.2 시스템 실행 순서 최적화

**UpdateInGroup 및 UpdateAfter 정리**:

```csharp
// InitializationSystemGroup (입력 처리)
PlayerInputSystem                // 입력 수집

// SimulationSystemGroup (게임 로직)
[UpdateAfter(typeof(PlayerInputSystem))]
PlayerMovementSystem              // 플레이어 이동

[UpdateAfter(typeof(PlayerMovementSystem))]
AutoShootSystem                   // 총알 발사

EnemyChaseSystem                  // 적 추격
BulletMovementSystem              // 총알 이동
BulletLifetimeSystem              // 총알 수명

// PhysicsSystemGroup (물리 처리)
Unity Physics                     // 충돌 감지

// SimulationSystemGroup (충돌 처리)
[UpdateAfter(typeof(PhysicsSystemGroup))]
BulletHitSystem                   // 총알-적 충돌

[UpdateAfter(typeof(PhysicsSystemGroup))]
PlayerDamageSystem                // 플레이어-적 충돌

[UpdateAfter(typeof(PlayerDamageSystem))]
GameOverSystem                    // 게임 오버 체크

EnemySpawnSystem                  // 적 스폰
WaveSystem                        // 웨이브 진행
GameStatsSystem                   // 통계 업데이트

// PresentationSystemGroup (UI 업데이트)
UIUpdateSystem                    // UI 표시
```

**검증 방법**:
1. Window → Analysis → Systems 열기
2. 시스템 실행 순서 확인
3. 의존성 위배 없는지 체크

#### 31.3 Burst 컴파일 검증

**Burst 적용 시스템 확인**:

모든 ISystem에 `[BurstCompile]` 속성 확인:
- ✅ PlayerMovementSystem
- ✅ AutoShootSystem
- ✅ EnemySpawnSystem
- ✅ EnemyChaseSystem
- ✅ BulletMovementSystem
- ✅ BulletLifetimeSystem
- ✅ BulletHitSystem
- ✅ PlayerDamageSystem
- ✅ GameStatsSystem
- ✅ WaveSystem

**Burst 미적용 시스템 (정상)**:
- ❌ UIUpdateSystem (MonoBehaviour 접근 필요)
- ❌ GameOverSystem (MonoBehaviour 접근 필요)
- ❌ PlayerInputSystem (Input System 접근 필요)

**Burst Inspector 확인**:
1. Jobs → Burst → Open Inspector
2. 각 시스템 컴파일 성공 여부 확인
3. 에러 있으면 수정

#### 31.4 성능 프로파일링

**Profiler 체크리스트**:

1. **Window → Analysis → Profiler** 열기
2. Play 모드에서 60초 이상 플레이
3. CPU 사용량 확인:
   - PlayerMovementSystem < 0.1ms
   - EnemyChaseSystem < 0.2ms
   - BulletMovementSystem < 0.2ms
   - UIUpdateSystem < 0.1ms
4. 전체 프레임 시간 < 16ms (60 FPS)
5. GC Alloc 확인 (매 프레임 0 바이트 목표)

**최적화 대상 발견 시**:
- Query 중복 호출 제거
- 불필요한 SystemAPI.GetSingleton 호출 최소화
- float3 계산 간소화

#### 31.5 코드 주석 정리

**주석 가이드라인**:

```csharp
// ✅ 좋은 주석: 왜(Why)를 설명
// 게임 오버 시 스폰 중지 (플레이어 경험 개선)
if (SystemAPI.HasSingleton<GameOverTag>())
    return;

// ❌ 나쁜 주석: 무엇(What)을 중복 설명
// GameOverTag가 있는지 체크
if (SystemAPI.HasSingleton<GameOverTag>())
    return;

// ✅ 복잡한 로직에만 주석
// 원형 분포로 스폰 (플레이어 주변 균등 배치)
float angle = random.NextFloat(0f, math.PI * 2f);
```

**각 파일 상단 요약 주석**:
```csharp
/// <summary>
/// 플레이어 이동 처리 시스템
/// - DPAD 입력을 기반으로 플레이어 Entity 이동
/// - Burst 컴파일 적용으로 고성능 처리
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    // ...
}
```

#### 31.6 SOLID 원칙 검증

**Single Responsibility (단일 책임)**:
- ✅ PlayerMovementSystem: 이동만 담당
- ✅ AutoShootSystem: 발사만 담당
- ✅ GameStatsSystem: 통계 업데이트만 담당

**Open/Closed (개방/폐쇄)**:
- ✅ 새 컴포넌트 추가로 기능 확장 가능
- ✅ 기존 시스템 수정 없이 새 시스템 추가 가능

**Liskov Substitution (리스코프 치환)**:
- ✅ ISystem 인터페이스 올바르게 구현
- ✅ IComponentData 구조체 올바르게 정의

**Interface Segregation (인터페이스 분리)**:
- ✅ 각 시스템은 필요한 컴포넌트만 쿼리
- ✅ 불필요한 의존성 없음

**Dependency Inversion (의존성 역전)**:
- ✅ 시스템은 구체적 구현이 아닌 컴포넌트에 의존
- ✅ UIManager는 인터페이스처럼 사용

**설계 원칙**:
- 간결함: 불필요한 추상화 배제
- 성능: Burst/Job 최대 활용
- 유지보수성: 명확한 책임 분리

**의존성**: TASK-029, TASK-030

**완료 조건**:
- [ ] 모든 시스템에서 ECB 패턴 통일
- [ ] 게임 오버 체크 패턴 통일
- [ ] 시스템 실행 순서 최적화 완료
- [ ] Burst 컴파일 적용 검증 (Inspector 확인)
- [ ] Profiler에서 성능 목표 달성 (60 FPS)
- [ ] GC Alloc 최소화 (매 프레임 0 바이트)
- [ ] 코드 주석 정리 완료
- [ ] SOLID 원칙 검증 완료
- [ ] 컴파일 에러 없음

**예상 작업량**: 6-8시간

---

### **TASK-032: 최종 통합 테스트 및 검증**

**카테고리**: 품질 보증
**우선순위**: P0 (최우선)
**설명**: 장시간 플레이 테스트, 성능 검증, 빌드 테스트를 통한 최종 검증

**구현 내용**:

#### 32.1 장시간 플레이 테스트

**테스트 시나리오**:

1. **5분 생존 테스트**:
   - Unity Editor Play 모드
   - 5분 이상 플레이 (또는 게임 오버까지)
   - 기록 사항:
     - 생존 시간
     - 처치 수
     - 도달한 웨이브
     - 체감 난이도

2. **반복 재시작 테스트**:
   - 게임 오버 후 Restart 버튼 클릭
   - 5회 이상 반복
   - 확인 사항:
     - 메모리 누수 없음 (Profiler Memory 확인)
     - 재시작 후 정상 작동
     - 통계 초기화 정상

3. **엣지 케이스 테스트**:
   - 플레이어 체력 1에서 생존
   - 몬스터 50마리 동시 스폰
   - 총알 100발 이상 동시 존재
   - 웨이브 10 이상 도달

#### 32.2 성능 검증

**성능 목표**:

| 지표 | 목표 | 측정 방법 |
|------|------|----------|
| FPS | 60 이상 | Profiler CPU |
| 프레임 시간 | < 16ms | Profiler CPU |
| GC Alloc | 0 바이트/프레임 | Profiler Memory |
| Entity 수 | 200개 이상 지원 | Entity Debugger |
| Burst 적용률 | 90% 이상 | Burst Inspector |

**Profiler 체크리스트**:

1. **CPU Usage**:
   - SimulationSystemGroup < 10ms
   - PresentationSystemGroup < 2ms
   - 렌더링 < 4ms

2. **Memory**:
   - GC Alloc: 0 바이트/프레임 (UI 업데이트 제외)
   - Managed Heap 증가율: 0 (누수 없음)
   - 5분 플레이 후 메모리 사용량 안정

3. **Rendering**:
   - Batches < 50
   - SetPass calls < 20
   - Triangles < 10K

#### 32.3 Entity Debugger 검증

**Entity Hierarchy 확인**:

1. Window → Entities → Hierarchy 열기
2. Play 모드에서 확인:
   - Player Entity 1개
   - Enemy Entity (동적 증가)
   - Bullet Entity (동적 증가/감소)
   - GameStats 싱글톤 1개
   - WaveConfig 싱글톤 1개 (웨이브 구현 시)
   - GameOverTag (게임 오버 시에만 생성)

3. 컴포넌트 데이터 실시간 확인:
   - PlayerHealth.CurrentHealth 감소 확인
   - GameStats.SurvivalTime 증가 확인
   - WaveConfig.CurrentWave 30초마다 증가 확인

#### 32.4 빌드 테스트

**Standalone Windows 빌드**:

1. File → Build Settings
2. Platform: PC, Mac & Linux Standalone
3. Target Platform: Windows
4. Architecture: x86_64
5. Development Build 체크 해제
6. Build
7. 빌드 실행 및 테스트:
   - 정상 시작
   - 입력 반응
   - UI 표시
   - 게임 오버 및 재시작
   - 성능 (60 FPS 유지)

**빌드 크기 확인**:
- 목표: < 100MB
- 불필요한 에셋 제거
- Unused Assets 정리

#### 32.5 최종 검증 체크리스트

**필수 검증 항목 (SPEC.md 기준)**:

**Phase 1: 플레이어 이동**
- [ ] WASD/화살표 키로 플레이어 이동 가능
- [ ] 3D 공간에서 자유롭게 이동
- [ ] Entity Debugger에서 PlayerTag 확인

**Phase 2: 자동 발사**
- [ ] 플레이어가 자동으로 총알 발사
- [ ] 총알이 위쪽으로 이동
- [ ] 5초 후 총알 자동 소멸

**Phase 4: 몬스터 스폰 및 추격**
- [ ] 몬스터가 주기적으로 스폰
- [ ] 몬스터가 플레이어 추격
- [ ] 최대 몬스터 수 제한 작동

**Phase 5: 충돌 및 체력**
- [ ] 총알이 몬스터에 맞으면 체력 감소
- [ ] 몬스터 체력 0 시 삭제
- [ ] 플레이어와 몬스터 충돌 시 플레이어 체력 감소

**Phase 6: UI 및 게임 오버**
- [ ] UI에 체력, 생존 시간, 처치 수 표시
- [ ] 플레이어 체력 0 시 게임 오버 UI 표시
- [ ] Restart 버튼으로 게임 재시작

**Phase 7: 밸런싱 및 웨이브** (이번 Phase)
- [ ] 게임 난이도가 적절함 (너무 쉽지도 어렵지도 않음)
- [ ] 웨이브가 30초마다 증가 (웨이브 구현 시)
- [ ] 웨이브별로 난이도 상승 (웨이브 구현 시)
- [ ] UI에 현재 웨이브 표시 (웨이브 구현 시)
- [ ] 5분 이상 플레이 시 성능 저하 없음
- [ ] 60 FPS 이상 유지
- [ ] 빌드가 정상 작동

**안정성 검증**:
- [ ] 5회 이상 재시작 반복 테스트 통과
- [ ] 메모리 누수 없음 (Profiler 확인)
- [ ] 콘솔에 에러/경고 없음
- [ ] 빌드 에러 없음

#### 32.6 문제 발견 시 대응

**일반적인 문제 및 해결**:

| 문제 | 원인 | 해결 방법 |
|------|------|----------|
| FPS 저하 | 과다한 Entity | MaxEnemies 감소, 총알 Lifetime 감소 |
| 메모리 누수 | ECB 미정리 | ECB 패턴 재확인 |
| UI 미업데이트 | 참조 누락 | UIManager 참조 재연결 |
| 재시작 실패 | 씬 설정 오류 | Build Settings에 씬 추가 확인 |
| 게임 오버 안됨 | PlayerDamageSystem 미작동 | Physics 설정 확인 |

**설계 원칙**:
- 철저함: 모든 기능 검증
- 현실성: 실제 플레이 환경 테스트
- 문서화: 발견된 문제 기록

**의존성**: TASK-029, TASK-030, TASK-031

**완료 조건**:
- [ ] 5분 생존 테스트 3회 이상 수행
- [ ] 재시작 테스트 5회 이상 수행
- [ ] Profiler에서 성능 목표 달성
- [ ] Entity Debugger에서 데이터 정상 확인
- [ ] Standalone 빌드 성공 및 실행 확인
- [ ] 모든 Phase 1-7 검증 항목 통과
- [ ] 콘솔에 에러/경고 없음
- [ ] 메모리 누수 없음

**예상 작업량**: 4-6시간

---

## Phase 7 전체 검증 체크리스트

### 필수 검증 항목 (SPEC.md 기준)

**난이도 밸런싱**:
- [ ] 초반(0-30초)은 쉽고 여유로움
- [ ] 중반(30-60초)은 적당한 난이도
- [ ] 후반(60초+)은 도전적인 난이도
- [ ] 평균 생존 시간 60-90초
- [ ] 게임이 너무 쉽거나 어렵지 않음

**웨이브 시스템** (구현 시):
- [ ] 웨이브가 30초마다 증가
- [ ] 웨이브별로 적 스폰 빨라짐
- [ ] 웨이브별로 적 이동 속도 빨라짐
- [ ] UI에 현재 웨이브 표시
- [ ] 게임 오버 시 웨이브 진행 중지

**코드 품질**:
- [ ] 중복 코드 없음
- [ ] 시스템 실행 순서 최적화됨
- [ ] Burst 컴파일 적용률 90% 이상
- [ ] SOLID 원칙 준수
- [ ] 코드 주석 적절히 작성됨

**성능**:
- [ ] 60 FPS 이상 유지
- [ ] 프레임 시간 < 16ms
- [ ] GC Alloc 0 바이트/프레임 (UI 제외)
- [ ] 5분 플레이 시 성능 저하 없음
- [ ] Entity 200개 이상 지원

**안정성**:
- [ ] 장시간 플레이(5분+) 정상 작동
- [ ] 재시작 반복 테스트 통과
- [ ] 메모리 누수 없음
- [ ] 빌드 정상 작동
- [ ] 콘솔 에러 없음

### 추가 검증 (완성도)

**게임플레이**:
- [ ] 게임이 재미있고 도전적임
- [ ] 난이도 곡선이 자연스러움
- [ ] UI가 직관적이고 가독성 좋음
- [ ] 조작감이 부드러움

**기술적 완성도**:
- [ ] 모든 Phase 1-7 기능 통합됨
- [ ] 코드 구조가 명확하고 확장 가능
- [ ] Entity Debugger에서 데이터 정상
- [ ] Profiler에서 최적화 확인됨

---

## 작업 순서 및 일정

### 권장 진행 순서

1. **Day 1 (4-6시간)**: TASK-029 완료
   - 현재 게임 플레이 분석
   - 밸런싱 파라미터 정의
   - Authoring 수치 조정
   - 반복 테스트 및 튜닝

2. **Day 2-3 (6-8시간)**: TASK-030 완료 (웨이브 시스템)
   - WaveConfig, WaveAuthoring 작성
   - WaveSystem 구현
   - EnemySpawnSystem, EnemyChaseSystem에 웨이브 적용
   - UI 연동
   - 씬에 WaveManager 추가
   - 테스트

3. **Day 4 (6-8시간)**: TASK-031 완료 (리팩토링)
   - 중복 코드 제거
   - 시스템 순서 최적화
   - Burst 검증
   - Profiler 체크
   - 주석 정리
   - SOLID 검증

4. **Day 5 (4-6시간)**: TASK-032 완료 (최종 검증)
   - 장시간 플레이 테스트
   - 성능 검증
   - Entity Debugger 확인
   - 빌드 테스트
   - 최종 체크리스트 확인

### 총 예상 소요 시간

**필수 작업** (TASK-029, 031, 032):
- 14-20시간 (약 3-4일)

**웨이브 포함** (TASK-029, 030, 031, 032):
- 20-28시간 (약 4-5일)

**버퍼 시간** (문제 해결 및 추가 튜닝):
- +4-6시간

**전체**: 24-34시간 (약 5-7일)

---

## 의존성 다이어그램

```
TASK-029 (난이도 밸런싱)
    ↓
TASK-030 (웨이브 시스템) ← 선택사항
    ↓
TASK-031 (코드 리팩토링)
    ↓
TASK-032 (최종 검증)
```

**병렬 작업 가능**:
- TASK-029와 TASK-031은 일부 병렬 가능 (밸런싱 테스트 중 코드 정리)
- TASK-030은 TASK-029 완료 후 진행 권장

---

## SOLID 원칙 준수 확인

### Single Responsibility Principle (단일 책임)
- **WaveSystem**: 웨이브 진행만 담당
- **GameStatsSystem**: 통계 업데이트만 담당
- **각 Authoring**: 각자의 Entity 설정만 담당

### Open/Closed Principle (개방/폐쇄)
- 새로운 난이도 배율 추가 시 WaveConfig 확장 가능
- 기존 시스템 수정 없이 새 밸런싱 파라미터 추가 가능

### Liskov Substitution Principle (리스코프 치환)
- ISystem, IComponentData를 올바르게 구현
- 모든 시스템이 ECS 규칙 준수

### Interface Segregation Principle (인터페이스 분리)
- 각 시스템은 필요한 컴포넌트만 쿼리
- WaveSystem은 WaveConfig만, EnemySpawnSystem은 EnemySpawnConfig와 WaveConfig만 접근

### Dependency Inversion Principle (의존성 역전)
- 시스템들은 구체적 구현이 아닌 컴포넌트(인터페이스)에 의존
- WaveConfig 싱글톤을 통한 느슨한 결합

---

## 문제 해결 가이드

### 일반적인 문제

**1. 게임이 너무 쉬움**
- 몬스터 SpawnInterval 감소 (더 빠르게 스폰)
- 몬스터 MoveSpeed 증가
- 플레이어 충돌 데미지 증가
- 총알 Damage 감소

**2. 게임이 너무 어려움**
- 플레이어 MaxHealth 증가
- 몬스터 Health 감소
- 총알 FireRate 감소 (더 빠르게 발사)
- 몬스터 SpawnInterval 증가

**3. 웨이브 난이도 상승이 너무 급격함**
- WaveConfig.SpawnIntervalMultiplier 증가 (0.92 → 0.95)
- WaveConfig.EnemySpeedMultiplier 감소 (1.08 → 1.05)
- WaveDuration 증가 (30초 → 45초)

**4. 웨이브 난이도 상승이 너무 완만함**
- WaveConfig.SpawnIntervalMultiplier 감소 (0.92 → 0.88)
- WaveConfig.EnemySpeedMultiplier 증가 (1.08 → 1.12)
- 추가 배율 도입 (MaxEnemies도 증가)

**5. FPS 저하**
- MaxEnemies 감소
- 총알 Lifetime 감소
- Profiler에서 병목 시스템 확인
- Burst 미적용 시스템 찾아 적용

**6. 메모리 누수**
- ECB 패턴 재확인 (Playback 자동 실행 확인)
- Profiler Memory에서 Managed Heap 증가 추적
- 불필요한 Object.FindObjectOfType 제거

**7. UI가 업데이트 안됨**
- UIManager 참조 재연결
- UIUpdateSystem이 실행되는지 확인 (RequireForUpdate)
- WaveText 참조 null 체크

**8. 빌드 에러**
- Build Settings에 씬 추가 확인
- Burst 컴파일 에러 해결
- 플랫폼별 패키지 확인

---

## 선택적 기능 (Phase 7 이후)

Phase 7 완료 후 추가로 구현 가능한 기능들:

### 파워업 시스템 (추가 2-4시간)
- 몬스터 사망 시 랜덤 아이템 드랍
- PowerUpTag, PowerUpType 컴포넌트
- 플레이어 충돌 시 능력치 향상
- UI에 현재 파워업 표시

### 멀티 총알 패턴 (추가 2-3시간)
- 3방향 발사 (좌/중/우)
- BulletDirection에 각도 추가
- AutoShootSystem 수정

### 사운드 및 이펙트 (추가 3-5시간)
- AudioSource 컴포넌트 추가
- 발사, 폭발, 피격 사운드
- Particle System 이펙트
- VFX Graph (선택)

### 추가 UI (추가 1-2시간)
- 메인 메뉴 화면
- 옵션 설정 (음량, 난이도)
- 하이스코어 저장

이러한 기능들은 **spec.md에 선택사항으로 명시**되어 있으며, Phase 7의 필수 요구사항은 아닙니다.

---

## 다음 단계 (Phase 7 완료 후)

Phase 7 완료 후:
1. **최종 코드 리뷰**: 모든 시스템 재검토
2. **Git 커밋**: "Phase 7 완료: 밸런싱 및 웨이브 시스템"
3. **문서화**: README 업데이트, 프로젝트 설정 가이드 작성
4. **배포 준비**: 빌드 최적화, 압축
5. **플레이 테스트**: 외부 테스터 피드백 수집 (선택)

**선택사항**:
- itch.io 등에 업로드
- 포트폴리오에 추가
- 추가 기능 구현 (파워업, 사운드 등)

---

## 참고 사항

### 밸런싱 철학

**핵심 원칙**:
- 쉽게 배우고, 마스터하기 어렵게
- 초반에는 관대하게, 후반에는 엄격하게
- 플레이어 실력 향상을 느낄 수 있게
- 운보다는 실력으로 승부

**난이도 곡선 설계**:
```
난이도
  ↑
  │                    /
  │                  /
  │                /
  │              /
  │           /
  │        /
  │     /
  │  /
  │/________________→ 시간
  0  30s  60s  120s
```

### Unity 설정

**Quality Settings**:
- VSync: On (안정적인 60 FPS)
- Anti-Aliasing: 2x MSAA
- Shadow Quality: Medium

**Physics Settings**:
- Fixed Timestep: 0.02 (50 Hz)
- Default Contact Offset: 0.01

### 성능 목표

**최소 사양 (60 FPS)**:
- 플레이어 1명
- 몬스터 50마리
- 총알 100발
- 웨이브 10 이상

**권장 사양**:
- 플레이어 1명
- 몬스터 100마리
- 총알 200발
- 무제한 웨이브

### 코드 스타일

**네이밍 규칙**:
- 컴포넌트: PascalCase (PlayerHealth)
- 시스템: PascalCase + System (PlayerMovementSystem)
- 변수: camelCase (currentHealth)
- 상수: UPPER_SNAKE_CASE (COLLISION_DAMAGE)

**파일 구조**:
- 1 파일 = 1 클래스/구조체
- 파일명 = 클래스명
- 관련 파일끼리 폴더 정리

---

**Phase 7 시작 준비 완료!**
TASK-029부터 순차적으로 진행하세요. 웨이브 시스템(TASK-030)은 선택사항이지만 게임 완성도를 크게 향상시킵니다.
