using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 대량 몬스터 소환 RPC 요청 (Client -> Server)
/// </summary>
public struct SpawnMassEnemiesRequest : IRpcCommand
{
    public int Count; // 소환할 몬스터 수
}
