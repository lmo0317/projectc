using Unity.Entities;

/// <summary>
/// 플레이어 Entity에서 연결 Entity로의 역참조
/// </summary>
public struct ConnectionOwner : IComponentData
{
    public Entity Entity;
}
