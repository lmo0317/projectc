using Unity.Burst;
using Unity.NetCode;

/// <summary>
/// 클라이언트 → 서버로 부활 요청 RPC
/// </summary>
[BurstCompile]
public struct RespawnRequestRpc : IRpcCommand
{
    // 요청만 하면 되므로 추가 데이터 필요 없음
    // 서버는 RPC를 보낸 연결의 NetworkId를 통해 어떤 플레이어인지 알 수 있음
}
