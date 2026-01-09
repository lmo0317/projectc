using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 몬스터-플레이어 충돌 처리 시스템 (Server에서만 실행)
/// 플레이어가 죽으면 PlayerDead 활성화 + 화면 밖으로 이동
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemyChaseSystem))]
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

            // 최적화: 제곱 거리 사용 (sqrt 연산 제거)
            float damageRangeSq = 2.0f * 2.0f; // 4.0

            // 모든 Enemy와 거리 체크
            foreach (var enemyTransform in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<EnemyTag>())
            {
                // 최적화: distancesq 사용 (sqrt 없음)
                float distSq = math.distancesq(playerPos, enemyTransform.ValueRO.Position);

                if (distSq < damageRangeSq)
                {
                    isInDanger = true;
                    break;
                }
            }

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
