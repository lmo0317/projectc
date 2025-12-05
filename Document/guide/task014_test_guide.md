# TASK-014: Phase 3 통합 테스트 가이드

## 📋 개요

본 문서는 **Phase 3: 몬스터 스폰 시스템**의 통합 테스트 및 검증을 위한 가이드입니다.

### Phase 3 완료 상태

**구현 완료된 항목**:
- ✅ **TASK-010**: 몬스터 컴포넌트 (EnemyTag, EnemyHealth, EnemySpeed, EnemySpawnConfig)
- ✅ **TASK-011**: Enemy Prefab 및 Material (빨간색 큐브)
- ✅ **TASK-012**: EnemySpawnManager 씬 설정
- ✅ **TASK-013**: EnemySpawnSystem (타이머 기반 랜덤 원형 스폰)

### 테스트 목적

Phase 3의 모든 기능이 올바르게 통합되어 작동하는지 검증:
1. 몬스터가 2초 간격으로 스폰되는가?
2. 플레이어 주변 원형으로 랜덤하게 스폰되는가?
3. 빨간색 큐브로 렌더링되는가?
4. 최대 50개 개수 제한이 작동하는가?
5. Phase 1 (플레이어 이동) 및 Phase 2 (자동 사격)와 정상 통합되는가?

---

## 🎮 Unity Editor 실행

### 1단계: Unity Hub에서 프로젝트 열기

1. **Unity Hub** 실행
2. **Projects** 탭에서 `projectc` 선택
3. Unity 버전 **6000.1.7f1**로 열기

### 2단계: 올바른 씬 열기

**방법 1: GameScene.unity 열기 (권장)**
- Unity Editor에서 `Project` 윈도우 열기
- `Assets/Scenes/GameScene.unity` 더블클릭

**방법 2: PlayerSubScene.unity 직접 열기**
- `Assets/Scenes/GameScene/PlayerSubScene.unity` 열기
- 이 씬에 **EnemySpawnManager**가 설정되어 있음

### 3단계: 씬 구성 확인

**Hierarchy 윈도우**에서 다음 GameObject 확인:
```
PlayerSubScene
├── Player                  (플레이어 Entity)
├── Ground                  (바닥)
└── EnemySpawnManager       (스폰 매니저) ← 이 GameObject가 중요!
```

**EnemySpawnManager 선택 후 Inspector 확인**:
```
Transform
  Position: (0, 0, 0)

Enemy Spawn Authoring (Script)
  Spawn Interval: 2         ← 2초 간격
  Enemy Prefab: Enemy       ← Prefab 참조 확인
  Spawn Radius: 10          ← 스폰 반경
  Max Enemies: 50           ← 최대 개수
```

**⚠️ 중요**: `Enemy Prefab` 필드가 비어있으면 스폰이 작동하지 않습니다!
- 비어있는 경우: `Assets/Prefabs/Enemy` Prefab을 드래그하여 할당

---

## ▶️ Play 모드 테스트

### 실행 방법

1. Unity Editor 상단의 **Play 버튼** 클릭 (또는 `Ctrl+P`)
2. Game View가 활성화되며 게임 실행됨

### 예상 동작

#### Phase 1: 플레이어 이동 (기존 기능)
- **WASD 키**로 플레이어(캡슐) 이동
- 카메라는 고정, 플레이어만 이동

#### Phase 2: 자동 사격 (기존 기능)
- **0.5초(또는 설정된 간격)마다** 플레이어 앞쪽으로 총알(노란색 구체) 자동 발사
- 총알은 직진하며 5초 후 자동 소멸

#### Phase 3: 몬스터 스폰 (이번 테스트 대상)
- **2초 간격**으로 몬스터 스폰
- **빨간색 큐브** 형태
- **플레이어 주변 8-10 유닛 거리**에 원형으로 스폰
- **스폰 위치가 매번 랜덤**하게 변경됨
- **최대 50개**까지만 스폰 (50개 도달 시 더 이상 스폰 안 됨)

### 시각적 확인 사항

**정상 동작**:
- Scene View 또는 Game View에서 **빨간색 큐브**들이 플레이어 주변에 나타남
- 큐브들이 **원형 패턴**으로 배치됨 (일직선 아님)
- 2초마다 **새로운 큐브** 추가됨
- 현재 몬스터 개수는 **Hierarchy → Entities** 탭에서 확인 가능

**비정상 동작**:
- ❌ 몬스터가 전혀 스폰되지 않음 → [문제 해결](#-문제-해결) 참조
- ❌ 몬스터가 보이지 않음 (Entity는 생성됨) → TransformUsageFlags 문제
- ❌ 스폰 위치가 항상 같음 → Random 초기화 문제

### Play 모드 종료

- **Play 버튼 다시 클릭** (또는 `Ctrl+P`)
- Scene View로 돌아감

---

## 🔍 Entity Debugger 검증

Unity ECS의 Entity 상태를 직접 확인하는 고급 검증 방법입니다.

### Entity Hierarchy 열기

1. Unity Editor 상단 메뉴: `Window` → `Entities` → `Hierarchy`
2. **Play 모드 실행 필수** (Entity는 런타임에만 생성됨)

### 확인할 Entity 목록

**Entities Hierarchy** 윈도우에서:

```
World (Default World)
├── PlayerTag Entity              (1개) - 플레이어
├── AutoShootConfig Entity        (1개) - 사격 매니저
├── EnemyTag Entities             (0 → 50개) - 몬스터들
└── EnemySpawnConfig Entity       (1개) - 스폰 매니저
```

### Enemy Entity 상세 검증

1. **Hierarchy**에서 `EnemyTag`를 가진 Entity 선택
2. **Inspector** 윈도우에서 컴포넌트 확인:

```
Components:
├── LocalTransform
│   └── Position: (x, 0, z)     ← 스폰 위치 (매번 다름)
│
├── EnemyTag                     ← 마커 컴포넌트
│
├── EnemyHealth
│   └── Value: 100               ← 체력 값
│
├── EnemySpeed
│   └── Value: 3                 ← 이동 속도 (Phase 4에서 사용)
│
├── RenderMesh                   ← 렌더링 정보
│   └── Material: EnemyMaterial (빨간색)
│
└── ... (기타 ECS 내부 컴포넌트)
```

### EnemySpawnConfig Entity 검증

1. **Hierarchy**에서 `EnemySpawnConfig`를 가진 Entity 선택
2. **Inspector**에서 확인:

```
EnemySpawnConfig
├── SpawnInterval: 2
├── TimeSinceLastSpawn: 0~2      ← 실시간 변화 (타이머)
├── EnemyPrefab: Entity(XX)      ← Prefab Entity 참조
├── SpawnRadius: 10
├── MaxEnemies: 50
└── RandomGenerator: (내부 상태)
```

**실시간 확인**:
- Play 모드 중 `TimeSinceLastSpawn` 값이 **0에서 2까지 증가**하는 것을 관찰
- 2에 도달하면 다시 0으로 리셋되며 새 몬스터 스폰

### Entity 개수 확인

**Hierarchy 윈도우 상단**:
- `Total Entities: XXX` → 현재 생성된 모든 Entity 개수
- `EnemyTag` 필터링하여 몬스터만 개수 확인 가능

**검증 방법**:
1. Play 모드 시작 직후: 몬스터 0개
2. 2초 후: 1개
3. 4초 후: 2개
4. ...
5. 100초 후: 50개 (최대 개수)
6. 102초 후: 여전히 50개 (더 이상 증가 안 함)

---

## 🖥️ Console 검증

### Console 윈도우 열기

Unity Editor 하단의 **Console** 탭 (또는 `Ctrl+Shift+C`)

### 정상 동작 시 Console 상태

**예상 로그**:
- 에러 없음 (빨간색 메시지 없음)
- 경고 없음 (노란색 메시지 없음)
- Unity ECS 시스템 초기화 로그만 표시 (회색)

**정상 예시**:
```
[Entities] Created 'PlayerInputSystem'
[Entities] Created 'PlayerMovementSystem'
[Entities] Created 'AutoShootSystem'
[Entities] Created 'EnemySpawnSystem'
[Entities] Baking completed
```

### 비정상 동작 시 Console 에러

**일반적인 에러 메시지**:

#### 에러 1: NullReferenceException
```
NullReferenceException: Object reference not set to an instance of an object
at EnemySpawnSystem.OnUpdate
```
**원인**: `EnemyPrefab`이 할당되지 않음
**해결**: Inspector에서 Enemy Prefab 할당

#### 에러 2: Baker 변환 에러
```
ArgumentException: TransformUsageFlags must include Renderable
```
**원인**: EnemyAuthoring.cs의 TransformUsageFlags 설정 오류
**해결**: `GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic)` 확인

#### 경고: MaxEnemies 도달
```
(정상) 경고 없음 - 시스템이 자동으로 스폰 중단
```

### Console 필터링

- **에러만 보기**: Console 상단의 🔴 아이콘 클릭
- **경고만 보기**: ⚠️ 아이콘 클릭
- **Clear**: 로그 초기화

---

## 🛠️ 문제 해결

### 문제 1: 몬스터가 전혀 스폰되지 않음

#### 확인 사항

1. **Console 에러 확인**
   - `NullReferenceException` → Prefab 미할당
   - Baker 에러 → Authoring 스크립트 문제

2. **EnemySpawnManager GameObject 존재 확인**
   - Hierarchy에서 `EnemySpawnManager` 검색
   - 없으면: PlayerSubScene.unity에 새로 생성 필요

3. **Enemy Prefab 할당 확인**
   - EnemySpawnManager 선택 → Inspector
   - `Enemy Prefab` 필드 확인
   - 비어있으면: `Assets/Prefabs/Enemy` 드래그

4. **EnemySpawnSystem 실행 확인**
   - Entity Debugger → Systems 탭
   - `EnemySpawnSystem` 검색
   - `Enabled: true` 확인

#### 해결 방법

**Prefab 할당**:
1. Hierarchy에서 `EnemySpawnManager` 선택
2. Inspector에서 `Enemy Prefab` 필드에 `Assets/Prefabs/Enemy` 드래그
3. Play 모드 재시작 (`Ctrl+P` 두 번)

**GameObject 생성** (없는 경우):
1. Hierarchy 우클릭 → `Create Empty`
2. 이름: `EnemySpawnManager`
3. `Add Component` → `EnemySpawnAuthoring`
4. Inspector 설정:
   - Spawn Interval: 2
   - Enemy Prefab: Enemy
   - Spawn Radius: 10
   - Max Enemies: 50
5. 씬 저장 (`Ctrl+S`)

---

### 문제 2: 몬스터가 렌더링되지 않음 (Entity는 생성됨)

#### 증상

- Entity Debugger에서 `EnemyTag` Entity 확인됨
- Game View 또는 Scene View에서 보이지 않음

#### 원인

`TransformUsageFlags.Renderable` 플래그 누락

#### 확인 방법

`Assets/Scripts/Authoring/EnemyAuthoring.cs` 파일 확인:

```csharp
// ❌ 잘못됨
var entity = GetEntity(TransformUsageFlags.Dynamic);

// ✅ 올바름
var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
```

#### 해결 방법

1. `EnemyAuthoring.cs` 파일 수정
2. `TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic` 확인
3. Unity Editor 재시작 (Baker 캐시 초기화)
4. Play 모드 재실행

---

### 문제 3: 스폰 타이머가 작동하지 않음

#### 증상

- 2초 후에도 몬스터가 스폰되지 않음
- Entity Debugger에서 `TimeSinceLastSpawn` 값이 증가하지 않음

#### 원인

`RefRW<T>` 로컬 변수 복사 (구조체 값 복사 문제)

#### 확인 방법

`Assets/Scripts/Systems/EnemySpawnSystem.cs` 확인:

```csharp
// ❌ 잘못됨 (로컬 복사)
var config = spawnConfig.ValueRW;
config.TimeSinceLastSpawn += deltaTime;  // 복사본만 수정됨!

// ✅ 올바름 (직접 접근)
spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;
```

#### 해결 방법

1. `EnemySpawnSystem.cs` 수정
2. `spawnConfig.ValueRW.필드명` 형태로 직접 접근
3. 중간 변수 생성 금지
4. Play 모드 재실행

---

### 문제 4: 최대 개수 제한 미작동

#### 증상

- 50개 이상 몬스터가 계속 스폰됨

#### 확인 방법

1. **Inspector 확인**
   - EnemySpawnManager 선택
   - `Max Enemies` 값 확인 (50인지?)

2. **EnemySpawnSystem 로직 확인**
   ```csharp
   if (spawnConfig.ValueRW.MaxEnemies > 0)
   {
       int enemyCount = SystemAPI.QueryBuilder()
           .WithAll<EnemyTag>()
           .Build()
           .CalculateEntityCount();

       if (enemyCount >= spawnConfig.ValueRW.MaxEnemies)
       {
           continue;  // 스폰 중단
       }
   }
   ```

#### 해결 방법

1. Inspector에서 `Max Enemies` 재설정 (50)
2. 또는 코드 수정 후 재컴파일

---

### 문제 5: 스폰 위치가 항상 같음

#### 증상

- 몬스터들이 모두 같은 위치에 스폰됨
- 원형 분포 안 됨

#### 원인

Random 초기화 문제 또는 난수 생성 로직 오류

#### 확인 방법

`EnemySpawnSystem.cs`:
```csharp
// 랜덤 각도 (0 ~ 2π)
float angle = spawnConfig.ValueRW.RandomGenerator.NextFloat(0f, math.PI * 2f);

// 랜덤 거리 (SpawnRadius의 80% ~ 100%)
float distance = spawnConfig.ValueRW.RandomGenerator.NextFloat(
    spawnConfig.ValueRW.SpawnRadius * 0.8f,
    spawnConfig.ValueRW.SpawnRadius
);
```

#### 해결 방법

1. Unity Editor 재시작 (Random 시드 재초기화)
2. `EnemySpawnAuthoring.cs`에서 Random 초기화 확인:
   ```csharp
   RandomGenerator = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks)
   ```
3. Play 모드 재실행

---

## ✅ 검증 체크리스트

### Phase 3 기능 검증

- [ ] **몬스터 스폰 확인**
  - [ ] Play 모드에서 빨간 큐브 생성됨
  - [ ] 2초 간격으로 스폰됨
  - [ ] 플레이어 주변 원형으로 스폰됨
  - [ ] 스폰 위치가 매번 랜덤함

- [ ] **렌더링 확인**
  - [ ] 빨간색 Material 적용됨
  - [ ] Cube 메시로 렌더링됨
  - [ ] Game View 및 Scene View에서 보임

- [ ] **개수 제한 확인**
  - [ ] 최대 50개까지만 스폰됨
  - [ ] 50개 도달 시 더 이상 스폰 안 됨

- [ ] **Entity 구조 확인**
  - [ ] Entity Debugger에서 EnemyTag Entity 확인
  - [ ] EnemyHealth.Value = 100
  - [ ] EnemySpeed.Value = 3
  - [ ] LocalTransform에 위치 값 존재

- [ ] **Console 확인**
  - [ ] Play 모드 실행 중 에러 없음
  - [ ] 경고 메시지 없음

### Phase 1-2 통합 검증

- [ ] **플레이어 이동 (Phase 1)**
  - [ ] WASD로 정상 이동
  - [ ] 몬스터 스폰과 독립적으로 작동

- [ ] **자동 사격 (Phase 2)**
  - [ ] 0.5초 간격으로 총알 발사
  - [ ] 총알이 정상 이동
  - [ ] 5초 후 소멸

### 코드 품질 검증

- [ ] **CLAUDE.md 규칙 준수**
  - [ ] TransformUsageFlags.Renderable | Dynamic 설정됨
  - [ ] RefRW 직접 접근 패턴 사용됨
  - [ ] 로컬 변수 복사 없음

- [ ] **SOLID 원칙**
  - [ ] 각 컴포넌트/시스템이 단일 책임
  - [ ] 컴포넌트 기반 확장 가능

---

## 📊 성능 확인 (선택 사항)

### Profiler 사용

1. `Window` → `Analysis` → `Profiler`
2. Play 모드 실행
3. **CPU Usage** 탭 확인:
   - `EnemySpawnSystem` 실행 시간 확인
   - 2초마다 짧은 스파이크 (정상)

### Stats 창 확인

1. Game View 우상단 **Stats** 버튼 클릭
2. 확인 항목:
   - **Batches**: 몬스터 개수만큼 증가 (정상)
   - **Tris**: 삼각형 개수 (큐브당 12개)
   - **FPS**: 60 유지 (정상)

---

## 🎯 다음 단계

### Phase 3 완료 조건

모든 체크리스트 항목이 ✅ 완료되면 **Phase 3 완료**입니다!

### Phase 4 예고: 몬스터 AI

Phase 3 완료 후 다음 단계:

**Phase 4 목표**:
- 몬스터가 플레이어를 향해 이동
- `EnemySpeed` 컴포넌트 활용
- 간단한 직선 추적 AI

**Phase 5 목표**:
- 총알-몬스터 충돌 감지
- `EnemyHealth` 감소
- 체력 0 시 몬스터 제거

---

## 📝 추가 참고 자료

### Unity ECS 공식 문서

- [Entities 패키지 개요](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Baker 시스템](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/baking.html)
- [SystemAPI 레퍼런스](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.SystemAPI.html)

### 프로젝트 문서

- `Document/phase3_execution_plan_part2.md` - Phase 3 상세 계획
- `CLAUDE.md` - 프로젝트 개발 가이드
- 각 TASK 커밋 메시지 참조

---

**문서 버전**: 1.0
**작성일**: 2025-11-30
**작성자**: Claude Code
**테스트 대상**: Phase 3 (TASK-010 ~ TASK-013)
