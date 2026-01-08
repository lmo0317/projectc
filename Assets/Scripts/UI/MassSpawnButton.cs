using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 대량 몬스터 소환 버튼 MonoBehaviour
/// 클라이언트에서 입력 필드의 수치만큼 몬스터를 소환
/// </summary>
[RequireComponent(typeof(Button))]
public class MassSpawnButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField; // 수치 입력 필드

    [Header("Default Settings")]
    [SerializeField] private int defaultSpawnCount = 500; // 기본 소환 수

    private Button button;
    private World clientWorld;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        // 클라이언트 World 찾기
        foreach (var world in World.All)
        {
            if (world.IsClient())
            {
                clientWorld = world;
                break;
            }
        }

        if (clientWorld == null)
        {
            Debug.LogError("[MassSpawnButton] 클라이언트 World를 찾을 수 없습니다!");
            button.interactable = false;
        }

        // InputField 기본값 설정
        if (inputField != null)
        {
            inputField.text = defaultSpawnCount.ToString();
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber; // 숫자만 입력 가능
        }
    }

    private void OnButtonClicked()
    {
        if (clientWorld == null)
        {
            Debug.LogError("[MassSpawnButton] 클라이언트 World가 없어서 RPC를 전송할 수 없습니다!");
            return;
        }

        // InputField에서 수치 가져오기
        int spawnCount = defaultSpawnCount;
        if (inputField != null && !string.IsNullOrEmpty(inputField.text))
        {
            if (int.TryParse(inputField.text, out int parsedValue))
            {
                spawnCount = Mathf.Clamp(parsedValue, 1, 10000); // 1~10000 범위로 제한
            }
            else
            {
                Debug.LogWarning("[MassSpawnButton] 입력값이 유효하지 않습니다. 기본값을 사용합니다.");
            }
        }

        var entityManager = clientWorld.EntityManager;

        // RPC Entity 생성
        var rpcEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(rpcEntity, new SpawnMassEnemiesRequest
        {
            Count = spawnCount
        });
        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);

        Debug.Log($"[MassSpawnButton] 서버에 {spawnCount}마리 몬스터 소환 RPC 전송!");
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
