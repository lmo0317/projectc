using Unity.Burst;
using Unity.NetCode;

/// <summary>
/// 디버그용: 클라이언트 → 서버로 플레이어 즉사 요청 RPC
/// Ctrl + Alt + Shift + K 단축키로 사용
/// </summary>
[BurstCompile]
public struct DebugKillRequestRpc : IRpcCommand
{
    // 요청만 하면 되므로 추가 데이터 필요 없음
    // 서버는 RPC를 보낸 연결의 NetworkId를 통해 어떤 플레이어인지 알 수 있음
}
