using Unity.Entities;
using Unity.Mathematics;

public struct EnemySpawnConfig : IComponentData
{
    public float SpawnInterval;      // 스폰 간격 (초)
    public float TimeSinceLastSpawn; // 마지막 스폰 후 경과 시간
    public Entity EnemyPrefab;       // 몬스터 Prefab Entity 참조
    public float SpawnRadius;        // 플레이어로부터 스폰 반경
    public int MaxEnemies;           // 동시 존재 가능한 최대 몬스터 수
    public Random RandomGenerator;   // 랜덤 생성기 (Burst 호환)
}
