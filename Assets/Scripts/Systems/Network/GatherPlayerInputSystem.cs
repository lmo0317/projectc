using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 클라이언트에서 입력 수집 (GhostInputSystemGroup)
/// NetcodeSamples 05_SpawnPlayer의 GatherAutoCommandsSystem 패턴
/// </summary>
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GatherPlayerInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
        RequireForUpdate<EnableMultiplayer>();
    }

    protected override void OnUpdate()
    {
        // 입력 읽기 (UnityEngine.Input 사용)
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);
        bool up = Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.S);
        bool fire = Input.GetKeyDown(KeyCode.Space);

        // GhostOwnerIsLocal 태그로 로컬 플레이어만 필터링
        foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            input.ValueRW = default;  // 초기화

            if (fire) input.ValueRW.Fire.Set();
            if (left) input.ValueRW.Horizontal -= 1;
            if (right) input.ValueRW.Horizontal += 1;
            if (down) input.ValueRW.Vertical -= 1;
            if (up) input.ValueRW.Vertical += 1;

            Debug.Log($"[Client Input] H:{input.ValueRW.Horizontal}, V:{input.ValueRW.Vertical}");
        }
    }
}
