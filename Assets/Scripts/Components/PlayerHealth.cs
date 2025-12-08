using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 체력
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerHealth : IComponentData
{
    public float CurrentHealth;
    public float MaxHealth;
}
