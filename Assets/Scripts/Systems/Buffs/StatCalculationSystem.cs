using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 버프 레벨 기반 스탯 수정치 계산 시스템 (Server에서만 실행)
/// PlayerBuffs → StatModifiers 변환
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(AutoShootSystem))]
[BurstCompile]
public partial struct StatCalculationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerBuffs>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (buffs, modifiers) in
                 SystemAPI.Query<RefRO<PlayerBuffs>, RefRW<StatModifiers>>()
                     .WithAll<PlayerTag>())
        {
            var levels = buffs.ValueRO;

            // 데미지 배율: 1.0 + 버프 보너스
            modifiers.ValueRW.DamageMultiplier = 1f + GetDamageBonus(levels.DamageLevel);

            // 공격 속도 배율: 1.0 - 버프 감소 (낮을수록 빠름)
            modifiers.ValueRW.FireRateMultiplier = 1f - GetFireRateReduction(levels.FireRateLevel);

            // 미사일 추가 개수
            modifiers.ValueRW.BonusMissileCount = GetMissileBonus(levels.MissileCountLevel);

            // 이동 속도 배율
            modifiers.ValueRW.SpeedMultiplier = 1f + GetSpeedBonus(levels.SpeedLevel);

            // 최대 체력 보너스
            modifiers.ValueRW.BonusMaxHealth = GetMaxHealthBonus(levels.MaxHealthLevel);

            // 체력 재생
            modifiers.ValueRW.HealthRegenPerSecond = GetHealthRegen(levels.HealthRegenLevel);

            // 자석 범위
            modifiers.ValueRW.MagnetRange = GetMagnetRange(levels.MagnetLevel);

            // 치명타
            var (critChance, critMult) = GetCriticalStats(levels.CriticalLevel);
            modifiers.ValueRW.CriticalChance = critChance;
            modifiers.ValueRW.CriticalMultiplier = critMult;
        }
    }

    // 레벨별 데미지 보너스 (배율)
    // Lv1: +10%, Lv2: +20%, Lv3: +35%, Lv4: +50%, Lv5: +75%
    private static float GetDamageBonus(int level)
    {
        return level switch
        {
            1 => 0.10f,
            2 => 0.20f,
            3 => 0.35f,
            4 => 0.50f,
            5 => 0.75f,
            _ => 0f
        };
    }

    // 레벨별 공격 속도 감소 (낮을수록 빠름)
    // Lv1: -15%, Lv2: -30%, Lv3: -45%, Lv4: -60%, Lv5: -80%
    private static float GetFireRateReduction(int level)
    {
        return level switch
        {
            1 => 0.15f,
            2 => 0.30f,
            3 => 0.45f,
            4 => 0.60f,
            5 => 0.80f,
            _ => 0f
        };
    }

    // 레벨별 추가 미사일 개수
    // Lv1: +1, Lv2: +2, Lv3: +3, Lv4: +4, Lv5: +6
    private static int GetMissileBonus(int level)
    {
        return level switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 6,
            _ => 0
        };
    }

    // 레벨별 이동 속도 보너스
    // Lv1: +10%, Lv2: +20%, Lv3: +30%, Lv4: +40%, Lv5: +50%
    private static float GetSpeedBonus(int level)
    {
        return level switch
        {
            1 => 0.10f,
            2 => 0.20f,
            3 => 0.30f,
            4 => 0.40f,
            5 => 0.50f,
            _ => 0f
        };
    }

    // 레벨별 최대 체력 보너스
    // Lv1: +20, Lv2: +40, Lv3: +70, Lv4: +100, Lv5: +150
    private static float GetMaxHealthBonus(int level)
    {
        return level switch
        {
            1 => 20f,
            2 => 40f,
            3 => 70f,
            4 => 100f,
            5 => 150f,
            _ => 0f
        };
    }

    // 레벨별 체력 재생 (초당)
    // Lv1: 1/s, Lv2: 2/s, Lv3: 3/s, Lv4: 5/s, Lv5: 8/s
    private static float GetHealthRegen(int level)
    {
        return level switch
        {
            1 => 1f,
            2 => 2f,
            3 => 3f,
            4 => 5f,
            5 => 8f,
            _ => 0f
        };
    }

    // 레벨별 자석 범위
    // Lv1: 3, Lv2: 5, Lv3: 7, Lv4: 10, Lv5: 15
    private static float GetMagnetRange(int level)
    {
        return level switch
        {
            1 => 3f,
            2 => 5f,
            3 => 7f,
            4 => 10f,
            5 => 15f,
            _ => 0f
        };
    }

    // 레벨별 치명타 확률 및 배율
    // Lv1: 5% (2x), Lv2: 10% (2x), Lv3: 15% (2.5x), Lv4: 20% (2.5x), Lv5: 30% (3x)
    private static (float chance, float multiplier) GetCriticalStats(int level)
    {
        return level switch
        {
            1 => (5f, 2.0f),
            2 => (10f, 2.0f),
            3 => (15f, 2.5f),
            4 => (20f, 2.5f),
            5 => (30f, 3.0f),
            _ => (0f, 1f)
        };
    }
}
