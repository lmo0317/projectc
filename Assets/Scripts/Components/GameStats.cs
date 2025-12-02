using Unity.Entities;

/// <summary>
/// 게임 통계 데이터 (생존 시간, 처치 수)
/// </summary>
public struct GameStats : IComponentData
{
    public float SurvivalTime;    // 생존 시간 (초)
    public int KillCount;          // 처치한 몬스터 수
}
