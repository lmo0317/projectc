using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Star 아이템 이동 시스템 (스폰 후 튀어오르는 효과)
/// Server에서만 실행
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct StarMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StarTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, movement) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<StarMovement>>()
                     .WithAll<StarTag>())
        {
            // 이미 정착한 Star는 처리하지 않음
            if (movement.ValueRO.IsSettled)
                continue;

            // 중력 적용
            movement.ValueRW.Velocity.y -= movement.ValueRO.Gravity * deltaTime;

            // 위치 업데이트
            float3 newPosition = transform.ValueRO.Position + movement.ValueRO.Velocity * deltaTime;

            // 바닥 충돌 체크
            if (newPosition.y <= movement.ValueRO.GroundY)
            {
                newPosition.y = movement.ValueRO.GroundY;

                // 튀는 효과 (수직 속도 반전 + 감쇠)
                float bounceVelocity = -movement.ValueRO.Velocity.y * movement.ValueRO.BounceDamping;

                // 속도가 충분히 작으면 정착
                if (math.abs(bounceVelocity) < 0.5f)
                {
                    movement.ValueRW.Velocity = float3.zero;
                    movement.ValueRW.IsSettled = true;
                }
                else
                {
                    movement.ValueRW.Velocity.y = bounceVelocity;
                    // 수평 속도도 감쇠
                    movement.ValueRW.Velocity.x *= movement.ValueRO.BounceDamping;
                    movement.ValueRW.Velocity.z *= movement.ValueRO.BounceDamping;
                }
            }

            transform.ValueRW.Position = newPosition;
        }
    }
}
