using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 버프 선택 UI 관리자
/// 서버에서 ShowBuffSelectionRpc를 받으면 UI를 표시하고
/// 플레이어가 선택하면 BuffSelectedRpc를 서버로 전송
/// 다른 플레이어가 선택 중이면 대기 UI 표시
/// </summary>
public class BuffSelectionUI : MonoBehaviour
{
    public static BuffSelectionUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject Panel;
    public TextMeshProUGUI TitleText;
    public BuffOptionCard[] OptionCards; // 3개의 카드

    [Header("Waiting UI References")]
    public GameObject WaitingPanel;
    public TextMeshProUGUI WaitingText;

    [Header("Settings")]
    public bool PauseGameOnShow = true;

    // 현재 표시된 버프 옵션들
    private int[] _currentOptions = new int[3];
    private bool _isShowing;
    private bool _isWaiting; // 다른 플레이어 대기 중

    private void Awake()
    {
        Debug.Log("[BuffSelectionUI] Awake 호출됨");

        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[BuffSelectionUI] Instance 설정됨");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 초기 상태: 숨김
        if (Panel != null)
        {
            Panel.SetActive(false);
        }

        if (WaitingPanel != null)
        {
            WaitingPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // OnEnable에서도 Instance 설정 (비활성화 후 활성화 시)
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[BuffSelectionUI] Instance 설정됨 (OnEnable)");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 버프 선택 UI 표시
    /// </summary>
    public void Show(int option1Type, int option1Level,
                     int option2Type, int option2Level,
                     int option3Type, int option3Level)
    {
        if (_isShowing)
            return;

        _isShowing = true;
        _currentOptions[0] = option1Type;
        _currentOptions[1] = option2Type;
        _currentOptions[2] = option3Type;

        // 패널 활성화
        if (Panel != null)
        {
            Panel.SetActive(true);
        }

        // 타이틀 설정
        if (TitleText != null)
        {
            TitleText.text = "레벨 업! 버프를 선택하세요";
        }

        // 카드 설정
        if (OptionCards != null && OptionCards.Length >= 3)
        {
            OptionCards[0]?.Setup(option1Type, option1Level, OnBuffSelected);
            OptionCards[1]?.Setup(option2Type, option2Level, OnBuffSelected);
            OptionCards[2]?.Setup(option3Type, option3Level, OnBuffSelected);
        }

        // 서버가 게임을 일시정지하므로 클라이언트에서는 Time.timeScale을 조정하지 않음
        // 네트워크 시뮬레이션이 멈추면 Ghost가 자동으로 멈춤

        // 커서 표시
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log($"[BuffSelectionUI] 버프 선택 UI 표시: {(BuffType)option1Type}, {(BuffType)option2Type}, {(BuffType)option3Type}");
    }

    /// <summary>
    /// 버프 선택 UI 숨기기
    /// </summary>
    public void Hide()
    {
        if (!_isShowing)
            return;

        _isShowing = false;

        // 패널 비활성화
        if (Panel != null)
        {
            Panel.SetActive(false);
        }

        // 서버가 게임 재개를 처리하므로 클라이언트에서는 Time.timeScale을 조정하지 않음

        Debug.Log("[BuffSelectionUI] 버프 선택 UI 숨김");
    }

    /// <summary>
    /// 버프 선택 시 호출
    /// </summary>
    private void OnBuffSelected(int buffType)
    {
        Debug.Log($"[BuffSelectionUI] 버프 선택됨: {(BuffType)buffType}");

        // 서버에 선택 결과 전송
        SendBuffSelectedRpc(buffType);

        // UI 숨기기
        Hide();
    }

    /// <summary>
    /// 서버에 BuffSelectedRpc 전송
    /// </summary>
    private void SendBuffSelectedRpc(int selectedBuffType)
    {
        // Client World 찾기
        World clientWorld = null;
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
            // 클라이언트 월드가 없으면 기본 월드 사용 (싱글플레이)
            clientWorld = World.DefaultGameObjectInjectionWorld;
        }

        if (clientWorld == null || !clientWorld.IsCreated)
        {
            Debug.LogWarning("[BuffSelectionUI] Client World를 찾을 수 없습니다.");
            return;
        }

        var entityManager = clientWorld.EntityManager;

        // RPC 엔티티 생성
        var rpcEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(rpcEntity, new BuffSelectedRpc
        {
            SelectedBuffType = selectedBuffType
        });
        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);

        Debug.Log($"[BuffSelectionUI] BuffSelectedRpc 전송: {(BuffType)selectedBuffType}");
    }

    /// <summary>
    /// 현재 UI가 표시 중인지 확인
    /// </summary>
    public bool IsShowing => _isShowing;

    /// <summary>
    /// 다른 플레이어가 버프 선택 중일 때 대기 UI 표시
    /// </summary>
    public void ShowWaiting()
    {
        if (_isWaiting || _isShowing)
            return;

        _isWaiting = true;

        // WaitingPanel이 없으면 씬에서 찾기
        if (WaitingPanel == null)
        {
            WaitingPanel = GameObject.Find("BuffWaitingPanel");
            if (WaitingPanel != null)
            {
                Debug.Log("[BuffSelectionUI] BuffWaitingPanel을 씬에서 찾음");
                // WaitingText도 찾기
                if (WaitingText == null)
                {
                    var textObj = WaitingPanel.GetComponentInChildren<TextMeshProUGUI>();
                    if (textObj != null)
                    {
                        WaitingText = textObj;
                    }
                }
            }
        }

        // 대기 패널 활성화
        if (WaitingPanel != null)
        {
            WaitingPanel.SetActive(true);
            Debug.Log("[BuffSelectionUI] 대기 UI 표시");
        }
        else
        {
            Debug.LogWarning("[BuffSelectionUI] WaitingPanel이 null입니다! Inspector에서 연결하거나 BuffWaitingPanel 오브젝트를 생성하세요.");
        }

        // 대기 텍스트 설정
        if (WaitingText != null)
        {
            WaitingText.text = "다른 플레이어가 버프를 선택 중입니다...";
        }
    }

    /// <summary>
    /// 대기 UI 숨기기
    /// </summary>
    public void HideWaiting()
    {
        if (!_isWaiting)
            return;

        _isWaiting = false;

        // 대기 패널 비활성화
        if (WaitingPanel != null)
        {
            WaitingPanel.SetActive(false);
        }

        Debug.Log("[BuffSelectionUI] 대기 UI 숨김");
    }

    /// <summary>
    /// 모든 UI 숨기기 (게임 재개 시 호출)
    /// </summary>
    public void HideAll()
    {
        Hide();
        HideWaiting();
    }

    /// <summary>
    /// 대기 중인지 확인
    /// </summary>
    public bool IsWaiting => _isWaiting;
}
