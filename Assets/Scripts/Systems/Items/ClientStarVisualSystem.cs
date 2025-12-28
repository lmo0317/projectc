using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 클라이언트에서 Star 비주얼을 관리하는 시스템
/// 서버로부터 RPC를 받아 Star 프리팹을 인스턴스화
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ClientStarVisualSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StarSpawnConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Star 스폰 설정 가져오기
        if (!SystemAPI.TryGetSingletonRW<StarSpawnConfig>(out var starSpawnConfig))
            return;

        // 1. StarSpawnRpc 처리 - 클라이언트에서 Star 비주얼 생성
        foreach (var (rpc, rpcEntity) in
                 SystemAPI.Query<RefRO<StarSpawnRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            // 프리팹에서 Star 비주얼 생성
            var starVisual = ecb.Instantiate(starSpawnConfig.ValueRO.StarPrefab);

            // 위치 설정
            ecb.SetComponent(starVisual, LocalTransform.FromPosition(rpc.ValueRO.Position));

            // 클라이언트용 Star 컴포넌트 추가
            ecb.AddComponent(starVisual, new ClientStarVisual
            {
                StarId = rpc.ValueRO.StarId
            });

            // 이동 컴포넌트 추가
            ecb.AddComponent(starVisual, new StarMovement
            {
                Velocity = rpc.ValueRO.Velocity,
                Gravity = 15f,
                GroundY = 0f,
                BounceDamping = 0.5f,
                IsSettled = false
            });

            // RPC 엔티티 삭제
            ecb.DestroyEntity(rpcEntity);
        }

        // 2. StarDestroyRpc 처리 - 클라이언트에서 Star 비주얼 삭제
        foreach (var (rpc, rpcEntity) in
                 SystemAPI.Query<RefRO<StarDestroyRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            int targetStarId = rpc.ValueRO.StarId;

            // 해당 ID의 Star 비주얼 찾아서 삭제
            foreach (var (clientStar, starEntity) in
                     SystemAPI.Query<RefRO<ClientStarVisual>>()
                         .WithEntityAccess())
            {
                if (clientStar.ValueRO.StarId == targetStarId)
                {
                    ecb.DestroyEntity(starEntity);
                    break;
                }
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(rpcEntity);
        }

        // 3. StarCollectRpc 처리 - 수집 이펙트 (기존 로직 유지)
        foreach (var (rpc, rpcEntity) in
                 SystemAPI.Query<RefRO<StarCollectRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            // TODO: 수집 이펙트 표시
            // 현재는 단순히 RPC 엔티티만 삭제

            // RPC 엔티티 삭제
            ecb.DestroyEntity(rpcEntity);
        }

        // 4. 클라이언트 Star 물리 업데이트 (서버와 동일한 로직)
        foreach (var (transform, movement) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<StarMovement>>()
                     .WithAll<ClientStarVisual>())
        {
            if (movement.ValueRO.IsSettled)
                continue;

            // 중력 적용
            float3 velocity = movement.ValueRO.Velocity;
            velocity.y -= movement.ValueRO.Gravity * deltaTime;

            // 위치 업데이트
            float3 newPos = transform.ValueRO.Position + velocity * deltaTime;

            // 바닥 충돌 체크
            if (newPos.y <= movement.ValueRO.GroundY)
            {
                newPos.y = movement.ValueRO.GroundY;

                // 바운스
                if (math.abs(velocity.y) > 0.5f)
                {
                    velocity.y = -velocity.y * movement.ValueRO.BounceDamping;
                    velocity.x *= 0.8f;
                    velocity.z *= 0.8f;
                }
                else
                {
                    // 정착
                    velocity = float3.zero;
                    movement.ValueRW.IsSettled = true;
                }
            }

            movement.ValueRW.Velocity = velocity;
            transform.ValueRW.Position = newPos;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>
/// 클라이언트 Star 비주얼 마커 컴포넌트
/// </summary>
public struct ClientStarVisual : IComponentData
{
    public int StarId;
}
