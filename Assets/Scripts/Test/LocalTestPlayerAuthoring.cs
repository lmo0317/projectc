using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 로컬 테스트용 플레이어 Authoring
/// 이 컴포넌트가 붙은 GameObject는 LocalTestPlayerTag Entity가 됨
/// </summary>
public class LocalTestPlayerAuthoring : MonoBehaviour
{
    class Baker : Baker<LocalTestPlayerAuthoring>
    {
        public override void Bake(LocalTestPlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LocalTestPlayerTag());
        }
    }
}
