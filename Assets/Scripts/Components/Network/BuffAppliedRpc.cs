using Unity.NetCode;

/// <summary>
/// 버프 적용 알림 RPC (Server → Client)
/// 어느 플레이어의 버프인지 구분하기 위해 PlayerNetworkId 포함
/// </summary>
public struct BuffAppliedRpc : IRpcCommand
{
    public int PlayerNetworkId;  // 버프를 받은 플레이어의 NetworkId
    public int BuffType;
    public int NewLevel;
}
