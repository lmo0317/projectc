using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyHealth : IComponentData
{
    [GhostField] public float Value;
}
