using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// Star 아이템의 포인트 값
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct StarValue : IComponentData
{
    [GhostField] public int Value; // 포인트 값 (기본 1)
}
