using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 재시작 관리 (MonoBehaviour)
/// </summary>
public class GameRestartManager : MonoBehaviour
{
    /// <summary>
    /// 씬 재로드를 통한 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        // 현재 씬 재로드
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

        Debug.Log($"Game Restarted! Scene: {currentSceneName}");
    }
}
