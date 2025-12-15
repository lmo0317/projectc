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

        // 살아있는 플레이어만 발사 (Simulate 태그 필터링, 죽은 플레이어 제외)
        foreach (var (transform, shootConfig, leftFirePoint, rightFirePoint, playerTag) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<LeftFirePointOffset>, RefRO<RightFirePointOffset>, RefRO<PlayerTag>>()
                     .WithAll<Simulate>()
                     .WithDisabled<PlayerDead>())
        {
            // ValueRW를 통해 컴포넌트 직접 수정 (로컬 복사본 생성 방지)
            shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

            // 발사 간격 체크
            if (shootConfig.ValueRW.TimeSinceLastShot >= shootConfig.ValueRW.FireRate)
            {
                shootConfig.ValueRW.TimeSinceLastShot = 0f;

                // === 타겟팅 로직 시작 ===
                const float MAX_TARGETING_RANGE = 30f;

                float3 playerPos = transform.ValueRO.Position;
                float3 playerForward = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));

                // 현재 발사할 쪽 결정
                bool shootFromLeft = shootConfig.ValueRW.ShootFromLeft;

                // 발사 방향은 항상 플레이어 전방
                float3 shootDirection = playerForward;

                // 가장 가까운 적 찾기 (각도 제한 없음)
                Entity targetEntity = Entity.Null;
                float closestDistance = float.MaxValue;

                foreach (var (enemyTransform, enemyEntity) in
                         SystemAPI.Query<RefRO<LocalTransform>>()
                             .WithAll<EnemyTag>()
                             .WithEntityAccess())
                {
                    float3 enemyPos = enemyTransform.ValueRO.Position;
                    float distance = math.distance(playerPos, enemyPos);

                    // 범위 내 + 가장 가까운 적 찾기
                    if (distance <= MAX_TARGETING_RANGE && distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetEntity = enemyEntity;
                    }
                }
                // === 타겟팅 로직 끝 ===

                // 총알 Entity 생성
                var bulletEntity = ecb.Instantiate(shootConfig.ValueRW.BulletPrefab);

                // 총알 방향에 맞는 회전값 계산
                quaternion bulletRotation = quaternion.LookRotationSafe(shootDirection, math.up());

                // 좌우 FirePoint 선택
                float3 localOffset = shootFromLeft ? leftFirePoint.ValueRO.LocalOffset : rightFirePoint.ValueRO.LocalOffset;

                // FirePoint 오프셋을 플레이어 회전에 맞게 월드 좌표로 변환
                float3 worldOffset = math.mul(transform.ValueRO.Rotation, localOffset);
                float3 spawnPos = playerPos + worldOffset;

                // 총알 위치와 회전 설정
                ecb.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(spawnPos, bulletRotation));

                // 총알 발사 방향 설정 (비행기 전방)
                ecb.SetComponent(bulletEntity, new BulletDirection { Value = shootDirection });

                // 미사일 타겟 설정 (타겟이 있으면)
                if (targetEntity != Entity.Null)
                {
                    ecb.SetComponent(bulletEntity, new MissileTarget { TargetEntity = targetEntity });
                }

                // 좌우 교대 토글
                shootConfig.ValueRW.ShootFromLeft = !shootFromLeft;
            }
        }
    }
}
