using Unity.NetCode;

/// <summary>
/// 버프 선택 UI 표시 요청 RPC (Server → Client)
/// </summary>
public struct ShowBuffSelectionRpc : IRpcCommand
{
    public int Option1BuffType;
    public int Option1CurrentLevel;
    public int Option2BuffType;
    public int Option2CurrentLevel;
    public int Option3BuffType;
    public int Option3CurrentLevel;
}
