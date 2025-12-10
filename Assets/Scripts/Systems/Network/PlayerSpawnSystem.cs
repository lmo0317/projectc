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

        // 새로운 플레이어 쿼리: PlayerSpawned 태그가 없는 연결
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

            // LinkedEntityGroup에 추가 (연결 끊김 시 자동 삭제)
            state.EntityManager.GetBuffer<LinkedEntityGroup>(connectionEntity)
                .Add(new LinkedEntityGroup { Value = player });

            // ConnectionOwner 추가 (역참조)
            state.EntityManager.AddComponentData(player, new ConnectionOwner { Entity = connectionEntity });

            // PlayerSpawned 마커 추가 (중복 스폰 방지)
            state.EntityManager.AddComponent<PlayerSpawned>(connectionEntity);
        }
    }
}
