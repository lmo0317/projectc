using Unity.Entities;
using Unity.Mathematics;
using Physics = Unity.Physics;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    [Header("몬스터 스탯")]
    public float Health = 100f;
    public float Speed = 3f;

    [Header("충돌 영역 설정 (Collider 컴포넌트 사용)")]
    [Tooltip("SphereCollider, BoxCollider, CapsuleCollider 중 하나를 GameObject에 추가하세요.\nCollider가 없으면 기본 반경 0.5f의 Sphere가 생성됩니다.")]
    public bool UseCustomCollider = true;

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new EnemyHealth { Value = authoring.Health });
            AddComponent(entity, new EnemySpeed { Value = authoring.Speed });

            // Material에 CollisionResponse = None 설정 (시뮬레이션 비활성화, 쿼리만 가능)
            var material = new Physics.Material
            {
                CollisionResponse = Physics.CollisionResponsePolicy.None // 쿼리 전용
            };

            // CollisionFilter 설정
            var collisionFilter = new Physics.CollisionFilter
            {
                BelongsTo = 1u << 2,    // Layer 2: Enemy
                CollidesWith = (1u << 0) | (1u << 1), // Layer 0: Player, Layer 1: Bullet
                GroupIndex = 0
            };

            // PhysicsCollider 생성 (Collider 컴포넌트에서 크기 읽기)
            var colliderRef = CreateColliderFromAuthoring(authoring, collisionFilter, material);

            AddComponent(entity, new Unity.Physics.PhysicsCollider { Value = colliderRef });

            // PhysicsVelocity 추가 (Kinematic Body로 만들어 Broadphase 업데이트)
            AddComponent(entity, new Unity.Physics.PhysicsVelocity
            {
                Linear = float3.zero,
                Angular = float3.zero
            });

            // PhysicsMass 추가 (Kinematic Body 설정)
            AddComponent(entity, Unity.Physics.PhysicsMass.CreateKinematic(Unity.Physics.MassProperties.UnitSphere));
        }

        /// <summary>
        /// Authoring의 Collider 컴포넌트 타입에 따라 적절한 Unity.Physics Collider를 생성
        /// </summary>
        private Unity.Entities.BlobAssetReference<Unity.Physics.Collider> CreateColliderFromAuthoring(
            EnemyAuthoring authoring,
            Physics.CollisionFilter filter,
            Physics.Material material)
        {
            // Collider 컴포넌트가 없으면 기본 Sphere 생성
            if (authoring.UseCustomCollider)
            {
                // 1. SphereCollider 확인
                var sphereCollider = authoring.GetComponent<SphereCollider>();
                if (sphereCollider != null)
                {
                    return Physics.SphereCollider.Create(
                        new Unity.Physics.SphereGeometry
                        {
                            Center = authoring.transform.InverseTransformPoint(sphereCollider.transform.TransformPoint(sphereCollider.center)),
                            Radius = sphereCollider.radius * math.max(authoring.transform.lossyScale.x,
                                     math.max(authoring.transform.lossyScale.y, authoring.transform.lossyScale.z))
                        },
                        filter,
                        material
                    );
                }

                // 2. BoxCollider 확인
                var boxCollider = authoring.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    var size = new float3(
                        math.abs(boxCollider.size.x * authoring.transform.lossyScale.x),
                        math.abs(boxCollider.size.y * authoring.transform.lossyScale.y),
                        math.abs(boxCollider.size.z * authoring.transform.lossyScale.z)
                    );
                    var center = authoring.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center));

                    return Physics.BoxCollider.Create(
                        new Unity.Physics.BoxGeometry
                        {
                            Center = center,
                            Size = size,
                            Orientation = quaternion.identity
                        },
                        filter,
                        material
                    );
                }

                // 3. CapsuleCollider 확인
                var capsuleCollider = authoring.GetComponent<CapsuleCollider>();
                if (capsuleCollider != null)
                {
                    // CapsuleCollider의 방향에 따라 설정
                    var height = capsuleCollider.height * math.abs(authoring.transform.lossyScale.y);
                    var radius = capsuleCollider.radius * math.max(authoring.transform.lossyScale.x, authoring.transform.lossyScale.z);

                    return Physics.CapsuleCollider.Create(
                        new Unity.Physics.CapsuleGeometry
                        {
                            Radius = radius,
                            Vertex0 = new float3(0, -math.max(0, height - 2f * radius) * 0.5f, 0),
                            Vertex1 = new float3(0, math.max(0, height - 2f * radius) * 0.5f, 0)
                        },
                        filter,
                        material
                    );
                }
            }

            // Collider가 없으면 기본값 (반경 0.5f Sphere)
            return Physics.SphereCollider.Create(
                new Unity.Physics.SphereGeometry
                {
                    Center = float3.zero,
                    Radius = 0.5f
                },
                filter,
                material
            );
        }
    }
}
