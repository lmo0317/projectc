using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 플레이어 스탯 UI 업데이트 시스템
/// Server/Client 모두에서 실행 (싱글플레이 지원)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct PlayerStatsUISystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StatModifiers>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (PlayerStatsUI.Instance == null)
            return;

        // 로컬 플레이어의 스탯 찾기
        foreach (var (stats, shootConfig, moveSpeed, health, _) in
                 SystemAPI.Query<RefRO<StatModifiers>, RefRO<AutoShootConfig>, RefRO<MovementSpeed>, RefRO<PlayerHealth>, RefRO<PlayerTag>>()
                     .WithAll<GhostOwnerIsLocal>())
        {
            UpdateUI(stats.ValueRO, shootConfig.ValueRO, moveSpeed.ValueRO, health.ValueRO);
            return;
        }

        // GhostOwnerIsLocal이 없는 경우 (싱글플레이)
        foreach (var (stats, shootConfig, moveSpeed, health, _) in
                 SystemAPI.Query<RefRO<StatModifiers>, RefRO<AutoShootConfig>, RefRO<MovementSpeed>, RefRO<PlayerHealth>, RefRO<PlayerTag>>()
                     .WithDisabled<PlayerDead>())
        {
            UpdateUI(stats.ValueRO, shootConfig.ValueRO, moveSpeed.ValueRO, health.ValueRO);
            return;
        }
    }

    private void UpdateUI(in StatModifiers stats, in AutoShootConfig shootConfig, in MovementSpeed moveSpeed, in PlayerHealth health)
    {
        // 기본 스탯 설정 (Inspector에서 설정한 값 또는 컴포넌트 값 사용)
        var ui = PlayerStatsUI.Instance;

        // 기본 스탯이 설정되지 않았으면 컴포넌트에서 가져옴
        if (ui.BaseSpeed <= 0f)
            ui.BaseSpeed = moveSpeed.Value;
        if (ui.BaseFireRate <= 0f)
            ui.BaseFireRate = shootConfig.BaseFireRate;
        if (ui.BaseMissileCount <= 0)
            ui.BaseMissileCount = shootConfig.BaseMissileCount;
        if (ui.BaseMaxHealth <= 0f)
            ui.BaseMaxHealth = health.MaxHealth - stats.BonusMaxHealth;

        ui.UpdateStats(
            stats.DamageMultiplier,
            stats.SpeedMultiplier,
            stats.FireRateMultiplier,
            stats.BonusMissileCount,
            stats.CriticalChance,
            stats.CriticalMultiplier,
            stats.MagnetRange,
            stats.HealthRegenPerSecond,
            stats.BonusMaxHealth
        );
    }
}
