using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 사망 상태 태그 (네트워크 동기화)
/// IEnableableComponent로 활성화/비활성화 가능
/// [GhostEnabledBit]으로 enabled 상태가 클라이언트에 동기화됨
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, SendDataForChildEntity = false)]
[GhostEnabledBit] // IEnableableComponent의 enabled 상태를 Ghost로 동기화
public struct PlayerDead : IComponentData, IEnableableComponent
{
}
