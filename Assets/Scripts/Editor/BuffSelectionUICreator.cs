using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 버프 선택 UI를 자동으로 생성하는 에디터 도구
/// </summary>
public class BuffSelectionUICreator : EditorWindow
{
    [MenuItem("Tools/Create Buff Selection UI")]
    public static void CreateBuffSelectionUI()
    {
        // Canvas 찾기 또는 생성
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 기존 BuffSelectionUI가 있는지 확인
        var existingUI = FindObjectOfType<BuffSelectionUI>();
        if (existingUI != null)
        {
            Debug.LogWarning("BuffSelectionUI가 이미 존재합니다!");
            Selection.activeGameObject = existingUI.gameObject;
            return;
        }

        // 메인 패널 생성
        var panelObj = CreatePanel(canvas.transform, "BuffSelectionPanel");
        var buffSelectionUI = panelObj.AddComponent<BuffSelectionUI>();

        // 배경 반투명 오버레이
        var bgImage = panelObj.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        // 컨텐츠 컨테이너
        var contentObj = CreatePanel(panelObj.transform, "Content");
        var contentImage = contentObj.GetComponent<Image>();
        contentImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        var contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.2f);
        contentRect.anchorMax = new Vector2(0.9f, 0.8f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // 타이틀 텍스트
        var titleObj = CreateText(contentObj.transform, "TitleText", "레벨 업! 버프를 선택하세요");
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 48;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.yellow;
        buffSelectionUI.TitleText = titleTmp;

        // 카드 컨테이너
        var cardsContainer = new GameObject("CardsContainer");
        cardsContainer.transform.SetParent(contentObj.transform, false);
        var cardsRect = cardsContainer.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.05f, 0.1f);
        cardsRect.anchorMax = new Vector2(0.95f, 0.8f);
        cardsRect.offsetMin = Vector2.zero;
        cardsRect.offsetMax = Vector2.zero;

        var horizontalLayout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 30;
        horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandWidth = true;
        horizontalLayout.childForceExpandHeight = true;

        // 3개의 카드 생성
        buffSelectionUI.OptionCards = new BuffOptionCard[3];
        for (int i = 0; i < 3; i++)
        {
            var card = CreateOptionCard(cardsContainer.transform, $"OptionCard_{i + 1}");
            buffSelectionUI.OptionCards[i] = card;
        }

        // BuffSelectionUI 설정
        buffSelectionUI.Panel = panelObj;
        buffSelectionUI.PauseGameOnShow = true;

        // 초기 상태: 숨김
        panelObj.SetActive(false);

        // 선택
        Selection.activeGameObject = panelObj;

        Debug.Log("BuffSelectionUI 생성 완료! Panel을 Canvas 하위에 배치했습니다.");
        Debug.Log("UIManager의 BuffSelectionUI 참조를 설정하거나, BuffSelectionUI.Instance를 사용하세요.");
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        obj.AddComponent<Image>();

        return obj;
    }

    private static GameObject CreateText(Transform parent, string name, string text)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;

        return obj;
    }

    private static BuffOptionCard CreateOptionCard(Transform parent, string name)
    {
        // 카드 컨테이너
        var cardObj = new GameObject(name);
        cardObj.transform.SetParent(parent, false);

        var cardRect = cardObj.AddComponent<RectTransform>();
        var cardImage = cardObj.AddComponent<Image>();
        cardImage.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        var verticalLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(15, 15, 15, 15);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        // 아이콘
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cardObj.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(80, 80);
        var iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;
        var iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredHeight = 80;
        iconLayout.preferredWidth = 80;

        // 이름 텍스트
        var nameObj = CreateText(cardObj.transform, "NameText", "버프 이름");
        var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 28;
        nameTmp.fontStyle = FontStyles.Bold;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 35;

        // 레벨 텍스트
        var levelObj = CreateText(cardObj.transform, "LevelText", "Lv.1 → Lv.2");
        var levelTmp = levelObj.GetComponent<TextMeshProUGUI>();
        levelTmp.fontSize = 22;
        levelTmp.color = Color.green;
        var levelLayout = levelObj.AddComponent<LayoutElement>();
        levelLayout.preferredHeight = 28;

        // 효과 텍스트
        var effectObj = CreateText(cardObj.transform, "EffectText", "+10%");
        var effectTmp = effectObj.GetComponent<TextMeshProUGUI>();
        effectTmp.fontSize = 24;
        effectTmp.color = new Color(0.8f, 0.8f, 0.8f);
        var effectLayout = effectObj.AddComponent<LayoutElement>();
        effectLayout.preferredHeight = 30;

        // 선택 버튼
        var buttonObj = new GameObject("SelectButton");
        buttonObj.transform.SetParent(cardObj.transform, false);
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);
        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var buttonColors = button.colors;
        buttonColors.highlightedColor = new Color(0.3f, 0.8f, 0.4f, 1f);
        buttonColors.pressedColor = new Color(0.1f, 0.4f, 0.2f, 1f);
        button.colors = buttonColors;

        var buttonLayout = buttonObj.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 50;
        buttonLayout.flexibleWidth = 1;

        // 버튼 텍스트
        var buttonTextObj = CreateText(buttonObj.transform, "Text", "선택");
        var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        var buttonTmp = buttonTextObj.GetComponent<TextMeshProUGUI>();
        buttonTmp.fontSize = 26;
        buttonTmp.fontStyle = FontStyles.Bold;

        // BuffOptionCard 컴포넌트 추가
        var optionCard = cardObj.AddComponent<BuffOptionCard>();
        optionCard.IconImage = iconImage;
        optionCard.NameText = nameTmp;
        optionCard.LevelText = levelTmp;
        optionCard.EffectText = effectTmp;
        optionCard.SelectButton = button;

        return optionCard;
    }
}
