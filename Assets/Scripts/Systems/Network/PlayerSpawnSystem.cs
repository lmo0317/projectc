using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 클라이언트 연결 시 플레이어 스폰 (NetcodeSamples 05_SpawnPlayer 패턴)
/// Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerSpawnSystem : ISystem
{
    private EntityQuery m_NewPlayersQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Spawner가 로드될 때까지 대기
        state.RequireForUpdate<Spawner>();
        // NetworkStreamInGame이 최소 하나 있을 때까지 대기
        state.RequireForUpdate<NetworkStreamInGame>();

        // 새로운 플레이어 쿼리: PlayerSpawned 태그가 없는 연결 (샘플과 동일)
        m_NewPlayersQuery = SystemAPI.QueryBuilder()
            .WithAll<NetworkId>()
            .WithNone<PlayerSpawned>()
            .Build();

        UnityEngine.Debug.Log("[PlayerSpawnSystem] OnCreate - Waiting for Spawner");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 새로운 플레이어가 없으면 리턴
        if (m_NewPlayersQuery.IsEmptyIgnoreFilter)
            return;

        var prefab = SystemAPI.GetSingleton<Spawner>().Player;

        // 새로 연결된 플레이어 스폰
        var connectionEntities = m_NewPlayersQuery.ToEntityArray(Allocator.Temp);
        var networkIds = m_NewPlayersQuery.ToComponentDataArray<NetworkId>(Allocator.Temp);

        // 새 플레이어 스폰 전에 연결이 끊긴 죽은 플레이어들 정리
        var deadPlayersToRemove = new NativeList<Entity>(Allocator.Temp);
        foreach (var (connectionOwner, playerEntity) in
                 SystemAPI.Query<RefRO<ConnectionOwner>>()
                     .WithAll<PlayerTag, PlayerDead>()
                     .WithEntityAccess())
        {
            // 연결 Entity가 더 이상 존재하지 않으면 삭제 대상
            if (!state.EntityManager.Exists(connectionOwner.ValueRO.Entity))
            {
                deadPlayersToRemove.Add(playerEntity);
            }
        }

        foreach (var deadPlayer in deadPlayersToRemove)
        {
            UnityEngine.Debug.Log($"[PlayerSpawnSystem] Removing orphaned dead player");
            state.EntityManager.DestroyEntity(deadPlayer);
        }
        deadPlayersToRemove.Dispose();

        for (var i = 0; i < connectionEntities.Length; i++)
        {
            var networkId = networkIds[i];
            var connectionEntity = connectionEntities[i];

            var player = state.EntityManager.Instantiate(prefab);

            UnityEngine.Debug.Log($"[SpawnPlayerSystem][{state.WorldUnmanaged.Name}] Spawning player for NetworkId {networkId.Value}");

            // 스폰 위치 오프셋 (겹치지 않게)
            var localTransform = state.EntityManager.GetComponentData<LocalTransform>(prefab);
            localTransform.Position.x += networkId.Value * 2;
            state.EntityManager.SetComponentData(player, localTransform);

            // GhostOwner 설정 (네트워크 소유권)
            state.EntityManager.SetComponentData(player, new GhostOwner { NetworkId = networkId.Value });

            // CommandTarget 설정 (입력 라우팅)
            state.EntityManager.SetComponentData(connectionEntity, new CommandTarget { targetEntity = player });

            // ConnectionOwner 추가 (역참조 - 연결 끊김 시 플레이어 정리용)
            state.EntityManager.AddComponentData(player, new ConnectionOwner { Entity = connectionEntity });

            // 참고: LinkedEntityGroup에 추가하지 않음!
            // 연결이 끊겨도 플레이어 Entity는 자동 삭제되지 않음
            // PlayerDisconnectCleanupSystem에서 명시적으로 처리

            // PlayerSpawned 마커 추가 (중복 스폰 방지)
            state.EntityManager.AddComponent<PlayerSpawned>(connectionEntity);
        }
    }
}
