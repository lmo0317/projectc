using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 식별 태그 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerTag : IComponentData
{
}
