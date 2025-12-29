using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 입력을 실제 움직임으로 변환 (예측 시뮬레이션)
/// NetcodeSamples 05_SpawnPlayer의 ProcessAutoCommandsSystem 패턴
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProcessPlayerInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInput>();
        state.RequireForUpdate<EnableMultiplayer>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (input, transform, speed, modifiers) in
                 SystemAPI.Query<RefRO<PlayerInput>, RefRW<LocalTransform>, RefRO<MovementSpeed>, RefRO<StatModifiers>>()
                     .WithAll<Simulate>()  // Netcode 예측 플래그
                     .WithDisabled<PlayerDead>())  // 죽은 플레이어는 움직이지 않음
        {
            // 입력 기반 이동
            if (input.ValueRO.Horizontal != 0 || input.ValueRO.Vertical != 0)
            {
                float3 movement = new float3(
                    input.ValueRO.Horizontal,
                    0,
                    input.ValueRO.Vertical
                );

                // 버프 적용된 이동 속도 계산
                float effectiveSpeed = speed.ValueRO.Value * modifiers.ValueRO.SpeedMultiplier;
                movement = math.normalizesafe(movement) * effectiveSpeed * deltaTime;
                transform.ValueRW.Position += movement;
                transform.ValueRW.Position.y = 0.5f;  // 높이 고정

                // 이동 방향으로 회전
                if (math.lengthsq(movement) > 0.01f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(movement, math.up());
                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRW.Rotation,
                        targetRotation,
                        10f * deltaTime
                    );
                }
            }

            // 발사 처리는 AutoShootSystem에서 자동 처리
            // PlayerInput.Fire는 수동 발사용으로 예약됨
        }
    }
}
