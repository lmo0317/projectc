using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 로비 씬을 자동으로 생성하는 에디터 도구
/// Dedicated 서버 / 클라이언트 분리 모드 UI
/// </summary>
public class LobbySceneCreator : EditorWindow
{
    // 폰트 경로
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Maplestory Bold SDF.asset";

    [MenuItem("Tools/Create Lobby Scene")]
    public static void CreateLobbyScene()
    {
        // 폰트 로드
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogWarning($"폰트를 찾을 수 없습니다: {FontAssetPath}. 기본 폰트를 사용합니다.");
        }

        // 새 씬 생성
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Canvas 생성
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 배경 패널
        var bgPanel = CreatePanel(canvasObj.transform, "Background");
        var bgImage = bgPanel.GetComponent<Image>();
        bgImage.color = new Color(0.1f, 0.05f, 0.15f, 1f);  // 어두운 보라색 배경

        // 타이틀 텍스트
        var titleObj = CreateText(canvasObj.transform, "TitleText", "Project C", fontAsset);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.75f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(600, 100);

        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 72;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.8f, 0.3f, 1f);  // 보라색

        // 서버 시작 버튼 (Dedicated 서버)
        var serverButtonObj = CreateButton(canvasObj.transform, "StartServerButton", "서버 시작", fontAsset);
        var serverButtonRect = serverButtonObj.GetComponent<RectTransform>();
        serverButtonRect.anchorMin = new Vector2(0.5f, 0.55f);
        serverButtonRect.anchorMax = new Vector2(0.5f, 0.55f);
        serverButtonRect.anchoredPosition = Vector2.zero;
        serverButtonRect.sizeDelta = new Vector2(300, 70);

        var serverButtonImage = serverButtonObj.GetComponent<Image>();
        serverButtonImage.color = new Color(0.3f, 0.5f, 0.7f, 1f);  // 파란색

        var serverButtonTmp = serverButtonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (serverButtonTmp != null)
        {
            serverButtonTmp.fontSize = 32;
            serverButtonTmp.fontStyle = FontStyles.Bold;
        }

        // 서버 주소 입력 필드
        var addressInputObj = CreateInputField(canvasObj.transform, "AddressInput", "127.0.0.1", fontAsset);
        var addressInputRect = addressInputObj.GetComponent<RectTransform>();
        addressInputRect.anchorMin = new Vector2(0.5f, 0.40f);
        addressInputRect.anchorMax = new Vector2(0.5f, 0.40f);
        addressInputRect.anchoredPosition = Vector2.zero;
        addressInputRect.sizeDelta = new Vector2(300, 50);

        // 서버 접속 버튼
        var connectButtonObj = CreateButton(canvasObj.transform, "ConnectButton", "서버 접속", fontAsset);
        var connectButtonRect = connectButtonObj.GetComponent<RectTransform>();
        connectButtonRect.anchorMin = new Vector2(0.5f, 0.28f);
        connectButtonRect.anchorMax = new Vector2(0.5f, 0.28f);
        connectButtonRect.anchoredPosition = Vector2.zero;
        connectButtonRect.sizeDelta = new Vector2(300, 70);

        var connectButtonImage = connectButtonObj.GetComponent<Image>();
        connectButtonImage.color = new Color(0.3f, 0.6f, 0.3f, 1f);  // 초록색

        var connectButtonTmp = connectButtonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (connectButtonTmp != null)
        {
            connectButtonTmp.fontSize = 32;
            connectButtonTmp.fontStyle = FontStyles.Bold;
        }

        // 상태 텍스트
        var statusObj = CreateText(canvasObj.transform, "StatusText", "서버를 시작하거나 서버에 접속하세요", fontAsset);
        var statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.12f);
        statusRect.anchorMax = new Vector2(0.5f, 0.12f);
        statusRect.anchoredPosition = Vector2.zero;
        statusRect.sizeDelta = new Vector2(600, 50);

        var statusTmp = statusObj.GetComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 24;
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.color = new Color(0.7f, 0.7f, 0.7f);

        // NetworkConnectionManager 오브젝트 생성
        var networkManagerObj = new GameObject("NetworkConnectionManager");
        networkManagerObj.AddComponent<NetworkConnectionManager>();

        // LobbyUI 컴포넌트 추가
        var lobbyUI = canvasObj.AddComponent<LobbyUI>();

        // SerializedObject를 통해 private 필드 설정
        var serializedLobbyUI = new SerializedObject(lobbyUI);
        serializedLobbyUI.FindProperty("startServerButton").objectReferenceValue = serverButtonObj.GetComponent<Button>();
        serializedLobbyUI.FindProperty("connectButton").objectReferenceValue = connectButtonObj.GetComponent<Button>();
        serializedLobbyUI.FindProperty("addressInput").objectReferenceValue = addressInputObj.GetComponent<TMP_InputField>();
        serializedLobbyUI.FindProperty("titleText").objectReferenceValue = titleTmp;
        serializedLobbyUI.FindProperty("statusText").objectReferenceValue = statusTmp;
        serializedLobbyUI.ApplyModifiedProperties();

        // EventSystem 확인/생성
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 씬 저장
        string scenePath = "Assets/Scenes/LobbyScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log($"로비 씬이 생성되었습니다: {scenePath}");
        Debug.Log("Build Settings에서 LobbyScene을 첫 번째 씬으로 설정하세요.");
        Debug.Log("사용법:");
        Debug.Log("  - '서버 시작' = Dedicated 서버 실행 (클라이언트 접속 대기)");
        Debug.Log("  - '서버 접속' = 클라이언트로 서버에 연결");

        // 선택
        Selection.activeGameObject = canvasObj;
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

    private static GameObject CreateText(Transform parent, string name, string text, TMP_FontAsset fontAsset = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        obj.AddComponent<RectTransform>();

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;

        // 폰트 설정
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }

        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string buttonText, TMP_FontAsset fontAsset = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();

        var image = obj.AddComponent<Image>();
        image.color = Color.white;

        var button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        button.colors = colors;

        // 버튼 텍스트
        var textObj = CreateText(obj.transform, "Text", buttonText, fontAsset);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return obj;
    }

    private static GameObject CreateInputField(Transform parent, string name, string placeholder, TMP_FontAsset fontAsset = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();

        var image = obj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var inputField = obj.AddComponent<TMP_InputField>();

        // Text Area
        var textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(obj.transform, false);
        var textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder
        var placeholderObj = CreateText(textAreaObj.transform, "Placeholder", placeholder, fontAsset);
        var placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        var placeholderTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
        placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f);
        placeholderTmp.fontSize = 28;
        placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Text
        var textObj = CreateText(textAreaObj.transform, "Text", "", fontAsset);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var textTmp = textObj.GetComponent<TextMeshProUGUI>();
        textTmp.color = Color.white;
        textTmp.fontSize = 28;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // InputField 설정
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = placeholderTmp;
        inputField.text = placeholder;

        return obj;
    }
}
