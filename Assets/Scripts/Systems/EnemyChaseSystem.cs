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
        // 플레이어로의 방향 벡터 계산
        float3 direction = PlayerPosition - transform.Position;

        // Y축은 무시 (XZ 평면 이동)
        direction.y = 0;

        // 거리 확인 (제곱 거리로 비교하여 sqrt 연산 절약)
        float distanceSq = math.lengthsq(direction);

        // 최소 거리 체크 (0.1 유닛 이내면 이동 안 함)
        if (distanceSq > 0.01f) // 0.1 * 0.1
        {
            // 방향 정규화
            float3 normalizedDirection = math.normalize(direction);

            // 이동 거리 = 방향 * 속도 * deltaTime
            float3 movement = normalizedDirection * speed.Value * DeltaTime;

            // Transform 위치 업데이트
            transform.Position += movement;
        }
    }
}
