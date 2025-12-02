using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 몬스터-플레이어 충돌 처리 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemyChaseSystem))]
[BurstCompile]
public partial struct PlayerDamageSystem : ISystem
{
    private double lastDamageTime;
    private const float DamageCooldown = 1.0f; // 1초 쿨다운 (연속 데미지 방지)

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
        state.RequireForUpdate<EnemyTag>();
        lastDamageTime = 0;
    }

    public void OnUpdate(ref SystemState state)
    {
        var currentTime = SystemAPI.Time.ElapsedTime;

        // 쿨다운 체크 (너무 빠른 연속 데미지 방지)
        if (currentTime - lastDamageTime < DamageCooldown)
        {
            return;
        }

        bool damageOccurred = false;

        // 플레이어 위치 가져오기
        foreach (var (playerTransform, playerHealth, playerEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerHealth>>()
                     .WithAll<PlayerTag>()
                     .WithEntityAccess())
        {
            float3 playerPos = playerTransform.ValueRO.Position;

            // 모든 Enemy와 거리 체크
            foreach (var (enemyTransform, enemyEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<EnemyTag>()
                         .WithEntityAccess())
            {
                float3 enemyPos = enemyTransform.ValueRO.Position;
                float distance = math.distance(playerPos, enemyPos);

                // 충돌 반경: Player(0.5) + Enemy(0.5) = 1.0
                if (distance < 1.0f)
                {
                    // 플레이어 데미지 적용 (고정 데미지 10)
                    playerHealth.ValueRW.CurrentHealth -= 10f;
                    damageOccurred = true;
                    break; // 한 프레임에 한 번만 데미지 적용
                }
            }

            // 데미지가 발생했으면 쿨다운 시작 및 체력 체크
            if (damageOccurred)
            {
                lastDamageTime = currentTime;

                // 체력 0 이하 체크
                if (playerHealth.ValueRO.CurrentHealth <= 0)
                {
                    Debug.Log("Game Over! Player Health: 0");
                    // Phase 6에서 게임 오버 UI로 확장 예정
                }

                break; // 플레이어는 하나뿐이므로 종료
            }
        }
    }
}
