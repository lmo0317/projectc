using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // IJobEntity를 사용한 병렬 처리
        new PlayerMovementJob
        {
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct PlayerMovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in PlayerInput input, in MovementSpeed speed)
    {
        if (math.lengthsq(input.Movement) > 0.01f)
        {
            // 입력 방향 정규화
            float2 direction = math.normalize(input.Movement);

            // 3D 공간 이동 방향 (XZ 평면)
            float3 moveDirection = new float3(direction.x, 0, direction.y);

            // Transform 위치 업데이트
            float3 movement = moveDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            // 이동 방향으로 즉시 회전
            quaternion targetRotation = quaternion.LookRotationSafe(moveDirection, math.up());
            transform.Rotation = targetRotation;
        }
    }
}
