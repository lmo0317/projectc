using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 서버 → 클라이언트로 Star 스폰 정보 전송 RPC
/// 클라이언트에서 Star 비주얼을 생성하기 위해 사용
/// </summary>
[BurstCompile]
public struct StarSpawnRpc : IRpcCommand
{
    public int StarId;           // Star 고유 ID (서버에서 생성)
    public float3 Position;      // 스폰 위치
}
