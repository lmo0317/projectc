using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 입력 - IInputComponentData로 자동 네트워크 전송
/// (NetcodeSamples 05_SpawnPlayer 패턴)
/// </summary>
public struct PlayerInput : IInputComponentData
{
    public int Horizontal;
    public int Vertical;
    public InputEvent Fire;  // 발사 버튼 (Phase 7에서 사용)
}
