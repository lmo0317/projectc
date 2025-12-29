using Unity.NetCode;

/// <summary>
/// 버프 적용 알림 RPC (Server → Client)
/// </summary>
public struct BuffAppliedRpc : IRpcCommand
{
    public int BuffType;
    public int NewLevel;
}
