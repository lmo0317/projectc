using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 총알이 입히는 데미지 값 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct DamageValue : IComponentData
{
    [GhostField] public float Value;
}
