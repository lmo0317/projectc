using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 텍스트 참조 관리 (MonoBehaviour)
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD Text References")]
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI SurvivalTimeText;
    public TextMeshProUGUI KillCountText;
    public TextMeshProUGUI StarPointsText;

    [Header("Game Over UI")]
    public GameObject GameOverPanel;
    public TextMeshProUGUI FinalStatsText;

    [Header("Exit")]
    public Button ExitButton;

    private void Awake()
    {
        // 게임 오버 패널 초기 비활성화
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }

        // 나가기 버튼 이벤트 연결
        if (ExitButton != null)
        {
            ExitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    private void OnExitButtonClicked()
    {
        Debug.Log("[UIManager] Exit button clicked - returning to lobby");

        if (NetworkConnectionManager.Instance != null)
        {
            NetworkConnectionManager.Instance.ReturnToLobby();
        }
        else
        {
            Debug.LogWarning("[UIManager] NetworkConnectionManager not found!");
            // fallback: 직접 씬 로드
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
    }

    /// <summary>
    /// HUD 텍스트 업데이트
    /// </summary>
    public void UpdateHealth(float current, float max)
    {
        if (HealthText != null)
        {
            HealthText.text = $"Health: {current:F0} / {max:F0}";
        }
    }

    public void UpdateSurvivalTime(float time)
    {
        if (SurvivalTimeText != null)
        {
            SurvivalTimeText.text = $"Time: {time:F1}s";
        }
    }

    public void UpdateKillCount(int kills)
    {
        if (KillCountText != null)
        {
            KillCountText.text = $"Kills: {kills}";
        }
    }

    public void UpdateStarPoints(int current, int threshold)
    {
        if (StarPointsText != null)
        {
            StarPointsText.text = $"SP: {current} / {threshold}";
        }
    }

    /// <summary>
    /// 게임 오버 UI 표시
    /// </summary>
    public void ShowGameOver(float survivalTime, int killCount)
    {
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);

            if (FinalStatsText != null)
            {
                FinalStatsText.text = $"Game Over!\n\nSurvival Time: {survivalTime:F1}s\nKills: {killCount}";
            }
        }
    }

    /// <summary>
    /// 게임 오버 UI 숨기기 (부활 시 호출)
    /// </summary>
    public void HideGameOver()
    {
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }
    }
}
