using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 버프로 인한 스탯 수정치 (네트워크 동기화)
/// StatCalculationSystem에서 PlayerBuffs 기반으로 계산
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct StatModifiers : IComponentData
{
    [GhostField] public float DamageMultiplier;      // 1.0 = 100% (기본값)
    [GhostField] public float FireRateMultiplier;    // 1.0 = 100%, 0.8 = 20% 빠름
    [GhostField] public int BonusMissileCount;       // 추가 미사일 개수
    [GhostField] public float SpeedMultiplier;       // 1.0 = 100% (기본값)
    [GhostField] public float BonusMaxHealth;        // 추가 최대 체력
    [GhostField] public float HealthRegenPerSecond;  // 초당 체력 재생량
    [GhostField] public float CriticalChance;        // 치명타 확률 (0~100)
    [GhostField] public float CriticalMultiplier;    // 치명타 배율 (2.0 = 2배)
    [GhostField] public float MagnetRange;           // 자석 효과 범위

    /// <summary>
    /// 기본값 생성
    /// </summary>
    public static StatModifiers Default => new StatModifiers
    {
        DamageMultiplier = 1f,
        FireRateMultiplier = 1f,
        BonusMissileCount = 0,
        SpeedMultiplier = 1f,
        BonusMaxHealth = 0f,
        HealthRegenPerSecond = 0f,
        CriticalChance = 0f,
        CriticalMultiplier = 1f,
        MagnetRange = 0f
    };
}
