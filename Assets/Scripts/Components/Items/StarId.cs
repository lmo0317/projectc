using Unity.Entities;

/// <summary>
/// Star 고유 ID (서버-클라이언트 간 동기화용)
/// </summary>
public struct StarId : IComponentData
{
    public int Value;
}
