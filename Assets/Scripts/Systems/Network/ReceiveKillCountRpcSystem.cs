using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// KillCount RPC를 수신하여 GameStats를 업데이트하는 시스템
/// Client와 Server 양쪽에서 실행됨 (Server도 자신의 RPC를 받음)
/// NetcodeSamples 03_RPC 패턴
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ReceiveKillCountRpcSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();  // 연결된 후에만 실행
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // RPC 수신 처리
        foreach (var (rpcData, entity) in
                 SystemAPI.Query<KillCountRpc>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            // GameStats 업데이트
            foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
            {
                stats.ValueRW.KillCount += rpcData.IncrementAmount;
                break;  // 싱글톤
            }

            // RPC Entity 삭제 (필수!)
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
