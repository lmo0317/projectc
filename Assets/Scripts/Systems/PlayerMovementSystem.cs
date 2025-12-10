using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 싱글플레이 전용 플레이어 움직임 시스템
/// 멀티플레이 모드에서는 ProcessPlayerInputSystem이 대신 동작
/// </summary>
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
        // 멀티플레이 모드에서는 실행하지 않음
        if (SystemAPI.HasSingleton<EnableMultiplayer>())
            return;

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
        // int Horizontal/Vertical을 float3로 변환
        if (input.Horizontal != 0 || input.Vertical != 0)
        {
            // 입력값을 float3로 변환 (XZ 평면)
            float3 moveDirection = new float3(
                input.Horizontal,
                0,
                input.Vertical
            );

            // 정규화하여 대각선 이동 속도 보정
            moveDirection = math.normalizesafe(moveDirection);

            // Transform 위치 업데이트
            float3 movement = moveDirection * speed.Value * DeltaTime;
            transform.Position += movement;

            // 이동 방향으로 즉시 회전
            if (math.lengthsq(moveDirection) > 0.01f)
            {
                quaternion targetRotation = quaternion.LookRotationSafe(moveDirection, math.up());
                transform.Rotation = targetRotation;
            }
        }
    }
}
