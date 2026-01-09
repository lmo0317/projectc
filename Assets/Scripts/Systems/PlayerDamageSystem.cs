using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 몬스터-플레이어 충돌 처리 시스템 (Server에서만 실행)
/// 플레이어가 죽으면 PlayerDead 활성화 + 화면 밖으로 이동
/// 이전 프레임의 Physics 데이터를 사용 (WaitForJobGroupID 방지)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerDamageSystem : ISystem
{
    private double lastDamageTime;
    private const float DamageCooldown = 1.0f; // 1초 쿨다운

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GhostOwner>();
        state.RequireForUpdate<EnemyTag>();
        lastDamageTime = 0;
    }

    public void OnUpdate(ref SystemState state)
    {
        var currentTime = SystemAPI.Time.ElapsedTime;

        // 쿨다운 체크
        if (currentTime - lastDamageTime < DamageCooldown)
            return;

        // PhysicsWorld 싱글톤 접근 (Physics 시스템이 초기화되어야 함)
        if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorldSingleton))
        {
            // Physics 시스템이 아직 초기화되지 않음
            return;
        }

        var collisionWorld = physicsWorldSingleton.PhysicsWorld.CollisionWorld;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 살아있는 플레이어만 처리 (PlayerDead가 비활성화된 플레이어)
        // 무적 모드(PlayerInvincible)인 플레이어는 제외
        foreach (var (playerTransform, playerHealth, ghostOwner, playerEntity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerHealth>, RefRO<GhostOwner>>()
                     .WithDisabled<PlayerDead>()
                     .WithDisabled<PlayerInvincible>()
                     .WithEntityAccess())
        {
            float3 playerPos = playerTransform.ValueRO.Position;
            bool isInDanger = false;

            // Physics 쿼리 설정
            float damageRadius = 2.0f;
            var filter = new CollisionFilter
            {
                BelongsTo = 1u << 0,    // Layer 0: Player
                CollidesWith = 1u << 2, // Layer 2: Enemy만 검색
                GroupIndex = 0
            };

            // OverlapSphere 쿼리 (BVH를 통한 O(log N) 검색)
            var hitList = new NativeList<DistanceHit>(Allocator.Temp);

            if (collisionWorld.OverlapSphere(playerPos, damageRadius, ref hitList, filter))
            {
                // 최소 1개 이상의 Enemy가 범위 내에 있음
                isInDanger = hitList.Length > 0;
            }

            hitList.Dispose();

            if (isInDanger)
            {
                // 데미지 적용
                playerHealth.ValueRW.CurrentHealth -= 10f;
                lastDamageTime = currentTime;

                Debug.Log($"[PlayerDamageSystem] Player {ghostOwner.ValueRO.NetworkId} hit! HP: {playerHealth.ValueRO.CurrentHealth}/{playerHealth.ValueRO.MaxHealth}");

                // 체력 0 이하 → 사망 처리
                if (playerHealth.ValueRO.CurrentHealth <= 0)
                {
                    playerHealth.ValueRW.CurrentHealth = 0;

                    // PlayerDead 활성화
                    ecb.SetComponentEnabled<PlayerDead>(playerEntity, true);

                    // 화면 밖으로 이동 (렌더링 안 되도록)
                    playerTransform.ValueRW.Position = new float3(0, -1000, 0);

                    Debug.Log($"[PlayerDamageSystem] Player {ghostOwner.ValueRO.NetworkId} died! Moved to Y=-1000");
                }

                break; // 한 프레임에 한 플레이어만 처리
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
