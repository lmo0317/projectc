using Unity.Entities;
using Unity.Mathematics;

public struct BulletDirection : IComponentData
{
    public float3 Value; // 정규화된 방향 벡터
}
