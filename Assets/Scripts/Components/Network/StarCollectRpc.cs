using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 서버 → 클라이언트로 Star 수집 이펙트 정보 전송 RPC
/// </summary>
[BurstCompile]
public struct StarCollectRpc : IRpcCommand
{
    public float3 Position;  // 수집 위치
    public int Value;        // 포인트 값
}
