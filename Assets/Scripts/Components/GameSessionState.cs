using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 게임 세션 상태 (Server에서만 사용)
///
/// === 세션 흐름 ===
/// 1. 서버 시작: IsGameActive=true, HasHadPlayers=false
/// 2. 첫 플레이어 접속: HasHadPlayers=true
/// 3. 모든 플레이어 죽음 + 연결 끊김: 게임 리셋 → 1번으로
/// </summary>
[GhostComponent(PrefabType = GhostPrefabType.Server)]
public struct GameSessionState : IComponentData
{
    /// <summary>
    /// 현재 게임이 활성 상태인지
    /// </summary>
    public bool IsGameActive;

    /// <summary>
    /// 플레이어가 한 명이라도 접속했었는지
    /// </summary>
    public bool HasHadPlayers;

    /// <summary>
    /// 게임 오버 상태인지 (현재 미사용)
    /// </summary>
    public bool IsGameOver;

    /// <summary>
    /// 게임 오버 시점 (현재 미사용)
    /// </summary>
    public double GameOverTime;
}
