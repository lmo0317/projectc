using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 따라다니도록 하는 스크립트
/// ECS World에서 내 플레이어 Entity를 찾아 위치를 추적
/// 탑다운 뷰로 고정된 각도 사용
/// </summary>
public class PlayerFollowCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    public Vector3 Offset = new Vector3(0f, 15f, -10f);  // 카메라 오프셋 (탑다운 뷰)
    public float SmoothSpeed = 5f;                        // 부드러운 이동 속도

    [Header("Fixed Rotation (Top-Down View)")]
    public Vector3 FixedRotation = new Vector3(50f, 0f, 0f);  // 고정 카메라 각도 (X: 아래를 바라보는 각도)

    private Vector3 targetPosition;
    private bool initialized = false;

    private void Start()
    {
        // 시작 시 고정 각도 설정
        transform.rotation = Quaternion.Euler(FixedRotation);
    }

    private void LateUpdate()
    {
        // Client World 찾기
        var clientWorld = GetClientWorld();
        if (clientWorld == null || !clientWorld.IsCreated)
            return;

        // 내 플레이어 위치 찾기
        Vector3 playerPosition;
        if (!TryGetMyPlayerPosition(clientWorld, out playerPosition))
        {
            return;
        }

        // 타겟 위치 계산 (플레이어 위치 + 오프셋)
        targetPosition = playerPosition + Offset;

        // 첫 프레임에서는 즉시 이동
        if (!initialized)
        {
            transform.position = targetPosition;
            initialized = true;
            return;
        }

        // 부드러운 카메라 이동 (위치만, 각도는 고정)
        transform.position = Vector3.Lerp(transform.position, targetPosition, SmoothSpeed * Time.deltaTime);
    }

    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                return world;
            }
        }
        return null;
    }

    private bool TryGetMyPlayerPosition(World clientWorld, out Vector3 position)
    {
        position = Vector3.zero;

        var entityManager = clientWorld.EntityManager;

        // NetworkId 싱글톤에서 내 네트워크 ID 가져오기
        if (!clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkId)).TryGetSingleton<NetworkId>(out var networkId))
        {
            return false;
        }

        int myNetworkId = networkId.Value;

        // 내 플레이어 Entity 찾기
        var query = entityManager.CreateEntityQuery(
            typeof(LocalTransform),
            typeof(GhostOwner),
            typeof(PlayerHealth)
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
        var owners = query.ToComponentDataArray<GhostOwner>(Unity.Collections.Allocator.Temp);

        bool found = false;
        for (int i = 0; i < entities.Length; i++)
        {
            // GhostOwner의 NetworkId가 내 것과 일치하는지 확인
            if (owners[i].NetworkId == myNetworkId)
            {
                // PlayerDead 상태 확인 (죽은 플레이어는 추적하지 않음)
                var entity = entities[i];
                if (entityManager.HasComponent<PlayerDead>(entity) &&
                    entityManager.IsComponentEnabled<PlayerDead>(entity))
                {
                    // 죽은 상태면 스킵
                    continue;
                }

                position = transforms[i].Position;
                found = true;
                break;
            }
        }

        entities.Dispose();
        transforms.Dispose();
        owners.Dispose();

        return found;
    }
}
