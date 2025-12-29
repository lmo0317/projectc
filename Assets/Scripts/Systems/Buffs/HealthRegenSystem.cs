using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 체력 재생 시스템 (Server에서만 실행)
/// HealthRegen 버프에 따라 초당 체력 회복
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StatCalculationSystem))]
[BurstCompile]
public partial struct HealthRegenSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StatModifiers>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (health, modifiers) in
                 SystemAPI.Query<RefRW<PlayerHealth>, RefRO<StatModifiers>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>())
        {
            // 체력 재생 버프가 있는 경우에만 처리
            if (modifiers.ValueRO.HealthRegenPerSecond > 0f)
            {
                // 최대 체력 계산 (기본 + 버프 보너스)
                float maxHealth = health.ValueRO.MaxHealth + modifiers.ValueRO.BonusMaxHealth;

                // 현재 체력이 최대 체력보다 낮으면 재생
                if (health.ValueRO.CurrentHealth < maxHealth)
                {
                    float regenAmount = modifiers.ValueRO.HealthRegenPerSecond * deltaTime;
                    health.ValueRW.CurrentHealth = math.min(
                        health.ValueRO.CurrentHealth + regenAmount,
                        maxHealth
                    );
                }
            }
        }
    }
}
