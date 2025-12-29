using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD에 현재 보유한 버프 아이콘 표시
/// </summary>
public class BuffIconsUI : MonoBehaviour
{
    public static BuffIconsUI Instance { get; private set; }

    [Header("Settings")]
    public Transform IconsContainer;        // 아이콘 부모 오브젝트
    public GameObject BuffIconPrefab;       // 버프 아이콘 프리팹
    public float IconSize = 40f;            // 아이콘 크기
    public float IconSpacing = 5f;          // 아이콘 간격

    [Header("Colors (버프 타입별)")]
    public Color DamageColor = new Color(1f, 0.3f, 0.3f);
    public Color SpeedColor = new Color(0.3f, 0.8f, 1f);
    public Color FireRateColor = new Color(1f, 0.8f, 0.3f);
    public Color MissileColor = new Color(0.8f, 0.3f, 1f);
    public Color MagnetColor = new Color(0.3f, 1f, 0.5f);
    public Color HealthRegenColor = new Color(1f, 0.5f, 0.5f);
    public Color MaxHealthColor = new Color(1f, 0.3f, 0.6f);
    public Color CriticalColor = new Color(1f, 1f, 0.3f);

    // 생성된 아이콘 오브젝트 캐시
    private Dictionary<BuffType, BuffIconItem> iconItems = new Dictionary<BuffType, BuffIconItem>();

    private void Awake()
    {
        Instance = this;

        // IconsContainer가 없으면 자신을 사용
        if (IconsContainer == null)
        {
            IconsContainer = transform;
        }

        // 프리팹이 없으면 기본 프리팹 생성
        if (BuffIconPrefab == null)
        {
            BuffIconPrefab = CreateDefaultIconPrefab();
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
    /// 기본 버프 아이콘 프리팹 생성
    /// </summary>
    private GameObject CreateDefaultIconPrefab()
    {
        var iconObj = new GameObject("BuffIcon");

        var rect = iconObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(IconSize, IconSize);

        // 배경 이미지
        var bgImage = iconObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 아이콘 이미지 (자식)
        var iconChild = new GameObject("Icon");
        iconChild.transform.SetParent(iconObj.transform, false);
        var iconRect = iconChild.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4, 4);
        iconRect.offsetMax = new Vector2(-4, -4);
        var iconImage = iconChild.AddComponent<Image>();
        iconImage.color = Color.white;

        // 레벨 텍스트 (자식)
        var levelChild = new GameObject("Level");
        levelChild.transform.SetParent(iconObj.transform, false);
        var levelRect = levelChild.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.5f, 0f);
        levelRect.anchorMax = new Vector2(1f, 0.4f);
        levelRect.offsetMin = Vector2.zero;
        levelRect.offsetMax = Vector2.zero;
        var levelText = levelChild.AddComponent<TextMeshProUGUI>();
        levelText.text = "1";
        levelText.fontSize = 14;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.BottomRight;
        levelText.color = Color.white;

        // 아웃라인 효과
        var outline = levelChild.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        iconObj.SetActive(false);
        return iconObj;
    }

    /// <summary>
    /// 버프 아이콘 업데이트
    /// </summary>
    public void UpdateBuffIcon(BuffType buffType, int level)
    {
        if (level <= 0)
        {
            // 레벨 0이면 아이콘 제거
            RemoveBuffIcon(buffType);
            return;
        }

        if (!iconItems.TryGetValue(buffType, out var iconItem))
        {
            // 새 아이콘 생성
            iconItem = CreateBuffIcon(buffType);
            iconItems[buffType] = iconItem;
        }

        // 레벨 업데이트
        iconItem.SetLevel(level);

        // 아이콘 재정렬
        ArrangeIcons();

        Debug.Log($"[BuffIconsUI] 버프 아이콘 업데이트: {buffType} Lv.{level}");
    }

    /// <summary>
    /// 버프 아이콘 제거
    /// </summary>
    public void RemoveBuffIcon(BuffType buffType)
    {
        if (iconItems.TryGetValue(buffType, out var iconItem))
        {
            if (iconItem.GameObject != null)
            {
                Destroy(iconItem.GameObject);
            }
            iconItems.Remove(buffType);
            ArrangeIcons();
        }
    }

    /// <summary>
    /// 모든 버프 아이콘 초기화
    /// </summary>
    public void ClearAllIcons()
    {
        foreach (var kvp in iconItems)
        {
            if (kvp.Value.GameObject != null)
            {
                Destroy(kvp.Value.GameObject);
            }
        }
        iconItems.Clear();
    }

    /// <summary>
    /// 버프 아이콘 생성
    /// </summary>
    private BuffIconItem CreateBuffIcon(BuffType buffType)
    {
        var iconObj = Instantiate(BuffIconPrefab, IconsContainer);
        iconObj.name = $"BuffIcon_{buffType}";
        iconObj.SetActive(true);

        var iconItem = new BuffIconItem
        {
            GameObject = iconObj,
            BuffType = buffType
        };

        // 아이콘 이미지 색상 설정
        var iconImage = iconObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = GetBuffColor(buffType);
        }

        // 레벨 텍스트 찾기
        iconItem.LevelText = iconObj.transform.Find("Level")?.GetComponent<TextMeshProUGUI>();

        return iconItem;
    }

    /// <summary>
    /// 아이콘 재정렬 (오른쪽 상단 기준, 왼쪽으로 확장)
    /// </summary>
    private void ArrangeIcons()
    {
        int index = 0;
        foreach (var kvp in iconItems)
        {
            if (kvp.Value.GameObject != null)
            {
                var rect = kvp.Value.GameObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // 오른쪽에서 왼쪽으로 배치 (음수 방향)
                    rect.anchoredPosition = new Vector2(-index * (IconSize + IconSpacing), 0);
                }
                index++;
            }
        }
    }

    /// <summary>
    /// 버프 타입별 색상 반환
    /// </summary>
    private Color GetBuffColor(BuffType buffType)
    {
        return buffType switch
        {
            BuffType.Damage => DamageColor,
            BuffType.Speed => SpeedColor,
            BuffType.FireRate => FireRateColor,
            BuffType.MissileCount => MissileColor,
            BuffType.Magnet => MagnetColor,
            BuffType.HealthRegen => HealthRegenColor,
            BuffType.MaxHealth => MaxHealthColor,
            BuffType.Critical => CriticalColor,
            _ => Color.white
        };
    }

    /// <summary>
    /// 버프 아이콘 아이템 클래스
    /// </summary>
    private class BuffIconItem
    {
        public GameObject GameObject;
        public BuffType BuffType;
        public TextMeshProUGUI LevelText;

        public void SetLevel(int level)
        {
            if (LevelText != null)
            {
                LevelText.text = level.ToString();
            }
        }
    }
}
