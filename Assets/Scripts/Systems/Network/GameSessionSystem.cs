using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 게임 세션 상태 관리 시스템 (Server에서만 실행)
///
/// === 정책 ===
/// - 서버는 항상 실행 유지
/// - 플레이어 1명 이상 접속 시 게임 시작
/// - 모든 플레이어가 죽고 연결이 끊기면 게임 초기화
/// - 초기화 후 새 플레이어 접속 시 처음부터 다시 시작
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(EnemySpawnSystem))]
public partial class GameSessionSystem : SystemBase
{
    private Entity sessionEntity;
    private bool hasInitialized = false;

    protected override void OnCreate()
    {
        // NetworkId가 존재할 때만 실행 (서버가 시작된 후)
        RequireForUpdate<NetworkId>();
    }

    protected override void OnStartRunning()
    {
        // 세션 상태 Entity 생성 (처음 한 번만)
        if (!hasInitialized)
        {
            sessionEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(sessionEntity, new GameSessionState
            {
                IsGameActive = true,
                HasHadPlayers = false,
                IsGameOver = false,
                GameOverTime = 0
            });

            Debug.Log("[GameSessionSystem] Session initialized - waiting for players");
            hasInitialized = true;
        }
    }

    protected override void OnUpdate()
    {
        if (!EntityManager.Exists(sessionEntity))
            return;

        var sessionState = EntityManager.GetComponentData<GameSessionState>(sessionEntity);

        // 연결된 플레이어 수 계산 (InGame 상태인 연결)
        int connectedPlayerCount = 0;
        foreach (var _ in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithAll<NetworkStreamInGame>())
        {
            connectedPlayerCount++;
        }

        // 살아있는 플레이어 수 계산
        int alivePlayerCount = 0;
        int deadPlayerCount = 0;

        foreach (var (playerTag, entity) in
                 SystemAPI.Query<RefRO<PlayerTag>>()
                     .WithAll<PlayerHealth>()
                     .WithEntityAccess())
        {
            bool isDead = EntityManager.HasComponent<PlayerDead>(entity) &&
                          EntityManager.IsComponentEnabled<PlayerDead>(entity);

            if (isDead)
                deadPlayerCount++;
            else
                alivePlayerCount++;
        }

        // 플레이어가 처음 접속했을 때
        if (alivePlayerCount > 0 && !sessionState.HasHadPlayers)
        {
            sessionState.HasHadPlayers = true;
            EntityManager.SetComponentData(sessionEntity, sessionState);
            Debug.Log("[GameSessionSystem] First player joined - game started");
        }

        // 게임 리셋 조건:
        // 1. 한 번이라도 플레이어가 있었고
        // 2. 현재 연결된 플레이어가 없고
        // 3. 살아있는 플레이어가 없을 때
        if (sessionState.HasHadPlayers && connectedPlayerCount == 0 && alivePlayerCount == 0)
        {
            Debug.Log($"[GameSessionSystem] All players gone - resetting game (dead entities: {deadPlayerCount})");
            ResetGameSession();
        }
    }

    /// <summary>
    /// 게임 세션 리셋 (새 게임 준비)
    /// </summary>
    private void ResetGameSession()
    {
        Debug.Log("[GameSessionSystem] Resetting game session...");

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 모든 Enemy 제거
        foreach (var (enemyTag, entity) in
                 SystemAPI.Query<RefRO<EnemyTag>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        // 죽은 플레이어 Entity 제거
        foreach (var (playerTag, entity) in
                 SystemAPI.Query<RefRO<PlayerTag>>()
                     .WithAll<PlayerDead>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        // 모든 총알 제거
        foreach (var (bulletTag, entity) in
                 SystemAPI.Query<RefRO<BulletTag>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        // 세션 상태 초기화
        var sessionState = EntityManager.GetComponentData<GameSessionState>(sessionEntity);
        sessionState.IsGameActive = true;
        sessionState.HasHadPlayers = false;
        sessionState.IsGameOver = false;
        EntityManager.SetComponentData(sessionEntity, sessionState);

        // GameStats 리셋
        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime = 0;
            stats.ValueRW.KillCount = 0;
        }

        Debug.Log("[GameSessionSystem] Reset complete - waiting for new players");
    }
}
