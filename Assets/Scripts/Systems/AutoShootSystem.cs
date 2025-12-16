using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 자동 발사 시스템 (Server만 실행)
/// TransformSystemGroup 직전에 실행하여 최신 위치에서 발사
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AutoShootConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // ECB 사용 (즉시 실행되는 ECB)
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 살아있는 플레이어만 발사
        foreach (var (transform, shootConfig, leftFirePoint, rightFirePoint, playerTag) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<LeftFirePointOffset>, RefRO<RightFirePointOffset>, RefRO<PlayerTag>>()
                     .WithDisabled<PlayerDead>())
        {
            shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

            if (shootConfig.ValueRW.TimeSinceLastShot >= shootConfig.ValueRW.FireRate)
            {
                shootConfig.ValueRW.TimeSinceLastShot = 0f;

                // === 타겟팅 로직 ===
                const float MAX_TARGETING_RANGE = 30f;
                float3 playerPos = transform.ValueRO.Position;
                float3 playerForward = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));

                bool shootFromLeft = shootConfig.ValueRW.ShootFromLeft;
                float3 shootDirection = playerForward;

                Entity targetEntity = Entity.Null;
                float closestDistance = float.MaxValue;

                foreach (var (enemyTransform, enemyEntity) in
                         SystemAPI.Query<RefRO<LocalTransform>>()
                             .WithAll<EnemyTag>()
                             .WithEntityAccess())
                {
                    float3 enemyPos = enemyTransform.ValueRO.Position;
                    float distance = math.distance(playerPos, enemyPos);

                    if (distance <= MAX_TARGETING_RANGE && distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetEntity = enemyEntity;
                    }
                }

                // === 미사일 생성 ===
                var bulletEntity = ecb.Instantiate(shootConfig.ValueRW.BulletPrefab);

                quaternion bulletRotation = quaternion.LookRotationSafe(shootDirection, math.up());
                float3 localOffset = shootFromLeft ? leftFirePoint.ValueRO.LocalOffset : rightFirePoint.ValueRO.LocalOffset;
                float3 worldOffset = math.mul(transform.ValueRO.Rotation, localOffset);
                float3 spawnPos = playerPos + worldOffset;

                ecb.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(spawnPos, bulletRotation));
                ecb.SetComponent(bulletEntity, new BulletDirection { Value = shootDirection });

                if (targetEntity != Entity.Null)
                {
                    ecb.SetComponent(bulletEntity, new MissileTarget { TargetEntity = targetEntity });
                }

                shootConfig.ValueRW.ShootFromLeft = !shootFromLeft;
            }
        }

        // ECB 즉시 실행 (반복문 밖에서)
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
