using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 버프 적용 시스템 (Server에서만 실행)
/// 클라이언트의 버프 선택 RPC를 받아 버프 레벨업 처리
/// 버프 선택 완료 시 게임 재개 처리
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

        // GameSessionState 가져오기
        Entity sessionEntity = Entity.Null;
        GameSessionState sessionState = default;
        foreach (var (session, entity) in SystemAPI.Query<RefRO<GameSessionState>>().WithEntityAccess())
        {
            sessionEntity = entity;
            sessionState = session.ValueRO;
            break;
        }

        // InGame 상태인 연결 목록 수집 (RPC 브로드캐스트용)
        var inGameConnections = new NativeList<Entity>(Allocator.Temp);
        foreach (var (networkId, connectionEntity) in
                 SystemAPI.Query<RefRO<NetworkId>>()
                     .WithAll<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            inGameConnections.Add(connectionEntity);
        }

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

                    // GameSessionState 업데이트 (버프 선택 중인 플레이어 수 감소)
                    if (sessionEntity != Entity.Null && sessionState.BuffSelectingPlayerCount > 0)
                    {
                        sessionState.BuffSelectingPlayerCount--;
                        state.EntityManager.SetComponentData(sessionEntity, sessionState);
                        Debug.Log($"[BuffApply] 버프 선택 완료! 남은 선택 중 플레이어: {sessionState.BuffSelectingPlayerCount}");

                        // 모든 플레이어가 선택 완료했으면 게임 재개 RPC 전송
                        if (sessionState.BuffSelectingPlayerCount == 0)
                        {
                            Debug.Log("[BuffApply] 모든 버프 선택 완료 - 게임 재개!");
                            foreach (var connectionEntity in inGameConnections)
                            {
                                var pauseRpcEntity = ecb.CreateEntity();
                                ecb.AddComponent(pauseRpcEntity, new GamePauseRpc
                                {
                                    IsPaused = false,
                                    SelectingPlayerNetworkId = 0
                                });
                                ecb.AddComponent(pauseRpcEntity, new SendRpcCommandRequest
                                {
                                    TargetConnection = connectionEntity
                                });
                            }
                        }
                    }

                    // 버프 적용 알림 RPC 전송 (모든 클라이언트에게)
                    foreach (var connectionEntity in inGameConnections)
                    {
                        var notifyRpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(notifyRpcEntity, new BuffAppliedRpc
                        {
                            BuffType = (int)selectedBuffType,
                            NewLevel = newLevel
                        });
                        ecb.AddComponent(notifyRpcEntity, new SendRpcCommandRequest
                        {
                            TargetConnection = connectionEntity
                        });
                    }
                }

                break;
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(rpcEntity);
        }

        inGameConnections.Dispose();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
