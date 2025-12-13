using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 피격 이펙트 RPC 수신 및 처리 시스템 (Client에서만 실행)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class HitEffectClientSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // HitEffectRpc 처리
        foreach (var (rpc, rpcEntity) in
                 SystemAPI.Query<RefRO<HitEffectRpc>>()
                     .WithAll<ReceiveRpcCommandRequest>()
                     .WithEntityAccess())
        {
            Vector3 position = rpc.ValueRO.Position;
            float damage = rpc.ValueRO.Damage;

            // 1. 파티클 이펙트 재생
            if (HitEffectPool.Instance != null)
            {
                HitEffectPool.Instance.PlayEffect(position);
            }

            // 2. 데미지 숫자 UI 표시
            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowDamage(position, damage);
            }

            // RPC Entity 제거
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
