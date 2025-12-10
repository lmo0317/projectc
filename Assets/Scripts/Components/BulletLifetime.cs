using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 총알 생존 시간 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct BulletLifetime : IComponentData
{
    [GhostField] public float RemainingTime; // 남은 생존 시간 (초)
}
