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
    public Transform FirePoint;     // 총알 발사 위치 (Inspector에서 할당)
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

            // PlayerDead 컴포넌트 추가 (비활성화 상태로 시작)
            AddComponent<PlayerDead>(entity);
            SetComponentEnabled<PlayerDead>(entity, false);

            // 총알 Prefab Entity 참조 가져오기 (Renderable | Dynamic 필요!)
            var bulletPrefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new AutoShootConfig
            {
                FireRate = authoring.FireRate,
                TimeSinceLastShot = 0f,
                BulletPrefab = bulletPrefabEntity
            });

            // 발사 위치 오프셋 추가
            float3 firePointOffset;
            if (authoring.FirePoint != null)
            {
                // FirePoint Transform의 로컬 위치 사용
                firePointOffset = authoring.FirePoint.localPosition;
            }
            else
            {
                // FirePoint가 없으면 기본값 (플레이어 앞쪽 약간 위)
                firePointOffset = new float3(0, 0.5f, 1f);
            }

            AddComponent(entity, new FirePointOffset
            {
                LocalOffset = firePointOffset
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
