# 플레이어 방향 제어 및 미사일 타겟팅 개선 - 최종 구현 계획

## 작업 개요

Phase 7 진입 전에 게임플레이 품질을 향상시키기 위한 2가지 개선사항:
1. 플레이어 비행기가 이동 방향을 바라보도록 즉시 회전
2. 미사일이 20 유닛 범위 내 가장 가까운 적을 자동 추적

## 사용자 결정사항

### 설계 결정
1. **플레이어 회전**: 즉시 회전 (입력에 즉각 반응)
2. **타겟팅 범위**: 20 유닛 제한 (너무 먼 적은 타겟 안함)
3. **적 없을 때**: 플레이어가 바라보는 방향으로 발사

### 탐색 완료 사항
- PlayerMovementSystem: IJobEntity 패턴 사용, LocalTransform 직접 수정
- AutoShootSystem: EntityCommandBuffer로 총알 생성, BulletDirection 설정
- 회전 함수: `quaternion.LookRotationSafe(direction, math.up())`
- 거리 계산: `math.distance(pos1, pos2)`

---

## 구현 계획

### TASK-1: PlayerMovementSystem에 방향 제어 추가

**목표**: 플레이어가 이동하는 방향으로 비행기 회전

**수정 파일**: `Assets/Scripts/Systems/PlayerMovementSystem.cs`

**구현 로직**:

```csharp
[BurstCompile]
public partial struct PlayerMovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in PlayerInput input, in MovementSpeed speed)
    {
        // 입력 감지
        if (math.lengthsq(input.Movement) > 0.01f)
        {
            // 1. 입력 방향 정규화 (float2 → float3)
            float2 direction = math.normalize(input.Movement);
            float3 moveDirection = new float3(direction.x, 0, direction.y);

            // 2. 이동 처리 (기존 코드)
            float3 movement = moveDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            // 3. 회전 처리 (새로 추가!)
            // 이동 방향을 바라보도록 즉시 회전
            quaternion targetRotation = quaternion.LookRotationSafe(moveDirection, math.up());
            transform.Rotation = targetRotation;
        }
    }
}
```

**핵심 변경사항**:
- `quaternion.LookRotationSafe(moveDirection, math.up())`: 이동 방향을 앞으로 하는 회전 생성
- `transform.Rotation = targetRotation`: 즉시 회전 (slerp 없음)
- 입력이 있을 때만 회전 (정지 시 방향 유지)

**예상 동작**:
- W 누름 → +Z 방향 바라봄
- A 누름 → -X 방향 바라봄
- S 누름 → -Z 방향 바라봄
- D 누름 → +X 방향 바라봄
- 대각선 이동 → 해당 대각선 방향 바라봄

---

### TASK-2: AutoShootSystem에 타겟팅 로직 추가

**목표**: 20 유닛 범위 내 가장 가까운 적을 타겟하는 미사일 발사

**수정 파일**: `Assets/Scripts/Systems/AutoShootSystem.cs`

**구현 로직**:

```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // 게임 오버 시 발사 중지 (기존 코드)
    if (SystemAPI.HasSingleton<GameOverTag>())
        return;

    float deltaTime = SystemAPI.Time.DeltaTime;

    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    // 플레이어의 발사 처리
    foreach (var (transform, shootConfig, playerTag) in
             SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<PlayerTag>>())
    {
        shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

        if (shootConfig.ValueRW.TimeSinceLastShot >= shootConfig.ValueRW.FireRate)
        {
            shootConfig.ValueRW.TimeSinceLastShot = 0f;

            // === 타겟팅 로직 (새로 추가!) ===
            const float MAX_TARGETING_RANGE = 20f;
            float3 playerPos = transform.ValueRO.Position;
            float3 targetDirection = float3.zero;
            bool targetFound = false;
            float closestDistance = float.MaxValue;

            // 모든 적 탐색
            foreach (var enemyTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
            {
                float3 enemyPos = enemyTransform.ValueRO.Position;
                float distance = math.distance(playerPos, enemyPos);

                // 범위 내 + 가장 가까운 적 찾기
                if (distance <= MAX_TARGETING_RANGE && distance < closestDistance)
                {
                    closestDistance = distance;
                    targetDirection = math.normalize(enemyPos - playerPos);
                    targetFound = true;
                }
            }

            // 타겟 없으면 플레이어가 바라보는 방향
            if (!targetFound)
            {
                // 플레이어의 Forward 방향 추출
                targetDirection = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));
            }
            // === 타겟팅 로직 끝 ===

            // 총알 Entity 생성 (기존 코드)
            var bulletEntity = ecb.Instantiate(shootConfig.ValueRW.BulletPrefab);

            // 총알 위치 설정
            ecb.SetComponent(bulletEntity, LocalTransform.FromPosition(playerPos));

            // 총알 발사 방향 설정 (변경!)
            ecb.SetComponent(bulletEntity, new BulletDirection { Value = targetDirection });
        }
    }
}
```

**핵심 변경사항**:
1. **MAX_TARGETING_RANGE = 20f**: 타겟팅 최대 거리
2. **적 탐색 루프**: 모든 `EnemyTag` Entity 순회
3. **거리 체크**: `distance <= MAX_TARGETING_RANGE`로 범위 제한
4. **가장 가까운 적 선택**: `closestDistance` 추적
5. **방향 계산**: `math.normalize(enemyPos - playerPos)`
6. **타겟 없을 때**: `math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1))`로 플레이어 앞 방향

**예상 동작**:
- 20 유닛 내 적 있음 → 가장 가까운 적 향해 발사
- 20 유닛 내 적 없음 → 플레이어가 바라보는 방향으로 발사
- 적 여러 마리 → 가장 가까운 적만 타겟

---

## 수정 파일 목록

1. **PlayerMovementSystem.cs**
   - 위치: `Assets/Scripts/Systems/PlayerMovementSystem.cs`
   - 변경: `PlayerMovementJob.Execute()` 메서드에 회전 로직 추가

2. **AutoShootSystem.cs**
   - 위치: `Assets/Scripts/Systems/AutoShootSystem.cs`
   - 변경: `OnUpdate()` 메서드에 타겟팅 로직 추가

## 새로 생성할 파일

없음 (기존 시스템만 수정)

---

## 검증 계획

### 1. 플레이어 방향 검증
- [ ] Unity Editor Play 모드 실행
- [ ] W 누르고 이동 → 비행기가 +Z(위) 방향 바라봄
- [ ] A 누르고 이동 → 비행기가 -X(왼쪽) 방향 바라봄
- [ ] S 누르고 이동 → 비행기가 -Z(아래) 방향 바라봄
- [ ] D 누르고 이동 → 비행기가 +X(오른쪽) 방향 바라봄
- [ ] 대각선 이동(WD) → 비행기가 대각선 방향 바라봄
- [ ] 입력 멈춤 → 마지막 방향 유지

### 2. 미사일 타겟팅 검증
- [ ] 적이 20 유닛 내에 있을 때 → 미사일이 가장 가까운 적 향해 발사
- [ ] 적이 20 유닛 밖에 있을 때 → 미사일이 플레이어가 바라보는 방향으로 발사
- [ ] 적이 없을 때 → 미사일이 플레이어가 바라보는 방향으로 발사
- [ ] 적 여러 마리 → 가장 가까운 적만 타겟됨
- [ ] Scene 뷰에서 미사일 궤적 확인

### 3. 성능 검증
- [ ] Profiler에서 AutoShootSystem CPU 사용량 확인
- [ ] 적 50마리 + 총알 100발 상황에서 60 FPS 유지
- [ ] Burst 컴파일 정상 작동 확인

---

## 예상 이슈 및 해결방법

### 이슈 1: 플레이어가 떨림
**원인**: 입력이 미세하게 변할 때 회전이 민감하게 반응
**해결**: 입력 임계값 확인 (`math.lengthsq(input.Movement) > 0.01f`는 이미 적용됨)

### 이슈 2: 타겟팅 범위가 너무 짧음/김
**원인**: MAX_TARGETING_RANGE 값 부적절
**해결**: 상수 값 조정 (20f → 15f 또는 25f)

### 이슈 3: 성능 저하
**원인**: 매 발사마다 모든 적 순회
**해결**: 현재는 0.5초마다 발사하므로 문제 없을 것으로 예상. 필요 시 범위 쿼리 최적화

### 이슈 4: 타겟 없을 때 이상한 방향
**원인**: `math.mul(rotation, forward)` 계산 오류
**해결**: 코드 재확인 또는 기본값 `new float3(0, 0, 1)` 사용

---

## SOLID 원칙 준수

### Single Responsibility (단일 책임)
- ✅ PlayerMovementSystem: 이동 + 회전 담당 (밀접한 관계)
- ✅ AutoShootSystem: 발사 + 타겟팅 담당 (밀접한 관계)

### Open/Closed (개방/폐쇄)
- ✅ MAX_TARGETING_RANGE를 상수로 정의 → 쉽게 조정 가능
- ✅ 새로운 타겟팅 로직 추가 시 확장 가능

### Liskov Substitution (리스코프 치환)
- ✅ IJobEntity, ISystem 인터페이스 올바르게 구현

### Interface Segregation (인터페이스 분리)
- ✅ 각 시스템은 필요한 컴포넌트만 쿼리

### Dependency Inversion (의존성 역전)
- ✅ 시스템은 구체적 구현이 아닌 컴포넌트에 의존

---

## 구현 순서

1. **PlayerMovementSystem 수정** (5-10분)
   - PlayerMovementJob.Execute()에 회전 로직 3줄 추가
   - 컴파일 확인

2. **AutoShootSystem 수정** (10-15분)
   - OnUpdate()에 타겟팅 로직 추가
   - 컴파일 확인

3. **Unity Editor 테스트** (10-15분)
   - Play 모드에서 플레이어 방향 확인
   - 미사일 타겟팅 동작 확인

4. **밸런싱 조정** (선택, 5-10분)
   - MAX_TARGETING_RANGE 값 조정
   - 게임플레이 체감 확인

**총 예상 시간**: 30-50분

---

## 완료 조건

- [x] 코드베이스 탐색 완료
- [x] 사용자 설계 결정 확인
- [ ] PlayerMovementSystem 수정
- [ ] AutoShootSystem 수정
- [ ] 컴파일 에러 없음
- [ ] 플레이어 이동 방향 회전 정상 작동
- [ ] 미사일 타겟팅 정상 작동 (범위 내 적 추적)
- [ ] 타겟 없을 때 플레이어 방향 발사 확인
- [ ] 성능 저하 없음 (60 FPS 유지)
- [ ] Burst 컴파일 정상
