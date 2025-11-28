using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AutoShootSystem))]
[BurstCompile]
public partial struct BulletMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        new BulletMovementJob
        {
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct BulletMovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in BulletDirection direction, in BulletSpeed speed)
    {
        // 방향 * 속도 * 시간 = 이동량
        float3 movement = direction.Value * speed.Value * DeltaTime;
        transform.Position += movement;
    }
}
