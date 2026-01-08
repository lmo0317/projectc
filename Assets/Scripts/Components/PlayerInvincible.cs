using Unity.Entities;

/// <summary>
/// 플레이어 무적 모드 태그 컴포넌트
/// 이 컴포넌트가 있는 플레이어는 데미지를 받지 않음
/// </summary>
public struct PlayerInvincible : IComponentData, IEnableableComponent
{
}
