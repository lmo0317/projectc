using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 버프 선택 클라이언트 시스템
/// 서버에서 ShowBuffSelectionRpc를 받으면 UI 표시
/// Server/Client 양쪽에서 실행 (싱글플레이 지원)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
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

        // ShowBuffSelectionRpc 처리
        foreach (var (rpc, entity) in
                 SystemAPI.Query<RefRO<ShowBuffSelectionRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            Debug.Log($"[BuffSelectionClientSystem] ShowBuffSelectionRpc 수신! World: {state.World.Name}");

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
