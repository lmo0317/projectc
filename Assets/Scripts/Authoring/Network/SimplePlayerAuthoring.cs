using Unity.Entities;
using UnityEngine;

/// <summary>
/// 테스트용 간단한 플레이어 Authoring
/// </summary>
public class SimplePlayerAuthoring : MonoBehaviour
{
    class Baker : Baker<SimplePlayerAuthoring>
    {
        public override void Bake(SimplePlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerTag());
        }
    }
}
