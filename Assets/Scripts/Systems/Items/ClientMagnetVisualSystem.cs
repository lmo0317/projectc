using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 클라이언트에서 Star 비주얼을 자석 효과로 끌어당기는 시스템
/// 고품질 나선형 궤적 + 3단계 가속 + 크기 변화 효과
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ClientStarVisualSystem))]
public partial class ClientMagnetVisualSystem : SystemBase
{
    // 속도 설정 (가까울수록 빠르게)
    private const float MinSpeed = 8f;      // 멀리: 천천히 시작
    private const float MaxSpeed = 35f;     // 가까이: 빠르게 빨려들어감

    // 나선 회전 속도 (약하게)
    private const float SpiralRotationSpeed = 3f;  // radians per second
    private const float SpiralIntensity = 0.8f;    // 나선 강도 (약하게)

    // 크기 변화
    private const float MinScale = 0.6f;  // 최소 크기 (플레이어 가까이)
    private const float MaxScale = 1.0f;  // 최대 크기 (멀리)

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
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

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
        foreach (var (starTransform, starVisual) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<ClientStarVisual>>())
        {
            float3 starPos = starTransform.ValueRO.Position;
            float distance = math.distance(starPos, playerPos);

            // 자석 범위 내에 있으면 플레이어 방향으로 이동
            if (distance <= magnetRange && distance > 0.05f)
            {
                // 처음 끌리기 시작할 때 시간 기록
                if (starVisual.ValueRO.IsBeingPulled == 0)
                {
                    starVisual.ValueRW.IsBeingPulled = 1;
                    starVisual.ValueRW.MagnetStartTime = currentTime;
                }

                // 거리 비율 계산 (0 = 플레이어 위치, 1 = 자석 범위 끝)
                float distanceRatio = distance / magnetRange;

                // 거리에 따른 속도 계산 (가까울수록 빠르게)
                // EaseInQuad 곡선 사용: 먼 곳에서는 느리게, 가까워질수록 급격히 가속
                float t = 1f - distanceRatio;  // 0 = 멀리, 1 = 가까이
                float acceleration = t * t;     // Quadratic ease-in
                float currentSpeed = math.lerp(MinSpeed, MaxSpeed, acceleration);

                // 기본 방향 (플레이어 향해)
                float3 toPlayer = playerPos - starPos;
                float3 directionToPlayer = math.normalizesafe(toPlayer);

                // 약간의 나선 효과 (너무 강하지 않게)
                Unity.Mathematics.Random random = new Unity.Mathematics.Random(starVisual.ValueRO.RandomSeed);
                float rotationDirection = (random.NextUInt() % 2 == 0) ? 1f : -1f;
                starVisual.ValueRW.SpiralAngle += SpiralRotationSpeed * rotationDirection * deltaTime;

                // 나선 오프셋 계산 (거리 비율에 따라 나선 강도 조절)
                float spiralStrength = distanceRatio * SpiralIntensity;  // 멀수록 나선 강함
                float3 spiralOffset = float3.zero;
                if (spiralStrength > 0.05f)
                {
                    // 플레이어 방향에 수직인 벡터 계산
                    float3 perpendicular = math.normalizesafe(math.cross(directionToPlayer, new float3(0, 1, 0)));
                    if (math.lengthsq(perpendicular) < 0.01f) // 위/아래에서 접근하는 경우
                    {
                        perpendicular = math.normalizesafe(math.cross(directionToPlayer, new float3(1, 0, 0)));
                    }

                    // 회전 적용
                    float cosAngle = math.cos(starVisual.ValueRO.SpiralAngle);
                    float sinAngle = math.sin(starVisual.ValueRO.SpiralAngle);
                    float3 tangent = math.normalizesafe(math.cross(directionToPlayer, perpendicular));
                    spiralOffset = (perpendicular * cosAngle + tangent * sinAngle) * spiralStrength;
                }

                // 최종 이동 방향 (직진 + 약간의 나선)
                float3 velocity = math.normalizesafe(directionToPlayer + spiralOffset) * currentSpeed;
                float moveDistance = math.length(velocity) * deltaTime;

                // 플레이어를 넘어가지 않도록 제한
                if (moveDistance > distance)
                {
                    moveDistance = distance;
                }

                // 위치 업데이트
                starTransform.ValueRW.Position += math.normalizesafe(velocity) * moveDistance;

                // 크기 변화 (가까워질수록 작아짐)
                float scale = math.lerp(MinScale, MaxScale, distanceRatio);
                starTransform.ValueRW.Scale = scale;
            }
            else if (starVisual.ValueRO.IsBeingPulled == 1)
            {
                // 범위를 벗어나면 상태 리셋
                starVisual.ValueRW.IsBeingPulled = 0;
                starVisual.ValueRW.SpiralAngle = 0f;
                starTransform.ValueRW.Scale = 1f;
            }
        }
    }
}
