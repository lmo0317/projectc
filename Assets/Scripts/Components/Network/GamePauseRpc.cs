using Unity.Burst;
using Unity.NetCode;

/// <summary>
/// 게임 일시정지 상태 변경 RPC (Server → All Clients)
/// 버프 선택 시작/종료 시 모든 클라이언트에 브로드캐스트
/// </summary>
[BurstCompile]
public struct GamePauseRpc : IRpcCommand
{
    /// <summary>
    /// true = 게임 일시정지 (누군가 버프 선택 중)
    /// false = 게임 재개 (버프 선택 완료)
    /// </summary>
    public bool IsPaused;

    /// <summary>
    /// 버프 선택 중인 플레이어의 NetworkId (자기 자신인지 확인용)
    /// 일시정지 해제 시에는 0
    /// </summary>
    public int SelectingPlayerNetworkId;
}
