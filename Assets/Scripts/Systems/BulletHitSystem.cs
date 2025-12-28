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
public partial struct BulletHitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
        state.RequireForUpdate<EnemyTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Ghost 프리팹 인스턴스화를 위해 BeginSimulationEntityCommandBufferSystem 사용
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Star 스폰 설정 가져오기 (없으면 스폰 안 함)
        bool hasStarSpawnConfig = SystemAPI.TryGetSingletonRW<StarSpawnConfig>(out var starSpawnConfig);

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

                    // 피격 이펙트 RPC 전송 (모든 클라이언트에게)
                    var hitEffectRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(hitEffectRpcEntity, new HitEffectRpc
                    {
                        Position = enemyPos,
                        Damage = damage
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(hitEffectRpcEntity);

                    // Enemy 체력이 0 이하면 삭제
                    if (enemyHealth.ValueRO.Value <= 0)
                    {
                        ecb.DestroyEntity(enemyEntity);

                        // Star 아이템 스폰
                        if (hasStarSpawnConfig)
                        {
                            UnityEngine.Debug.Log($"[BulletHit] Spawning Star at {enemyPos}");
                            SpawnStar(ref ecb, ref starSpawnConfig.ValueRW, enemyPos);
                        }
                        else
                        {
                            UnityEngine.Debug.Log("[BulletHit] No StarSpawnConfig!");
                        }

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

        // BeginSimulationEntityCommandBufferSystem이 자동으로 Playback 처리
    }

    /// <summary>
    /// Star 아이템 스폰 (서버에서 직접 엔티티 생성 + 클라이언트에 RPC 전송)
    /// </summary>
    private void SpawnStar(ref EntityCommandBuffer ecb, ref StarSpawnConfig config, float3 position)
    {
        // 서버에서 직접 엔티티 생성 (Ghost 프리팹 대신)
        var starEntity = ecb.CreateEntity();

        // 랜덤 방향으로 튀어오르는 속도 계산
        float angle = config.RandomGenerator.NextFloat(0f, math.PI * 2f);
        float force = config.RandomGenerator.NextFloat(config.SpawnForceMin, config.SpawnForceMax);
        float horizontalForce = config.RandomGenerator.NextFloat(0f, config.SpreadRadius);

        float3 velocity = new float3(
            math.cos(angle) * horizontalForce,
            force,  // 위로 튀어오름
            math.sin(angle) * horizontalForce
        );

        // Star ID 생성 (간단하게 랜덤 사용)
        int starId = config.RandomGenerator.NextInt(1, int.MaxValue);

        // 서버 엔티티에 필수 컴포넌트 추가
        ecb.AddComponent(starEntity, new StarTag());
        ecb.AddComponent(starEntity, new StarValue { Value = 1 });
        ecb.AddComponent(starEntity, new StarId { Value = starId });
        ecb.AddComponent(starEntity, LocalTransform.FromPosition(position));
        ecb.AddComponent(starEntity, new StarMovement
        {
            Velocity = velocity,
            Gravity = 15f,
            GroundY = 0f,
            BounceDamping = 0.5f,
            IsSettled = false
        });

        // 클라이언트에 Star 스폰 RPC 전송
        var rpcEntity = ecb.CreateEntity();
        ecb.AddComponent(rpcEntity, new StarSpawnRpc
        {
            StarId = starId,
            Position = position,
            Velocity = velocity
        });
        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
    }
}
