using Unity.Entities;
using UnityEngine;

/// <summary>
/// 게임 통계 싱글톤 Entity 생성
/// </summary>
public class GameStatsAuthoring : MonoBehaviour
{
    class Baker : Baker<GameStatsAuthoring>
    {
        public override void Bake(GameStatsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GameStats
            {
                SurvivalTime = 0f,
                KillCount = 0
            });
        }
    }
}
