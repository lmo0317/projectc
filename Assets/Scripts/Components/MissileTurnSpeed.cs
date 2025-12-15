using Unity.Entities;

/// <summary>
/// 미사일 회전 속도
/// </summary>
public struct MissileTurnSpeed : IComponentData
{
    public float Value; // 초당 회전 각도 (degree)
}
