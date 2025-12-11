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
        Debug.Log("[UIUpdateSystem] OnCreate called");
        RequireForUpdate<NetworkId>();
        // PlayerHealth와 GameStats는 RequireForUpdate에서 제거 (OnUpdate에서 체크)
    }

    protected override void OnStartRunning()
    {
        Debug.Log("[UIUpdateSystem] OnStartRunning called");

        // UIManager 참조 찾기
        uiManager = Object.FindObjectOfType<UIManager>();

        if (uiManager == null)
        {
            Debug.LogWarning("[UIUpdateSystem] UIManager not found in scene!");
        }
        else
        {
            Debug.Log("[UIUpdateSystem] UIManager found successfully");
        }
    }

    protected override void OnUpdate()
    {
        if (uiManager == null) return;

        // 내 NetworkId 가져오기
        if (!SystemAPI.HasSingleton<NetworkId>())
            return;

        var myNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;

        // 1. 내 플레이어 체력 업데이트 (GhostOwner로 필터링)
        bool foundPlayer = false;
        foreach (var (health, ghostOwner) in
                 SystemAPI.Query<RefRO<PlayerHealth>, RefRO<GhostOwner>>())
        {
            Debug.Log($"[UIUpdateSystem] Checking player - NetworkId: {ghostOwner.ValueRO.NetworkId}, MyId: {myNetworkId}, Health: {health.ValueRO.CurrentHealth}/{health.ValueRO.MaxHealth}");

            if (ghostOwner.ValueRO.NetworkId != myNetworkId)
                continue;

            foundPlayer = true;
            uiManager.UpdateHealth(health.ValueRO.CurrentHealth, health.ValueRO.MaxHealth);
            Debug.Log($"[UIUpdateSystem] Updated UI - Health: {health.ValueRO.CurrentHealth}/{health.ValueRO.MaxHealth}");
            break; // 내 플레이어만 찾으면 종료
        }

        if (!foundPlayer)
        {
            Debug.LogWarning($"[UIUpdateSystem] Player not found! MyNetworkId: {myNetworkId}");
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
