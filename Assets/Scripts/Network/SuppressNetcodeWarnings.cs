using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// Netcode의 불필요한 경고 메시지 억제
/// Editor 환경에서 성능이 낮아 발생하는 경고를 숨김
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class SuppressNetcodeWarnings : SystemBase
{
    protected override void OnCreate()
    {
        // Application.runInBackground 경고 억제 (프로퍼티로 설정)
        NetDebug.SuppressApplicationRunInBackgroundWarning = true;

        // Editor 환경에서는 성능 경고가 정상이므로 로그 레벨 조정
        Debug.Log("[SuppressNetcodeWarnings] Netcode warnings suppression initialized");
    }

    protected override void OnUpdate()
    {
        // 아무 작업 안 함 (OnCreate에서만 설정)
    }
}
