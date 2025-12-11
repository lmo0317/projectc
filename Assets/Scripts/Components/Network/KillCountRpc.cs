using Unity.NetCode;

/// <summary>
/// Enemy 처치 시 Server에서 Client로 전송하는 RPC
/// NetcodeSamples 03_RPC 패턴
/// </summary>
public struct KillCountRpc : IRpcCommand
{
    public int IncrementAmount;  // 증가량 (보통 1)
}
