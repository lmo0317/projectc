using Unity.Burst;
using Unity.Entities;

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
