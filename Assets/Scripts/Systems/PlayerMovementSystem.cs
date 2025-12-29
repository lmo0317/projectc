using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 싱글플레이 전용 플레이어 움직임 시스템
/// 멀티플레이 모드에서는 ProcessPlayerInputSystem이 대신 동작
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StatCalculationSystem))]
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

        // StatModifiers를 사용하여 이동 속도 버프 적용
        foreach (var (transform, input, speed, modifiers) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInput>, RefRO<MovementSpeed>, RefRO<StatModifiers>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>())
        {
            // int Horizontal/Vertical을 float3로 변환
            if (input.ValueRO.Horizontal != 0 || input.ValueRO.Vertical != 0)
            {
                // 입력값을 float3로 변환 (XZ 평면)
                float3 moveDirection = new float3(
                    input.ValueRO.Horizontal,
                    0,
                    input.ValueRO.Vertical
                );

                // 정규화하여 대각선 이동 속도 보정
                moveDirection = math.normalizesafe(moveDirection);

                // 버프 적용된 이동 속도 계산
                float effectiveSpeed = speed.ValueRO.Value * modifiers.ValueRO.SpeedMultiplier;

                // Transform 위치 업데이트
                float3 movement = moveDirection * effectiveSpeed * deltaTime;
                transform.ValueRW.Position += movement;

                // 이동 방향으로 즉시 회전
                if (math.lengthsq(moveDirection) > 0.01f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(moveDirection, math.up());
                    transform.ValueRW.Rotation = targetRotation;
                }
            }
        }
    }
}
