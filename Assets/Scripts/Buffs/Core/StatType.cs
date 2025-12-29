/// <summary>
/// 스탯 종류 열거형
/// </summary>
public enum StatType
{
    None = 0,

    // 공격 관련
    Damage = 1,
    FireRate = 2,
    MissileCount = 3,
    CriticalChance = 4,
    CriticalMultiplier = 5,

    // 이동 관련
    MovementSpeed = 10,

    // 생존 관련
    MaxHealth = 20,
    HealthRegen = 21,

    // 유틸리티
    MagnetRange = 30,
    MagnetSpeed = 31,
}
