using Unity.Collections;
using Unity.Entities;
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

            // 클라이언트용 Star 컴포넌트 추가 (랜덤 시드 초기화)
            ecb.AddComponent(starVisual, new ClientStarVisual
            {
                StarId = rpc.ValueRO.StarId,
                SpiralAngle = 0f,
                MagnetStartTime = 0f,
                RandomSeed = (uint)(rpc.ValueRO.StarId * 73856093), // StarId 기반 랜덤 시드
                IsBeingPulled = 0
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

        // 3. StarCollectRpc 처리 - 수집 이펙트
        foreach (var (rpc, rpcEntity) in
                 SystemAPI.Query<RefRO<StarCollectRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            // 수집 이펙트 재생
            if (StarCollectEffectPool.Instance != null)
            {
                StarCollectEffectPool.Instance.PlayEffect(rpc.ValueRO.Position);
            }

            // RPC 엔티티 삭제
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>
/// 클라이언트 Star 비주얼 마커 컴포넌트
/// 자석 효과를 위한 추가 데이터 포함
/// </summary>
public struct ClientStarVisual : IComponentData
{
    public int StarId;
    public float SpiralAngle;        // 나선 회전 각도 (radians)
    public float MagnetStartTime;    // 자석에 끌리기 시작한 시간
    public uint RandomSeed;          // 각 스타마다 다른 움직임을 위한 랜덤 시드
    public byte IsBeingPulled;       // 자석에 끌리고 있는지 (0 or 1)
}
