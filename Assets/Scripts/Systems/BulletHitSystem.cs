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
        // 버프 선택 중이면 충돌 처리 중지
        foreach (var sessionState in SystemAPI.Query<RefRO<GameSessionState>>())
        {
            if (sessionState.ValueRO.IsGamePaused)
                return;
        }

        // Ghost 프리팹 인스턴스화를 위해 BeginSimulationEntityCommandBufferSystem 사용
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Star 스폰 설정 가져오기 (없으면 스폰 안 함)
        bool hasStarSpawnConfig = SystemAPI.TryGetSingletonRW<StarSpawnConfig>(out var starSpawnConfig);

        // 플레이어의 StatModifiers 가져오기
        StatModifiers playerModifiers = StatModifiers.Default;
        foreach (var modifiers in SystemAPI.Query<RefRO<StatModifiers>>()
                     .WithAll<PlayerTag>()
                     .WithDisabled<PlayerDead>())
        {
            playerModifiers = modifiers.ValueRO;
            break;  // 첫 번째 살아있는 플레이어 사용
        }

        // InGame 상태인 연결 목록 수집 (RPC 전송용)
        var inGameConnections = new NativeList<Entity>(Allocator.Temp);
        foreach (var (networkId, connectionEntity) in
                 SystemAPI.Query<RefRO<NetworkId>>()
                     .WithAll<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            inGameConnections.Add(connectionEntity);
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
                    // 총알 데미지 가져오기
                    float bulletDamage = SystemAPI.GetComponent<DamageValue>(bulletEntity).Value;

                    // 최종 데미지 계산: 총알 데미지 * 데미지 배율
                    float finalDamage = bulletDamage * playerModifiers.DamageMultiplier;

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

                    // 피격 이펙트 RPC 전송 (InGame 상태인 클라이언트에게만)
                    foreach (var connectionEntity in inGameConnections)
                    {
                        var hitEffectRpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(hitEffectRpcEntity, new HitEffectRpc
                        {
                            Position = enemyPos,
                            Damage = (int)finalDamage,
                            IsCritical = isCritical
                        });
                        ecb.AddComponent(hitEffectRpcEntity, new SendRpcCommandRequest
                        {
                            TargetConnection = connectionEntity
                        });
                    }

                    // Enemy 체력이 0 이하면 삭제
                    if (enemyHealth.ValueRO.Value <= 0)
                    {
                        ecb.DestroyEntity(enemyEntity);

                        // Star 아이템 스폰
                        if (hasStarSpawnConfig)
                        {
                            SpawnStar(ref ecb, ref starSpawnConfig.ValueRW, enemyPos, inGameConnections);
                        }

                        // RPC 생성: InGame 상태인 Client에게만 KillCount +1 전송
                        foreach (var connectionEntity in inGameConnections)
                        {
                            var rpcEntity = ecb.CreateEntity();
                            ecb.AddComponent(rpcEntity, new KillCountRpc
                            {
                                IncrementAmount = 1
                            });
                            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
                            {
                                TargetConnection = connectionEntity
                            });
                        }
                    }

                    // 총알 삭제
                    ecb.DestroyEntity(bulletEntity);
                    break; // 이 총알은 처리 완료
                }
            }
        }

        inGameConnections.Dispose();

        // BeginSimulationEntityCommandBufferSystem이 자동으로 Playback 처리
    }

    /// <summary>
    /// Star 아이템 스폰 (서버에서 직접 엔티티 생성 + 클라이언트에 RPC 전송)
    /// </summary>
    private void SpawnStar(ref EntityCommandBuffer ecb, ref StarSpawnConfig config, float3 position, NativeList<Entity> inGameConnections)
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

        // 클라이언트에 Star 스폰 RPC 전송 (InGame 상태인 클라이언트에게만)
        foreach (var connectionEntity in inGameConnections)
        {
            var rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new StarSpawnRpc
            {
                StarId = starId,
                Position = spawnPos
            });
            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest
            {
                TargetConnection = connectionEntity
            });
        }
    }
}
