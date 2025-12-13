using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 부활 요청 RPC 처리 시스템 (Server에서만 실행)
/// RespawnRequestRpc를 받으면 해당 플레이어를 부활시킴
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerRespawnSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<RespawnRequestRpc>();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // RespawnRequestRpc 처리
        foreach (var (rpc, receiveEntity, rpcEntity) in
                 SystemAPI.Query<RefRO<RespawnRequestRpc>, RefRO<ReceiveRpcCommandRequest>>()
                     .WithEntityAccess())
        {
            // RPC를 보낸 연결의 Entity 가져오기
            var connectionEntity = receiveEntity.ValueRO.SourceConnection;

            // 연결 Entity에서 NetworkId 가져오기
            if (!EntityManager.HasComponent<NetworkId>(connectionEntity))
            {
                Debug.LogWarning("[PlayerRespawnSystem] Connection has no NetworkId!");
                ecb.DestroyEntity(rpcEntity);
                continue;
            }

            var networkId = EntityManager.GetComponentData<NetworkId>(connectionEntity).Value;
            Debug.Log($"[PlayerRespawnSystem] Received respawn request from NetworkId: {networkId}");

            // 해당 플레이어 Entity 찾기
            bool foundPlayer = false;
            foreach (var (ghostOwner, playerHealth, transform, playerEntity) in
                     SystemAPI.Query<RefRO<GhostOwner>, RefRW<PlayerHealth>, RefRW<LocalTransform>>()
                         .WithAll<PlayerDead>() // 죽은 플레이어만
                         .WithEntityAccess())
            {
                if (ghostOwner.ValueRO.NetworkId == networkId)
                {
                    // 1. 체력 회복
                    playerHealth.ValueRW.CurrentHealth = playerHealth.ValueRO.MaxHealth;

                    // 2. 스폰 위치로 이동 (랜덤 위치)
                    float randomX = Unity.Mathematics.Random.CreateFromIndex((uint)networkId).NextFloat(-5f, 5f);
                    float randomZ = Unity.Mathematics.Random.CreateFromIndex((uint)(networkId + 100)).NextFloat(-5f, 5f);
                    transform.ValueRW.Position = new float3(randomX, 0.5f, randomZ);

                    // 3. PlayerDead 비활성화
                    ecb.SetComponentEnabled<PlayerDead>(playerEntity, false);

                    Debug.Log($"[PlayerRespawnSystem] Player {networkId} respawned at ({randomX:F1}, 0.5, {randomZ:F1})");
                    foundPlayer = true;
                    break;
                }
            }

            if (!foundPlayer)
            {
                Debug.LogWarning($"[PlayerRespawnSystem] Dead player not found for NetworkId: {networkId}");
            }

            // RPC Entity 제거
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
