using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 플레이어 연결 끊김 처리 시스템 (Server에서만 실행)
///
/// === 역할 ===
/// - 클라이언트 연결이 끊기면 해당 플레이어를 "죽음" 상태로 전환
/// - 플레이어 Entity는 즉시 삭제하지 않음 (GameSessionSystem에서 처리)
/// - 다른 플레이어의 게임에 영향 없음
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PlayerSpawnSystem))]
public partial class PlayerDisconnectCleanupSystem : SystemBase
{
    protected override void OnCreate()
    {
        // 주의: NetworkId를 RequireForUpdate하면 연결이 없을 때 시스템이 멈춤!
        // 연결이 끊긴 후에도 죽은 플레이어 정리를 위해 계속 실행되어야 함
        RequireForUpdate<PlayerTag>();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 현재 InGame 연결 수 로깅
        int inGameConnectionCount = 0;
        foreach (var _ in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithAll<NetworkStreamInGame>())
        {
            inGameConnectionCount++;
        }

        // 살아있는 플레이어 중 연결이 끊긴 플레이어 찾기
        foreach (var (connectionOwner, ghostOwner, playerHealth, entity) in
                 SystemAPI.Query<RefRO<ConnectionOwner>, RefRO<GhostOwner>, RefRW<PlayerHealth>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>()
                     .WithEntityAccess())
        {
            var connectionEntity = connectionOwner.ValueRO.Entity;

            // 연결 Entity가 삭제되었는지 확인
            if (!EntityManager.Exists(connectionEntity))
            {
                int networkId = ghostOwner.ValueRO.NetworkId;
                Debug.Log($"[PlayerDisconnectCleanup] Player {networkId} disconnected - marking as dead (remaining connections: {inGameConnectionCount})");

                // 체력 0으로 설정
                playerHealth.ValueRW.CurrentHealth = 0;

                // PlayerDead 활성화
                ecb.SetComponentEnabled<PlayerDead>(entity, true);

                // 화면 밖으로 이동
                ecb.SetComponent(entity, LocalTransform.FromPosition(new float3(0, -1000, 0)));
            }
        }

        // 이미 죽은 플레이어 중 연결이 끊긴 플레이어도 Entity 삭제 처리
        // (이들은 GameSessionSystem에서 정리될 것이지만, 로그 추가)
        foreach (var (connectionOwner, ghostOwner, entity) in
                 SystemAPI.Query<RefRO<ConnectionOwner>, RefRO<GhostOwner>>()
                     .WithAll<PlayerTag, PlayerDead>()
                     .WithEntityAccess())
        {
            var connectionEntity = connectionOwner.ValueRO.Entity;

            // 연결 Entity가 삭제되었는지 확인
            if (!EntityManager.Exists(connectionEntity))
            {
                int networkId = ghostOwner.ValueRO.NetworkId;
                Debug.Log($"[PlayerDisconnectCleanup] Dead player {networkId}'s connection gone - removing entity (remaining connections: {inGameConnectionCount})");

                // 죽은 플레이어 Entity 직접 삭제
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
