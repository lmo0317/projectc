using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 연결 상태를 콘솔에 출력하는 디버그 시스템
/// 성능 문제로 비활성화됨 - 필요시 DisableAutoCreation 속성 제거
/// </summary>
[DisableAutoCreation]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ConnectionDebugSystem : SystemBase
{
    private bool hasLoggedConnection = false;

    protected override void OnUpdate()
    {
        if (hasLoggedConnection) return;

        // Server에서 연결 확인
        if (World.IsServer())
        {
            foreach (var id in SystemAPI.Query<RefRO<NetworkId>>())
            {
                Debug.Log($"[Server] Client connected: NetworkId = {id.ValueRO.Value}");
                hasLoggedConnection = true;
            }
        }

        // Client에서 연결 확인
        if (World.IsClient())
        {
            if (SystemAPI.HasSingleton<NetworkId>())
            {
                var myId = SystemAPI.GetSingleton<NetworkId>();
                Debug.Log($"[Client] Connected to server: My NetworkId = {myId.Value}");
                hasLoggedConnection = true;
            }
        }
    }
}
