using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 자동 발사 설정 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AutoShootConfig : IComponentData
{
    [GhostField] public float FireRate;           // 발사 간격 (초)
    [GhostField] public float TimeSinceLastShot;  // 마지막 발사 후 경과 시간
    [GhostField] public bool ShootFromLeft;       // 좌우 교대 발사 플래그 (true=왼쪽, false=오른쪽)
    public Entity BulletPrefab;                   // 총알 Prefab Entity 참조 (서버만)
}
