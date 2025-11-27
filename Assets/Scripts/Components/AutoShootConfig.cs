using Unity.Entities;

public struct AutoShootConfig : IComponentData
{
    public float FireRate;           // 발사 간격 (초)
    public float TimeSinceLastShot;  // 마지막 발사 후 경과 시간
    public Entity BulletPrefab;      // 총알 Prefab Entity 참조
}
