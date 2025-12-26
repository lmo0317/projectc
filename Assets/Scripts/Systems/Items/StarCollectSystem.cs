using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Star 아이템 수집 시스템 (Server에서만 실행)
/// 플레이어가 Star에 가까이 가면 수집
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarMovementSystem))]
[BurstCompile]
public partial struct StarCollectSystem : ISystem
{
    private const float CollectRadius = 1.0f; // 수집 가능 거리

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StarTag>();
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 모든 플레이어에 대해
        foreach (var (playerTransform, playerPoints, playerEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerStarPoints>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>()
                     .WithEntityAccess())
        {
            float3 playerPos = playerTransform.ValueRO.Position;

            // 모든 Star와 거리 체크
            foreach (var (starTransform, starValue, starEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<StarValue>>()
                         .WithAll<StarTag>()
                         .WithEntityAccess())
            {
                float3 starPos = starTransform.ValueRO.Position;
                float distance = math.distance(playerPos, starPos);

                if (distance < CollectRadius)
                {
                    // 포인트 추가
                    playerPoints.ValueRW.CurrentPoints += starValue.ValueRO.Value;
                    playerPoints.ValueRW.TotalCollected += starValue.ValueRO.Value;

                    // Star 수집 이펙트 RPC 전송
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new StarCollectRpc
                    {
                        Position = starPos,
                        Value = starValue.ValueRO.Value
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                    // Star 삭제
                    ecb.DestroyEntity(starEntity);
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
