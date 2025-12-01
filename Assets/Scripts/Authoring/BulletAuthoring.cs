using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 5f;
    public float ColliderRadius = 0.2f; // 총알 충돌 반경

    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletLifetime { RemainingTime = authoring.Lifetime });
            AddComponent(entity, new BulletDirection { Value = float3.zero }); // 발사 시 설정됨

            // PhysicsCollider 추가 (Sphere)
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 1,    // Layer 1: Bullet
                    CollidesWith = 1u << 2  // Layer 2: Enemy만 충돌
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
