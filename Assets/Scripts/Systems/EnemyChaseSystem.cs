using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Enemy가 가장 가까운 플레이어를 추적하면서 서로 겹치지 않도록 하는 시스템
///
/// 최적화 전략:
/// 1. Spatial Hashing - O(N²) → O(N*K) (K = 주변 셀만 체크)
/// 2. 부드러운 분리 - 위치 직접 수정 대신 이동에 반영
/// 3. 최대 체크 수 제한 - 주변 몬스터 최대 8개만 체크
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    // Spatial Hash 설정
    private const float CellSize = 3.0f; // 셀 크기 (분리 반경의 약 2배)
    private const int MaxEnemiesPerCell = 16;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GhostOwner>();
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 게임 일시정지 체크
        foreach (var sessionState in SystemAPI.Query<RefRO<GameSessionState>>())
        {
            if (sessionState.ValueRO.IsGamePaused)
                return;
        }

        float deltaTime = SystemAPI.Time.DeltaTime;

        // 1. 플레이어 위치 수집
        var playerPositions = new NativeList<float3>(Allocator.TempJob);
        foreach (var transform in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<GhostOwner, PlayerHealth>()
                     .WithDisabled<PlayerDead>())
        {
            playerPositions.Add(transform.ValueRO.Position);
        }

        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            return;
        }

        // 2. Enemy 위치 수집
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

        // 3. Spatial Hash Map 생성
        var spatialHashMap = new NativeParallelMultiHashMap<int, int>(enemyCount * 2, Allocator.TempJob);

        // 각 Enemy를 해당 셀에 등록
        for (int i = 0; i < enemyCount; i++)
        {
            int cellKey = GetCellKey(enemyPositions[i]);
            spatialHashMap.Add(cellKey, i);
        }

        // 4. Chase Job 실행
        var jobHandle = new EnemyChaseJob
        {
            AllPlayerPositions = playerPositions.AsArray(),
            DeltaTime = deltaTime,
            AllEnemyPositions = enemyPositions,
            SpatialHashMap = spatialHashMap,
            CellSize = CellSize
        }.ScheduleParallel(state.Dependency);

        // 5. 리소스 정리
        enemyPositions.Dispose(jobHandle);
        playerPositions.Dispose(jobHandle);
        spatialHashMap.Dispose(jobHandle);
        state.Dependency = jobHandle;
    }

    [BurstCompile]
    private static int GetCellKey(float3 position)
    {
        int x = (int)math.floor(position.x / CellSize);
        int z = (int)math.floor(position.z / CellSize);
        // 간단한 해시: 큰 소수를 사용
        return x * 73856093 ^ z * 19349663;
    }
}

[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    [ReadOnly] public NativeArray<float3> AllPlayerPositions;
    [ReadOnly] public NativeArray<float3> AllEnemyPositions;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialHashMap;
    public float DeltaTime;
    public float CellSize;

    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        float3 currentPosition = transform.Position;

        // ===== 1. 가장 가까운 플레이어 찾기 =====
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

        // ===== 2. 공격 범위 체크 =====
        const float attackRange = 1.5f;
        const float attackRangeSq = attackRange * attackRange;
        bool isInAttackRange = closestDistSq <= attackRangeSq;

        // ===== 3. 주변 몬스터와의 분리 계산 (Spatial Hash 사용) =====
        float3 separationOffset = float3.zero;
        const float minDistance = 1.0f; // 몬스터 간 최소 거리
        const float minDistSq = minDistance * minDistance;

        // 현재 셀과 주변 8개 셀 체크
        int currentCellX = (int)math.floor(currentPosition.x / CellSize);
        int currentCellZ = (int)math.floor(currentPosition.z / CellSize);

        int checkedCount = 0;
        const int maxChecks = 8; // 최대 8개 몬스터만 체크

        // 3x3 셀 범위 탐색
        for (int dx = -1; dx <= 1 && checkedCount < maxChecks; dx++)
        {
            for (int dz = -1; dz <= 1 && checkedCount < maxChecks; dz++)
            {
                int cellKey = (currentCellX + dx) * 73856093 ^ (currentCellZ + dz) * 19349663;

                if (SpatialHashMap.TryGetFirstValue(cellKey, out int enemyIndex, out var iterator))
                {
                    do
                    {
                        if (checkedCount >= maxChecks) break;

                        float3 otherPosition = AllEnemyPositions[enemyIndex];
                        float distSq = math.distancesq(currentPosition, otherPosition);

                        // 자기 자신 제외 (거리가 거의 0)
                        if (distSq < 0.0001f)
                            continue;

                        // 최소 거리 이내면 분리
                        if (distSq < minDistSq)
                        {
                            float dist = math.sqrt(distSq);
                            float3 pushDir = currentPosition - otherPosition;
                            pushDir.y = 0;

                            float pushDirLenSq = math.lengthsq(pushDir);
                            if (pushDirLenSq > 0.0001f)
                            {
                                pushDir = pushDir / math.sqrt(pushDirLenSq); // normalize

                                // 겹친 정도에 비례하여 분리 (거리가 가까울수록 강하게)
                                float overlap = minDistance - dist;
                                separationOffset += pushDir * overlap * 0.3f;
                            }

                            checkedCount++;
                        }
                    } while (SpatialHashMap.TryGetNextValue(out enemyIndex, ref iterator) && checkedCount < maxChecks);
                }
            }
        }

        // ===== 4. 회전 처리 (항상 플레이어 바라봄) =====
        float3 lookDirection = targetPlayerPosition - currentPosition;
        lookDirection.y = 0;
        if (math.lengthsq(lookDirection) > 0.01f)
        {
            float3 normalizedLookDir = math.normalize(lookDirection);
            quaternion lookRotation = quaternion.LookRotationSafe(normalizedLookDir, math.up());
            transform.Rotation = math.slerp(transform.Rotation, lookRotation, 4.0f * DeltaTime);
        }

        // ===== 5. 이동 처리 =====
        float3 movement = float3.zero;

        // 공격 범위 밖이면 플레이어 추적
        if (!isInAttackRange)
        {
            float3 chaseDirection = targetPlayerPosition - currentPosition;
            chaseDirection.y = 0;

            if (math.lengthsq(chaseDirection) > 0.01f)
            {
                float3 moveDir = math.normalize(chaseDirection);
                movement = moveDir * speed.Value * DeltaTime;
            }
        }

        // 분리 오프셋 적용 (공격 범위 안팎 모두)
        movement += separationOffset;

        // 최종 이동 적용
        if (math.lengthsq(movement) > 0.0001f)
        {
            transform.Position += movement;
        }
    }
}
