using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 게임 통계 업데이트 시스템 (생존 시간 증가)
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct GameStatsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameStats>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // GameStats 싱글톤 업데이트
        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime += deltaTime;
            break; // 싱글톤이므로 하나만 처리
        }
    }
}
