using Unity.Entities;
using UnityEngine;

/// <summary>
/// Star 아이템 Authoring (Enemy 사망 시 드롭되는 수집 아이템)
///
/// ★ Unity Editor 설정 필수 ★
/// Star 프리팹에 GhostAuthoringComponent를 추가해야 클라이언트에서 보입니다:
/// 1. Assets/Prefabs/StarAuthoring.prefab 선택
/// 2. Add Component → Ghost Authoring Component
/// 3. Supported Ghost Modes = All (또는 Interpolated)
/// 4. Default Ghost Mode = Interpolated (부드러운 움직임)
/// </summary>
public class StarAuthoring : MonoBehaviour
{
    [Header("Star 설정")]
    public int PointValue = 1;          // 수집 시 획득 포인트

    class Baker : Baker<StarAuthoring>
    {
        public override void Bake(StarAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new StarTag());
            AddComponent(entity, new StarValue { Value = authoring.PointValue });
            // StarMovement는 스폰 시 BulletHitSystem에서 추가됨
        }
    }
}
