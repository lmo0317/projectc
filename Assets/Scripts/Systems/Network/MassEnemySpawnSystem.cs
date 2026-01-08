using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 대량 몬스터 소환 RPC 처리 시스템 (Server Only)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct MassEnemySpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawnConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // EnemySpawnConfig 가져오기
        var spawnConfigEntity = SystemAPI.GetSingletonEntity<EnemySpawnConfig>();
        var spawnConfig = SystemAPI.GetComponent<EnemySpawnConfig>(spawnConfigEntity);

        // RPC 처리
        foreach (var (rpcRequest, rpcEntity) in
                 SystemAPI.Query<RefRO<SpawnMassEnemiesRequest>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            int spawnCount = rpcRequest.ValueRO.Count;

            // 살아있는 플레이어 위치 수집
            var alivePlayerPositions = new NativeList<float3>(Allocator.Temp);
            foreach (var transform in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<GhostOwner, PlayerHealth>()
                         .WithDisabled<PlayerDead>())
            {
                alivePlayerPositions.Add(transform.ValueRO.Position);
            }

            // 플레이어가 없으면 스폰 안 함
            if (alivePlayerPositions.Length == 0)
            {
                alivePlayerPositions.Dispose();
                ecb.DestroyEntity(rpcEntity);
                continue;
            }

            // 대량 몬스터 소환
            for (int i = 0; i < spawnCount; i++)
            {
                // 랜덤 플레이어 선택
                int randomPlayerIndex = spawnConfig.RandomGenerator.NextInt(0, alivePlayerPositions.Length);
                float3 targetPlayerPosition = alivePlayerPositions[randomPlayerIndex];

                // 랜덤 원형 위치 계산
                float angle = spawnConfig.RandomGenerator.NextFloat(0f, math.PI * 2f);
                float distance = spawnConfig.RandomGenerator.NextFloat(
                    spawnConfig.SpawnRadius * 0.5f,
                    spawnConfig.SpawnRadius * 1.5f  // 더 넓은 범위에 스폰
                );

                float3 offset = new float3(
                    math.cos(angle) * distance,
                    0f,
                    math.sin(angle) * distance
                );

                float3 spawnPosition = targetPlayerPosition + offset;

                // 몬스터 Entity 생성
                var enemyEntity = ecb.Instantiate(spawnConfig.EnemyPrefab);
                ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(spawnPosition));
            }

            alivePlayerPositions.Dispose();

            // RPC 엔티티 제거
            ecb.DestroyEntity(rpcEntity);

            // Random Generator 업데이트
            SystemAPI.SetComponent(spawnConfigEntity, spawnConfig);
        }
    }
}
