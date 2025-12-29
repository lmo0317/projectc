using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 버프 적용 시스템 (Server에서만 실행)
/// 클라이언트의 버프 선택 RPC를 받아 버프 레벨업 처리
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BuffApplySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BuffSelectedRpc>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // BuffSelectedRpc 처리
        foreach (var (rpc, receiveRpc, rpcEntity) in
                 SystemAPI.Query<RefRO<BuffSelectedRpc>, RefRO<ReceiveRpcCommandRequest>>()
                     .WithEntityAccess())
        {
            var sourceConnection = receiveRpc.ValueRO.SourceConnection;
            var selectedBuffType = (BuffType)rpc.ValueRO.SelectedBuffType;

            // 해당 연결의 플레이어 찾기
            foreach (var (buffs, selectionState, ghostOwner, playerEntity) in
                     SystemAPI.Query<RefRW<PlayerBuffs>, RefRW<BuffSelectionState>, RefRO<GhostOwner>>()
                         .WithAll<PlayerTag>()
                         .WithEntityAccess())
            {
                // GhostOwner의 NetworkId와 연결의 NetworkId 비교
                if (!SystemAPI.HasComponent<NetworkId>(sourceConnection))
                    continue;

                var connectionNetworkId = SystemAPI.GetComponent<NetworkId>(sourceConnection);
                if (ghostOwner.ValueRO.NetworkId != connectionNetworkId.Value)
                    continue;

                // 버프 선택 중 상태 확인
                if (!selectionState.ValueRO.IsSelecting)
                    continue;

                // 선택된 버프가 옵션에 있는지 확인
                int selectedInt = (int)selectedBuffType;
                if (selectedInt != selectionState.ValueRO.Option1 &&
                    selectedInt != selectionState.ValueRO.Option2 &&
                    selectedInt != selectionState.ValueRO.Option3)
                    continue;

                // 버프 레벨업
                int oldLevel = buffs.ValueRO.GetLevel(selectedBuffType);
                if (buffs.ValueRW.TryLevelUp(selectedBuffType))
                {
                    int newLevel = buffs.ValueRO.GetLevel(selectedBuffType);

                    // 버프 선택 상태 해제
                    selectionState.ValueRW.IsSelecting = false;
                    selectionState.ValueRW.Option1 = -1;
                    selectionState.ValueRW.Option2 = -1;
                    selectionState.ValueRW.Option3 = -1;

                    // 버프 적용 알림 RPC 전송
                    var notifyRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(notifyRpcEntity, new BuffAppliedRpc
                    {
                        BuffType = (int)selectedBuffType,
                        NewLevel = newLevel
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(notifyRpcEntity);
                }

                break;
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
