using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 총알 식별 태그 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletTag : IComponentData
{
}
