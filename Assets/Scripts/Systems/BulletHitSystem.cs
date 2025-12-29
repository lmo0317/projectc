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
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BulletTag>();
        state.RequireForUpdate<EnemyTag>();
        _random = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);
    }

    public void OnUpdate(ref SystemState state)
    {
        // Ghost 프리팹 인스턴스화를 위해 BeginSimulationEntityCommandBufferSystem 사용
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Star 스폰 설정 가져오기 (없으면 스폰 안 함)
        bool hasStarSpawnConfig = SystemAPI.TryGetSingletonRW<StarSpawnConfig>(out var starSpawnConfig);

        // 플레이어의 StatModifiers 가져오기 (데미지/치명타 버프용)
        StatModifiers playerModifiers = StatModifiers.Default;
        foreach (var modifiers in SystemAPI.Query<RefRO<StatModifiers>>().WithAll<PlayerTag>())
        {
            playerModifiers = modifiers.ValueRO;
            break;  // 첫 번째 플레이어 사용
        }

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
                    // 기본 데미지 가져오기
                    float baseDamage = SystemAPI.GetComponent<DamageValue>(bulletEntity).Value;

                    // 버프 적용된 데미지 계산
                    float finalDamage = baseDamage * playerModifiers.DamageMultiplier;

                    // 치명타 판정
                    bool isCritical = false;
                    if (playerModifiers.CriticalChance > 0f)
                    {
                        float roll = _random.NextFloat(0f, 100f);
                        if (roll < playerModifiers.CriticalChance)
                        {
                            isCritical = true;
                            finalDamage *= playerModifiers.CriticalMultiplier;
                        }
                    }

                    // 데미지 적용
                    enemyHealth.ValueRW.Value -= (int)finalDamage;

                    // 피격 이펙트 RPC 전송 (모든 클라이언트에게)
                    var hitEffectRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(hitEffectRpcEntity, new HitEffectRpc
                    {
                        Position = enemyPos,
                        Damage = (int)finalDamage,
                        IsCritical = isCritical
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(hitEffectRpcEntity);

                    // Enemy 체력이 0 이하면 삭제
                    if (enemyHealth.ValueRO.Value <= 0)
                    {
                        ecb.DestroyEntity(enemyEntity);

                        // Star 아이템 스폰
                        if (hasStarSpawnConfig)
                        {
                            SpawnStar(ref ecb, ref starSpawnConfig.ValueRW, enemyPos);
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
        // 서버에서 직접 엔티티 생성
        var starEntity = ecb.CreateEntity();

        // Star ID 생성
        int starId = config.RandomGenerator.NextInt(1, int.MaxValue);

        // 바닥 위치에 스폰 (Y = 0)
        float3 spawnPos = new float3(position.x, 0f, position.z);

        // 서버 엔티티에 필수 컴포넌트 추가
        ecb.AddComponent(starEntity, new StarTag());
        ecb.AddComponent(starEntity, new StarValue { Value = 1 });
        ecb.AddComponent(starEntity, new StarId { Value = starId });
        ecb.AddComponent(starEntity, LocalTransform.FromPosition(spawnPos));

        // 클라이언트에 Star 스폰 RPC 전송
        var rpcEntity = ecb.CreateEntity();
        ecb.AddComponent(rpcEntity, new StarSpawnRpc
        {
            StarId = starId,
            Position = spawnPos
        });
        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
    }
}
