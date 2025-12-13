using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 게임 오버 감지 시스템 (Client에서만 실행)
/// 내 플레이어의 PlayerDead가 활성화되면 GameOver UI 표시
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class GameOverSystem : SystemBase
{
    private UIManager uiManager;
    private bool wasDeadLastFrame = false;

    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }

    protected override void OnStartRunning()
    {
        uiManager = Object.FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("[GameOverSystem] UIManager not found!");
        }
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<NetworkId>())
            return;

        var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;
        bool isDeadNow = false;

        // 내 플레이어 찾기
        foreach (var (ghostOwner, entity) in
                 SystemAPI.Query<RefRO<GhostOwner>>()
                     .WithAll<PlayerHealth>()
                     .WithEntityAccess())
        {
            if (ghostOwner.ValueRO.NetworkId == myNetworkId)
            {
                // PlayerDead가 활성화되어 있는지 확인
                if (EntityManager.HasComponent<PlayerDead>(entity) &&
                    EntityManager.IsComponentEnabled<PlayerDead>(entity))
                {
                    isDeadNow = true;
                }
                break;
            }
        }

        // 죽음 상태 변화 감지 (false → true: 방금 죽음)
        if (isDeadNow && !wasDeadLastFrame)
        {
            Debug.Log($"[GameOverSystem] My player (NetworkId {myNetworkId}) died! Showing GameOver UI");

            // 게임 통계 가져오기
            float survivalTime = 0f;
            int killCount = 0;

            foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
            {
                survivalTime = stats.ValueRO.SurvivalTime;
                killCount = stats.ValueRO.KillCount;
                break;
            }

            // GameOver UI 표시
            if (uiManager != null)
            {
                uiManager.ShowGameOver(survivalTime, killCount);
            }
        }
        // 부활 감지 (true → false: 방금 부활)
        else if (!isDeadNow && wasDeadLastFrame)
        {
            Debug.Log($"[GameOverSystem] My player (NetworkId {myNetworkId}) respawned! Hiding GameOver UI");

            // GameOver UI 숨기기
            if (uiManager != null)
            {
                uiManager.HideGameOver();
            }
        }

        wasDeadLastFrame = isDeadNow;
    }
}
