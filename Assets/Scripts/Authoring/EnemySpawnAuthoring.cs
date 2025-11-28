using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemySpawnAuthoring : MonoBehaviour
{
    public float SpawnInterval = 2f;
    public GameObject EnemyPrefab;
    public float SpawnRadius = 10f;
    public int MaxEnemies = 50;

    class Baker : Baker<EnemySpawnAuthoring>
    {
        public override void Bake(EnemySpawnAuthoring authoring)
        {
            // 스폰 매니저는 움직이지 않고 렌더링도 안 됨
            var entity = GetEntity(TransformUsageFlags.None);

            // GameObject Prefab을 Entity Prefab으로 변환
            var enemyPrefabEntity = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemySpawnConfig
            {
                SpawnInterval = authoring.SpawnInterval,
                TimeSinceLastSpawn = 0f,
                EnemyPrefab = enemyPrefabEntity,
                SpawnRadius = authoring.SpawnRadius,
                MaxEnemies = authoring.MaxEnemies,
                RandomGenerator = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks)
            });
        }
    }
}
