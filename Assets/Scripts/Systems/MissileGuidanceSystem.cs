using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 미사일 유도 시스템
/// 미사일이 타겟을 향해 방향을 전환하도록 처리
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(BulletMovementSystem))]
[BurstCompile]
public partial struct MissileGuidanceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MissileTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        new MissileGuidanceJob
        {
            DeltaTime = deltaTime,
            LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct MissileGuidanceJob : IJobEntity
{
    public float DeltaTime;
    [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

    void Execute(
        ref BulletDirection direction,
        in LocalTransform transform,
        in MissileTarget target,
        in MissileTurnSpeed turnSpeed)
    {
        // 타겟 Entity가 유효하지 않으면 직진
        if (target.TargetEntity == Entity.Null)
            return;

        // 타겟 Entity가 존재하는지 확인
        if (!LocalTransformLookup.TryGetComponent(target.TargetEntity, out var targetTransform))
            return;

        // 타겟 방향 계산
        float3 toTarget = targetTransform.Position - transform.Position;
        float distanceToTarget = math.length(toTarget);

        // 타겟이 너무 가까우면 스킵 (0으로 나누기 방지)
        if (distanceToTarget < 0.01f)
            return;

        float3 targetDirection = toTarget / distanceToTarget;

        // 현재 방향에서 타겟 방향으로 부드럽게 회전
        float maxRotationRad = math.radians(turnSpeed.Value * DeltaTime);

        // Slerp를 사용하여 부드러운 회전
        float3 newDirection = RotateTowards(direction.Value, targetDirection, maxRotationRad);
        direction.Value = newDirection;
    }

    // 방향을 부드럽게 회전시키는 함수
    private float3 RotateTowards(float3 current, float3 target, float maxRadians)
    {
        float dot = math.dot(current, target);

        // 이미 같은 방향이면 타겟 방향 반환
        if (dot >= 0.9999f)
            return target;

        // 회전 각도 계산
        float angle = math.acos(math.clamp(dot, -1f, 1f));

        // 최대 회전 각도 제한
        if (angle <= maxRadians)
            return target;

        // 부분 회전
        float t = maxRadians / angle;
        return math.normalize(math.lerp(current, target, t));
    }
}
