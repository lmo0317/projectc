using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 총알 속도 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletSpeed : IComponentData
{
    [GhostField] public float Value;
}
