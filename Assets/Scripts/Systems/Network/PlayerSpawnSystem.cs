using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 클라이언트 연결 시 플레이어 스폰
/// Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerSpawnSystem : SystemBase
{
    private Entity m_PlayerPrefab;

    protected override void OnCreate()
    {
        RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    protected override void OnUpdate()
    {
        // Prefab 찾기 (처음 한 번만)
        if (m_PlayerPrefab == Entity.Null)
        {
            // PlayerTag를 가진 Entity 찾기
            var query = EntityManager.CreateEntityQuery(typeof(PlayerTag), typeof(Prefab));
            var prefabs = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            if (prefabs.Length > 0)
            {
                m_PlayerPrefab = prefabs[0];
                Debug.Log($"[PlayerSpawnSystem] Found PlayerPrefab: {m_PlayerPrefab}");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnSystem] PlayerPrefab not found! Make sure PlayerPrefab has PlayerTag and is in the scene.");
            }

            prefabs.Dispose();

            if (m_PlayerPrefab == Entity.Null)
                return;
        }

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);

        // 연결된 클라이언트 중 플레이어가 없는 경우
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithNone<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            // InGame 태그 추가 (스폰 완료 표시)
            ecb.AddComponent<NetworkStreamInGame>(entity);

            // 플레이어 스폰
            var player = ecb.Instantiate(m_PlayerPrefab);

            // 스폰 위치 설정 (2명이므로 좌/우 배치)
            // NetworkId는 1부터 시작하므로 -1 해서 0, 1로 만듦
            int playerIndex = id.ValueRO.Value - 1;
            float3 spawnPosition = new float3(playerIndex * 4f - 2f, 0.5f, 0f);
            // Player 0: x=-2, Player 1: x=2

            ecb.SetComponent(player, LocalTransform.FromPosition(spawnPosition));

            // 소유권 설정
            ecb.SetComponent(player, new GhostOwner { NetworkId = id.ValueRO.Value });

            Debug.Log($"[Server] Player spawned for NetworkId {id.ValueRO.Value} at {spawnPosition}");
        }
    }
}
