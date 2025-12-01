using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
[BurstCompile]
public partial struct EnemyChaseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 플레이어와 적이 모두 존재할 때만 실행
        state.RequireForUpdate<PlayerTag>();
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 플레이어 위치 쿼리 (한 번만)
        float3 playerPosition = float3.zero;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPosition = transform.ValueRO.Position;
            break; // 플레이어는 1명이므로 첫 번째만
        }

        // 모든 몬스터를 병렬로 처리
        new EnemyChaseJob
        {
            PlayerPosition = playerPosition,
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct EnemyChaseJob : IJobEntity
{
    public float3 PlayerPosition;
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in EnemySpeed speed)
    {
        // Transform 직접 수정 방식으로 복구
        float3 direction = PlayerPosition - transform.Position;
        direction.y = 0;

        float distanceSq = math.lengthsq(direction);

        if (distanceSq > 0.01f)
        {
            float3 normalizedDirection = math.normalize(direction);
            float3 movement = normalizedDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            quaternion targetRotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 10f * DeltaTime);
        }
    }
}
