using Unity.Burst;
using Unity.NetCode;

/// <summary>
/// 디버그용: 클라이언트 → 서버로 Star 포인트 추가 요청 RPC
/// BuffDebugWindow에서 "+10 포인트" 등의 버튼 클릭 시 사용
/// </summary>
[BurstCompile]
public struct DebugAddPointsRpc : IRpcCommand
{
    public int Amount;
}
