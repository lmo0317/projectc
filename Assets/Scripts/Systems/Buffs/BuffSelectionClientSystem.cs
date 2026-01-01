using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 버프 선택 클라이언트 시스템
/// 서버에서 ShowBuffSelectionRpc를 받으면 UI 표시
/// GamePauseRpc로 다른 플레이어 대기 UI 처리
/// 클라이언트에서만 실행 (UI 처리가 필요)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BuffSelectionClientSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        Debug.Log($"[BuffSelectionClientSystem] OnCreate - World: {state.World.Name}");
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 로컬 플레이어의 NetworkId 가져오기 (여러 방법 시도)
        int localNetworkId = GetLocalPlayerNetworkId(ref state);

        // GamePauseRpc 처리 (게임 일시정지/재개)
        foreach (var (rpc, entity) in
                 SystemAPI.Query<RefRO<GamePauseRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            bool isPaused = rpc.ValueRO.IsPaused;
            int selectingPlayerId = rpc.ValueRO.SelectingPlayerNetworkId;

            Debug.Log($"[BuffSelectionClientSystem] GamePauseRpc 수신: IsPaused={isPaused}, SelectingPlayer={selectingPlayerId}, LocalPlayer={localNetworkId}");

            if (BuffSelectionUI.Instance == null)
            {
                Debug.LogWarning("[BuffSelectionClientSystem] BuffSelectionUI.Instance가 없습니다!");
                ecb.DestroyEntity(entity);
                continue;
            }

            if (isPaused)
            {
                // 버프 선택 중인 플레이어가 나 자신이 아니면 대기 UI 표시
                // 자기 자신인지 확인하는 로직 개선
                bool isMySelection = (localNetworkId > 0 && selectingPlayerId == localNetworkId);

                if (!isMySelection)
                {
                    Debug.Log($"[BuffSelectionClientSystem] 다른 플레이어({selectingPlayerId})가 선택 중 - 대기 UI 표시");
                    BuffSelectionUI.Instance.ShowWaiting();
                }
                else
                {
                    Debug.Log($"[BuffSelectionClientSystem] 내가 선택 중 - 대기 UI 표시 안 함");
                }
            }
            else
            {
                // 게임 재개 - 모든 UI 숨기기
                Debug.Log($"[BuffSelectionClientSystem] 게임 재개 - UI 숨기기");
                BuffSelectionUI.Instance.HideAll();
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(entity);
        }

        // ShowBuffSelectionRpc 처리
        foreach (var (rpc, entity) in
                 SystemAPI.Query<RefRO<ShowBuffSelectionRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            Debug.Log($"[BuffSelectionClientSystem] ShowBuffSelectionRpc 수신! World: {state.World.Name}");

            // 대기 UI가 표시 중이면 숨기기
            if (BuffSelectionUI.Instance != null && BuffSelectionUI.Instance.IsWaiting)
            {
                BuffSelectionUI.Instance.HideWaiting();
            }

            // UI 표시
            if (BuffSelectionUI.Instance != null)
            {
                BuffSelectionUI.Instance.Show(
                    rpc.ValueRO.Option1BuffType, rpc.ValueRO.Option1CurrentLevel,
                    rpc.ValueRO.Option2BuffType, rpc.ValueRO.Option2CurrentLevel,
                    rpc.ValueRO.Option3BuffType, rpc.ValueRO.Option3CurrentLevel
                );
            }
            else
            {
                Debug.LogWarning("[BuffSelectionClientSystem] BuffSelectionUI.Instance가 없습니다!");
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(entity);
        }

        // BuffAppliedRpc 처리 (버프 적용 알림)
        foreach (var (rpc, entity) in
                 SystemAPI.Query<RefRO<BuffAppliedRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            var buffType = (BuffType)rpc.ValueRO.BuffType;
            var newLevel = rpc.ValueRO.NewLevel;

            Debug.Log($"[BuffSelectionClientSystem] 버프 적용됨: {buffType} Lv.{newLevel}");

            // 버프 획득 이펙트 재생
            if (BuffEffectPool.Instance != null)
            {
                // 플레이어 위치에서 이펙트 재생
                var playerPos = GetLocalPlayerPosition(ref state);
                if (playerPos.HasValue)
                {
                    BuffEffectPool.Instance.PlayEffect(playerPos.Value, buffType);
                }
            }

            // HUD 버프 아이콘 업데이트
            if (BuffIconsUI.Instance != null)
            {
                BuffIconsUI.Instance.UpdateBuffIcon(buffType, newLevel);
            }

            // 사운드 효과 재생
            if (GameSoundManager.Instance != null)
            {
                GameSoundManager.Instance.PlayBuffAcquired(buffType);
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    /// <summary>
    /// 로컬 플레이어의 NetworkId 가져오기
    /// 여러 방법으로 시도하여 확실하게 찾기
    /// </summary>
    private int GetLocalPlayerNetworkId(ref SystemState state)
    {
        // 1. GhostOwnerIsLocal을 가진 플레이어의 NetworkId 반환
        foreach (var ghostOwner in SystemAPI.Query<RefRO<GhostOwner>>()
                     .WithAll<PlayerTag, GhostOwnerIsLocal>())
        {
            return ghostOwner.ValueRO.NetworkId;
        }

        // 2. NetworkStreamConnection에서 NetworkId 가져오기 (클라이언트 연결 정보)
        foreach (var networkId in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithAll<NetworkStreamConnection>())
        {
            return networkId.ValueRO.Value;
        }

        // 없으면 0 반환
        return 0;
    }

    /// <summary>
    /// 로컬 플레이어의 위치 가져오기
    /// </summary>
    private Unity.Mathematics.float3? GetLocalPlayerPosition(ref SystemState state)
    {
        foreach (var (transform, _) in
                 SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>, RefRO<PlayerTag>>()
                     .WithAll<GhostOwnerIsLocal>())
        {
            return transform.ValueRO.Position;
        }

        // GhostOwnerIsLocal이 없는 경우 (싱글플레이)
        foreach (var (transform, _) in
                 SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>, RefRO<PlayerTag>>()
                     .WithDisabled<PlayerDead>())
        {
            return transform.ValueRO.Position;
        }

        return null;
    }
}
