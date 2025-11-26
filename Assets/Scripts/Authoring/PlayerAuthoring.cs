using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInput { Movement = float2.zero });
        }
    }
}
