using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// Multiplayer Play Mode 호환 네트워크 부트스트랩
/// ClientServerBootstrap 상속을 통해 자동 연결 지원
/// </summary>
[UnityEngine.Scripting.Preserve]
public class SimpleNetworkBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // 백그라운드에서도 실행되도록 설정 (멀티플레이 필수)
        Application.runInBackground = true;

        // AutoConnectPort 설정으로 자동 연결 활성화
        // 서버는 AutoConnectPort에서 리스닝, 클라이언트는 loopback:AutoConnectPort로 연결
        AutoConnectPort = 7979;

        // 기본 클라이언트/서버 World 생성 (Multiplayer Play Mode 설정에 따라 결정됨)
        CreateDefaultClientServerWorlds();

        // 생성된 World에 Tick Rate 설정 추가
        foreach (var world in World.All)
        {
            if (world.IsServer() || world.IsClient())
            {
                SetTickRate(world);
            }
        }

        Debug.Log($"[SimpleNetworkBootstrap] Initialized with AutoConnectPort={AutoConnectPort}");
        return true;
    }

    private void SetTickRate(World world)
    {
        if (world.EntityManager.CreateEntityQuery(typeof(ClientServerTickRate)).IsEmpty)
        {
            var tickRateEntity = world.EntityManager.CreateEntity(typeof(ClientServerTickRate));
            world.EntityManager.SetComponentData(tickRateEntity, new ClientServerTickRate
            {
                // Editor에서는 성능 경고가 자주 발생하므로 낮은 틱 레이트 사용
                SimulationTickRate = 20,  // 20Hz로 낮춤
                NetworkTickRate = 20,      // 20Hz로 낮춤
                MaxSimulationStepsPerFrame = 8,  // 더 많은 보정 허용
                TargetFrameRateMode = ClientServerTickRate.FrameRateMode.Sleep
            });
        }
    }
}
