using Unity.Entities;
using UnityEngine;

/// <summary>
/// Star 스폰 설정 Authoring (씬에 하나만 배치)
/// </summary>
public class StarSpawnAuthoring : MonoBehaviour
{
    [Header("Star 프리팹")]
    public GameObject StarPrefab;

    [Header("스폰 물리 설정")]
    public float SpawnForceMin = 3f;    // 튀어오르는 힘 최소
    public float SpawnForceMax = 5f;    // 튀어오르는 힘 최대
    public float SpreadRadius = 0.5f;   // 퍼지는 범위

    class Baker : Baker<StarSpawnAuthoring>
    {
        public override void Bake(StarSpawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            var starPrefabEntity = GetEntity(authoring.StarPrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new StarSpawnConfig
            {
                StarPrefab = starPrefabEntity,
                SpawnForceMin = authoring.SpawnForceMin,
                SpawnForceMax = authoring.SpawnForceMax,
                SpreadRadius = authoring.SpreadRadius,
                RandomGenerator = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks)
            });
        }
    }
}
