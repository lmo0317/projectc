using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// 간단한 네트워크 연결 테스트용 Bootstrap
/// </summary>
public class SimpleNetworkBootstrap : MonoBehaviour
{
    public ushort Port = 7979;

    void Start()
    {
        // 백그라운드에서도 실행되도록 설정 (멀티플레이 필수)
        Application.runInBackground = true;

        // Editor 환경 VSync 비활성화 (성능 향상)
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30; // 30 FPS로 제한 (경고 방지)

        // Server World 생성
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");

        // Server World에 틱 레이트 설정
        SetTickRate(server);

        var serverEp = NetworkEndpoint.AnyIpv4.WithPort(Port);

        // Server 리스닝 시작
        var listenEntity = server.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
        server.EntityManager.SetComponentData(listenEntity, new NetworkStreamRequestListen { Endpoint = serverEp });

        Debug.Log($"[Server] Listening on port {Port}");

        // Client World 생성
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

        // Client World에 틱 레이트 설정
        SetTickRate(client);

        var clientEp = NetworkEndpoint.LoopbackIpv4.WithPort(Port);

        // Server에 연결
        var connectEntity = client.EntityManager.CreateEntity(typeof(NetworkStreamRequestConnect));
        client.EntityManager.SetComponentData(connectEntity, new NetworkStreamRequestConnect { Endpoint = clientEp });

        Debug.Log($"[Client] Connecting to 127.0.0.1:{Port}");
    }

    private void SetTickRate(World world)
    {
        if (world.EntityManager.CreateEntityQuery(typeof(ClientServerTickRate)).IsEmpty)
        {
            var tickRateEntity = world.EntityManager.CreateEntity(typeof(ClientServerTickRate));
            world.EntityManager.SetComponentData(tickRateEntity, new ClientServerTickRate
            {
                // Editor에서 경고 방지를 위해 30Hz로 낮춤 (빌드에서는 60Hz 사용 가능)
                SimulationTickRate = 30,
                NetworkTickRate = 30,
                MaxSimulationStepsPerFrame = 4,
                TargetFrameRateMode = ClientServerTickRate.FrameRateMode.Auto
            });
        }
    }
}
