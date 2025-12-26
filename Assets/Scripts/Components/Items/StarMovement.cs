using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Star 아이템의 이동 컴포넌트 (스폰 시 튀어오르는 효과용)
/// </summary>
public struct StarMovement : IComponentData
{
    public float3 Velocity;      // 현재 속도
    public float Gravity;        // 중력 가속도
    public float GroundY;        // 바닥 높이
    public float BounceDamping;  // 튀는 감쇠율
    public bool IsSettled;       // 정착 완료 여부
}
