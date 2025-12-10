using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float FireRate = 0.5f;  // 초당 2발
    public GameObject BulletPrefab; // Inspector에서 할당
    public float ColliderRadius = 0.5f; // 플레이어 충돌 반경
    public float MaxHealth = 100f; // 최대 체력

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            // Ghost 네트워크 동기화를 위한 컴포넌트 추가
            // 이 Entity가 네트워크로 복제되도록 설정
            // (GhostAuthoringComponent 대신 Baker에서 직접 추가 가능)

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });

            // PlayerInput 추가 (기본값으로 초기화)
            AddComponent<PlayerInput>(entity);

            AddComponent(entity, new PlayerHealth
            {
                CurrentHealth = authoring.MaxHealth,
                MaxHealth = authoring.MaxHealth
            }); // 체력 추가

            // 총알 Prefab Entity 참조 가져오기
            var bulletPrefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new AutoShootConfig
            {
                FireRate = authoring.FireRate,
                TimeSinceLastShot = 0f,
                BulletPrefab = bulletPrefabEntity
            });

            // PhysicsCollider 추가 (Sphere)
            var collider = Unity.Physics.SphereCollider.Create(
                new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = authoring.ColliderRadius
                },
                new CollisionFilter
                {
                    BelongsTo = 1u << 0,    // Layer 0: Player
                    CollidesWith = 1u << 2  // Layer 2: Enemy만 충돌
                }
            );
            AddComponent(entity, new PhysicsCollider { Value = collider });
        }
    }
}
