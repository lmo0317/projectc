using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 플레이어 입력 - Owner만 쓰기 가능
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToOwner)]
public struct PlayerInput : IComponentData
{
    public float2 Movement;
}
