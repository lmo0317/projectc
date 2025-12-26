using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// Star 수집 RPC 수신 및 처리 시스템 (Client에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class StarCollectClientSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // StarCollectRpc 처리
        foreach (var (rpc, rpcEntity) in
                 SystemAPI.Query<RefRO<StarCollectRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            Vector3 position = rpc.ValueRO.Position;
            int value = rpc.ValueRO.Value;

            // Star 수집 이펙트 재생 (나중에 StarCollectEffectPool 추가 시 사용)
            // 현재는 로그만 출력
            Debug.Log($"[Client] Star collected at {position}, +{value} points");

            // TODO: Star 수집 이펙트 파티클 재생
            // TODO: 수집 사운드 재생
            // TODO: UI 포인트 애니메이션

            // RPC Entity 제거
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
