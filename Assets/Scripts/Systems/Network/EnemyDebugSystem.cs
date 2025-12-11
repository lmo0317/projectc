using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Enemy 이동 디버그 시스템 (임시)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class EnemyDebugSystem : SystemBase
{
    private double nextLogTime = 0;

    protected override void OnCreate()
    {
        RequireForUpdate<EnemyTag>();
    }

    protected override void OnUpdate()
    {
        // 1초마다 로그
        if (SystemAPI.Time.ElapsedTime < nextLogTime)
            return;

        nextLogTime = SystemAPI.Time.ElapsedTime + 1.0;

        string worldName = World.Name;
        int enemyCount = 0;

        foreach (var (transform, speed) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemySpeed>>()
            .WithAll<EnemyTag>())
        {
            enemyCount++;
            if (enemyCount == 1) // 첫 번째 Enemy만 로그
            {
                Debug.Log($"[{worldName}] Enemy Position: {transform.ValueRO.Position}, Speed: {speed.ValueRO.Value}");
            }
        }

        Debug.Log($"[{worldName}] Total Enemies: {enemyCount}");
    }
}
