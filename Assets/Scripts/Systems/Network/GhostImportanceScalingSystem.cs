using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.NetCode.LowLevel.Unsafe;
using Unity.Transforms;

/// <summary>
/// Enemy Ghost의 중요도를 플레이어와의 거리에 따라 동적으로 조절
/// - 0-50m: 매 프레임 (Importance 100)
/// - 50-100m: 2프레임마다 (Importance 50)
/// - 100m+: 4프레임마다 (Importance 25)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSendSystem))] // GhostSendSystem 이전에 중요도 업데이트
[BurstCompile]
public partial struct GhostImportanceScalingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GhostOwner>(); // 플레이어 존재 시에만 실행
        state.RequireForUpdate<EnemyTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 살아있는 플레이어 위치 수집
        var playerPositions = new NativeList<float3>(Allocator.TempJob);
        foreach (var transform in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<GhostOwner, PlayerHealth>()
                     .WithDisabled<PlayerDead>())
        {
            playerPositions.Add(transform.ValueRO.Position);
        }

        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            return;
        }

        // Enemy 수 계산 (디버그용)
        int enemyCount = 0;
        foreach (var _ in SystemAPI.Query<RefRO<EnemyTag>>())
        {
            enemyCount++;
        }

        UnityEngine.Debug.Log($"[GhostImportanceScalingSystem] 실행 중 - Enemy: {enemyCount}, Player: {playerPositions.Length}");

        // Job으로 병렬 처리
        var job = new UpdateImportanceJob
        {
            PlayerPositions = playerPositions.AsArray()
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
        playerPositions.Dispose(state.Dependency);
    }
}

[BurstCompile]
public partial struct UpdateImportanceJob : IJobEntity
{
    [ReadOnly] public NativeArray<float3> PlayerPositions;

    void Execute(ref GhostDistanceImportance importance, in LocalTransform transform)
    {
        // 가장 가까운 플레이어와의 거리 계산
        float closestDistSq = float.MaxValue;
        for (int i = 0; i < PlayerPositions.Length; i++)
        {
            float distSq = math.distancesq(transform.Position, PlayerPositions[i]);
            closestDistSq = math.min(closestDistSq, distSq);
        }

        float closestDist = math.sqrt(closestDistSq);

        // 거리 기반 중요도 설정
        if (closestDist < 50f)
        {
            importance.ImportanceValue = 100; // 매 프레임
        }
        else if (closestDist < 100f)
        {
            importance.ImportanceValue = 50; // 2프레임마다
        }
        else
        {
            importance.ImportanceValue = 25; // 4프레임마다
        }
    }
}
