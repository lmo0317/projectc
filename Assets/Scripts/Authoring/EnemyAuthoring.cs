using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float Health = 100f;
    public float Speed = 3f;
    public float ColliderRadius = 0.5f; // 몬스터 충돌 반경

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new EnemyHealth { Value = authoring.Health });
            AddComponent(entity, new EnemySpeed { Value = authoring.Speed });

            // PhysicsCollider 추가 (Sphere) - Trigger로 설정
            // Material에 RaiseTriggerEvents 설정
            var material = new Unity.Physics.Material
            {
                CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents
            };

            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 2,    // Layer 2: Enemy
                    CollidesWith = (1u << 0) | (1u << 1), // Layer 0: Player, Layer 1: Bullet
                    GroupIndex = 0
                },
                material
            );

            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
