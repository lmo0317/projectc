using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// NetCode 없이 로컬에서 Enemy 추적을 테스트하는 시스템
/// EnemyChaseSystem과 동일한 로직이지만 ServerSimulation 필터 없음
///
/// 활성화하려면: [DisableAutoCreation] 주석 제거/추가
/// </summary>
// [DisableAutoCreation] // 테스트 활성화됨 - 테스트 끝나면 주석 해제
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct LocalEnemyChaseSystem : ISystem
{
    private const float CellSize = 3.0f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemyTag>();
        state.RequireForUpdate<LocalTestPlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 1. 테스트 플레이어 위치 수집
        var playerPositions = new NativeList<float3>(Allocator.TempJob);
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<LocalTestPlayerTag>())
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

        for (int i = 0; i < enemyCount; i++)
        {
            int cellKey = GetCellKey(enemyPositions[i]);
            spatialHashMap.Add(cellKey, i);
        }

        // 4. Chase Job 실행
        var jobHandle = new LocalEnemyChaseJob
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
        return x * 73856093 ^ z * 19349663;
    }
}

[BurstCompile]
public partial struct LocalEnemyChaseJob : IJobEntity
{
    [ReadOnly] public NativeArray<float3> AllPlayerPositions;
    [ReadOnly] public NativeArray<float3> AllEnemyPositions;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialHashMap;
    public float DeltaTime;
    public float CellSize;

    void Execute(ref LocalTransform transform, in EnemySpeed speed, in EnemyTag _)
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

        // 2. 공격 범위 체크
        const float attackRange = 1.5f;
        const float attackRangeSq = attackRange * attackRange;
        bool isInAttackRange = closestDistSq <= attackRangeSq;

        // 3. 주변 몬스터와의 분리 계산
        float3 separationOffset = float3.zero;
        const float minDistance = 1.0f;
        const float minDistSq = minDistance * minDistance;

        int currentCellX = (int)math.floor(currentPosition.x / CellSize);
        int currentCellZ = (int)math.floor(currentPosition.z / CellSize);

        int checkedCount = 0;
        const int maxChecks = 8;

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

                        if (distSq < 0.0001f)
                            continue;

                        if (distSq < minDistSq)
                        {
                            float dist = math.sqrt(distSq);
                            float3 pushDir = currentPosition - otherPosition;
                            pushDir.y = 0;

                            float pushDirLenSq = math.lengthsq(pushDir);
                            if (pushDirLenSq > 0.0001f)
                            {
                                pushDir = pushDir / math.sqrt(pushDirLenSq);
                                float overlap = minDistance - dist;
                                separationOffset += pushDir * overlap * 0.3f;
                            }

                            checkedCount++;
                        }
                    } while (SpatialHashMap.TryGetNextValue(out enemyIndex, ref iterator) && checkedCount < maxChecks);
                }
            }
        }

        // 4. 회전 처리
        float3 lookDirection = targetPlayerPosition - currentPosition;
        lookDirection.y = 0;
        if (math.lengthsq(lookDirection) > 0.01f)
        {
            float3 normalizedLookDir = math.normalize(lookDirection);
            quaternion lookRotation = quaternion.LookRotationSafe(normalizedLookDir, math.up());
            transform.Rotation = math.slerp(transform.Rotation, lookRotation, 4.0f * DeltaTime);
        }

        // 5. 이동 처리
        float3 movement = float3.zero;

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

        movement += separationOffset;

        if (math.lengthsq(movement) > 0.0001f)
        {
            transform.Position += movement;
        }
    }
}

/// <summary>
/// 로컬 테스트용 플레이어 태그
/// </summary>
public struct LocalTestPlayerTag : IComponentData { }
