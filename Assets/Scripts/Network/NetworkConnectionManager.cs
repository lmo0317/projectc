using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 연결 관리자
/// Dedicated 서버 / 클라이언트 분리 모드 지원
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

    // 현재 모드
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
    /// Dedicated 서버로 시작 (서버만 실행, 화면 없음)
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

    /// <summary>
    /// 호스트로 게임 시작 (서버 + 클라이언트 동시 실행)
    /// 테스트용 또는 싱글플레이용
    /// </summary>
    public void StartAsHost()
    {
        Debug.Log($"[NetworkConnectionManager] Starting as Host on port {port}");
        currentMode = NetworkMode.Server;  // 서버 역할도 함

        // 기존 Local World 제거
        DestroyLocalWorld();

        // Server World 생성
        var serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        SetTickRate(serverWorld);

        // Client World 생성
        var clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        SetTickRate(clientWorld);

        // DefaultGameObjectInjectionWorld 설정
        World.DefaultGameObjectInjectionWorld = clientWorld;

        // 게임 씬 로드
        SceneManager.sceneLoaded += OnHostSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnHostSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnHostSceneLoaded;

        if (scene.name != gameSceneName)
            return;

        Debug.Log($"[NetworkConnectionManager] Host scene loaded: {scene.name}");

        // 서버 Listen
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
                    Debug.Log($"[NetworkConnectionManager] Host server listening on port {port}");
                }
                else
                {
                    Debug.LogError($"[NetworkConnectionManager] Failed to listen on port {port}");
                }
            }
        }

        // 클라이언트 로컬 연결
        var clientWorld = GetClientWorld();
        if (clientWorld != null)
        {
            var clientEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort(port);
            using var clientQuery = clientWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());

            if (!clientQuery.IsEmpty)
            {
                var connectionEntity = clientQuery.GetSingletonRW<NetworkStreamDriver>()
                    .ValueRW.Connect(clientWorld.EntityManager, clientEndpoint);

                if (connectionEntity != Entity.Null)
                {
                    Debug.Log($"[NetworkConnectionManager] Host client connecting to localhost:{port}");
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
    /// 로비로 돌아가기 (네트워크 연결 해제)
    /// </summary>
    public void ReturnToLobby()
    {
        Debug.Log("[NetworkConnectionManager] Returning to lobby");
        currentMode = NetworkMode.None;

        // 모든 네트워크 World 제거
        var worldsToDestroy = new List<World>();
        foreach (var world in World.All)
        {
            if (world.IsClient() || world.IsServer())
            {
                worldsToDestroy.Add(world);
            }
        }

        foreach (var world in worldsToDestroy)
        {
            Debug.Log($"[NetworkConnectionManager] Disposing {world.Name}");
            world.Dispose();
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
