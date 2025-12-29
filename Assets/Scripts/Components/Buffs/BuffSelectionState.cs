using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 버프 선택 상태 (서버에서 관리)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct BuffSelectionState : IComponentData
{
    [GhostField] public bool IsSelecting;    // 버프 선택 중인지 여부
    [GhostField] public int Option1;         // 버프 옵션 1 (BuffType 캐스팅)
    [GhostField] public int Option2;         // 버프 옵션 2
    [GhostField] public int Option3;         // 버프 옵션 3
}
