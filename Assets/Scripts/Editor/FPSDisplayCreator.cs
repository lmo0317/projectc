using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// FPS 표시 UI를 자동으로 생성하는 에디터 도구
/// </summary>
public class FPSDisplayCreator : EditorWindow
{
    [MenuItem("Tools/Create FPS Display")]
    public static void CreateFPSDisplay()
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
        }

        // 기존 FPSDisplay가 있는지 확인
        var existingFPS = Object.FindObjectOfType<FPSDisplay>();
        if (existingFPS != null)
        {
            Debug.LogWarning("FPSDisplay가 이미 존재합니다!");
            Selection.activeGameObject = existingFPS.gameObject;
            return;
        }

        // FPSDisplay 오브젝트 생성
        var fpsObj = new GameObject("FPSDisplay");
        fpsObj.transform.SetParent(canvas.transform, false);

        var rect = fpsObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fpsObj.AddComponent<FPSDisplay>();

        // 선택
        Selection.activeGameObject = fpsObj;

        Debug.Log("FPSDisplay 생성 완료! 왼쪽 하단에 FPS가 표시됩니다.");
    }
}
