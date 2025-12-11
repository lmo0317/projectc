using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemySpeed : IComponentData
{
    [GhostField] public float Value;
}
