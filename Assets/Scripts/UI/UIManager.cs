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

    [Header("Game Over UI")]
    public GameObject GameOverPanel;
    public TextMeshProUGUI FinalStatsText;

    [Header("Restart")]
    public Button RestartButton;

    private void Awake()
    {
        // 게임 오버 패널 초기 비활성화
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }

        // 재시작 버튼 이벤트 연결
        if (RestartButton != null)
        {
            RestartButton.onClick.AddListener(OnRestartButtonClicked);
        }
    }

    private void OnRestartButtonClicked()
    {
        var restartManager = GetComponent<GameRestartManager>();
        if (restartManager != null)
        {
            restartManager.RestartGame();
        }
        else
        {
            Debug.LogWarning("GameRestartManager not found on GameUI!");
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
}
