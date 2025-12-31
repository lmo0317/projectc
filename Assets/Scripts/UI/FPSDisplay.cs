using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 왼쪽 하단에 FPS 표시
/// </summary>
public class FPSDisplay : MonoBehaviour
{
    public static FPSDisplay Instance { get; private set; }

    [Header("Settings")]
    public float UpdateInterval = 0.5f;  // FPS 업데이트 주기 (초)
    public int FontSize = 24;
    public Color TextColor = Color.green;
    public Color LowFPSColor = Color.red;      // 낮은 FPS일 때 색상
    public int LowFPSThreshold = 30;           // 낮은 FPS 기준

    private TextMeshProUGUI fpsText;
    private float deltaTime = 0f;
    private float timer = 0f;
    private int frameCount = 0;

    private void Awake()
    {
        Instance = this;
        CreateFPSText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CreateFPSText()
    {
        // FPS 텍스트 오브젝트 생성
        var textObj = new GameObject("FPSText");
        textObj.transform.SetParent(transform, false);

        var rect = textObj.AddComponent<RectTransform>();
        // 왼쪽 하단에 배치
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(10, 10);
        rect.sizeDelta = new Vector2(150, 40);

        fpsText = textObj.AddComponent<TextMeshProUGUI>();
        fpsText.fontSize = FontSize;
        fpsText.color = TextColor;
        fpsText.alignment = TextAlignmentOptions.BottomLeft;
        fpsText.text = "FPS: --";

        // 아웃라인 효과 추가 (가독성 향상)
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
    }

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= UpdateInterval)
        {
            float fps = frameCount / timer;
            UpdateFPSDisplay(fps);

            // 리셋
            timer = 0f;
            frameCount = 0;
        }
    }

    private void UpdateFPSDisplay(float fps)
    {
        if (fpsText == null) return;

        int fpsInt = Mathf.RoundToInt(fps);
        fpsText.text = $"FPS: {fpsInt}";

        // 낮은 FPS일 때 색상 변경
        fpsText.color = fpsInt < LowFPSThreshold ? LowFPSColor : TextColor;
    }

    /// <summary>
    /// FPS 표시 토글
    /// </summary>
    public void Toggle()
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
        }
    }

    /// <summary>
    /// FPS 표시 활성화/비활성화
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(visible);
        }
    }
}
