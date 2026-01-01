using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 총알 생명주기 시스템
/// 서버에서 버프 선택 중이면 생명주기 감소 일시정지
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BulletMovementSystem))]
[BurstCompile]
public partial struct BulletLifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 버프 선택 중이면 총알 생명주기 감소 중지
        foreach (var sessionState in SystemAPI.Query<RefRO<GameSessionState>>())
        {
            if (sessionState.ValueRO.IsGamePaused)
                return;
        }

        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 총알 생명주기 감소 및 삭제
        foreach (var (lifetime, entity) in
                 SystemAPI.Query<RefRW<BulletLifetime>>()
                     .WithAll<BulletTag>()
                     .WithEntityAccess())
        {
            // ValueRW를 통해 컴포넌트 직접 수정 (로컬 복사본 생성 방지)
            lifetime.ValueRW.RemainingTime -= deltaTime;

            // 생존 시간 종료 시 Entity 삭제
            if (lifetime.ValueRW.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
