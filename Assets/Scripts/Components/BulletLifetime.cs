using Unity.Entities;

public struct BulletLifetime : IComponentData
{
    public float RemainingTime; // 남은 생존 시간 (초)
}
