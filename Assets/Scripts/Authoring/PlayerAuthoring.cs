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
    public Transform LeftFirePoint;  // 왼쪽 총알 발사 위치 (Inspector에서 할당)
    public Transform RightFirePoint; // 오른쪽 총알 발사 위치 (Inspector에서 할당)
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
                ShootFromLeft = true, // 왼쪽부터 시작
                BulletPrefab = bulletPrefabEntity
            });

            // 왼쪽 발사 위치 오프셋 추가
            float3 leftFirePointOffset;
            if (authoring.LeftFirePoint != null)
            {
                leftFirePointOffset = authoring.LeftFirePoint.localPosition;
            }
            else
            {
                // 기본값: 플레이어 왼쪽 날개 위치
                leftFirePointOffset = new float3(-1f, 0, 1f);
            }

            AddComponent(entity, new LeftFirePointOffset
            {
                LocalOffset = leftFirePointOffset
            });

            // 오른쪽 발사 위치 오프셋 추가
            float3 rightFirePointOffset;
            if (authoring.RightFirePoint != null)
            {
                rightFirePointOffset = authoring.RightFirePoint.localPosition;
            }
            else
            {
                // 기본값: 플레이어 오른쪽 날개 위치
                rightFirePointOffset = new float3(1f, 0, 1f);
            }

            AddComponent(entity, new RightFirePointOffset
            {
                LocalOffset = rightFirePointOffset
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
