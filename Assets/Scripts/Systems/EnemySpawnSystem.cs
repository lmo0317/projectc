using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Enemy 스폰 시스템 (Server에서만 실행)
/// 살아있는 플레이어가 없으면 스폰만 중지 (Enemy는 유지)
/// 게임 리셋 시 Enemy 제거는 GameSessionSystem에서 처리
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AutoShootSystem))]
[BurstCompile]
public partial struct EnemySpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawnConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 버프 선택 중이면 Enemy 스폰 중지
        foreach (var sessionState in SystemAPI.Query<RefRO<GameSessionState>>())
        {
            if (sessionState.ValueRO.IsGamePaused)
                return;
        }

        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 살아있는 플레이어 위치 수집 (PlayerDead가 비활성화된 플레이어만)
        var alivePlayerPositions = new NativeList<float3>(Allocator.Temp);
        foreach (var transform in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<GhostOwner, PlayerHealth>()
                     .WithDisabled<PlayerDead>())
        {
            alivePlayerPositions.Add(transform.ValueRO.Position);
        }

        // 살아있는 플레이어가 없으면 스폰만 중지 (Enemy는 유지)
        // 다른 플레이어가 재연결할 수 있으므로 게임 상태는 유지
        if (alivePlayerPositions.Length == 0)
        {
            alivePlayerPositions.Dispose();
            return;
        }

        // 스폰 매니저 처리
        foreach (var spawnConfig in SystemAPI.Query<RefRW<EnemySpawnConfig>>())
        {
            spawnConfig.ValueRW.TimeSinceLastSpawn += deltaTime;

            // 스폰 간격 체크
            if (spawnConfig.ValueRW.TimeSinceLastSpawn >= spawnConfig.ValueRW.SpawnInterval)
            {
                spawnConfig.ValueRW.TimeSinceLastSpawn = 0f;

                // 최대 몬스터 수 체크
                if (spawnConfig.ValueRW.MaxEnemies > 0)
                {
                    int enemyCount = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount();
                    if (enemyCount >= spawnConfig.ValueRW.MaxEnemies)
                    {
                        continue;
                    }
                }

                // 랜덤하게 살아있는 플레이어 한 명 선택
                int randomPlayerIndex = spawnConfig.ValueRW.RandomGenerator.NextInt(0, alivePlayerPositions.Length);
                float3 targetPlayerPosition = alivePlayerPositions[randomPlayerIndex];

                // 랜덤 위치 계산 (원형 분포)
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

                float3 spawnPosition = targetPlayerPosition + offset;

                // 몬스터 Entity 생성
                var enemyEntity = ecb.Instantiate(spawnConfig.ValueRW.EnemyPrefab);
                ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(spawnPosition));
            }
        }

        alivePlayerPositions.Dispose();
    }
}
