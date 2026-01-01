using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 연결 관리자
///
/// === 정책 ===
/// 1. 서버 (Dedicated Server)
///    - 서버만 실행 (클라이언트 없음)
///    - 한번 켜지면 계속 유지 (종료 없음)
///    - 플레이어 1명 이상 접속 시 게임 시작
///    - 모든 플레이어가 죽고 나가면 게임 초기화
///    - 새 플레이어 접속 시 처음부터 다시 시작
///
/// 2. 클라이언트
///    - 서버에 접속만 함
///    - 죽으면 "나가기" 버튼으로 로비 복귀
///    - 로비에서 다시 접속 가능
/// </summary>
public class NetworkConnectionManager : MonoBehaviour
{
    public static NetworkConnectionManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private ushort port = 7979;
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private string gameSceneName = "GameSceneSpace";

    public string ServerAddress
    {
        get => serverAddress;
        set => serverAddress = value;
    }

    public ushort Port
    {
        get => port;
        set => port = value;
    }

    // 현재 모드 (서버 또는 클라이언트만)
    private enum NetworkMode { None, Server, Client }
    private NetworkMode currentMode = NetworkMode.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Dedicated 서버로 시작 (서버만 실행)
    /// 서버는 한번 시작되면 계속 유지됨
    /// </summary>
    public void StartDedicatedServer()
    {
        Debug.Log($"[NetworkConnectionManager] Starting Dedicated Server on port {port}");
        currentMode = NetworkMode.Server;

        // 기존 Local World 제거
        DestroyLocalWorld();

        // Server World만 생성
        var serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        SetTickRate(serverWorld);

        // DefaultGameObjectInjectionWorld 설정
        World.DefaultGameObjectInjectionWorld = serverWorld;

        // 게임 씬 로드
        SceneManager.sceneLoaded += OnServerSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnServerSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnServerSceneLoaded;

        if (scene.name != gameSceneName)
            return;

        Debug.Log($"[NetworkConnectionManager] Server scene loaded: {scene.name}");

        // 서버 Listen 시작
        var serverWorld = GetServerWorld();
        if (serverWorld != null)
        {
            var serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
            using var serverQuery = serverWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());

            if (!serverQuery.IsEmpty)
            {
                bool success = serverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
                if (success)
                {
                    Debug.Log($"[NetworkConnectionManager] Server listening on port {port}");
                    Debug.Log("[NetworkConnectionManager] Server is ready - waiting for players");
                }
                else
                {
                    Debug.LogError($"[NetworkConnectionManager] Failed to listen on port {port}");
                }
            }
        }
    }

    /// <summary>
    /// 클라이언트로 서버에 접속
    /// </summary>
    public void ConnectToServer()
    {
        ConnectToServer(serverAddress, port);
    }

    /// <summary>
    /// 클라이언트로 지정된 서버에 접속
    /// </summary>
    public void ConnectToServer(string address, ushort serverPort)
    {
        Debug.Log($"[NetworkConnectionManager] Connecting to {address}:{serverPort}");
        currentMode = NetworkMode.Client;
        serverAddress = address;
        port = serverPort;

        // 기존 Local World 제거
        DestroyLocalWorld();

        // Client World만 생성
        var clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        SetTickRate(clientWorld);

        // DefaultGameObjectInjectionWorld 설정
        World.DefaultGameObjectInjectionWorld = clientWorld;

        // 게임 씬 로드
        SceneManager.sceneLoaded += OnClientSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnClientSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnClientSceneLoaded;

        if (scene.name != gameSceneName)
            return;

        Debug.Log($"[NetworkConnectionManager] Client scene loaded: {scene.name}");

        // 서버에 연결
        var clientWorld = GetClientWorld();
        if (clientWorld != null)
        {
            var clientEndpoint = NetworkEndpoint.Parse(serverAddress, port);
            using var clientQuery = clientWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());

            if (!clientQuery.IsEmpty)
            {
                var connectionEntity = clientQuery.GetSingletonRW<NetworkStreamDriver>()
                    .ValueRW.Connect(clientWorld.EntityManager, clientEndpoint);

                if (connectionEntity != Entity.Null)
                {
                    Debug.Log($"[NetworkConnectionManager] Client connecting to {serverAddress}:{port}");
                }
                else
                {
                    Debug.LogError("[NetworkConnectionManager] Failed to initiate client connection");
                }
            }
        }
    }

    private World GetServerWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsServer())
                return world;
        }
        return null;
    }

    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient())
                return world;
        }
        return null;
    }

    private void DestroyLocalWorld()
    {
        var worldsToDestroy = new List<World>();
        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                worldsToDestroy.Add(world);
            }
        }

        foreach (var world in worldsToDestroy)
        {
            Debug.Log($"[NetworkConnectionManager] Destroying local world: {world.Name}");
            world.Dispose();
        }
    }

    private void SetTickRate(World world)
    {
        if (world.EntityManager.CreateEntityQuery(typeof(ClientServerTickRate)).IsEmpty)
        {
            var tickRateEntity = world.EntityManager.CreateEntity(typeof(ClientServerTickRate));
            world.EntityManager.SetComponentData(tickRateEntity, new ClientServerTickRate
            {
                SimulationTickRate = 20,
                NetworkTickRate = 20,
                MaxSimulationStepsPerFrame = 8,
                TargetFrameRateMode = ClientServerTickRate.FrameRateMode.Sleep
            });
        }
    }

    /// <summary>
    /// 로비로 돌아가기 (클라이언트 전용)
    /// 서버는 절대 종료되지 않음
    /// </summary>
    public void ReturnToLobby()
    {
        Debug.Log($"[NetworkConnectionManager] Returning to lobby (current mode: {currentMode})");

        if (currentMode == NetworkMode.Client)
        {
            ReturnToLobbyAsClient();
        }
        else if (currentMode == NetworkMode.Server)
        {
            // 서버는 로비로 돌아가지 않음 - 계속 실행
            Debug.LogWarning("[NetworkConnectionManager] Server cannot return to lobby - server must keep running");
        }
    }

    /// <summary>
    /// 클라이언트가 로비로 돌아갈 때
    /// 서버 연결 끊고 로비로 이동
    /// </summary>
    private void ReturnToLobbyAsClient()
    {
        Debug.Log("[NetworkConnectionManager] Client returning to lobby");
        currentMode = NetworkMode.None;

        // 클라이언트 World만 제거
        var clientWorld = GetClientWorld();
        if (clientWorld != null)
        {
            Debug.Log($"[NetworkConnectionManager] Disposing client world: {clientWorld.Name}");
            clientWorld.Dispose();
        }

        // 로컬 World 재생성
        ClientServerBootstrap.CreateLocalWorld("DefaultWorld");

        // 로비 씬으로 이동
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// 현재 서버 모드인지 확인
    /// </summary>
    public bool IsServer => currentMode == NetworkMode.Server;

    /// <summary>
    /// 현재 클라이언트 모드인지 확인
    /// </summary>
    public bool IsClient => currentMode == NetworkMode.Client;
}
