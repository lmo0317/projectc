# Phase 6 실행 계획: UI 시스템 - 체력 표시 및 게임 오버

## 프로젝트 개요

**Phase**: Phase 6 - UI 시스템 - 체력 표시 및 게임 오버
**목표**: 플레이어 체력, 생존 시간, 처치 수를 표시하는 UI 구현 및 게임 오버 시스템 완성
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수

## Phase 6 개발 사항 요약

이 단계에서는 게임 상태를 시각적으로 표시하는 UI 시스템과 게임 오버 로직을 구현합니다.

### 구현 범위
1. 게임 통계 데이터 컴포넌트 (생존 시간, 처치 수)
2. UI Canvas 및 TextMeshPro 설정
3. ECS 데이터를 UI에 연결하는 시스템
4. 게임 오버 UI 및 재시작 로직

---

## 태스크 분해

### **TASK-024: 게임 통계 컴포넌트 및 싱글톤 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 게임 통계를 저장하고 관리하는 ECS 컴포넌트 및 싱글톤 Entity 구현

**구현 내용**:

#### 24.1 GameStats 컴포넌트 정의
`Assets/Scripts/Components/GameStats.cs` 작성:

```csharp
using Unity.Entities;

/// <summary>
/// 게임 통계 데이터 (생존 시간, 처치 수)
/// </summary>
public struct GameStats : IComponentData
{
    public float SurvivalTime;    // 생존 시간 (초)
    public int KillCount;          // 처치한 몬스터 수
}
```

#### 24.2 GameStatsSingleton Authoring 구현
`Assets/Scripts/Authoring/GameStatsAuthoring.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 게임 통계 싱글톤 Entity 생성
/// </summary>
public class GameStatsAuthoring : MonoBehaviour
{
    class Baker : Baker<GameStatsAuthoring>
    {
        public override void Bake(GameStatsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GameStats
            {
                SurvivalTime = 0f,
                KillCount = 0
            });
        }
    }
}
```

#### 24.3 GameStats 업데이트 시스템 구현
`Assets/Scripts/Systems/GameStatsSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 게임 통계 업데이트 시스템 (생존 시간 증가)
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct GameStatsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameStats>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // GameStats 싱글톤 업데이트
        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime += deltaTime;
            break; // 싱글톤이므로 하나만 처리
        }
    }
}
```

#### 24.4 BulletHitSystem에 처치 카운트 추가
`Assets/Scripts/Systems/BulletHitSystem.cs` 수정:

```csharp
// Enemy 체력이 0 이하면 삭제
if (enemyHealth.ValueRO.Value <= 0)
{
    ecb.DestroyEntity(enemyEntity);

    // 처치 카운트 증가
    foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
    {
        stats.ValueRW.KillCount++;
        break; // 싱글톤
    }
}
```

**설계 원칙**:
- 싱글톤 패턴: 게임에 하나의 GameStats만 존재
- 단일 책임: GameStatsSystem은 시간 증가만 담당
- 간결한 구조: 불필요한 복잡도 배제

**의존성**: Phase 5 완료 (BulletHitSystem 존재)

**완료 조건**:
- [ ] GameStats 컴포넌트 작성 완료
- [ ] GameStatsAuthoring 작성 완료
- [ ] GameStatsSystem 작성 완료
- [ ] BulletHitSystem에 KillCount 증가 로직 추가
- [ ] 컴파일 에러 없음
- [ ] Play 모드에서 생존 시간이 증가함
- [ ] 몬스터 처치 시 KillCount 증가 확인 (Entity Debugger)

**예상 작업량**: 2-3시간

---

### **TASK-025: UI Canvas 및 TextMeshPro 설정**

**카테고리**: UI
**우선순위**: P0 (최우선)
**설명**: 게임 UI Canvas를 생성하고 체력, 생존 시간, 처치 수를 표시하는 TextMeshPro 요소 배치

**구현 내용**:

#### 25.1 UI Canvas 생성
Unity Editor 작업:

1. **Canvas 생성**:
   - Hierarchy → UI → Canvas 생성
   - 이름: "GameUI"
   - Canvas Scaler 설정:
     - UI Scale Mode: Scale With Screen Size
     - Reference Resolution: 1920x1080

2. **Panel 생성 (배경, 선택사항)**:
   - GameUI 하위에 UI → Panel 생성
   - 이름: "HUDPanel"
   - 색상: 반투명 검정 (Alpha: 50)
   - Anchor: 상단 좌측
   - Width: 300, Height: 150

#### 25.2 TextMeshPro 요소 생성

**체력 표시**:
1. HUDPanel 하위에 TextMeshPro - Text 생성
2. 이름: "HealthText"
3. Text: "Health: 100"
4. Font Size: 36
5. Color: White
6. Alignment: Left
7. Position: 상단 좌측 (Anchor Preset: Top Left)

**생존 시간 표시**:
1. HUDPanel 하위에 TextMeshPro - Text 생성
2. 이름: "SurvivalTimeText"
3. Text: "Time: 0s"
4. Font Size: 32
5. Color: Yellow
6. Position: HealthText 아래 (Y: -50)

**처치 수 표시**:
1. HUDPanel 하위에 TextMeshPro - Text 생성
2. 이름: "KillCountText"
3. Text: "Kills: 0"
4. Font Size: 32
5. Color: Red
6. Position: SurvivalTimeText 아래 (Y: -50)

#### 25.3 UI Manager MonoBehaviour 생성
`Assets/Scripts/UI/UIManager.cs` 작성:

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// UI 텍스트 참조 관리 (MonoBehaviour)
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD Text References")]
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI SurvivalTimeText;
    public TextMeshProUGUI KillCountText;

    [Header("Game Over UI")]
    public GameObject GameOverPanel;
    public TextMeshProUGUI FinalStatsText;

    private void Awake()
    {
        // 게임 오버 패널 초기 비활성화
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// HUD 텍스트 업데이트
    /// </summary>
    public void UpdateHealth(float current, float max)
    {
        if (HealthText != null)
        {
            HealthText.text = $"Health: {current:F0} / {max:F0}";
        }
    }

    public void UpdateSurvivalTime(float time)
    {
        if (SurvivalTimeText != null)
        {
            SurvivalTimeText.text = $"Time: {time:F1}s";
        }
    }

    public void UpdateKillCount(int kills)
    {
        if (KillCountText != null)
        {
            KillCountText.text = $"Kills: {kills}";
        }
    }

    /// <summary>
    /// 게임 오버 UI 표시
    /// </summary>
    public void ShowGameOver(float survivalTime, int killCount)
    {
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);

            if (FinalStatsText != null)
            {
                FinalStatsText.text = $"Game Over!\n\nSurvival Time: {survivalTime:F1}s\nKills: {killCount}";
            }
        }
    }
}
```

#### 25.4 GameUI에 UIManager 부착
1. GameUI Canvas 선택
2. Add Component → UIManager
3. Inspector에서 텍스트 참조 연결:
   - Health Text → HealthText
   - Survival Time Text → SurvivalTimeText
   - Kill Count Text → KillCountText

**설계 원칙**:
- UI와 ECS 분리: UIManager는 MonoBehaviour로 관리
- 단일 책임: UI 업데이트만 담당
- 간결한 인터페이스: 필요한 메서드만 노출

**의존성**: TASK-024

**완료 조건**:
- [ ] GameUI Canvas 생성 완료
- [ ] TextMeshPro 요소 3개 생성 및 배치
- [ ] UIManager 스크립트 작성 완료
- [ ] UIManager에 텍스트 참조 연결 완료
- [ ] Play 모드에서 UI가 화면에 표시됨
- [ ] 컴파일 에러 없음

**예상 작업량**: 2-3시간

---

### **TASK-026: UI 업데이트 시스템 구현**

**카테고리**: UI
**우선순위**: P0 (최우선)
**설명**: ECS 데이터를 읽어 UI 텍스트를 업데이트하는 시스템 구현

**구현 내용**:

#### 26.1 UIUpdateSystem 구현
`Assets/Scripts/Systems/UIUpdateSystem.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// ECS 데이터를 읽어 UI를 업데이트하는 시스템 (MainThread)
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    private UIManager uiManager;

    protected override void OnCreate()
    {
        RequireForUpdate<GameStats>();
        RequireForUpdate<PlayerHealth>();
    }

    protected override void OnStartRunning()
    {
        // UIManager 참조 찾기
        uiManager = Object.FindObjectOfType<UIManager>();

        if (uiManager == null)
        {
            Debug.LogWarning("UIManager not found in scene!");
        }
    }

    protected override void OnUpdate()
    {
        if (uiManager == null) return;

        // 1. 플레이어 체력 업데이트
        foreach (var health in SystemAPI.Query<RefRO<PlayerHealth>>().WithAll<PlayerTag>())
        {
            uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            break; // 플레이어는 하나
        }

        // 2. 게임 통계 업데이트
        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            uiManager.UpdateSurvivalTime(stats.ValueRO.SurvivalTime);
            uiManager.UpdateKillCount(stats.ValueRO.KillCount);
            break; // 싱글톤
        }
    }
}
```

**설계 원칙**:
- SystemBase 사용: MainThread에서 실행 (MonoBehaviour 접근 가능)
- PresentationSystemGroup: 렌더링 직전 UI 업데이트
- 성능 최적화: 매 프레임 업데이트하지만 UI 업데이트는 빠름

**의존성**: TASK-025

**완료 조건**:
- [ ] UIUpdateSystem 작성 완료
- [ ] PresentationSystemGroup에서 실행됨
- [ ] Play 모드에서 체력이 실시간 업데이트됨
- [ ] 생존 시간이 증가함 (초 단위)
- [ ] 몬스터 처치 시 처치 수 증가함
- [ ] 컴파일 에러 없음
- [ ] UI 업데이트로 인한 성능 저하 없음 (60 FPS 유지)

**예상 작업량**: 2-3시간

---

### **TASK-027: 게임 오버 UI 및 시스템 구현**

**카테고리**: 게임플레이/UI
**우선순위**: P0 (최우선)
**설명**: 플레이어 체력이 0이 되면 게임 오버 UI를 표시하고 Entity 스폰을 중지하는 시스템 구현

**구현 내용**:

#### 27.1 게임 오버 UI 생성

**GameOver Panel 생성**:
1. GameUI Canvas 하위에 UI → Panel 생성
2. 이름: "GameOverPanel"
3. Anchor: Stretch (전체 화면)
4. Color: 반투명 검정 (Alpha: 200)
5. 초기 상태: 비활성화 (Inspector에서 체크 해제)

**Game Over 텍스트**:
1. GameOverPanel 하위에 TextMeshPro - Text 생성
2. 이름: "FinalStatsText"
3. Text: "Game Over!\n\nSurvival Time: 0s\nKills: 0"
4. Font Size: 48
5. Color: White
6. Alignment: Center
7. Position: 화면 중앙

**재시작 버튼**:
1. GameOverPanel 하위에 UI → Button - TextMeshPro 생성
2. 이름: "RestartButton"
3. Button Text: "Restart"
4. Font Size: 32
5. Position: FinalStatsText 아래 (Y: -100)
6. Width: 200, Height: 60

#### 27.2 UIManager에 게임 오버 패널 연결
1. GameUI Canvas 선택
2. UIManager Inspector에서:
   - Game Over Panel → GameOverPanel
   - Final Stats Text → FinalStatsText

#### 27.3 GameOverTag 컴포넌트 추가
`Assets/Scripts/Components/GameOverTag.cs` 작성:

```csharp
using Unity.Entities;

/// <summary>
/// 게임 오버 상태 태그 (싱글톤)
/// </summary>
public struct GameOverTag : IComponentData
{
}
```

#### 27.4 GameOverSystem 구현
`Assets/Scripts/Systems/GameOverSystem.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 게임 오버 감지 및 처리 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerDamageSystem))]
public partial class GameOverSystem : SystemBase
{
    private UIManager uiManager;
    private bool gameOverTriggered = false;

    protected override void OnCreate()
    {
        RequireForUpdate<PlayerHealth>();
        RequireForUpdate<GameStats>();
    }

    protected override void OnStartRunning()
    {
        uiManager = Object.FindObjectOfType<UIManager>();
    }

    protected override void OnUpdate()
    {
        if (gameOverTriggered) return;

        // 플레이어 체력 체크
        foreach (var health in SystemAPI.Query<RefRO<PlayerHealth>>().WithAll<PlayerTag>())
        {
            if (health.ValueRO.CurrentHealth <= 0)
            {
                // 게임 오버 트리거
                TriggerGameOver();
                gameOverTriggered = true;
            }
            break;
        }
    }

    private void TriggerGameOver()
    {
        // 게임 통계 가져오기
        float survivalTime = 0f;
        int killCount = 0;

        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            survivalTime = stats.ValueRO.SurvivalTime;
            killCount = stats.ValueRO.KillCount;
            break;
        }

        // UI 표시
        if (uiManager != null)
        {
            uiManager.ShowGameOver(survivalTime, killCount);
        }

        // GameOverTag 추가 (다른 시스템에서 감지 가능)
        Entity gameOverEntity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(gameOverEntity, new GameOverTag());

        Debug.Log($"Game Over! Survival Time: {survivalTime:F1}s, Kills: {killCount}");
    }
}
```

#### 27.5 Enemy 및 Bullet 스폰 중지
`Assets/Scripts/Systems/EnemySpawnSystem.cs` 수정:

```csharp
public void OnUpdate(ref SystemState state)
{
    // 게임 오버 시 스폰 중지
    if (SystemAPI.HasSingleton<GameOverTag>())
        return;

    // 기존 스폰 로직...
}
```

`Assets/Scripts/Systems/AutoShootSystem.cs` 수정:

```csharp
public void OnUpdate(ref SystemState state)
{
    // 게임 오버 시 발사 중지
    if (SystemAPI.HasSingleton<GameOverTag>())
        return;

    // 기존 발사 로직...
}
```

**설계 원칙**:
- 단일 책임: GameOverSystem은 게임 오버 감지만 담당
- 태그 기반 상태 관리: GameOverTag로 상태 공유
- 간결한 로직: 필요한 최소 기능만 구현

**의존성**: TASK-026

**완료 조건**:
- [ ] GameOverPanel UI 생성 완료
- [ ] RestartButton 생성 완료
- [ ] GameOverTag 컴포넌트 작성 완료
- [ ] GameOverSystem 작성 완료
- [ ] EnemySpawnSystem에 게임 오버 체크 추가
- [ ] AutoShootSystem에 게임 오버 체크 추가
- [ ] Play 모드에서 체력 0 시 게임 오버 UI 표시
- [ ] 게임 오버 시 최종 통계가 정확하게 표시됨
- [ ] 게임 오버 후 스폰 및 발사 중지 확인
- [ ] 컴파일 에러 없음

**예상 작업량**: 3-4시간

---

### **TASK-028: 재시작 기능 구현 및 통합 테스트**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 재시작 버튼 클릭 시 게임을 초기 상태로 되돌리는 기능 구현 및 Phase 6 전체 검증

**구현 내용**:

#### 28.1 GameRestartManager 구현
`Assets/Scripts/UI/GameRestartManager.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 재시작 관리 (MonoBehaviour)
/// </summary>
public class GameRestartManager : MonoBehaviour
{
    /// <summary>
    /// 씬 재로드를 통한 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        // 현재 씬 재로드
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// ECS Entity 기반 재시작 (대안 방법)
    /// </summary>
    public void RestartGameECS()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var entityManager = world.EntityManager;

        // 1. 모든 Enemy 삭제
        var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
        entityManager.DestroyEntity(enemyQuery);

        // 2. 모든 Bullet 삭제
        var bulletQuery = entityManager.CreateEntityQuery(typeof(BulletTag));
        entityManager.DestroyEntity(bulletQuery);

        // 3. GameOverTag 삭제
        var gameOverQuery = entityManager.CreateEntityQuery(typeof(GameOverTag));
        entityManager.DestroyEntity(gameOverQuery);

        // 4. 플레이어 체력 초기화
        foreach (var (health, entity) in SystemAPI.Query<RefRW<PlayerHealth>>().WithAll<PlayerTag>().WithEntityAccess())
        {
            health.ValueRW.CurrentHealth = health.ValueRO.MaxHealth;
            break;
        }

        // 5. 게임 통계 초기화
        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime = 0f;
            stats.ValueRW.KillCount = 0;
            break;
        }

        // 6. GameOverSystem 상태 초기화
        var gameOverSystem = world.GetExistingSystemManaged<GameOverSystem>();
        if (gameOverSystem != null)
        {
            // 리플렉션 또는 public 메서드로 gameOverTriggered = false 설정
            // 간단한 방법: 씬 재로드 사용
        }

        // 7. UI 초기화
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && uiManager.GameOverPanel != null)
        {
            uiManager.GameOverPanel.SetActive(false);
        }

        Debug.Log("Game Restarted!");
    }
}
```

**참고**: ECS 기반 재시작은 복잡하므로 **씬 재로드 방식**을 권장합니다.

#### 28.2 RestartButton에 클릭 이벤트 연결

**간단한 방법 (씬 재로드)**:
1. GameUI Canvas에 GameRestartManager 컴포넌트 추가
2. RestartButton 선택
3. Inspector → Button 컴포넌트 → OnClick()
4. + 버튼 클릭 → GameUI 드래그
5. Function: GameRestartManager → RestartGame()

**대안 방법 (코드)**:
UIManager.cs에 추가:

```csharp
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Restart")]
    public Button RestartButton;

    private void Awake()
    {
        // 게임 오버 패널 초기 비활성화
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }

        // 재시작 버튼 이벤트 연결
        if (RestartButton != null)
        {
            RestartButton.onClick.AddListener(OnRestartButtonClicked);
        }
    }

    private void OnRestartButtonClicked()
    {
        var restartManager = GetComponent<GameRestartManager>();
        if (restartManager != null)
        {
            restartManager.RestartGame();
        }
    }
}
```

#### 28.3 통합 테스트

**Play 모드 전체 테스트**:
1. Unity Editor에서 Play 버튼 클릭
2. 게임 시작 확인:
   - UI에 체력, 시간, 처치 수 표시
   - 플레이어 이동 가능
   - 총알 자동 발사
   - 몬스터 스폰
3. 생존 시간 증가 확인 (1초씩)
4. 몬스터 처치 시 처치 수 증가 확인
5. 플레이어 체력 감소 확인 (몬스터 충돌 시)
6. 플레이어 체력 0 도달 시:
   - 게임 오버 UI 표시
   - 최종 통계 정확성 확인
   - 스폰 및 발사 중지 확인
7. 재시작 버튼 클릭:
   - 게임이 초기 상태로 복귀
   - UI 초기화
   - 플레이어 체력 100
   - 통계 0

**Entity Debugger 검증**:
1. Window → Entities → Hierarchy 열기
2. Play 모드에서 확인:
   - GameStats 싱글톤 확인
   - SurvivalTime 증가 확인
   - KillCount 증가 확인
3. 게임 오버 시 GameOverTag Entity 생성 확인

**성능 검증**:
1. Window → Analysis → Profiler 열기
2. UIUpdateSystem CPU 사용률 확인 (0.1ms 이하)
3. 60 FPS 유지 확인

#### 28.4 밸런싱 조정
필요 시 다음 값 조정:

- **플레이어 체력**: `PlayerAuthoring.MaxHealth` (기본 100)
- **몬스터 데미지**: `PlayerDamageSystem` 고정값 (기본 10)
- **몬스터 체력**: `EnemyAuthoring.Health` (기본 100)
- **총알 데미지**: `BulletAuthoring.Damage` (기본 25)

**의존성**: TASK-027

**완료 조건**:
- [ ] GameRestartManager 작성 완료
- [ ] RestartButton에 클릭 이벤트 연결 완료
- [ ] Play 모드에서 전체 게임플레이 정상 작동
- [ ] UI가 실시간으로 업데이트됨
- [ ] 게임 오버 시 UI 표시 및 스폰 중지
- [ ] 재시작 버튼 클릭 시 게임 초기화
- [ ] 60 FPS 이상 유지
- [ ] 컴파일 에러 없음
- [ ] 콘솔에 에러 없음
- [ ] UI가 게임플레이를 방해하지 않음

**예상 작업량**: 3-4시간

---

## Phase 6 전체 검증 체크리스트

### 필수 검증 항목 (SPEC.md 기준)
- [ ] 게임 실행 시 플레이어 체력이 UI에 표시됨
- [ ] 생존 시간이 초 단위로 증가함
- [ ] 몬스터를 처치하면 카운터가 증가함
- [ ] 플레이어 체력이 0이 되면 게임 오버 UI가 표시됨
- [ ] 게임 오버 시 최종 통계가 정확하게 표시됨
- [ ] 재시작 버튼 클릭 시 게임이 초기 상태로 돌아감
- [ ] UI 업데이트로 인한 성능 저하 없음
- [ ] UI가 게임플레이를 방해하지 않음 (적절한 배치)

### 추가 검증 (안정성)
- [ ] 장시간 플레이(5분) 시 UI 업데이트 정상 작동
- [ ] 재시작 여러 번 반복해도 문제 없음
- [ ] 빌드 테스트 (Standalone Windows)
- [ ] 메모리 누수 없음

---

## 작업 순서 및 일정

### 권장 진행 순서
1. **Day 1 (2-3시간)**: TASK-024 완료
   - GameStats 컴포넌트 작성
   - GameStatsSystem 구현
   - BulletHitSystem 수정

2. **Day 1-2 (2-3시간)**: TASK-025 완료
   - UI Canvas 생성
   - TextMeshPro 배치
   - UIManager 작성

3. **Day 2 (2-3시간)**: TASK-026 완료
   - UIUpdateSystem 구현
   - 실시간 UI 업데이트 테스트

4. **Day 2-3 (3-4시간)**: TASK-027 완료
   - 게임 오버 UI 생성
   - GameOverSystem 구현
   - 스폰 중지 로직 추가

5. **Day 3 (3-4시간)**: TASK-028 완료
   - 재시작 기능 구현
   - 통합 테스트
   - 최종 검증

### 총 예상 소요 시간
- **필수 작업**: 12-17시간 (약 2-3일)
- **검증 및 문제 해결**: 추가 2-3시간
- **전체**: 14-20시간

---

## 의존성 다이어그램

```
TASK-024 (게임 통계 컴포넌트)
    ↓
TASK-025 (UI Canvas 설정)
    ↓
TASK-026 (UI 업데이트 시스템)
    ↓
TASK-027 (게임 오버 시스템)
    ↓
TASK-028 (재시작 및 검증)
```

---

## SOLID 원칙 준수 확인

### Single Responsibility Principle (단일 책임)
- **GameStats**: 게임 통계 데이터만 저장
- **GameStatsSystem**: 생존 시간 증가만 담당
- **UIManager**: UI 업데이트만 담당
- **GameOverSystem**: 게임 오버 감지만 담당
- **UIUpdateSystem**: ECS → UI 데이터 전달만 담당

### Open/Closed Principle (개방/폐쇄)
- 새로운 통계 항목 추가 시 GameStats만 수정
- UI 요소 추가 시 UIManager만 확장

### Liskov Substitution Principle (리스코프 치환)
- IComponentData, SystemBase를 올바르게 구현

### Interface Segregation Principle (인터페이스 분리)
- 각 시스템은 필요한 컴포넌트만 쿼리
- UIManager는 필요한 메서드만 노출

### Dependency Inversion Principle (의존성 역전)
- 시스템은 구체적 UI가 아닌 UIManager 인터페이스에 의존

---

## 문제 해결 가이드

### 일반적인 문제

**1. UI가 표시되지 않음**
- Canvas Scaler 설정 확인
- UIManager 참조 연결 확인
- TextMeshPro 임포트 확인

**2. UI가 업데이트되지 않음**
- UIUpdateSystem이 실행되는지 확인
- UIManager 참조가 null이 아닌지 확인
- PlayerHealth, GameStats Entity 존재 확인

**3. 게임 오버 UI가 표시되지 않음**
- GameOverPanel이 비활성화되어 있는지 확인 (초기 상태)
- GameOverSystem이 실행되는지 확인
- 플레이어 체력이 실제로 0 이하인지 확인

**4. 재시작이 작동하지 않음**
- RestartButton의 OnClick 이벤트 연결 확인
- GameRestartManager 컴포넌트 부착 확인
- 씬 이름이 올바른지 확인

**5. 성능 저하**
- UIUpdateSystem을 매 프레임이 아닌 값 변경 시에만 업데이트하도록 최적화
- Text 업데이트를 문자열 비교로 중복 방지

---

## 다음 단계 (Phase 7 준비)

Phase 6 완료 후:
1. **코드 리뷰**: 모든 UI 시스템 검토
2. **Git 커밋**: "Phase 6 완료: UI 시스템 및 게임 오버"
3. **문서화**: UI 아키텍처 README에 기록
4. **Phase 7 준비**: 웨이브 시스템 및 밸런싱 설계 검토

---

## 참고 사항

### 코드 스타일
- C# Naming Convention 준수
- UI 메서드는 동사로 시작 (Update, Show 등)
- 간결한 주석 (필요시에만)

### Unity 설정
- TextMeshPro 폰트 에셋 확인
- Canvas Event Camera 설정 (필요 시)
- UI Auto Layout 활용

### 성능 고려사항
- UI 업데이트는 PresentationSystemGroup에서 실행
- 불필요한 문자열 생성 최소화 (StringBuilder 고려)
- UI 요소 수 최소화

### UI 디자인 팁
- 가독성 우선: 큰 폰트, 명확한 색상
- 게임플레이 방해 최소화: HUD는 모서리 배치
- 일관성: 모든 텍스트 동일한 폰트 사용

---

**Phase 6 시작 준비 완료!**
TASK-024부터 순차적으로 진행하세요.
