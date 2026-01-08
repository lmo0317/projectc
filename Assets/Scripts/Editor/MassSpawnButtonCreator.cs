using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 대량 몬스터 소환 버튼 UI를 자동으로 생성하는 에디터 도구
/// </summary>
public class MassSpawnButtonCreator : EditorWindow
{
    [MenuItem("Tools/Create Mass Spawn Button")]
    public static void CreateMassSpawnButton()
    {
        // Canvas 찾기 또는 생성
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("Canvas가 없어서 새로 생성했습니다.");
        }

        // 기존 MassSpawnButton이 있는지 확인
        var existingButton = Object.FindObjectOfType<MassSpawnButton>();
        if (existingButton != null)
        {
            Debug.LogWarning("MassSpawnButton이 이미 존재합니다!");
            Selection.activeGameObject = existingButton.gameObject;
            return;
        }

        // 패널 오브젝트 생성 (InputField + Button을 담을 컨테이너)
        var panelObj = new GameObject("MassSpawnPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        // 패널 RectTransform 설정 (오른쪽 하단)
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0); // 오른쪽 하단
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = new Vector2(-20, 20); // 오른쪽 하단에서 20픽셀 안쪽
        panelRect.sizeDelta = new Vector2(240, 100); // 패널 크기

        // 패널 배경 (약간 투명한 검정)
        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // === InputField 생성 ===
        var inputFieldObj = new GameObject("InputField");
        inputFieldObj.transform.SetParent(panelObj.transform, false);

        var inputRect = inputFieldObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 1); // 왼쪽 상단 기준
        inputRect.anchorMax = new Vector2(1, 1);
        inputRect.pivot = new Vector2(0.5f, 1);
        inputRect.anchoredPosition = new Vector2(0, -10); // 상단에서 10픽셀 아래
        inputRect.sizeDelta = new Vector2(-20, 35); // 양쪽 10픽셀 여백, 높이 35

        var inputImage = inputFieldObj.AddComponent<Image>();
        inputImage.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 어두운 배경

        var inputField = inputFieldObj.AddComponent<TMP_InputField>();
        inputField.textComponent = CreateInputTextComponent(inputFieldObj, "Text");
        inputField.placeholder = CreateInputPlaceholder(inputFieldObj, "Placeholder");

        // === 버튼 생성 ===
        var buttonObj = new GameObject("MassSpawnButton");
        buttonObj.transform.SetParent(panelObj.transform, false);

        // RectTransform 설정 (InputField 아래)
        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0); // 왼쪽 하단 기준
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 10); // 하단에서 10픽셀 위
        rect.sizeDelta = new Vector2(-20, 40); // 양쪽 10픽셀 여백, 높이 40

        // Image 컴포넌트 (버튼 배경)
        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 0.2f, 0.9f); // 초록색 배경

        // Button 컴포넌트
        var button = buttonObj.AddComponent<Button>();

        // 버튼 색상 설정
        var colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f, 1f);
        colors.pressedColor = new Color(0.15f, 0.5f, 0.15f, 1f);
        colors.selectedColor = new Color(0.25f, 0.65f, 0.25f, 1f);
        button.colors = colors;

        // MassSpawnButton 스크립트 추가
        var massSpawnScript = buttonObj.AddComponent<MassSpawnButton>();

        // 버튼 텍스트 오브젝트 생성 (TextMeshPro)
        var buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        var buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        var buttonTextMesh = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonTextMesh.text = "소환";
        buttonTextMesh.fontSize = 18;
        buttonTextMesh.color = Color.white;
        buttonTextMesh.alignment = TextAlignmentOptions.Center;
        buttonTextMesh.fontStyle = FontStyles.Bold;

        // MassSpawnButton 스크립트에 InputField 연결
        var serializedObject = new SerializedObject(massSpawnScript);
        serializedObject.FindProperty("inputField").objectReferenceValue = inputField;
        serializedObject.ApplyModifiedProperties();

        // 선택
        Selection.activeGameObject = panelObj;

        // Undo 지원
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Mass Spawn UI");

        Debug.Log("MassSpawnButton UI 생성 완료! 오른쪽 하단에 InputField와 버튼이 배치되었습니다.");
    }

    /// <summary>
    /// InputField의 Text 컴포넌트 생성
    /// </summary>
    private static TextMeshProUGUI CreateInputTextComponent(GameObject parent, string name)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        var textMesh = textObj.AddComponent<TextMeshProUGUI>();
        textMesh.text = "";
        textMesh.fontSize = 16;
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignmentOptions.Center;

        return textMesh;
    }

    /// <summary>
    /// InputField의 Placeholder 컴포넌트 생성
    /// </summary>
    private static Graphic CreateInputPlaceholder(GameObject parent, string name)
    {
        var placeholderObj = new GameObject(name);
        placeholderObj.transform.SetParent(parent.transform, false);

        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 5);
        placeholderRect.offsetMax = new Vector2(-10, -5);

        var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "몬스터 수 입력...";
        placeholderText.fontSize = 16;
        placeholderText.color = new Color(1f, 1f, 1f, 0.5f); // 반투명 흰색
        placeholderText.alignment = TextAlignmentOptions.Center;
        placeholderText.fontStyle = FontStyles.Italic;

        return placeholderText;
    }
}
