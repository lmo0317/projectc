using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 총알 발사 위치의 로컬 오프셋
/// PlayerAuthoring의 FirePoint Transform에서 Baking됨
/// </summary>
public struct FirePointOffset : IComponentData
{
    public float3 LocalOffset;  // 발사 위치의 로컬 오프셋
}
