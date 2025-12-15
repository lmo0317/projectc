using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 왼쪽 총알 발사 위치의 로컬 오프셋
/// PlayerAuthoring의 LeftFirePoint Transform에서 Baking됨
/// </summary>
public struct LeftFirePointOffset : IComponentData
{
    public float3 LocalOffset;  // 발사 위치의 로컬 오프셋
}

/// <summary>
/// 오른쪽 총알 발사 위치의 로컬 오프셋
/// PlayerAuthoring의 RightFirePoint Transform에서 Baking됨
/// </summary>
public struct RightFirePointOffset : IComponentData
{
    public float3 LocalOffset;  // 발사 위치의 로컬 오프셋
}
