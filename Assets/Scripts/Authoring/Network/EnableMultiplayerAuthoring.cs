using Unity.Entities;
using UnityEngine;

/// <summary>
/// 멀티플레이 기능 활성화
/// </summary>
public class EnableMultiplayerAuthoring : MonoBehaviour
{
    class Baker : Baker<EnableMultiplayerAuthoring>
    {
        public override void Bake(EnableMultiplayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<EnableMultiplayer>(entity);
        }
    }
}
