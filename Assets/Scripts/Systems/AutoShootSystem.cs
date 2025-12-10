using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 자동 발사 시스템 (Server에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AutoShootConfig>();
        state.RequireForUpdate<EnableMultiplayer>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 플레이어의 발사 처리 (Simulate 태그 필터링)
        foreach (var (transform, shootConfig, playerTag) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<PlayerTag>>()
                     .WithAll<Simulate>())
        {
            // ValueRW를 통해 컴포넌트 직접 수정 (로컬 복사본 생성 방지)
            shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

            // 발사 간격 체크
            if (shootConfig.ValueRW.TimeSinceLastShot >= shootConfig.ValueRW.FireRate)
            {
                shootConfig.ValueRW.TimeSinceLastShot = 0f;

                // === 타겟팅 로직 시작 ===
                const float MAX_TARGETING_RANGE = 20f;
                float3 playerPos = transform.ValueRO.Position;
                float3 targetDirection = float3.zero;
                bool targetFound = false;
                float closestDistance = float.MaxValue;

                // 모든 적 탐색
                foreach (var enemyTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
                {
                    float3 enemyPos = enemyTransform.ValueRO.Position;
                    float distance = math.distance(playerPos, enemyPos);

                    // 범위 내 + 가장 가까운 적 찾기
                    if (distance <= MAX_TARGETING_RANGE && distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetDirection = math.normalize(enemyPos - playerPos);
                        targetFound = true;
                    }
                }

                // 타겟 없으면 플레이어가 바라보는 방향
                if (!targetFound)
                {
                    // 플레이어의 Forward 방향 추출
                    targetDirection = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));
                }
                // === 타겟팅 로직 끝 ===

                // 총알 Entity 생성
                var bulletEntity = ecb.Instantiate(shootConfig.ValueRW.BulletPrefab);

                // 총알 방향에 맞는 회전값 계산
                quaternion bulletRotation = quaternion.LookRotationSafe(targetDirection, math.up());

                // 총알 위치와 회전 설정
                ecb.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(playerPos, bulletRotation));

                // 총알 발사 방향 설정 (타겟팅 적용)
                ecb.SetComponent(bulletEntity, new BulletDirection { Value = targetDirection });
            }
        }
    }
}
