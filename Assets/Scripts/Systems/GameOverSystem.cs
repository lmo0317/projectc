using Unity.Entities;
using UnityEngine;

/// <summary>
/// 게임 오버 감지 및 처리 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerDamageSystem))]
public partial class GameOverSystem : SystemBase
{
    private UIManager uiManager;
    private bool gameOverTriggered = false;

    protected override void OnCreate()
    {
        RequireForUpdate<PlayerHealth>();
        RequireForUpdate<GameStats>();
    }

    protected override void OnStartRunning()
    {
        uiManager = Object.FindObjectOfType<UIManager>();
    }

    protected override void OnUpdate()
    {
        if (gameOverTriggered) return;

        // EntityCommandBuffer 생성
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

        // 플레이어 체력 체크
        foreach (var health in SystemAPI.Query<RefRO<PlayerHealth>>().WithAll<PlayerTag>())
        {
            if (health.ValueRO.CurrentHealth <= 0)
            {
                // 게임 오버 트리거
                TriggerGameOver(ecb);
                gameOverTriggered = true;
            }
            break;
        }
    }

    private void TriggerGameOver(EntityCommandBuffer ecb)
    {
        // 게임 통계 가져오기
        float survivalTime = 0f;
        int killCount = 0;

        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            survivalTime = stats.ValueRO.SurvivalTime;
            killCount = stats.ValueRO.KillCount;
            break;
        }

        // UI 표시
        if (uiManager != null)
        {
            uiManager.ShowGameOver(survivalTime, killCount);
        }

        // GameOverTag 추가 (EntityCommandBuffer 사용)
        Entity gameOverEntity = ecb.CreateEntity();
        ecb.AddComponent(gameOverEntity, new GameOverTag());

        Debug.Log($"Game Over! Survival Time: {survivalTime:F1}s, Kills: {killCount}");
    }
}
