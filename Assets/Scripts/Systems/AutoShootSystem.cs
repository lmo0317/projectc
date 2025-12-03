using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
[BurstCompile]
public partial struct AutoShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // SubScene 환경에서 테스트를 위해 임시로 주석 처리
        // state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 게임 오버 시 발사 중지
        if (SystemAPI.HasSingleton<GameOverTag>())
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 플레이어의 발사 처리
        foreach (var (transform, shootConfig, playerTag) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<AutoShootConfig>, RefRO<PlayerTag>>())
        {
            // ValueRW를 통해 컴포넌트 직접 수정 (로컬 복사본 생성 방지)
            shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

            // 발사 간격 체크
            if (shootConfig.ValueRW.TimeSinceLastShot >= shootConfig.ValueRW.FireRate)
            {
                shootConfig.ValueRW.TimeSinceLastShot = 0f;

                // 총알 Entity 생성
                var bulletEntity = ecb.Instantiate(shootConfig.ValueRW.BulletPrefab);

                // 총알 위치 설정 (플레이어 위치)
                ecb.SetComponent(bulletEntity, LocalTransform.FromPosition(transform.ValueRO.Position));

                // 총알 발사 방향 설정 (초기에는 위쪽 고정: +Z)
                ecb.SetComponent(bulletEntity, new BulletDirection { Value = new float3(0, 0, 1) });
            }
        }
    }
}
