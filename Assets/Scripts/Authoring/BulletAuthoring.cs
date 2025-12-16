using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 5f;
    public float ColliderRadius = 0.2f; // 총알 충돌 반경
    public float Damage = 25f; // 데미지 값
    public bool IsMissile = true; // 미사일 여부
    public float MissileTurnSpeed = 180f; // 미사일 회전 속도 (초당 각도)

    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            // Note: Ghost 설정은 Bullet Prefab에 GhostAuthoringComponent를 추가해야 함
            // Unity Editor: Bullet Prefab → Add Component → Ghost Authoring Component
            // SupportedGhostModes = Predicted 로 설정 필요

            AddComponent(entity, new BulletTag());
            AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
            AddComponent(entity, new BulletLifetime { RemainingTime = authoring.Lifetime });
            AddComponent(entity, new BulletDirection { Value = float3.zero }); // 발사 시 설정됨
            AddComponent(entity, new DamageValue { Value = authoring.Damage }); // 데미지 추가

            // 미사일 컴포넌트 추가
            if (authoring.IsMissile)
            {
                AddComponent(entity, new MissileTag());
                AddComponent(entity, new MissileTarget { TargetEntity = Entity.Null }); // 발사 시 설정됨
                AddComponent(entity, new MissileTurnSpeed { Value = authoring.MissileTurnSpeed });
            }

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
                    BelongsTo = 1u << 1,    // Layer 1: Bullet
                    CollidesWith = 1u << 2, // Layer 2: Enemy만 충돌
                    GroupIndex = 0
                },
                material
            );

            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
