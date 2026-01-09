using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

/// <summary>
/// 자동 발사 시스템 (Server만 실행)
/// 이전 프레임의 Physics 데이터를 사용 (WaitForJobGroupID 방지)
/// StatCalculationSystem 이후에 실행하여 버프 적용된 스탯 사용
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StatCalculationSystem))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AutoShootConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 버프 선택 중이면 발사 중지
        foreach (var sessionState in SystemAPI.Query<RefRO<GameSessionState>>())
        {
            if (sessionState.ValueRO.IsGamePaused)
                return;
        }

        // PhysicsWorld 싱글톤 접근
        if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
        {
            return; // Physics 시스템 초기화 대기
        }

        var collisionWorld = physicsWorldSingleton.PhysicsWorld.CollisionWorld;
        float deltaTime = SystemAPI.Time.DeltaTime;

        // ECB 사용 (즉시 실행되는 ECB)
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 살아있는 플레이어만 발사
        foreach (var (transform, shootConfig, leftFirePoint, rightFirePoint, modifiers, playerTag) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<LeftFirePointOffset>, RefRO<RightFirePointOffset>, RefRO<StatModifiers>, RefRO<PlayerTag>>()
                     .WithDisabled<PlayerDead>())
        {
            shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

            // 버프 적용된 발사 속도 계산
            float effectiveFireRate = shootConfig.ValueRO.BaseFireRate * modifiers.ValueRO.FireRateMultiplier;

            if (shootConfig.ValueRW.TimeSinceLastShot >= effectiveFireRate)
            {
                shootConfig.ValueRW.TimeSinceLastShot = 0f;

                // === 타겟팅 로직 (Unity.Physics OverlapSphere 사용) ===
                const float MAX_TARGETING_RANGE = 30f;
                float3 playerPos = transform.ValueRO.Position;
                float3 playerForward = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));

                Entity targetEntity = Entity.Null;
                float closestDistance = float.MaxValue;

                // Physics 쿼리로 범위 내 Enemy만 검색 (O(log N))
                var filter = new CollisionFilter
                {
                    BelongsTo = 1u << 0,    // Layer 0: Player
                    CollidesWith = 1u << 2, // Layer 2: Enemy만 검색
                    GroupIndex = 0
                };

                var hitList = new NativeList<DistanceHit>(Allocator.Temp);
                if (collisionWorld.OverlapSphere(playerPos, MAX_TARGETING_RANGE, ref hitList, filter))
                {
                    // 가장 가까운 Enemy 찾기
                    for (int i = 0; i < hitList.Length; i++)
                    {
                        float distance = hitList[i].Distance;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            targetEntity = hitList[i].Entity;
                        }
                    }
                }
                hitList.Dispose();

                // === 버프 적용된 미사일 개수 계산 ===
                int missileCount = shootConfig.ValueRO.BaseMissileCount + modifiers.ValueRO.BonusMissileCount;
                missileCount = math.max(1, missileCount);  // 최소 1개

                // === 미사일 생성 (개수만큼 반복) ===
                for (int i = 0; i < missileCount; i++)
                {
                    var bulletEntity = ecb.Instantiate(shootConfig.ValueRO.BulletPrefab);

                    // 미사일 발사 방향 계산 (여러 발일 경우 부채꼴로 퍼짐)
                    float3 shootDirection = playerForward;
                    if (missileCount > 1)
                    {
                        // 부채꼴 각도 계산 (총 30도 범위, 미사일 개수에 따라 분산)
                        float spreadAngle = 30f;  // 총 펼침 각도
                        float angleStep = spreadAngle / (missileCount - 1);
                        float currentAngle = -spreadAngle / 2f + angleStep * i;
                        quaternion rotation = quaternion.AxisAngle(math.up(), math.radians(currentAngle));
                        shootDirection = math.mul(rotation, playerForward);
                    }

                    quaternion bulletRotation = quaternion.LookRotationSafe(shootDirection, math.up());

                    // 좌우 교대 발사 위치
                    bool shootFromLeft = (shootConfig.ValueRO.ShootFromLeft != (i % 2 == 1));
                    float3 localOffset = shootFromLeft ? leftFirePoint.ValueRO.LocalOffset : rightFirePoint.ValueRO.LocalOffset;
                    float3 worldOffset = math.mul(transform.ValueRO.Rotation, localOffset);
                    float3 spawnPos = playerPos + worldOffset;

                    ecb.SetComponent(bulletEntity, LocalTransform.FromPositionRotation(spawnPos, bulletRotation));
                    ecb.SetComponent(bulletEntity, new BulletDirection { Value = shootDirection });

                    if (targetEntity != Entity.Null)
                    {
                        ecb.SetComponent(bulletEntity, new MissileTarget { TargetEntity = targetEntity });
                    }
                }

                // 다음 발사 시 좌우 반전
                shootConfig.ValueRW.ShootFromLeft = !shootConfig.ValueRO.ShootFromLeft;
            }
        }

        // ECB 즉시 실행 (반복문 밖에서)
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
