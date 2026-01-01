using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Multiplayer Play Mode 호환 네트워크 부트스트랩
/// ClientServerBootstrap 상속을 통해 자동 연결 지원
/// LobbyScene에서는 자동 연결 비활성화
/// </summary>
[UnityEngine.Scripting.Preserve]
public class SimpleNetworkBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // 백그라운드에서도 실행되도록 설정 (멀티플레이 필수)
        Application.runInBackground = true;

        // 프레임레이트 제한 (빌드에서 무제한 fps로 인한 파티클/이펙트 문제 방지)
        Application.targetFrameRate = 60;

        // 현재 씬 확인
        var activeScene = SceneManager.GetActiveScene().name;

        // 로비 씬에서 시작하면 자동 연결 비활성화
        if (activeScene == "LobbyScene")
        {
            AutoConnectPort = 0;
            CreateLocalWorld(defaultWorldName);
            Debug.Log("[SimpleNetworkBootstrap] Lobby mode - no auto connect");
            return true;
        }

        // 게임 씬에서 직접 시작 (테스트용): 기존 자동 연결
        AutoConnectPort = 7979;
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
