using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// 버프 선택 시스템 (Server에서만 실행)
/// 포인트가 임계값에 도달하면 랜덤 3개 버프 선택 UI 요청
/// 버프 선택 중 게임 일시정지 처리
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarCollectSystem))]
public partial struct BuffSelectionSystem : ISystem
{
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerStarPoints>();
        _random = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);
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

        foreach (var (starPoints, buffs, selectionState, ghostOwner, entity) in
                 SystemAPI.Query<RefRW<PlayerStarPoints>, RefRO<PlayerBuffs>, RefRW<BuffSelectionState>, RefRO<GhostOwner>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>()
                     .WithEntityAccess())
        {
            // 이미 선택 중이면 스킵
            if (selectionState.ValueRO.IsSelecting)
                continue;

            // 포인트가 임계값에 도달했는지 체크
            if (starPoints.ValueRO.CurrentPoints >= starPoints.ValueRO.NextBuffThreshold)
            {
                Debug.Log($"[BuffSelection] 버프 선택 트리거! 포인트: {starPoints.ValueRO.CurrentPoints} >= {starPoints.ValueRO.NextBuffThreshold}");

                // 포인트 차감
                starPoints.ValueRW.CurrentPoints -= starPoints.ValueRW.NextBuffThreshold;

                // 다음 임계값 계산 (점점 증가)
                int selectionCount = starPoints.ValueRO.BuffSelectionCount;
                starPoints.ValueRW.NextBuffThreshold = CalculateNextThreshold(selectionCount);
                starPoints.ValueRW.BuffSelectionCount++;

                // 랜덤 3개 버프 선택 (최대 레벨 아닌 것들 중에서)
                var options = SelectRandomBuffs(buffs.ValueRO, ref _random);

                Debug.Log($"[BuffSelection] 선택된 버프: {(BuffType)options.x}, {(BuffType)options.y}, {(BuffType)options.z}");

                // 버프 선택 상태 활성화
                selectionState.ValueRW.IsSelecting = true;
                selectionState.ValueRW.Option1 = (int)options.x;
                selectionState.ValueRW.Option2 = (int)options.y;
                selectionState.ValueRW.Option3 = (int)options.z;

                // GameSessionState 업데이트 (버프 선택 중인 플레이어 수 증가)
                if (sessionEntity != Entity.Null)
                {
                    sessionState.BuffSelectingPlayerCount++;
                    state.EntityManager.SetComponentData(sessionEntity, sessionState);
                    Debug.Log($"[BuffSelection] 게임 일시정지! 버프 선택 중 플레이어: {sessionState.BuffSelectingPlayerCount}");
                }

                // 모든 클라이언트에 게임 일시정지 RPC 전송
                int selectingPlayerId = ghostOwner.ValueRO.NetworkId;
                foreach (var connectionEntity in inGameConnections)
                {
                    var pauseRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(pauseRpcEntity, new GamePauseRpc
                    {
                        IsPaused = true,
                        SelectingPlayerNetworkId = selectingPlayerId
                    });
                    ecb.AddComponent(pauseRpcEntity, new SendRpcCommandRequest
                    {
                        TargetConnection = connectionEntity
                    });
                }

                // 버프 선택 중인 플레이어에게 UI 표시 RPC 전송
                Entity targetConnection = Entity.Null;
                foreach (var (networkId, connEntity) in
                         SystemAPI.Query<RefRO<NetworkId>>()
                             .WithAll<NetworkStreamInGame>()
                             .WithEntityAccess())
                {
                    if (networkId.ValueRO.Value == selectingPlayerId)
                    {
                        targetConnection = connEntity;
                        break;
                    }
                }

                if (targetConnection != Entity.Null)
                {
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new ShowBuffSelectionRpc
                    {
                        Option1BuffType = (int)options.x,
                        Option1CurrentLevel = buffs.ValueRO.GetLevel((BuffType)options.x),
                        Option2BuffType = (int)options.y,
                        Option2CurrentLevel = buffs.ValueRO.GetLevel((BuffType)options.y),
                        Option3BuffType = (int)options.z,
                        Option3CurrentLevel = buffs.ValueRO.GetLevel((BuffType)options.z)
                    });
                    ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                    {
                        TargetConnection = targetConnection
                    });
                }

                Debug.Log("[BuffSelection] ShowBuffSelectionRpc 전송됨");
            }
        }

        inGameConnections.Dispose();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    /// <summary>
    /// 다음 버프 필요 포인트 계산
    /// 1차: 10, 2차: 15, 3차: 20, 4차: 30, 5차: 40, 6차+: +15씩
    /// </summary>
    private static int CalculateNextThreshold(int selectionCount)
    {
        return selectionCount switch
        {
            0 => 10,
            1 => 15,
            2 => 20,
            3 => 30,
            4 => 40,
            _ => 40 + (selectionCount - 4) * 15
        };
    }

    /// <summary>
    /// 최대 레벨이 아닌 버프 중 랜덤 3개 선택
    /// </summary>
    private static int3 SelectRandomBuffs(in PlayerBuffs buffs, ref Random random)
    {
        // 선택 가능한 버프 목록 (최대 레벨 아닌 것)
        var availableBuffs = new NativeList<int>(8, Allocator.Temp);

        for (int i = 0; i < 8; i++)
        {
            var buffType = (BuffType)i;
            if (buffs.GetLevel(buffType) < PlayerBuffs.MaxLevel)
            {
                availableBuffs.Add(i);
            }
        }

        int3 result = new int3(-1, -1, -1);

        // 3개 선택 (또는 가능한 만큼)
        int selectCount = math.min(3, availableBuffs.Length);

        for (int i = 0; i < selectCount; i++)
        {
            int randomIndex = random.NextInt(0, availableBuffs.Length);
            int selectedBuff = availableBuffs[randomIndex];

            // Fisher-Yates 방식으로 제거 (마지막 요소와 교체 후 제거)
            availableBuffs[randomIndex] = availableBuffs[availableBuffs.Length - 1];
            availableBuffs.RemoveAt(availableBuffs.Length - 1);

            // 결과에 저장
            switch (i)
            {
                case 0: result.x = selectedBuff; break;
                case 1: result.y = selectedBuff; break;
                case 2: result.z = selectedBuff; break;
            }
        }

        availableBuffs.Dispose();

        // 선택 가능한 버프가 3개 미만이면 중복 허용 (이미 선택된 것 재사용)
        if (result.y == -1 && result.x != -1) result.y = result.x;
        if (result.z == -1 && result.x != -1) result.z = result.x;

        return result;
    }
}
