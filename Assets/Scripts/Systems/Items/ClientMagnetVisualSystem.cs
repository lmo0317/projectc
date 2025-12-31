using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 클라이언트에서 Star 비주얼을 자석 효과로 끌어당기는 시스템
/// 서버의 MagnetSystem과 동일한 로직으로 비주얼 동기화
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ClientStarVisualSystem))]
public partial class ClientMagnetVisualSystem : SystemBase
{
    // 거리 기반 속도 설정
    private const float MinSpeed = 5f;    // 가까울 때 최소 속도
    private const float MaxSpeed = 30f;   // 멀 때 최대 속도

    private MagnetRangeIndicator rangeIndicator;

    protected override void OnCreate()
    {
        RequireForUpdate<ClientStarVisual>();
    }

    protected override void OnStartRunning()
    {
        // MagnetRangeIndicator 찾기 또는 생성
        rangeIndicator = Object.FindObjectOfType<MagnetRangeIndicator>();
        if (rangeIndicator == null)
        {
            var indicatorObj = new GameObject("MagnetRangeIndicator");
            rangeIndicator = indicatorObj.AddComponent<MagnetRangeIndicator>();
        }
    }

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 로컬 플레이어의 위치와 자석 범위 찾기
        float3 playerPos = float3.zero;
        float magnetRange = 0f;
        bool hasPlayer = false;

        // GhostOwnerIsLocal이 있는 플레이어 찾기 (멀티플레이)
        foreach (var (transform, modifiers) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<StatModifiers>>()
                     .WithAll<PlayerTag, GhostOwnerIsLocal>()
                     .WithDisabled<PlayerDead>())
        {
            playerPos = transform.ValueRO.Position;
            magnetRange = modifiers.ValueRO.MagnetRange;
            hasPlayer = true;
            break;
        }

        // GhostOwnerIsLocal이 없는 경우 (싱글플레이)
        if (!hasPlayer)
        {
            foreach (var (transform, modifiers) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<StatModifiers>>()
                         .WithAll<PlayerTag>()
                         .WithDisabled<PlayerDead>())
            {
                playerPos = transform.ValueRO.Position;
                magnetRange = modifiers.ValueRO.MagnetRange;
                hasPlayer = true;
                break;
            }
        }

        // 자석 범위 인디케이터 업데이트
        if (rangeIndicator != null)
        {
            if (hasPlayer && magnetRange > 0f)
            {
                rangeIndicator.UpdateRange(magnetRange, new Vector3(playerPos.x, playerPos.y, playerPos.z));
            }
            else
            {
                rangeIndicator.UpdateRange(0f, Vector3.zero);
            }
        }

        // 플레이어가 없거나 자석 버프가 없으면 종료
        if (!hasPlayer || magnetRange <= 0f)
            return;

        // 모든 Star 비주얼에 대해 자석 효과 적용
        foreach (var (starTransform, _) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<ClientStarVisual>>())
        {
            float3 starPos = starTransform.ValueRO.Position;
            float distance = math.distance(starPos, playerPos);

            // 자석 범위 내에 있으면 플레이어 방향으로 이동
            if (distance <= magnetRange && distance > 0.1f)
            {
                float3 direction = math.normalizesafe(playerPos - starPos);

                // 거리에 따른 속도 계산: 멀수록 빠르게, 가까울수록 느리게
                float distanceRatio = distance / magnetRange;  // 0 ~ 1 (가까움 ~ 멂)
                float speed = math.lerp(MinSpeed, MaxSpeed, distanceRatio);

                float moveDistance = speed * deltaTime;

                // 플레이어보다 더 멀리 가지 않도록 제한
                if (moveDistance > distance)
                {
                    moveDistance = distance;
                }

                starTransform.ValueRW.Position += direction * moveDistance;
            }
        }
    }
}
