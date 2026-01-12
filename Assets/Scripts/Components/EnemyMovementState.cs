using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// Enemy 이동 상태를 저장하는 컴포넌트
/// 방향 스무딩을 위해 이전 이동 방향을 저장
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct EnemyMovementState : IComponentData
{
    /// <summary>
    /// 이전 프레임의 이동 방향 (정규화된 값)
    /// </summary>
    public float3 PreviousDirection;

    /// <summary>
    /// 이전 프레임의 목표 회전값
    /// </summary>
    public quaternion PreviousTargetRotation;

    /// <summary>
    /// 초기화 여부
    /// </summary>
    public bool IsInitialized;
}
