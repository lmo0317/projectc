using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // WASD 또는 Arrow Keys 입력 읽기
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down

        float2 input = new float2(horizontal, vertical);

        // 모든 PlayerInput 컴포넌트에 입력값 저장
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref PlayerInput playerInput) =>
            {
                playerInput.Movement = input;
            })
            .Schedule();
    }
}
