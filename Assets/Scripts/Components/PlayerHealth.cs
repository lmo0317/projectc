using Unity.Entities;

/// <summary>
/// 플레이어 체력
/// </summary>
public struct PlayerHealth : IComponentData
{
    public float CurrentHealth;
    public float MaxHealth;
}
