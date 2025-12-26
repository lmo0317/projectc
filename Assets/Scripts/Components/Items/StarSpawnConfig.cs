using Unity.Entities;

/// <summary>
/// Star 스폰 설정 (싱글톤)
/// </summary>
public struct StarSpawnConfig : IComponentData
{
    public Entity StarPrefab;        // Star 프리팹 Entity
    public float SpawnForceMin;      // 스폰 시 튀어오르는 힘 최소값
    public float SpawnForceMax;      // 스폰 시 튀어오르는 힘 최대값
    public float SpreadRadius;       // 퍼지는 범위
    public Unity.Mathematics.Random RandomGenerator;
}
