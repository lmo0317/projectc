using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 화면 UI
/// Dedicated 서버 시작 또는 클라이언트로 서버 접속
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startServerButton;
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (startServerButton != null)
        {
            startServerButton.onClick.AddListener(OnStartServerClicked);
        }

        if (startHostButton != null)
        {
            startHostButton.onClick.AddListener(OnStartHostClicked);
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectClicked);
        }

        // 기본 주소 설정
        if (addressInput != null)
        {
            addressInput.text = "127.0.0.1";
        }

        UpdateStatus("서버를 시작하거나 서버에 접속하세요");
    }

    private void OnStartServerClicked()
    {
        if (NetworkConnectionManager.Instance == null)
        {
            Debug.LogError("[LobbyUI] NetworkConnectionManager not found!");
            UpdateStatus("오류: NetworkConnectionManager가 없습니다");
            return;
        }

        UpdateStatus("서버 시작 중...");
        SetButtonsInteractable(false);

        NetworkConnectionManager.Instance.StartDedicatedServer();
    }

    private void OnStartHostClicked()
    {
        if (NetworkConnectionManager.Instance == null)
        {
            Debug.LogError("[LobbyUI] NetworkConnectionManager not found!");
            UpdateStatus("오류: NetworkConnectionManager가 없습니다");
            return;
        }

        UpdateStatus("호스트로 시작 중...");
        SetButtonsInteractable(false);

        NetworkConnectionManager.Instance.StartAsHost();
    }

    private void OnConnectClicked()
    {
        if (NetworkConnectionManager.Instance == null)
        {
            Debug.LogError("[LobbyUI] NetworkConnectionManager not found!");
            UpdateStatus("오류: NetworkConnectionManager가 없습니다");
            return;
        }

        string address = addressInput != null ? addressInput.text : "127.0.0.1";
        if (string.IsNullOrEmpty(address))
        {
            address = "127.0.0.1";
        }

        UpdateStatus($"서버에 접속 중... ({address})");
        SetButtonsInteractable(false);

        NetworkConnectionManager.Instance.ConnectToServer(address, NetworkConnectionManager.Instance.Port);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (startServerButton != null)
            startServerButton.interactable = interactable;
        if (startHostButton != null)
            startHostButton.interactable = interactable;
        if (connectButton != null)
            connectButton.interactable = interactable;
        if (addressInput != null)
            addressInput.interactable = interactable;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
