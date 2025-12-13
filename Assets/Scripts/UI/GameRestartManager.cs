using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// 게임 재시작 관리 (MonoBehaviour)
/// Restart 버튼 클릭 시 서버에 부활 RPC 전송
/// </summary>
public class GameRestartManager : MonoBehaviour
{
    /// <summary>
    /// 부활 요청 (RPC를 서버로 전송)
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GameRestartManager] Restart button clicked!");

        // Client World 찾기
        foreach (var world in World.All)
        {
            if (!world.IsClient())
                continue;

            var entityManager = world.EntityManager;

            // 서버로 연결된 NetworkStreamConnection 찾기
            var connectionQuery = entityManager.CreateEntityQuery(
                typeof(NetworkStreamConnection),
                typeof(NetworkId)
            );

            if (connectionQuery.IsEmpty)
            {
                Debug.LogWarning("[GameRestartManager] No network connection found!");
                continue;
            }

            var connections = connectionQuery.ToEntityArray(Allocator.Temp);
            if (connections.Length == 0)
            {
                connections.Dispose();
                continue;
            }

            var connectionEntity = connections[0];
            connections.Dispose();

            // RPC Entity 생성 및 전송
            var rpcEntity = entityManager.CreateEntity();
            entityManager.AddComponent<RespawnRequestRpc>(rpcEntity);
            entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
            entityManager.SetComponentData(rpcEntity, new SendRpcCommandRequest
            {
                TargetConnection = Entity.Null // 서버로 전송 (Null = broadcast/server)
            });

            Debug.Log("[GameRestartManager] Sent RespawnRequestRpc to server!");
            break;
        }
    }
}
