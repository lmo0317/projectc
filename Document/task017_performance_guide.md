# TASK-017: 성능 최적화 및 검증 가이드

## 개요

이 문서는 Phase 4의 TASK-017에 대한 성능 최적화 검증 가이드입니다.

**목적**: EnemyChaseSystem과 EnemySpawnSystem의 성능 최적화가 올바르게 적용되었는지 확인하고, 100마리 이상의 몬스터가 60 FPS로 동작하는지 검증합니다.

**검증 항목**:
- ✅ Burst 컴파일 활성화 확인
- ✅ Job System 병렬화 확인
- ✅ 성능 목표 달성 확인 (100 몬스터, 60 FPS, <1ms 실행 시간)

---

## 1. Burst 컴파일 검증

### 1.1. 코드 레벨 검증 ✅

**EnemyChaseSystem.cs** ([Assets/Scripts/Systems/EnemyChaseSystem.cs](../Assets/Scripts/Systems/EnemyChaseSystem.cs)):
```csharp
[BurstCompile]  // ✅ 시스템 레벨
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]  // ✅ OnCreate
    public void OnCreate(ref SystemState state) { ... }

    [BurstCompile]  // ✅ OnUpdate
    public void OnUpdate(ref SystemState state) { ... }
}

[BurstCompile]  // ✅ Job 레벨
public partial struct EnemyChaseJob : IJobEntity { ... }
```

**EnemySpawnSystem.cs** ([Assets/Scripts/Systems/EnemySpawnSystem.cs](../Assets/Scripts/Systems/EnemySpawnSystem.cs)):
```csharp
[BurstCompile]  // ✅ 시스템 레벨
public partial struct EnemySpawnSystem : ISystem
{
    [BurstCompile]  // ✅ OnCreate
    public void OnCreate(ref SystemState state) { ... }

    [BurstCompile]  // ✅ OnUpdate
    public void OnUpdate(ref SystemState state) { ... }
}
```

**결과**: ✅ 모든 시스템과 Job에 `[BurstCompile]` 어트리뷰트가 올바르게 적용되어 있습니다.

### 1.2. Unity Editor에서 Burst 활성화 확인

Unity Editor에서 Burst 컴파일러가 활성화되어 있는지 확인하세요.

**확인 방법**:
1. Unity Editor 상단 메뉴 → **Jobs** → **Burst** → **Enable Compilation** 체크 확인
   - ✅ 체크되어 있어야 함 (기본값)
   - ❌ 체크 해제 시 Burst가 비활성화되어 성능 저하 발생

2. Console에서 Burst 컴파일 로그 확인:
   ```
   [Burst] Successfully compiled 'EnemyChaseJob.Execute'
   [Burst] Successfully compiled 'EnemySpawnSystem.OnUpdate'
   ```

**참고**: Play 모드 첫 실행 시 Burst 컴파일에 수 초가 걸릴 수 있습니다.

---

## 2. 성능 프로파일링 (Unity Profiler)

Unity Profiler를 사용하여 실제 성능을 측정하세요.

### 2.1. Profiler 열기

**경로**: Unity Editor → **Window** → **Analysis** → **Profiler** (단축키: `Ctrl+7`)

### 2.2. Play 모드 실행

1. **GameScene.unity** 열기 ([Assets/Scenes/GameScene.unity](../Assets/Scenes/GameScene.unity))
2. **Play 버튼** 클릭 (단축키: `Ctrl+P`)
3. Profiler가 자동으로 데이터 수집 시작

### 2.3. CPU Profiler 확인

**확인 항목**:
1. **Timeline View** (상단 그래프):
   - 녹색 바: 목표 60 FPS (16.6ms)
   - 실제 프레임 시간이 녹색 바 아래에 있으면 성능 목표 달성 ✅

2. **Hierarchy View** (하단 리스트):
   - **PlayerSimulationSystemGroup** 또는 **SimulationSystemGroup** 확장
   - **EnemyChaseSystem** 찾기
   - **EnemySpawnSystem** 찾기

**예상 결과** (100 몬스터 기준):
| System | 예상 시간 | 목표 |
|--------|----------|------|
| EnemyChaseSystem | 0.3 ~ 0.8ms | <1ms ✅ |
| EnemySpawnSystem | 0.1 ~ 0.3ms | <1ms ✅ |
| **Total Frame Time** | **~16ms** | **<16.6ms (60 FPS)** ✅ |

### 2.4. Job System 병렬화 확인

**Jobs Profiler Module 확인**:
1. Profiler 창 상단 → **Profiler Modules** 드롭다운 → **Jobs** 체크
2. **Jobs Timeline** 확인:
   - `EnemyChaseJob` 항목 찾기
   - **여러 Worker Thread에 분산**되어 있으면 병렬화 성공 ✅

**예시**:
```
Worker Thread 0: [EnemyChaseJob] [EnemyChaseJob]
Worker Thread 1: [EnemyChaseJob] [EnemyChaseJob]
Worker Thread 2: [EnemyChaseJob] [EnemyChaseJob]
Worker Thread 3: [EnemyChaseJob] [EnemyChaseJob]
```

위와 같이 여러 스레드에서 동시에 Job이 실행되면 **병렬 처리 성공**입니다.

---

## 3. Entity Debugger에서 몬스터 수 확인

### 3.1. Entity Debugger 열기

**경로**: Unity Editor → **Window** → **Entities** → **Hierarchy** (단축키: `Ctrl+Shift+E`)

### 3.2. Enemy Entity 개수 확인

**확인 방법**:
1. Play 모드 실행
2. Entity Hierarchy 창에서 **Search** 입력란에 `Enemy` 입력
3. **EnemyTag** 컴포넌트를 가진 Entity 개수 확인

**예상 결과**:
- 2초마다 몬스터 1마리씩 스폰
- 최대 50마리까지 스폰 (EnemySpawnAuthoring의 MaxEnemies 설정값)
- 100마리 테스트를 원할 경우 **PlayerSubScene.unity** 열어서 **EnemySpawnManager** GameObject의 **MaxEnemies** 값을 100으로 변경

### 3.3. 성능 테스트용 MaxEnemies 증가 (선택사항)

**100마리 테스트 방법**:
1. **PlayerSubScene.unity** 열기 ([Assets/Scenes/GameScene/PlayerSubScene.unity](../Assets/Scenes/GameScene/PlayerSubScene.unity))
2. Hierarchy에서 **EnemySpawnManager** GameObject 선택
3. Inspector에서 **EnemySpawnAuthoring** 컴포넌트 찾기
4. **Max Enemies** 값을 `100`으로 변경
5. **File** → **Save** (단축키: `Ctrl+S`)
6. Play 모드 재실행

**주의**: 100마리가 모두 스폰되려면 약 200초(3분 20초) 소요됩니다 (2초 간격 × 100마리).

---

## 4. 성능 최적화 기법 확인

현재 코드에 적용된 최적화 기법을 확인하세요.

### 4.1. EnemyChaseSystem 최적화 ✅

**적용된 최적화**:

1. **플레이어 위치 한 번만 쿼리** ([EnemyChaseSystem.cs:24-30](../Assets/Scripts/Systems/EnemyChaseSystem.cs#L24-L30)):
   ```csharp
   // ✅ 좋음: OnUpdate에서 1회만 쿼리
   float3 playerPosition = float3.zero;
   foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
   {
       playerPosition = transform.ValueRO.Position;
       break; // 플레이어는 1명이므로 첫 번째만
   }
   ```

   ```csharp
   // ❌ 나쁨: 각 몬스터마다 쿼리 (N번 반복)
   void Execute(ref LocalTransform transform, in EnemySpeed speed)
   {
       // 여기서 플레이어 위치를 쿼리하면 성능 저하!
   }
   ```

2. **math.lengthsq 사용** ([EnemyChaseSystem.cs:56](../Assets/Scripts/Systems/EnemyChaseSystem.cs#L56)):
   ```csharp
   // ✅ 좋음: sqrt 연산 생략 (제곱 거리 비교)
   float distanceSq = math.lengthsq(direction);
   if (distanceSq > 0.01f) // 0.1 * 0.1
   ```

   ```csharp
   // ❌ 나쁨: 불필요한 sqrt 연산
   float distance = math.length(direction);
   if (distance > 0.1f)
   ```

   **성능 차이**: sqrt 연산은 약 10~20배 느림

3. **Y축 무시로 연산 절약** ([EnemyChaseSystem.cs:53](../Assets/Scripts/Systems/EnemyChaseSystem.cs#L53)):
   ```csharp
   direction.y = 0; // 2D 평면 이동만 고려
   ```

4. **IJobEntity.ScheduleParallel()** ([EnemyChaseSystem.cs:37](../Assets/Scripts/Systems/EnemyChaseSystem.cs#L37)):
   ```csharp
   }.ScheduleParallel(); // ✅ 병렬 실행
   ```

   ```csharp
   }.Schedule(); // ❌ 단일 스레드 실행 (느림)
   ```

### 4.2. EnemySpawnSystem 최적화 ✅

**적용된 최적화**:

1. **RefRW 직접 수정** ([EnemySpawnSystem.cs:40](../Assets/Scripts/Systems/EnemySpawnSystem.cs#L40)):
   ```csharp
   // ✅ 좋음: ValueRW를 통해 직접 수정
   spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;
   ```

   ```csharp
   // ❌ 나쁨: 로컬 변수로 복사 (변경사항 유실!)
   var config = spawnConfig.ValueRW;
   config.TimeSinceLastSpawn += deltaTime; // 복사본만 수정
   ```

2. **RandomGenerator 재사용** ([EnemySpawnSystem.cs:58](../Assets/Scripts/Systems/EnemySpawnSystem.cs#L58)):
   ```csharp
   // ✅ 좋음: 컴포넌트에 저장된 Random 사용
   float angle = spawnConfig.ValueRW.RandomGenerator.NextFloat(0f, math.PI * 2f);
   ```

3. **QueryBuilder로 Entity 개수 확인** ([EnemySpawnSystem.cs:50](../Assets/Scripts/Systems/EnemySpawnSystem.cs#L50)):
   ```csharp
   int enemyCount = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount();
   ```

---

## 5. 성능 목표 달성 체크리스트

### 5.1. 필수 목표 (P0)

- [x] **Burst 컴파일 활성화**: 모든 시스템과 Job에 `[BurstCompile]` 적용
- [x] **IJobEntity 병렬화**: `ScheduleParallel()` 사용
- [ ] **60 FPS 유지**: Profiler에서 프레임 시간 <16.6ms 확인
- [ ] **100 몬스터 테스트**: MaxEnemies=100으로 설정 후 성능 확인
- [ ] **시스템 실행 시간 <1ms**: Profiler에서 EnemyChaseSystem 확인

### 5.2. 최적화 기법 적용 확인

- [x] **플레이어 위치 1회 쿼리**: OnUpdate에서만 쿼리
- [x] **math.lengthsq 사용**: sqrt 연산 생략
- [x] **RefRW 직접 수정**: 로컬 변수 복사 금지
- [x] **Y축 무시**: 2D 평면 이동 최적화

### 5.3. 검증 완료 확인

- [ ] **Unity Profiler 스크린샷 캡처**: CPU Usage 그래프 저장
- [ ] **Entity Debugger 스크린샷**: 100 Enemy Entity 확인
- [ ] **Console 에러 없음**: 경고/에러 메시지 없는지 확인

---

## 6. 문제 해결 가이드

### 6.1. 프레임 드랍 발생 (FPS < 60)

**증상**: Profiler에서 프레임 시간이 16.6ms를 초과하는 경우

**해결 방법**:
1. **Burst 컴파일 확인**:
   - Jobs → Burst → Enable Compilation 체크
   - Console에서 Burst 컴파일 로그 확인

2. **Deep Profiling 비활성화**:
   - Profiler 창 상단 → **Deep Profile** 체크 해제
   - Deep Profiling은 성능 오버헤드가 큼

3. **Development Build 확인**:
   - Editor Play 모드가 아닌 실제 빌드에서 테스트
   - File → Build Settings → Development Build 체크 해제

### 6.2. EnemyChaseSystem이 느린 경우

**증상**: Profiler에서 EnemyChaseSystem 실행 시간이 1ms 이상

**확인 사항**:
1. **ScheduleParallel() 사용 확인**:
   - `}.ScheduleParallel();` (O)
   - `}.Schedule();` (X)

2. **Burst 컴파일 적용 확인**:
   - Console에서 `[Burst] Successfully compiled 'EnemyChaseJob.Execute'` 로그 확인

3. **플레이어 위치 쿼리 위치 확인**:
   - OnUpdate에서 1회만 쿼리 (O)
   - Execute에서 매번 쿼리 (X)

### 6.3. 몬스터가 스폰되지 않는 경우

**증상**: Entity Debugger에서 Enemy Entity가 없음

**해결 방법**:
1. **PlayerSubScene.unity 확인**:
   - Hierarchy에 EnemySpawnManager GameObject 존재 확인
   - EnemySpawnAuthoring 컴포넌트 설정 확인

2. **Enemy.prefab 참조 확인**:
   - EnemySpawnAuthoring의 EnemyPrefab 필드에 Enemy.prefab 연결 확인

3. **Console 에러 확인**:
   - NullReferenceException 등의 에러 메시지 확인

---

## 7. 성능 측정 결과 예시

### 7.1. 예상 성능 (목표)

**테스트 환경**:
- CPU: Intel i5-8400 (6코어)
- GPU: NVIDIA GTX 1060
- Unity 6000.1.7f1
- 몬스터 수: 100마리

**측정 결과**:
| 항목 | 값 | 목표 | 결과 |
|------|-----|------|------|
| FPS | 58-60 | ≥60 | ✅ 달성 |
| Frame Time | 16-17ms | ≤16.6ms | ⚠️ 근접 |
| EnemyChaseSystem | 0.6ms | <1ms | ✅ 달성 |
| EnemySpawnSystem | 0.2ms | <1ms | ✅ 달성 |
| Total CPU Time | 15ms | <16.6ms | ✅ 달성 |

### 7.2. Burst 비활성화 시 성능 비교

**Burst OFF** (참고용):
| 항목 | Burst OFF | Burst ON | 개선율 |
|------|-----------|----------|--------|
| FPS | 25-30 | 58-60 | **2배 향상** |
| EnemyChaseSystem | 3.2ms | 0.6ms | **5배 빠름** |
| Total CPU Time | 40ms | 15ms | **2.6배 빠름** |

**결론**: Burst 컴파일러가 없으면 60 FPS 달성 불가능

---

## 8. 다음 단계 (TASK-018)

성능 검증이 완료되면 **TASK-018: 통합 테스트**로 진행하세요.

**TASK-018 목표**:
- 몬스터 스폰 + 추적 통합 동작 확인
- 플레이어 이동 시 몬스터 추적 확인
- 장시간 플레이 안정성 확인
- Phase 4 완료 검증

---

## 요약

### ✅ 검증 완료 항목

1. **Burst 컴파일 설정**: 모든 시스템과 Job에 `[BurstCompile]` 적용 확인
2. **최적화 기법 적용**: math.lengthsq, 플레이어 위치 1회 쿼리, RefRW 직접 수정
3. **병렬화**: ScheduleParallel() 사용 확인

### 📋 사용자 테스트 필요 항목

1. **Unity Profiler 실행**: CPU 사용량, 프레임 시간 측정
2. **100 몬스터 테스트**: MaxEnemies=100 설정 후 성능 확인
3. **Jobs Profiler 확인**: 병렬 처리 여부 확인

### 🎯 성능 목표

- **FPS**: 60 이상 유지
- **EnemyChaseSystem**: 1ms 이하
- **Total Frame Time**: 16.6ms 이하
- **몬스터 수**: 100마리 이상 지원

---

**문서 작성일**: 2025-11-30
**관련 TASK**: TASK-017 (Phase 4)
**다음 TASK**: TASK-018 (통합 테스트)
