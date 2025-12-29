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

            // TODO: 버프 획득 이펙트/사운드 재생

            // RPC 엔티티 삭제
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
