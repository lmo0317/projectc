using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 디버그용 Star 포인트 추가 RPC 처리 시스템 (Server에서만 실행)
/// DebugAddPointsRpc를 받으면 해당 플레이어에게 포인트 추가
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DebugAddPointsSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DebugAddPointsRpc>();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // DebugAddPointsRpc 처리
        foreach (var (rpc, receiveEntity, rpcEntity) in
                 SystemAPI.Query<RefRO<DebugAddPointsRpc>, RefRO<ReceiveRpcCommandRequest>>()
                     .WithEntityAccess())
        {
            // RPC를 보낸 연결의 Entity 가져오기
            var connectionEntity = receiveEntity.ValueRO.SourceConnection;

            // 연결 Entity에서 NetworkId 가져오기
            if (!EntityManager.HasComponent<NetworkId>(connectionEntity))
            {
                Debug.LogWarning("[DebugAddPointsSystem] Connection has no NetworkId!");
                ecb.DestroyEntity(rpcEntity);
                continue;
            }

            var networkId = EntityManager.GetComponentData<NetworkId>(connectionEntity).Value;
            int amount = rpc.ValueRO.Amount;
            Debug.Log($"[DebugAddPointsSystem] Received add points request from NetworkId: {networkId}, Amount: {amount}");

            // 해당 플레이어 Entity 찾기
            bool foundPlayer = false;
            foreach (var (ghostOwner, starPoints, playerEntity) in
                     SystemAPI.Query<RefRO<GhostOwner>, RefRW<PlayerStarPoints>>()
                         .WithAll<PlayerTag>()
                         .WithEntityAccess())
            {
                if (ghostOwner.ValueRO.NetworkId == networkId)
                {
                    // 포인트 추가
                    starPoints.ValueRW.CurrentPoints += amount;
                    starPoints.ValueRW.TotalCollected += amount;

                    Debug.Log($"[DebugAddPointsSystem] Player {networkId} received +{amount} points! (Current: {starPoints.ValueRW.CurrentPoints})");
                    foundPlayer = true;
                    break;
                }
            }

            if (!foundPlayer)
            {
                Debug.LogWarning($"[DebugAddPointsSystem] Player not found for NetworkId: {networkId}");
            }

            // RPC Entity 제거
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
