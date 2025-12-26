using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어가 수집한 Star 포인트 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerStarPoints : IComponentData
{
    [GhostField] public int CurrentPoints;       // 현재 포인트
    [GhostField] public int TotalCollected;      // 총 수집량 (누적)
    [GhostField] public int NextBuffThreshold;   // 다음 버프 필요 포인트
    [GhostField] public int BuffSelectionCount;  // 버프 선택 횟수
}
