using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// 총알-몬스터 충돌 처리 시스템 (Server에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BulletMovementSystem))]
[UpdateAfter(typeof(EnemyChaseSystem))]
[BurstCompile]
public partial struct BulletHitSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 모든 총알에 대해
        foreach (var (bulletTransform, bulletEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<BulletTag>()
                     .WithEntityAccess())
        {
            float3 bulletPos = bulletTransform.ValueRO.Position;

            // 모든 Enemy와 거리 체크
            foreach (var (enemyTransform, enemyHealth, enemyEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<EnemyHealth>>()
                         .WithAll<EnemyTag>()
                         .WithEntityAccess())
            {
                float3 enemyPos = enemyTransform.ValueRO.Position;
                float distance = math.distance(bulletPos, enemyPos);

                // 충돌 반경: 총알(0.2) + Enemy(0.5) = 0.7
                if (distance < 0.7f)
                {
                    // 데미지 적용
                    var damage = SystemAPI.GetComponent<DamageValue>(bulletEntity).Value;
                    enemyHealth.ValueRW.Value -= damage;

                    // Enemy 체력이 0 이하면 삭제
                    if (enemyHealth.ValueRO.Value <= 0)
                    {
                        ecb.DestroyEntity(enemyEntity);

                        // RPC 생성: 모든 Client + Server에게 KillCount +1 브로드캐스트
                        var rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new KillCountRpc
                        {
                            IncrementAmount = 1
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
                        // TargetConnection 생략 → 모든 Client에게 브로드캐스트
                    }

                    // 총알 삭제
                    ecb.DestroyEntity(bulletEntity);
                    break; // 이 총알은 처리 완료
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
