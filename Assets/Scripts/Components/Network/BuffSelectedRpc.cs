using Unity.NetCode;

/// <summary>
/// 버프 선택 결과 RPC (Client → Server)
/// </summary>
public struct BuffSelectedRpc : IRpcCommand
{
    public int SelectedBuffType;  // 선택된 BuffType
}
