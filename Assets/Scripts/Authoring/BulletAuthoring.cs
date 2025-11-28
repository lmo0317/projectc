using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Lifetime = 5f;

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
        }
    }
}
