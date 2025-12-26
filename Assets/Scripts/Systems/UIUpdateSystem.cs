using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// ECS 데이터를 읽어 UI를 업데이트하는 시스템 (Client에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    private UIManager uiManager;

    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }

    protected override void OnStartRunning()
    {
        uiManager = Object.FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("[UIUpdateSystem] UIManager not found in scene!");
        }
    }

    protected override void OnUpdate()
    {
        if (uiManager == null) return;

        if (!SystemAPI.HasSingleton<NetworkId>())
            return;

        var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;

        // 1. 내 플레이어 체력 및 Star 포인트 업데이트 (GhostOwner로 필터링)
        foreach (var (health, starPoints, ghostOwner) in
                 SystemAPI.Query<RefRO<PlayerHealth>, RefRO<PlayerStarPoints>, RefRO<GhostOwner>>())
        {
            if (ghostOwner.ValueRO.NetworkId != myNetworkId)
                continue;

            uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            uiManager.UpdateStarPoints(starPoints.ValueRO.CurrentPoints, starPoints.ValueRO.NextBuffThreshold);
            break;
        }

        // 2. 게임 통계 업데이트
        foreach (var stats in SystemAPI.Query<RefRO<GameStats>>())
        {
            uiManager.UpdateSurvivalTime(stats.ValueRO.SurvivalTime);
            uiManager.UpdateKillCount(stats.ValueRO.KillCount);
            break;
        }
    }
}
