using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 네트워크 연결을 InGame 상태로 만들어 Ghost 동기화 시작
/// Client와 Server 둘 다에서 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial class GoInGameSystem : SystemBase
{
    private EntityQuery m_NewConnections;

    protected override void OnCreate()
    {
        RequireForUpdate<EnableGoInGame>();
        m_NewConnections = SystemAPI.QueryBuilder()
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>()
            .Build();
        RequireForUpdate(m_NewConnections);
    }

    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        FixedString32Bytes worldName = World.Name;

        // NetworkId가 설정되면 즉시 InGame 상태로 전환
        foreach (var (id, ent) in SystemAPI.Query<NetworkId>()
                     .WithNone<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            Debug.Log($"[{worldName}] Go in game connection {id.Value}");
            commandBuffer.AddComponent<NetworkStreamInGame>(ent);
        }

        commandBuffer.Playback(EntityManager);
    }
}
