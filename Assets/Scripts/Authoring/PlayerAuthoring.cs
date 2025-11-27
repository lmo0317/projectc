using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float FireRate = 0.5f;  // 초당 2발
    public GameObject BulletPrefab; // Inspector에서 할당

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInput { Movement = float2.zero });

            // 총알 Prefab Entity 참조 가져오기
            var bulletPrefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new AutoShootConfig
            {
                FireRate = authoring.FireRate,
                TimeSinceLastShot = 0f,
                BulletPrefab = bulletPrefabEntity
            });
        }
    }
}
