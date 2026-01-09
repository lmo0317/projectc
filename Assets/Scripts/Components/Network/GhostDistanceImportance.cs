using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// Enemy Ghost의 거리 기반 중요도 설정
/// 플레이어로부터 멀수록 업데이트 빈도 감소
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct GhostDistanceImportance : IComponentData
{
    [GhostField] public int ImportanceValue; // 0-100 중요도
}
