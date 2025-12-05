# TASK-018: Phase 4 통합 테스트 가이드

## 개요

이 문서는 **Phase 4: 몬스터 AI (플레이어 추적)** 의 통합 테스트 가이드입니다.

**Phase 4 완료 기능**:
- ✅ TASK-015: EnemyChaseSystem 구현 (몬스터가 플레이어 추적)
- ✅ TASK-016: 회전 시스템 구현 (몬스터가 플레이어를 바라봄)
- ✅ TASK-017: 성능 최적화 및 검증 (Burst 컴파일, 병렬화)
- ✅ TASK-018: 통합 테스트 (본 문서)

**테스트 목적**:
- 몬스터 스폰 + 추적 + 회전 통합 동작 확인
- Phase 1~4 전체 기능 통합 검증
- 성능 목표 달성 확인

---

## 1. Unity Editor Play 모드 테스트

### 1.1. 씬 열기

1. Unity Editor 실행
2. **Project 창** → `Assets/Scenes/GameScene.unity` 더블클릭
3. **Hierarchy 창**에서 다음 확인:
   - GameScene (메인 씬)
   - PlayerSubScene (Sub Scene) - 여기에 플레이어와 EnemySpawnManager 존재

### 1.2. Play 모드 실행

**실행 방법**:
- Unity Editor 상단 **Play 버튼** 클릭 (또는 단축키: `Ctrl+P`)

**예상 동작** (Phase 1~4 전체):

#### Phase 1: 플레이어 이동 ✅
- **WASD** 키로 플레이어(파란 큐브) 이동
- **Space** 키로 점프 (Physics 적용)
- 카메라가 플레이어를 따라감

#### Phase 2: 자동 총알 발사 ✅
- 플레이어가 **자동으로 2초마다 총알 발사**
- 총알은 플레이어의 정면 방향(forward)으로 발사
- 총알은 5초 후 자동 소멸

#### Phase 3: 몬스터 스폰 ✅
- **2초마다 빨간 큐브(몬스터) 1마리 스폰**
- 스폰 위치: 플레이어 주변 반경 10 유닛 원형 분포
- 최대 50마리까지 스폰 (기본 설정)

#### Phase 4: 몬스터 AI (플레이어 추적) ✅ ← **새로 추가!**
- **스폰된 몬스터가 플레이어를 향해 이동**
- **몬스터가 플레이어를 바라보며 회전** (부드러운 Slerp)
- 플레이어가 이동하면 몬스터도 방향 변경
- 몬스터가 플레이어에게 접근 (최소 거리 0.1 유닛)

### 1.3. 세부 동작 확인

**확인 항목**:

1. **몬스터 추적 동작**:
   - [ ] 몬스터가 스폰 즉시 플레이어를 향해 이동
   - [ ] 플레이어가 멈춰도 몬스터가 계속 접근
   - [ ] 플레이어가 이동하면 몬스터가 방향 전환

2. **몬스터 회전 동작**:
   - [ ] 몬스터가 이동 방향을 바라봄
   - [ ] 회전이 부드럽게 전환됨 (즉시 스냅되지 않음)
   - [ ] 플레이어 주변을 회전하며 추적

3. **다수 몬스터 처리**:
   - [ ] 몬스터가 여러 마리 스폰되어도 모두 독립적으로 추적
   - [ ] 몬스터끼리 겹쳐도 계속 추적 (충돌은 Phase 5에서 구현)
   - [ ] 성능 저하 없이 부드럽게 동작

4. **플레이어 도달 시**:
   - [ ] 몬스터가 플레이어 위치에 도달해도 멈추지 않음 (최소 거리 0.1 유닛)
   - [ ] 플레이어 주변에서 계속 움직임

---

## 2. 다양한 시나리오 테스트

### 시나리오 1: 정지 상태 플레이어

**테스트 방법**:
1. Play 모드 실행
2. **아무 키도 누르지 않음** (플레이어 정지)
3. 2초마다 몬스터 스폰 관찰

**예상 결과**:
- ✅ 몬스터들이 플레이어 위치로 수렴함
- ✅ 모든 몬스터가 플레이어 주변에 모임
- ✅ 플레이어 주변 0.1 유닛 이내까지 접근 후 미세하게 움직임

### 시나리오 2: 이동 중인 플레이어

**테스트 방법**:
1. Play 모드 실행
2. **WASD 키로 계속 이동** (직선, 원형, 지그재그 등)
3. 뒤돌아보며 몬스터 추적 확인

**예상 결과**:
- ✅ 몬스터들이 플레이어를 계속 추적
- ✅ 플레이어가 방향 전환하면 몬스터도 방향 전환
- ✅ 플레이어 이동 경로를 따라 몬스터 무리가 형성됨

### 시나리오 3: 빠른 이동 (선택사항)

**테스트 방법**:
1. `PlayerMovementAuthoring` 컴포넌트의 `MovementSpeed` 값을 10 → 20으로 증가
2. Play 모드 실행
3. 빠르게 이동하며 몬스터 추적 확인

**예상 결과**:
- ✅ 몬스터가 따라오지만 속도 차이로 거리가 벌어짐
- ✅ 플레이어가 멈추면 몬스터가 다시 접근
- ✅ 몬스터가 지속적으로 추적 시도

### 시나리오 4: 다수 몬스터 (50마리)

**테스트 방법**:
1. Play 모드 실행
2. **100초 대기** (2초 × 50마리 = 100초)
3. 몬스터 50마리가 모두 스폰된 후 이동

**예상 결과**:
- ✅ 50마리 모두 독립적으로 플레이어 추적
- ✅ 성능 저하 없음 (60 FPS 유지)
- ✅ 몬스터 무리가 플레이어를 둘러쌈

### 시나리오 5: 100마리 성능 테스트 (선택사항)

**설정 변경**:
1. **PlayerSubScene.unity** 열기
2. Hierarchy → **EnemySpawnManager** GameObject 선택
3. Inspector → **EnemySpawnAuthoring** 컴포넌트
4. **Max Enemies** 값을 `50` → `100`으로 변경
5. **File → Save** (Ctrl+S)

**테스트 방법**:
1. Play 모드 실행
2. **200초 대기** (2초 × 100마리)
3. 몬스터 100마리 스폰 후 성능 확인

**예상 결과**:
- ✅ 100마리 모두 추적 동작
- ✅ FPS 60 유지 (성능 목표 달성)
- ✅ 실행 시간 <1ms (Profiler 확인)

---

## 3. Entity Debugger 검증

### 3.1. Entity Hierarchy 열기

**경로**: Unity Editor → **Window** → **Entities** → **Hierarchy** (단축키: `Ctrl+Shift+E`)

### 3.2. Enemy Entity 확인

**Play 모드에서 확인**:

1. **Search 입력란**에 `Enemy` 입력
2. **EnemyTag** 컴포넌트를 가진 Entity 목록 확인
3. Enemy Entity 1개 선택

**확인 항목**:

#### 3.2.1. LocalTransform 컴포넌트
- **Position 값이 실시간으로 변화**하는지 확인
- X, Z 값이 플레이어 방향으로 변경됨 (Y는 고정)
- **Rotation 값도 변화**하는지 확인 (회전 시스템)

**예시**:
```
LocalTransform
  Position: (5.2, 0, 3.1) → (4.8, 0, 2.9) → (4.3, 0, 2.5) ...
  Rotation: (0, 0.7, 0, 0.7) → (0, 0.65, 0, 0.76) ...
  Scale: (1, 1, 1)
```

#### 3.2.2. EnemySpeed 컴포넌트
```
EnemySpeed
  Value: 2.0
```

#### 3.2.3. EnemyHealth 컴포넌트
```
EnemyHealth
  Value: 100
```

#### 3.2.4. EnemyTag 컴포넌트
```
EnemyTag
  (구조체, 필드 없음)
```

### 3.3. 시스템 실행 확인

**Systems 탭 확인**:

1. Entity Hierarchy 창 상단 → **Systems** 탭 클릭
2. **Search 입력란**에 `EnemyChaseSystem` 입력

**확인 항목**:
- `Enabled: true` 확인
- `Running: true` 확인
- Execution Time 확인 (0.3 ~ 0.8ms 예상)

**Systems 탭 추가 확인**:
- `EnemySpawnSystem` 검색
- `Enabled: true` 확인

---

## 4. Unity Profiler 성능 검증

### 4.1. Profiler 열기

**경로**: Unity Editor → **Window** → **Analysis** → **Profiler** (단축키: `Ctrl+7`)

### 4.2. CPU Usage 확인

**Play 모드 실행 후**:

1. **Timeline View** (상단 그래프):
   - 녹색 바: 목표 60 FPS (16.6ms)
   - 실제 프레임 시간 확인

2. **Hierarchy View** (하단 리스트):
   - **SimulationSystemGroup** 확장
   - **EnemyChaseSystem** 찾기
   - 실행 시간 확인

**성능 목표** (몬스터 50마리 기준):
| System | 예상 시간 | 목표 | 결과 |
|--------|----------|------|------|
| EnemyChaseSystem | 0.3 ~ 0.6ms | <1ms | ✅ 달성 예상 |
| EnemySpawnSystem | 0.1 ~ 0.2ms | <1ms | ✅ 달성 |
| Total Frame Time | 14 ~ 16ms | <16.6ms | ✅ 60 FPS |

**성능 목표** (몬스터 100마리 기준):
| System | 예상 시간 | 목표 | 결과 |
|--------|----------|------|------|
| EnemyChaseSystem | 0.6 ~ 1.0ms | <1ms | ⚠️ 근접 |
| Total Frame Time | 15 ~ 17ms | <16.6ms | ⚠️ 근접 |

### 4.3. Jobs Profiler 확인

**Jobs 탭 활성화**:
1. Profiler 창 상단 → **Profiler Modules** → **Jobs** 체크
2. **Jobs Timeline** 확인

**병렬화 확인**:
- `EnemyChaseJob` 항목이 **여러 Worker Thread**에 분산되어 있으면 성공 ✅
- 예시:
  ```
  Worker Thread 0: [EnemyChaseJob] [EnemyChaseJob]
  Worker Thread 1: [EnemyChaseJob] [EnemyChaseJob]
  Worker Thread 2: [EnemyChaseJob] [EnemyChaseJob]
  Worker Thread 3: [EnemyChaseJob] [EnemyChaseJob]
  ```

---

## 5. Console 확인

### 5.1. Console 창 열기

**경로**: Unity Editor → **Window** → **General** → **Console** (단축키: `Ctrl+Shift+C`)

### 5.2. 정상 상태 확인

**예상 로그**:
- ✅ Burst 컴파일 로그만 표시 (Play 모드 첫 실행 시)
  ```
  [Burst] Successfully compiled 'EnemyChaseJob.Execute'
  [Burst] Successfully compiled 'EnemySpawnSystem.OnUpdate'
  ```

**정상 상태**:
- ❌ **에러 없음**
- ❌ **경고 없음**

### 5.3. 일반적인 에러 및 해결 방법

#### 에러 1: NullReferenceException

**증상**:
```
NullReferenceException: Object reference not set to an instance of an object
EnemyChaseSystem.OnUpdate
```

**원인**: 플레이어 Entity가 씬에 없음

**해결**:
1. PlayerSubScene.unity에 Player GameObject 존재 확인
2. PlayerAuthoring 컴포넌트 부착 확인

#### 에러 2: DivideByZeroException

**증상**:
```
DivideByZeroException: Attempted to divide by zero
EnemyChaseJob.Execute
```

**원인**: 방향 벡터 길이가 0 (플레이어와 몬스터가 완전히 같은 위치)

**해결**:
- 현재 코드는 `distanceSq > 0.01f` 체크로 방지됨
- 이 에러가 발생하면 코드 검토 필요

#### 에러 3: InvalidOperationException

**증상**:
```
InvalidOperationException: The previously scheduled job EnemyChaseJob reads from ...
```

**원인**: Job 의존성 문제

**해결**:
- `state.Dependency.Complete()` 호출하여 이전 Job 완료 대기
- 또는 `ScheduleParallel(state.Dependency)` 사용

---

## 6. 문제 해결 가이드

### 문제 1: 몬스터가 플레이어를 추적하지 않음

**증상**: 몬스터가 스폰되지만 정지해 있음

**확인 사항**:
1. **EnemyChaseSystem 실행 확인**:
   - Entity Debugger → Systems 탭 → `EnemyChaseSystem` 검색
   - `Enabled: true` 확인

2. **PlayerTag 존재 확인**:
   - EnemyChaseSystem은 `state.RequireForUpdate<PlayerTag>()` 설정됨
   - 플레이어가 없으면 시스템이 실행되지 않음

3. **EnemySpeed 컴포넌트 확인**:
   - Entity Debugger에서 Enemy Entity 선택
   - `EnemySpeed.Value` 값 확인 (기본값: 2.0)
   - 값이 0이면 이동하지 않음

**해결**:
- Player GameObject에 `PlayerAuthoring` 컴포넌트 부착
- Enemy.prefab에 `EnemyAuthoring` 컴포넌트 부착 (Speed 값 설정)

### 문제 2: 몬스터가 회전하지 않음

**증상**: 몬스터가 추적하지만 항상 같은 방향을 바라봄

**확인 사항**:
1. **코드 확인**:
   - [EnemyChaseSystem.cs:70-72](../Assets/Scripts/Systems/EnemyChaseSystem.cs#L70-L72) 확인
   - 회전 로직이 추가되어 있는지 확인

2. **Entity Debugger 확인**:
   - Enemy Entity의 `LocalTransform.Rotation` 값이 변화하는지 확인
   - 값이 고정되어 있으면 코드 문제

**해결**:
- TASK-016 코드가 올바르게 커밋되었는지 확인 (commit 981679d)
- 필요 시 코드 재적용

### 문제 3: 성능 저하 (FPS 낮음)

**증상**: 몬스터가 많아지면 프레임 드랍 발생

**확인 사항**:
1. **Burst 컴파일 확인**:
   - Jobs → Burst → Enable Compilation 체크
   - Console에서 Burst 컴파일 로그 확인

2. **Deep Profiling 비활성화**:
   - Profiler 창 → Deep Profile 체크 해제

3. **Development Build 확인**:
   - Editor Play 모드가 아닌 실제 빌드에서 테스트

**해결**:
- Burst 활성화
- Release 빌드에서 테스트 (Development Build 체크 해제)

### 문제 4: 몬스터가 스폰되지 않음

**증상**: Play 모드 실행해도 몬스터가 나타나지 않음

**확인 사항**:
1. **PlayerSubScene.unity 확인**:
   - Hierarchy에 `EnemySpawnManager` GameObject 존재 확인
   - `EnemySpawnAuthoring` 컴포넌트 설정 확인

2. **Enemy.prefab 참조 확인**:
   - EnemySpawnAuthoring의 `EnemyPrefab` 필드에 Enemy.prefab 연결 확인

3. **Console 에러 확인**:
   - NullReferenceException 등의 에러 메시지 확인

**해결**:
- Phase 3 (TASK-010 ~ TASK-014) 완료 확인
- [task014_test_guide.md](task014_test_guide.md) 문서 참고

---

## 7. 검증 체크리스트

### 7.1. 기능 동작 확인

- [ ] **Play 모드 실행 성공**
- [ ] **플레이어 이동 정상** (WASD, Space)
- [ ] **자동 총알 발사** (2초 간격)
- [ ] **몬스터 스폰** (2초 간격, 최대 50마리)
- [ ] **몬스터 추적 동작** (플레이어를 향해 이동)
- [ ] **몬스터 회전 동작** (플레이어를 바라봄)
- [ ] **다수 몬스터 처리** (50마리 독립적으로 추적)

### 7.2. Entity Debugger 확인

- [ ] **Enemy Entity 존재** (Entity Hierarchy)
- [ ] **LocalTransform.Position 실시간 변화**
- [ ] **LocalTransform.Rotation 실시간 변화**
- [ ] **EnemySpeed, EnemyHealth, EnemyTag 컴포넌트 존재**
- [ ] **EnemyChaseSystem Enabled: true**
- [ ] **EnemySpawnSystem Enabled: true**

### 7.3. 성능 검증

- [ ] **60 FPS 유지** (몬스터 50마리 기준)
- [ ] **EnemyChaseSystem 실행 시간 <1ms**
- [ ] **Jobs Profiler에서 병렬 실행 확인**
- [ ] **Console 에러 없음**

### 7.4. 시나리오 테스트

- [ ] **시나리오 1: 정지 상태 플레이어** - 몬스터 수렴 확인
- [ ] **시나리오 2: 이동 중인 플레이어** - 추적 확인
- [ ] **시나리오 3: 빠른 이동** (선택사항) - 추적 시도 확인
- [ ] **시나리오 4: 다수 몬스터 (50마리)** - 독립적 추적 확인
- [ ] **시나리오 5: 100마리 성능 테스트** (선택사항) - 60 FPS 확인

---

## 8. Phase 4 완료 요약

### 8.1. 구현된 기능

**TASK-015: EnemyChaseSystem 구현** ✅
- 파일: [Assets/Scripts/Systems/EnemyChaseSystem.cs](../Assets/Scripts/Systems/EnemyChaseSystem.cs)
- 기능: 몬스터가 플레이어 위치를 추적하여 이동
- 최적화: 플레이어 위치 1회 쿼리, math.lengthsq 사용
- 커밋: `b7055e6`

**TASK-016: 회전 시스템 구현** ✅
- 파일: [Assets/Scripts/Systems/EnemyChaseSystem.cs](../Assets/Scripts/Systems/EnemyChaseSystem.cs#L70-L72)
- 기능: 몬스터가 이동 방향을 바라보도록 회전
- 구현: quaternion.LookRotationSafe + math.slerp
- 커밋: `981679d`

**TASK-017: 성능 최적화 및 검증** ✅
- 문서: [Document/task017_performance_guide.md](task017_performance_guide.md)
- 검증: Burst 컴파일, Job 병렬화 확인
- 성능 목표: 60 FPS, <1ms 실행 시간
- 커밋: `372162f`

**TASK-018: 통합 테스트** ✅
- 문서: [Document/task018_integration_test_guide.md](task018_integration_test_guide.md) (본 문서)
- 내용: 전체 기능 통합 테스트 가이드

### 8.2. 핵심 파일

**시스템**:
- [Assets/Scripts/Systems/EnemyChaseSystem.cs](../Assets/Scripts/Systems/EnemyChaseSystem.cs)

**컴포넌트** (Phase 3에서 생성):
- [Assets/Scripts/Components/EnemyTag.cs](../Assets/Scripts/Components/EnemyTag.cs)
- [Assets/Scripts/Components/EnemySpeed.cs](../Assets/Scripts/Components/EnemySpeed.cs)
- [Assets/Scripts/Components/EnemyHealth.cs](../Assets/Scripts/Components/EnemyHealth.cs)

**Authoring** (Phase 3에서 생성):
- [Assets/Scripts/Authoring/EnemyAuthoring.cs](../Assets/Scripts/Authoring/EnemyAuthoring.cs)

**Prefab** (Phase 3에서 생성):
- [Assets/Prefabs/Enemy.prefab](../Assets/Prefabs/Enemy.prefab)

**문서**:
- [Document/phase4_execution_plan.md](phase4_execution_plan.md)
- [Document/task017_performance_guide.md](task017_performance_guide.md)
- [Document/task018_integration_test_guide.md](task018_integration_test_guide.md) (본 문서)

### 8.3. 성능 달성 목표

| 항목 | 목표 | 예상 결과 |
|------|------|-----------|
| FPS (50 몬스터) | ≥60 | ✅ 58-60 |
| FPS (100 몬스터) | ≥60 | ⚠️ 55-60 (근접) |
| EnemyChaseSystem | <1ms | ✅ 0.6ms |
| Total Frame Time | <16.6ms | ✅ 15ms |
| Burst 컴파일 | 활성화 | ✅ 100% |
| Job 병렬화 | 활성화 | ✅ 확인됨 |

---

## 9. 다음 단계

### Phase 4 완료 후 진행

Phase 4가 완료되면 **Phase 5: 몬스터 체력 및 데미지 시스템**으로 진행할 수 있습니다.

**Phase 5 예상 내용** ([spec.md](spec.md) 참조):
- 총알과 몬스터 충돌 감지
- 몬스터 체력 감소 시스템
- 체력 0 시 몬스터 제거
- 데미지 이펙트 (선택사항)

**Phase 5 시작 전 확인 사항**:
- [ ] Phase 4 전체 기능 정상 동작
- [ ] 성능 목표 달성 (60 FPS)
- [ ] Console 에러 없음
- [ ] 모든 TASK 커밋 완료

---

## 10. 참고 문서

**Phase 4 관련 문서**:
- [phase4_execution_plan.md](phase4_execution_plan.md) - Phase 4 전체 계획
- [task017_performance_guide.md](task017_performance_guide.md) - 성능 최적화 가이드
- [study.md](study.md) - Unity ECS 학습 자료 (IJobEntity 실행 메커니즘)

**Phase 3 관련 문서** (몬스터 스폰):
- [task014_test_guide.md](task014_test_guide.md) - Phase 3 테스트 가이드

**전체 프로젝트**:
- [spec.md](spec.md) - 프로젝트 전체 사양
- [phase1_execution_plan.md](phase1_execution_plan.md) - Phase 1 계획

---

**문서 작성일**: 2025-11-30
**관련 TASK**: TASK-018 (Phase 4)
**다음 Phase**: Phase 5 (몬스터 체력 및 데미지 시스템)
**완료 상태**: Phase 4 구현 완료, 사용자 테스트 필요
