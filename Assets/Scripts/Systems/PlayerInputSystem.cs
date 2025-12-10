using Unity.Entities;
using UnityEngine;

/// <summary>
/// 플레이어 입력 수집 시스템 (임시 버전)
/// Phase 4에서 GatherPlayerInputSystem으로 재작성될 예정
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // WASD 키 입력 읽기
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);
        bool up = Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.S);
        bool fire = Input.GetKeyDown(KeyCode.Space);

        // 모든 PlayerInput 컴포넌트에 입력값 저장
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref PlayerInput playerInput) =>
            {
                // 초기화
                playerInput.Horizontal = 0;
                playerInput.Vertical = 0;

                if (fire) playerInput.Fire.Set();
                if (left) playerInput.Horizontal -= 1;
                if (right) playerInput.Horizontal += 1;
                if (down) playerInput.Vertical -= 1;
                if (up) playerInput.Vertical += 1;
            })
            .Schedule();
    }
}
