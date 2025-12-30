using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 자석 효과 시스템 (Server에서만 실행)
/// Magnet 버프가 있는 플레이어 주변의 Star를 끌어당김
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StatCalculationSystem))]
[UpdateBefore(typeof(StarCollectSystem))]
[BurstCompile]
public partial struct MagnetSystem : ISystem
{
    // 거리 기반 속도 설정
    private const float MinSpeed = 5f;    // 가까울 때 최소 속도
    private const float MaxSpeed = 30f;   // 멀 때 최대 속도

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StarTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 플레이어 위치와 자석 범위 수집
        var playerPositions = new NativeList<float3>(Allocator.Temp);
        var magnetRanges = new NativeList<float>(Allocator.Temp);

        foreach (var (transform, modifiers) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<StatModifiers>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>())
        {
            // 자석 버프가 있는 경우에만 추가
            if (modifiers.ValueRO.MagnetRange > 0f)
            {
                playerPositions.Add(transform.ValueRO.Position);
                magnetRanges.Add(modifiers.ValueRO.MagnetRange);
            }
        }

        // 자석 효과가 있는 플레이어가 없으면 종료
        if (playerPositions.Length == 0)
        {
            playerPositions.Dispose();
            magnetRanges.Dispose();
            return;
        }

        // 모든 Star에 대해 자석 효과 적용
        foreach (var (starTransform, starEntity) in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<StarTag>()
                     .WithEntityAccess())
        {
            float3 starPos = starTransform.ValueRO.Position;
            float3 closestPlayerPos = float3.zero;
            float closestDistance = float.MaxValue;
            bool inRange = false;

            // 가장 가까운 플레이어 찾기 (자석 범위 내)
            float closestMagnetRange = 0f;
            for (int i = 0; i < playerPositions.Length; i++)
            {
                float3 playerPos = playerPositions[i];
                float magnetRange = magnetRanges[i];
                float distance = math.distance(starPos, playerPos);

                if (distance <= magnetRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayerPos = playerPos;
                    closestMagnetRange = magnetRange;
                    inRange = true;
                }
            }

            // 자석 범위 내에 있으면 플레이어 방향으로 이동
            if (inRange)
            {
                float3 direction = math.normalizesafe(closestPlayerPos - starPos);

                // 거리에 따른 속도 계산: 멀수록 빠르게, 가까울수록 느리게
                float distanceRatio = closestDistance / closestMagnetRange;  // 0 ~ 1 (가까움 ~ 멂)
                float speed = math.lerp(MinSpeed, MaxSpeed, distanceRatio);

                float moveDistance = speed * deltaTime;

                // 플레이어보다 더 멀리 가지 않도록 제한
                if (moveDistance > closestDistance)
                {
                    moveDistance = closestDistance;
                }

                starTransform.ValueRW.Position += direction * moveDistance;
            }
        }

        playerPositions.Dispose();
        magnetRanges.Dispose();
    }
}
