using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 미사일 타겟 Entity (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct MissileTarget : IComponentData
{
    [GhostField] public Entity TargetEntity; // 추적할 타겟 Entity
}
