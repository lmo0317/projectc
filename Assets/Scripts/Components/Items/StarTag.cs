using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// Star 아이템 식별 태그 (서버에서만 관리)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct StarTag : IComponentData
{
}
