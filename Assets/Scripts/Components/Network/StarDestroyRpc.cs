using Unity.Burst;
using Unity.NetCode;

/// <summary>
/// 서버 → 클라이언트로 Star 삭제 정보 전송 RPC
/// </summary>
[BurstCompile]
public struct StarDestroyRpc : IRpcCommand
{
    public int StarId;  // 삭제할 Star ID
}
