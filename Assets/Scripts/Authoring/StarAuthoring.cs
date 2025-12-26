using Unity.Entities;
using UnityEngine;

/// <summary>
/// Star 아이템 Authoring (Enemy 사망 시 드롭되는 수집 아이템)
/// </summary>
public class StarAuthoring : MonoBehaviour
{
    [Header("Star 설정")]
    public int PointValue = 1;          // 수집 시 획득 포인트

    class Baker : Baker<StarAuthoring>
    {
        public override void Bake(StarAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new StarTag());
            AddComponent(entity, new StarValue { Value = authoring.PointValue });
            // StarMovement는 스폰 시 BulletHitSystem에서 추가됨
        }
    }
}
