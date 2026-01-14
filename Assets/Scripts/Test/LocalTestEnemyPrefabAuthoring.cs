using Unity.Entities;
using UnityEngine;

/// <summary>
/// 테스트용 Enemy Prefab을 Entity로 등록하는 Authoring
/// SubScene에 이 컴포넌트가 붙은 GameObject를 배치하고 EnemyPrefab을 할당하면
/// 해당 Prefab이 Entity Prefab으로 변환됨
/// </summary>
public class LocalTestEnemyPrefabAuthoring : MonoBehaviour
{
    public GameObject EnemyPrefab;

    class Baker : Baker<LocalTestEnemyPrefabAuthoring>
    {
        public override void Bake(LocalTestEnemyPrefabAuthoring authoring)
        {
            if (authoring.EnemyPrefab == null) return;

            var entity = GetEntity(TransformUsageFlags.None);

            // Prefab을 Entity로 변환하여 참조 저장
            var prefabEntity = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new LocalTestEnemyPrefabReference
            {
                Prefab = prefabEntity
            });
        }
    }
}

public struct LocalTestEnemyPrefabReference : IComponentData
{
    public Entity Prefab;
}
