using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class EnableGoInGameAuthoring : MonoBehaviour
{
    class Baker : Baker<EnableGoInGameAuthoring>
    {
        public override void Bake(EnableGoInGameAuthoring authoring)
        {
            EnableGoInGame component = default(EnableGoInGame);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
