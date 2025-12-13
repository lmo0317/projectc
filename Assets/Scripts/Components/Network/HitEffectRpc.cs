using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;

/// <summary>
/// 서버 → 클라이언트로 피격 이펙트 정보 전송 RPC
/// </summary>
[BurstCompile]
public struct HitEffectRpc : IRpcCommand
{
    public float3 Position;  // 피격 위치
    public float Damage;     // 데미지 양
}
