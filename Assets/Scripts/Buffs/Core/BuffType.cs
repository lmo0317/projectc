/// <summary>
/// 버프 종류 열거형
/// </summary>
public enum BuffType
{
    Damage = 0,        // 데미지 증가
    Speed = 1,         // 이동 속도 증가
    FireRate = 2,      // 공격 속도 증가
    MissileCount = 3,  // 미사일 개수 증가
    Magnet = 4,        // 자석 효과 (아이템 흡수)
    HealthRegen = 5,   // 체력 재생
    MaxHealth = 6,     // 최대 체력 증가
    Critical = 7,      // 치명타 확률
}
