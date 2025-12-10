using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 총알 이동 방향 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletDirection : IComponentData
{
    [GhostField] public float3 Value; // 정규화된 방향 벡터
}
