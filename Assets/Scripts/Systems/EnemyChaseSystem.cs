using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Enemy가 플레이어를 추적하면서 서로 겹치지 않도록 하는 시스템
/// - 플레이어 추적 (Chase)
/// - Enemy 간 충돌 회피 (Separation)
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 플레이어와 적이 모두 존재할 때만 실행
        state.RequireForUpdate<PlayerTag>();
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 플레이어 위치 쿼리 (한 번만)
        float3 playerPosition = float3.zero;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPosition = transform.ValueRO.Position;
            break; // 플레이어는 1명이므로 첫 번째만
        }

        // 1단계: 모든 Enemy의 위치를 수집
        var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag, LocalTransform>().Build();
        int enemyCount = enemyQuery.CalculateEntityCount();

        if (enemyCount == 0) return;

        var enemyPositions = new NativeArray<float3>(enemyCount, Allocator.TempJob);

        int index = 0;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
        {
            enemyPositions[index] = transform.ValueRO.Position;
            index++;
        }

        // 2단계: 플레이어 추적 + 충돌 회피 로직 실행
        new EnemyChaseJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = deltaTime,
            AllEnemyPositions = enemyPositions
        }.ScheduleParallel();

        // 3단계: Job 완료 후 NativeArray 정리
        state.Dependency.Complete();
        enemyPositions.Dispose();
    }
}

[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;
    [ReadOnly] public NativeArray<float3> AllEnemyPositions;

    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        float3 currentPosition = transform.Position;

        // 1. 플레이어를 향하는 방향 계산 (정규화)
        float3 chaseDirection = PlayerPosition - currentPosition;
        chaseDirection.y = 0;
        float3 normalizedChaseDirection = float3.zero;
        if (math.lengthsq(chaseDirection) > 0.01f)
        {
            normalizedChaseDirection = math.normalize(chaseDirection);
        }

        // 2. 다른 Enemy들로부터 떨어지는 방향 계산 (Separation)
        float3 separationDirection = float3.zero;
        float separationRadius = 5.0f; // 충돌 회피 반경 증가 (Enemy 간 최소 거리)
        int nearbyCount = 0;

        for (int i = 0; i < AllEnemyPositions.Length; i++)
        {
            float3 otherPosition = AllEnemyPositions[i];

            // 자기 자신은 제외
            if (math.distancesq(currentPosition, otherPosition) < 0.01f)
                continue;

            float distance = math.distance(currentPosition, otherPosition);

            // 일정 반경 내의 다른 Enemy가 있으면 분리 힘 적용
            if (distance < separationRadius)
            {
                // 다른 Enemy로부터 멀어지는 방향
                float3 awayDirection = currentPosition - otherPosition;
                awayDirection.y = 0;

                // 거리가 가까울수록 강한 분리 힘 (제곱으로 증가)
                float strength = 1.0f - (distance / separationRadius);
                strength = strength * strength; // 가까울수록 훨씬 강한 힘

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

        // 3. 최종 이동 방향 = 추적 방향 + 분리 방향 (둘 다 정규화된 상태)
        // separationDirection에 더 높은 가중치를 줘서 충돌 회피 우선
        float3 finalDirection = normalizedChaseDirection + separationDirection * 5.0f;
        finalDirection.y = 0;

        float distanceSq = math.lengthsq(finalDirection);

        if (distanceSq > 0.01f)
        {
            float3 normalizedDirection = math.normalize(finalDirection);
            float3 movement = normalizedDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            // 이동 방향으로 회전
            quaternion targetRotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 10f * DeltaTime);
        }
    }
}
