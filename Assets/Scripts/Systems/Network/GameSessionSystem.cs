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
[UpdateAfter(typeof(PlayerDisconnectCleanupSystem))]
[UpdateBefore(typeof(EnemySpawnSystem))]
public partial class GameSessionSystem : SystemBase
{
    private Entity sessionEntity;
    private bool hasInitialized = false;
    private bool needsCleanup = false;  // 다음 플레이어 접속 시 정리 필요

    protected override void OnCreate()
    {
        // GameSessionState가 존재할 때만 실행
        // 주의: NetworkId를 RequireForUpdate하면 연결이 없을 때 시스템이 멈춤!
        // 연결 없어도 계속 실행되어야 정리 로직이 동작함
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

        // PlayerDead가 활성화된 플레이어는 죽은 것으로 카운트
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

        // 총 플레이어 Entity 수
        int totalPlayerEntities = alivePlayerCount + deadPlayerCount;

        int enemyCount = CountEnemies();

        // 디버그: 매 프레임 상태 로깅 (조건이 근접할 때만 또는 연결 없을 때)
        if (sessionState.HasHadPlayers && (connectedPlayerCount == 0 || alivePlayerCount <= 1))
        {
            Debug.Log($"[GameSessionSystem] State check - connected: {connectedPlayerCount}, alive: {alivePlayerCount}, dead: {deadPlayerCount}, total: {totalPlayerEntities}, enemies: {enemyCount}, HasHadPlayers: {sessionState.HasHadPlayers}, needsCleanup: {needsCleanup}");
        }

        // 모든 플레이어가 나갔을 때 정리 플래그 설정
        // 조건: 플레이어가 있었고, 현재 연결이 없고, 살아있는 플레이어도 없음
        if (sessionState.HasHadPlayers && connectedPlayerCount == 0 && alivePlayerCount == 0)
        {
            Debug.Log($"[GameSessionSystem] All players gone - marking for cleanup (enemies: {enemyCount}, dead: {deadPlayerCount})");
            needsCleanup = true;

            // 죽은 플레이어 Entity들도 즉시 제거 (다음 접속 전에)
            CleanupDeadPlayers();

            // 세션 상태 리셋 (다음 접속 시 새 게임으로 인식)
            sessionState.HasHadPlayers = false;
            sessionState.IsGameActive = true;
            sessionState.IsGameOver = false;
            EntityManager.SetComponentData(sessionEntity, sessionState);
        }

        // 새 플레이어가 접속했을 때
        if (alivePlayerCount > 0 && !sessionState.HasHadPlayers)
        {
            // 정리가 필요하면 실행
            if (needsCleanup || enemyCount > 0 || deadPlayerCount > 0)
            {
                Debug.Log($"[GameSessionSystem] New player joined - cleaning up (enemies: {enemyCount}, stars: {CountStars()}, dead: {deadPlayerCount})");
                CleanupPreviousGame();
                needsCleanup = false;
            }

            sessionState.HasHadPlayers = true;
            EntityManager.SetComponentData(sessionEntity, sessionState);
            Debug.Log("[GameSessionSystem] First player joined - new game started");
        }
    }

    private int CountEnemies()
    {
        int count = 0;
        foreach (var _ in SystemAPI.Query<RefRO<EnemyTag>>())
        {
            count++;
        }
        return count;
    }

    private int CountStars()
    {
        int count = 0;
        foreach (var _ in SystemAPI.Query<RefRO<StarTag>>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// 이전 게임의 잔여물 정리
    /// </summary>
    private void CleanupPreviousGame()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        int enemiesRemoved = 0;
        int starsRemoved = 0;
        int bulletsRemoved = 0;
        int deadPlayersRemoved = 0;

        // 모든 Enemy 제거
        foreach (var (enemyTag, entity) in
                 SystemAPI.Query<RefRO<EnemyTag>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
            enemiesRemoved++;
        }

        // 모든 Star 제거
        foreach (var (starTag, entity) in
                 SystemAPI.Query<RefRO<StarTag>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
            starsRemoved++;
        }

        // 모든 총알 제거
        foreach (var (bulletTag, entity) in
                 SystemAPI.Query<RefRO<BulletTag>>()
                     .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
            bulletsRemoved++;
        }

        // 죽은 플레이어 Entity 제거 (enabled 상태 확인)
        foreach (var (playerTag, entity) in
                 SystemAPI.Query<RefRO<PlayerTag>>()
                     .WithEntityAccess())
        {
            if (EntityManager.HasComponent<PlayerDead>(entity) &&
                EntityManager.IsComponentEnabled<PlayerDead>(entity))
            {
                ecb.DestroyEntity(entity);
                deadPlayersRemoved++;
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        Debug.Log($"[GameSessionSystem] Cleanup done - removed {enemiesRemoved} enemies, {starsRemoved} stars, {bulletsRemoved} bullets, {deadPlayersRemoved} dead players");

        // GameStats 리셋
        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime = 0;
            stats.ValueRW.KillCount = 0;
        }
    }

    /// <summary>
    /// 죽은 플레이어 Entity만 즉시 제거 (연결 끊김 시 호출)
    /// </summary>
    private void CleanupDeadPlayers()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        int count = 0;

        foreach (var (playerTag, entity) in
                 SystemAPI.Query<RefRO<PlayerTag>>()
                     .WithEntityAccess())
        {
            if (EntityManager.HasComponent<PlayerDead>(entity) &&
                EntityManager.IsComponentEnabled<PlayerDead>(entity))
            {
                ecb.DestroyEntity(entity);
                count++;
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        if (count > 0)
        {
            Debug.Log($"[GameSessionSystem] CleanupDeadPlayers - removed {count} dead players");
        }
    }
}
