using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Star 아이템 수집 시스템 (Server에서만 실행)
/// 플레이어가 Star에 가까이 가면 수집, 일정 거리 내에서는 자동으로 끌려옴
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StarMovementSystem))]
public partial struct StarCollectSystem : ISystem
{
    private const float CollectRadius = 1.5f;   // 수집 가능 거리
    private const float MagnetRadius = 8.0f;    // 자석 효과 거리
    private const float MagnetSpeed = 15.0f;    // 끌려오는 속도

    public void OnCreate(ref SystemState state)
    {
        // RequireForUpdate 제거 - 시스템이 항상 실행되도록 함
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 디버그: Star 개수 확인 (Star가 있을 때만 출력)
        int starCount = SystemAPI.QueryBuilder().WithAll<StarTag, StarValue, StarId>().Build().CalculateEntityCount();
        if (starCount > 0)
        {
            UnityEngine.Debug.Log($"[StarCollect] Stars in server: {starCount}");
        }

        // 모든 플레이어에 대해
        foreach (var (playerTransform, playerPoints, playerEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerStarPoints>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>()
                     .WithEntityAccess())
        {
            float3 playerPos = playerTransform.ValueRO.Position;

            // 1. Star 수집 (StarMovement 없이도 동작)
            foreach (var (starTransform, starValue, starId, starEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<StarValue>, RefRO<StarId>>()
                         .WithAll<StarTag>()
                         .WithEntityAccess())
            {
                float3 starPos = starTransform.ValueRO.Position;
                float distance = math.distance(playerPos, starPos);

                // 수집 범위 내
                if (distance < CollectRadius)
                {
                    // 포인트 추가
                    playerPoints.ValueRW.CurrentPoints += starValue.ValueRO.Value;
                    playerPoints.ValueRW.TotalCollected += starValue.ValueRO.Value;

                    UnityEngine.Debug.Log($"[StarCollect] Collected Star ID:{starId.ValueRO.Value}, Points: {playerPoints.ValueRO.CurrentPoints}");

                    // Star 수집 이펙트 RPC 전송
                    var rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new StarCollectRpc
                    {
                        Position = starPos,
                        Value = starValue.ValueRO.Value
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                    // 클라이언트에 Star 삭제 RPC 전송
                    var destroyRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(destroyRpcEntity, new StarDestroyRpc
                    {
                        StarId = starId.ValueRO.Value
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(destroyRpcEntity);

                    // 서버에서 Star 삭제
                    ecb.DestroyEntity(starEntity);
                }
            }

            // 2. 자석 효과 (StarMovement가 있는 Star만)
            foreach (var (starTransform, starMovement) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<StarMovement>>()
                         .WithAll<StarTag>())
            {
                float3 starPos = starTransform.ValueRO.Position;
                float distance = math.distance(playerPos, starPos);

                // 자석 범위 내 - Star가 정착한 상태일 때만 끌려옴
                if (distance < MagnetRadius && distance >= CollectRadius && starMovement.ValueRO.IsSettled)
                {
                    // 플레이어 방향으로 이동
                    float3 direction = math.normalize(playerPos - starPos);
                    float3 newPos = starPos + direction * MagnetSpeed * deltaTime;
                    starTransform.ValueRW.Position = newPos;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
