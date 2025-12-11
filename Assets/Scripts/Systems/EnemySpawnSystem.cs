using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Enemy 스폰 시스템 (Server에서만 실행)
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
        // 스폰 설정과 플레이어가 있을 때만 실행
        state.RequireForUpdate<EnemySpawnConfig>();
        state.RequireForUpdate<GhostOwner>(); // 네트워크 플레이어 확인
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 게임 오버 시 스폰 중지
        if (SystemAPI.HasSingleton<GameOverTag>())
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;

        // EntityCommandBuffer 생성
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 모든 플레이어 위치 수집
        var playerPositions = new Unity.Collections.NativeList<float3>(Unity.Collections.Allocator.Temp);
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<GhostOwner>())
        {
            playerPositions.Add(transform.ValueRO.Position);
        }

        // 플레이어가 없으면 스폰하지 않음
        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            return;
        }

        // 스폰 매니저 처리
        foreach (var spawnConfig in SystemAPI.Query<RefRW<EnemySpawnConfig>>())
        {
            // ValueRW를 통해 직접 수정 (로컬 변수 복사 금지!)
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

                // 랜덤하게 플레이어 한 명 선택
                int randomPlayerIndex = spawnConfig.ValueRW.RandomGenerator.NextInt(0, playerPositions.Length);
                float3 targetPlayerPosition = playerPositions[randomPlayerIndex];

                // 랜덤 위치 계산 (원형 분포)
                float angle = spawnConfig.ValueRW.RandomGenerator.NextFloat(0f, math.PI * 2f);
                float distance = spawnConfig.ValueRW.RandomGenerator.NextFloat(
                    spawnConfig.ValueRW.SpawnRadius * 0.8f,
                    spawnConfig.ValueRW.SpawnRadius
                );

                // 원형 좌표 → 직교 좌표 변환
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

        // NativeList 정리
        playerPositions.Dispose();
    }
}
