using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Enemy가 가장 가까운 플레이어를 추적하면서 서로 겹치지 않도록 하는 시스템
/// - 다중 플레이어 지원 (가장 가까운 플레이어 추적)
/// - Enemy 간 충돌 회피 (Separation)
/// - Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 플레이어와 적이 모두 존재할 때만 실행
        state.RequireForUpdate<GhostOwner>(); // 네트워크 플레이어 확인
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 버프 선택 중이면 Enemy 이동 중지
        foreach (var sessionState in SystemAPI.Query<RefRO<GameSessionState>>())
        {
            if (sessionState.ValueRO.IsGamePaused)
                return;
        }

        float deltaTime = SystemAPI.Time.DeltaTime;

        // 1단계: 살아있는 플레이어 위치 수집 (PlayerDead가 비활성화된 플레이어만)
        var playerPositions = new NativeList<float3>(Allocator.TempJob);
        foreach (var transform in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<GhostOwner, PlayerHealth>()
                     .WithDisabled<PlayerDead>())
        {
            playerPositions.Add(transform.ValueRO.Position);
        }

        // 살아있는 플레이어가 없으면 추적하지 않음
        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            return;
        }

        // 2단계: 모든 Enemy의 위치를 수집
        var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag, LocalTransform>().Build();
        int enemyCount = enemyQuery.CalculateEntityCount();

        if (enemyCount == 0)
        {
            playerPositions.Dispose();
            return;
        }

        var enemyPositions = new NativeArray<float3>(enemyCount, Allocator.TempJob);

        int index = 0;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
        {
            enemyPositions[index] = transform.ValueRO.Position;
            index++;
        }

        // 3단계: 가장 가까운 플레이어 추적 + 충돌 회피 로직 실행
        var jobHandle = new EnemyChaseJob
        {
            AllPlayerPositions = playerPositions.AsArray(),
            DeltaTime = deltaTime,
            AllEnemyPositions = enemyPositions
        }.ScheduleParallel(state.Dependency);

        // 4단계: Job 완료 후 NativeArray 정리
        // 최적화: Complete() 대신 의존성 체인으로 처리
        enemyPositions.Dispose(jobHandle);
        playerPositions.Dispose(jobHandle);
        state.Dependency = jobHandle;
    }
}

[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    [ReadOnly] public NativeArray<float3> AllPlayerPositions;
    public float DeltaTime;
    [ReadOnly] public NativeArray<float3> AllEnemyPositions;

    void Execute(ref LocalTransform transform, in EnemySpeed speed, ref EnemyMovementState movementState)
    {
        float3 currentPosition = transform.Position;

        // 1. 가장 가까운 플레이어 찾기
        float3 targetPlayerPosition = float3.zero;
        float closestDistSq = float.MaxValue;

        for (int i = 0; i < AllPlayerPositions.Length; i++)
        {
            float distSq = math.distancesq(currentPosition, AllPlayerPositions[i]);
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                targetPlayerPosition = AllPlayerPositions[i];
            }
        }

        // 플레이어와의 공격 범위 (이 범위 안에 들어오면 이동 중지)
        float attackRange = 1.5f;
        float attackRangeSq = attackRange * attackRange;

        // 플레이어와 충분히 가까우면 이동하지 않고 제자리 유지
        if (closestDistSq <= attackRangeSq)
        {
            // 플레이어를 바라보는 방향만 유지 (회전만 수행)
            float3 lookDirection = targetPlayerPosition - currentPosition;
            lookDirection.y = 0;

            if (math.lengthsq(lookDirection) > 0.01f)
            {
                float3 normalizedLookDir = math.normalize(lookDirection);
                quaternion targetRotation = quaternion.LookRotationSafe(normalizedLookDir, math.up());

                // 부드러운 회전
                transform.Rotation = math.slerp(transform.Rotation, targetRotation, 5.0f * DeltaTime);

                // 이동 상태 업데이트 (방향 저장)
                movementState.PreviousDirection = normalizedLookDir;
                movementState.PreviousTargetRotation = targetRotation;
                movementState.IsInitialized = true;
            }

            return; // 이동하지 않고 종료
        }

        // 2. 가장 가까운 플레이어를 향하는 방향 계산 (정규화)
        float3 chaseDirection = targetPlayerPosition - currentPosition;
        chaseDirection.y = 0;
        float3 normalizedChaseDirection = float3.zero;
        if (math.lengthsq(chaseDirection) > 0.01f)
        {
            normalizedChaseDirection = math.normalize(chaseDirection);
        }

        // 3. 다른 Enemy들로부터 떨어지는 방향 계산 (Separation)
        // 최적화: 모든 Enemy를 체크하지 않고 샘플링 (최대 20개만 체크)
        float3 separationDirection = float3.zero;
        float separationRadius = 3.0f; // 충돌 회피 반경 (Enemy 간 최소 거리) - 5.0에서 3.0으로 감소
        float separationRadiusSq = separationRadius * separationRadius; // 제곱 거리로 미리 계산
        int nearbyCount = 0;

        int totalEnemies = AllEnemyPositions.Length;
        int maxChecks = math.min(20, totalEnemies); // 최대 20개만 체크
        int step = math.max(1, totalEnemies / maxChecks); // 샘플링 간격

        for (int i = 0; i < totalEnemies; i += step)
        {
            float3 otherPosition = AllEnemyPositions[i];

            // 자기 자신은 제외
            float distSq = math.distancesq(currentPosition, otherPosition);
            if (distSq < 0.01f)
                continue;

            // 일정 반경 내의 다른 Enemy가 있으면 분리 힘 적용
            // 최적화: 제곱 거리로 비교 (sqrt 연산 제거)
            if (distSq < separationRadiusSq)
            {
                float distance = math.sqrt(distSq); // 필요한 경우만 sqrt 계산

                // 다른 Enemy로부터 멀어지는 방향
                float3 awayDirection = currentPosition - otherPosition;
                awayDirection.y = 0;

                // 거리가 가까울수록 강한 분리 힘 (선형으로 변경 - 더 부드러운 분리)
                float strength = 1.0f - (distance / separationRadius);
                // 제곱 제거: 급격한 힘 변화 방지

                if (math.lengthsq(awayDirection) > 0.01f)
                {
                    separationDirection += math.normalize(awayDirection) * strength;
                    nearbyCount++;
                }
            }
        }

        // 평균 분리 방향 계산
        if (nearbyCount > 0)
        {
            separationDirection /= nearbyCount;
        }

        // 4. 최종 이동 방향 = 추적 방향 + 분리 방향 (둘 다 정규화된 상태)
        // 분리 가중치 감소: 5.0 → 1.5 (추적 방향 우선, 부드러운 회피)
        float3 rawFinalDirection = normalizedChaseDirection + separationDirection * 1.5f;
        rawFinalDirection.y = 0;

        float distanceSq = math.lengthsq(rawFinalDirection);

        if (distanceSq > 0.01f)
        {
            float3 normalizedRawDirection = math.normalize(rawFinalDirection);

            // 5. 방향 스무딩 - 이전 방향과 보간하여 급격한 변화 방지
            float3 smoothedDirection;
            if (movementState.IsInitialized)
            {
                // 방향 보간 (lerp factor 0.1 = 매우 부드러운 전환)
                // 값이 작을수록 더 부드럽게 전환됨
                float directionSmoothFactor = 3.0f * DeltaTime; // 프레임 독립적 스무딩
                smoothedDirection = math.lerp(movementState.PreviousDirection, normalizedRawDirection, directionSmoothFactor);

                // 보간 후 재정규화 (길이가 약간 변할 수 있음)
                if (math.lengthsq(smoothedDirection) > 0.01f)
                {
                    smoothedDirection = math.normalize(smoothedDirection);
                }
                else
                {
                    smoothedDirection = normalizedRawDirection;
                }
            }
            else
            {
                // 첫 프레임: 초기화
                smoothedDirection = normalizedRawDirection;
                movementState.IsInitialized = true;
            }

            // 스무딩된 방향 저장
            movementState.PreviousDirection = smoothedDirection;

            // 6. 스무딩된 방향으로 이동
            float3 movement = smoothedDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            // 7. 회전도 스무딩 - 목표 회전을 저장하고 매우 부드럽게 보간
            quaternion targetRotation = quaternion.LookRotationSafe(smoothedDirection, math.up());

            // 회전 스무딩 (이전 목표 회전과 현재 목표 회전 사이를 보간)
            if (movementState.IsInitialized && !movementState.PreviousTargetRotation.Equals(quaternion.identity))
            {
                // 목표 회전 자체를 스무딩
                targetRotation = math.slerp(movementState.PreviousTargetRotation, targetRotation, 3.0f * DeltaTime);
            }
            movementState.PreviousTargetRotation = targetRotation;

            // 현재 회전에서 스무딩된 목표 회전으로 부드럽게 전환
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 3.0f * DeltaTime);
        }
    }
}
