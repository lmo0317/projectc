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
        // 이동 처리
        float3 movement = direction.Value * speed.Value * DeltaTime;
        transform.Position += movement;

        // 날아가는 방향으로 회전
        if (math.lengthsq(direction.Value) > 0.001f)
        {
            quaternion targetRotation = quaternion.LookRotationSafe(direction.Value, math.up());
            transform.Rotation = targetRotation;
        }
    }
}
