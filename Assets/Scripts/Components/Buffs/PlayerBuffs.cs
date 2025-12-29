using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어의 버프 레벨 (네트워크 동기화)
/// 각 버프의 레벨 (0 = 미획득, 1~5 = 레벨)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerBuffs : IComponentData
{
    [GhostField] public int DamageLevel;           // 데미지 증가
    [GhostField] public int SpeedLevel;            // 이동 속도 증가
    [GhostField] public int FireRateLevel;         // 공격 속도 증가
    [GhostField] public int MissileCountLevel;     // 미사일 개수 증가
    [GhostField] public int MagnetLevel;           // 자석 효과
    [GhostField] public int HealthRegenLevel;      // 체력 재생
    [GhostField] public int MaxHealthLevel;        // 최대 체력 증가
    [GhostField] public int CriticalLevel;         // 치명타 확률

    public const int MaxLevel = 5;

    /// <summary>
    /// 버프 타입으로 레벨 가져오기
    /// </summary>
    public int GetLevel(BuffType buffType)
    {
        return buffType switch
        {
            BuffType.Damage => DamageLevel,
            BuffType.Speed => SpeedLevel,
            BuffType.FireRate => FireRateLevel,
            BuffType.MissileCount => MissileCountLevel,
            BuffType.Magnet => MagnetLevel,
            BuffType.HealthRegen => HealthRegenLevel,
            BuffType.MaxHealth => MaxHealthLevel,
            BuffType.Critical => CriticalLevel,
            _ => 0
        };
    }

    /// <summary>
    /// 버프 타입으로 레벨 설정하기
    /// </summary>
    public void SetLevel(BuffType buffType, int level)
    {
        switch (buffType)
        {
            case BuffType.Damage: DamageLevel = level; break;
            case BuffType.Speed: SpeedLevel = level; break;
            case BuffType.FireRate: FireRateLevel = level; break;
            case BuffType.MissileCount: MissileCountLevel = level; break;
            case BuffType.Magnet: MagnetLevel = level; break;
            case BuffType.HealthRegen: HealthRegenLevel = level; break;
            case BuffType.MaxHealth: MaxHealthLevel = level; break;
            case BuffType.Critical: CriticalLevel = level; break;
        }
    }

    /// <summary>
    /// 버프 레벨업 (최대 레벨 제한)
    /// </summary>
    public bool TryLevelUp(BuffType buffType)
    {
        int currentLevel = GetLevel(buffType);
        if (currentLevel >= MaxLevel)
            return false;

        SetLevel(buffType, currentLevel + 1);
        return true;
    }
}
