using Unity.Entities;
using UnityEngine;

/// <summary>
/// ECS 데이터를 읽어 UI를 업데이트하는 시스템 (MainThread)
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    private UIManager uiManager;

    protected override void OnCreate()
    {
        RequireForUpdate<GameStats>();
        RequireForUpdate<PlayerHealth>();
    }

    protected override void OnStartRunning()
    {
        // UIManager 참조 찾기
        uiManager = Object.FindObjectOfType<UIManager>();

        if (uiManager == null)
        {
            Debug.LogWarning("UIManager not found in scene!");
        }
    }

    protected override void OnUpdate()
    {
        if (uiManager == null) return;

        // 1. 플레이어 체력 업데이트
        foreach (var health in SystemAPI.Query<RefRO<PlayerHealth>>().WithAll<PlayerTag>())
        {
            uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            break; // 플레이어는 하나
        }

        // 2. 게임 통계 업데이트
        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            uiManager.UpdateSurvivalTime(stats.ValueRO.SurvivalTime);
            uiManager.UpdateKillCount(stats.ValueRO.KillCount);
            break; // 싱글톤
        }
    }
}
