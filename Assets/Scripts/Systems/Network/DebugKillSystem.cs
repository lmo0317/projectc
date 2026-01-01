using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 디버그용 플레이어 즉사 RPC 처리 시스템 (Server에서만 실행)
/// DebugKillRequestRpc를 받으면 해당 플레이어를 즉시 죽임
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DebugKillSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DebugKillRequestRpc>();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // DebugKillRequestRpc 처리
        foreach (var (rpc, receiveEntity, rpcEntity) in
                 SystemAPI.Query<RefRO<DebugKillRequestRpc>, RefRO<ReceiveRpcCommandRequest>>()
                     .WithEntityAccess())
        {
            // RPC를 보낸 연결의 Entity 가져오기
            var connectionEntity = receiveEntity.ValueRO.SourceConnection;

            // 연결 Entity에서 NetworkId 가져오기
            if (!EntityManager.HasComponent<NetworkId>(connectionEntity))
            {
                Debug.LogWarning("[DebugKillSystem] Connection has no NetworkId!");
                ecb.DestroyEntity(rpcEntity);
                continue;
            }

            var networkId = EntityManager.GetComponentData<NetworkId>(connectionEntity).Value;
            Debug.Log($"[DebugKillSystem] Received kill request from NetworkId: {networkId}");

            // 해당 플레이어 Entity 찾기
            bool foundPlayer = false;
            foreach (var (ghostOwner, playerHealth, transform, playerEntity) in
                     SystemAPI.Query<RefRO<GhostOwner>, RefRW<PlayerHealth>, RefRW<LocalTransform>>()
                         .WithDisabled<PlayerDead>() // 살아있는 플레이어만
                         .WithEntityAccess())
            {
                if (ghostOwner.ValueRO.NetworkId == networkId)
                {
                    // 1. 체력 0으로 설정
                    playerHealth.ValueRW.CurrentHealth = 0;

                    // 2. 화면 밖으로 이동
                    transform.ValueRW.Position = new float3(0, -1000, 0);

                    // 3. PlayerDead 활성화
                    ecb.SetComponentEnabled<PlayerDead>(playerEntity, true);

                    Debug.Log($"[DebugKillSystem] Player {networkId} killed!");
                    foundPlayer = true;
                    break;
                }
            }

            if (!foundPlayer)
            {
                Debug.LogWarning($"[DebugKillSystem] Alive player not found for NetworkId: {networkId}");
            }

            // RPC Entity 제거
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
