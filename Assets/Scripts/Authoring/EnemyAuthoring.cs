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

            // Ghost Importance Scaling 컴포넌트 추가 (거리 기반 네트워크 동기화 최적화)
            AddComponent(entity, new GhostDistanceImportance
            {
                ImportanceValue = 100 // 기본값: 최고 우선순위
            });

            // PhysicsCollider 추가 (Sphere) - 쿼리 전용으로 설정
            // Material에 CollisionResponse = None 설정 (시뮬레이션 비활성화, 쿼리만 가능)
            var material = new Unity.Physics.Material
            {
                CollisionResponse = CollisionResponsePolicy.None // 쿼리 전용
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

            // PhysicsVelocity 추가 (Kinematic Body로 만들어 Broadphase 업데이트)
            AddComponent(entity, new PhysicsVelocity
            {
                Linear = float3.zero,
                Angular = float3.zero
            });

            // PhysicsMass 추가 (Kinematic Body 설정)
            AddComponent(entity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
        }
    }
}
