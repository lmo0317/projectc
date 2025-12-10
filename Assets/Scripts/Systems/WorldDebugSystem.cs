using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// World 타입과 Entity 개수를 출력하는 디버그 시스템
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class WorldDebugSystem : SystemBase
{
    private double lastLogTime = 0;

    protected override void OnUpdate()
    {
        // 5초마다 한 번씩 로그
        if (SystemAPI.Time.ElapsedTime - lastLogTime < 5.0)
            return;

        lastLogTime = SystemAPI.Time.ElapsedTime;

        // World 타입 확인
        string worldType = "Unknown";
        if (World.IsServer())
            worldType = "ServerWorld";
        else if (World.IsClient())
            worldType = "ClientWorld";
        else if (World.IsThinClient())
            worldType = "ThinClientWorld";

        // Entity 개수 세기
        int playerCount = 0;
        int bulletCount = 0;
        int connectionCount = 0;

        // PlayerTag를 가진 Entity 개수
        foreach (var _ in SystemAPI.Query<RefRO<PlayerTag>>())
            playerCount++;

        // BulletTag를 가진 Entity 개수
        foreach (var _ in SystemAPI.Query<RefRO<BulletTag>>())
            bulletCount++;

        // NetworkConnection Entity 개수
        foreach (var _ in SystemAPI.Query<RefRO<NetworkId>>())
            connectionCount++;

        Debug.Log($"[{worldType}] Players: {playerCount}, Bullets: {bulletCount}, Connections: {connectionCount}");
    }
}
