using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 자동 발사 설정 (네트워크 동기화)
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AutoShootConfig : IComponentData
{
    [GhostField] public float BaseFireRate;       // 기본 발사 간격 (초) - 버프 적용 전
    [GhostField] public float TimeSinceLastShot;  // 마지막 발사 후 경과 시간
    [GhostField] public bool ShootFromLeft;       // 좌우 교대 발사 플래그 (true=왼쪽, false=오른쪽)
    [GhostField] public int BaseMissileCount;     // 기본 미사일 개수 - 버프 적용 전
    public Entity BulletPrefab;                   // 총알 Prefab Entity 참조 (서버만)
}
