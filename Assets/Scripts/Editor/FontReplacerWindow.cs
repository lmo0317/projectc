using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 씬의 모든 Text 컴포넌트 폰트를 일괄 변경하는 에디터 도구
/// Legacy Text (UnityEngine.UI.Text)와 TextMeshPro (TextMeshProUGUI) 모두 지원
/// </summary>
public class FontReplacerWindow : EditorWindow
{
    // Legacy Text용 폰트
    private Font _legacyFont;

    // TextMeshPro용 폰트
    private TMP_FontAsset _tmpFont;

    // 검색 결과
    private List<Text> _foundLegacyTexts = new List<Text>();
    private List<TextMeshProUGUI> _foundTmpTexts = new List<TextMeshProUGUI>();

    // 스크롤 위치
    private Vector2 _scrollPosition;

    // 옵션
    private bool _includeInactive = true;
    private bool _showPreview = true;

    [MenuItem("Tools/Font Replacer")]
    public static void ShowWindow()
    {
        var window = GetWindow<FontReplacerWindow>("Font Replacer");
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("폰트 일괄 변경 도구", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("씬의 모든 Text 컴포넌트 폰트를 지정한 폰트로 변경합니다.", MessageType.Info);
        EditorGUILayout.Space(10);

        // 옵션
        EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);
        _includeInactive = EditorGUILayout.Toggle("비활성화된 오브젝트 포함", _includeInactive);
        _showPreview = EditorGUILayout.Toggle("미리보기 표시", _showPreview);

        EditorGUILayout.Space(10);

        // Legacy Text 섹션
        EditorGUILayout.LabelField("Legacy Text (UI.Text)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        _legacyFont = (Font)EditorGUILayout.ObjectField("폰트", _legacyFont, typeof(Font), false);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // TextMeshPro 섹션
        EditorGUILayout.LabelField("TextMeshPro (TMP_Text)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        _tmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP 폰트 에셋", _tmpFont, typeof(TMP_FontAsset), false);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(15);

        // 버튼들
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("씬에서 Text 찾기", GUILayout.Height(30)))
        {
            FindAllTexts();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 검색 결과 표시
        if (_foundLegacyTexts.Count > 0 || _foundTmpTexts.Count > 0)
        {
            EditorGUILayout.LabelField($"검색 결과: Legacy Text {_foundLegacyTexts.Count}개, TMP {_foundTmpTexts.Count}개", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // 일괄 변경 버튼
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _legacyFont != null && _foundLegacyTexts.Count > 0;
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button($"Legacy Text 폰트 변경 ({_foundLegacyTexts.Count}개)", GUILayout.Height(25)))
            {
                ReplaceLegacyFonts();
            }

            GUI.enabled = _tmpFont != null && _foundTmpTexts.Count > 0;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"TMP 폰트 변경 ({_foundTmpTexts.Count}개)", GUILayout.Height(25)))
            {
                ReplaceTmpFonts();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 전체 변경 버튼
            GUI.enabled = (_legacyFont != null && _foundLegacyTexts.Count > 0) ||
                         (_tmpFont != null && _foundTmpTexts.Count > 0);
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("모든 폰트 일괄 변경", GUILayout.Height(35)))
            {
                ReplaceAllFonts();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            // 미리보기
            if (_showPreview)
            {
                EditorGUILayout.Space(10);
                DrawPreview();
            }
        }
    }

    private void FindAllTexts()
    {
        _foundLegacyTexts.Clear();
        _foundTmpTexts.Clear();

        // Legacy Text 찾기
        var legacyTexts = _includeInactive
            ? Resources.FindObjectsOfTypeAll<Text>()
            : FindObjectsOfType<Text>();

        foreach (var text in legacyTexts)
        {
            // 프리팹 에셋은 제외 (씬의 오브젝트만)
            if (!EditorUtility.IsPersistent(text) && text.gameObject.scene.isLoaded)
            {
                _foundLegacyTexts.Add(text);
            }
        }

        // TextMeshPro 찾기
        var tmpTexts = _includeInactive
            ? Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
            : FindObjectsOfType<TextMeshProUGUI>();

        foreach (var text in tmpTexts)
        {
            // 프리팹 에셋은 제외 (씬의 오브젝트만)
            if (!EditorUtility.IsPersistent(text) && text.gameObject.scene.isLoaded)
            {
                _foundTmpTexts.Add(text);
            }
        }

        Debug.Log($"[FontReplacer] 검색 완료: Legacy Text {_foundLegacyTexts.Count}개, TMP {_foundTmpTexts.Count}개");
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(250));

        // Legacy Text 목록
        if (_foundLegacyTexts.Count > 0)
        {
            EditorGUILayout.LabelField("Legacy Text:", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var text in _foundLegacyTexts)
            {
                if (text == null) continue;

                EditorGUILayout.BeginHorizontal();

                // 오브젝트 선택 버튼
                if (GUILayout.Button("→", GUILayout.Width(25)))
                {
                    Selection.activeGameObject = text.gameObject;
                    EditorGUIUtility.PingObject(text.gameObject);
                }

                string fontName = text.font != null ? text.font.name : "(None)";
                EditorGUILayout.LabelField($"{GetHierarchyPath(text.gameObject)}", GUILayout.MaxWidth(250));
                EditorGUILayout.LabelField($"[{fontName}]", EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // TMP 목록
        if (_foundTmpTexts.Count > 0)
        {
            EditorGUILayout.LabelField("TextMeshPro:", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var text in _foundTmpTexts)
            {
                if (text == null) continue;

                EditorGUILayout.BeginHorizontal();

                // 오브젝트 선택 버튼
                if (GUILayout.Button("→", GUILayout.Width(25)))
                {
                    Selection.activeGameObject = text.gameObject;
                    EditorGUIUtility.PingObject(text.gameObject);
                }

                string fontName = text.font != null ? text.font.name : "(None)";
                EditorGUILayout.LabelField($"{GetHierarchyPath(text.gameObject)}", GUILayout.MaxWidth(250));
                EditorGUILayout.LabelField($"[{fontName}]", EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();
    }

    private void ReplaceLegacyFonts()
    {
        if (_legacyFont == null)
        {
            Debug.LogWarning("[FontReplacer] Legacy Font가 지정되지 않았습니다.");
            return;
        }

        int count = 0;
        Undo.RecordObjects(_foundLegacyTexts.ToArray(), "Replace Legacy Fonts");

        foreach (var text in _foundLegacyTexts)
        {
            if (text == null) continue;

            text.font = _legacyFont;
            EditorUtility.SetDirty(text);
            count++;
        }

        Debug.Log($"[FontReplacer] Legacy Text {count}개의 폰트를 '{_legacyFont.name}'으로 변경했습니다.");
    }

    private void ReplaceTmpFonts()
    {
        if (_tmpFont == null)
        {
            Debug.LogWarning("[FontReplacer] TMP Font Asset이 지정되지 않았습니다.");
            return;
        }

        int count = 0;
        Undo.RecordObjects(_foundTmpTexts.ToArray(), "Replace TMP Fonts");

        foreach (var text in _foundTmpTexts)
        {
            if (text == null) continue;

            text.font = _tmpFont;
            EditorUtility.SetDirty(text);
            count++;
        }

        Debug.Log($"[FontReplacer] TextMeshPro {count}개의 폰트를 '{_tmpFont.name}'으로 변경했습니다.");
    }

    private void ReplaceAllFonts()
    {
        if (_legacyFont != null && _foundLegacyTexts.Count > 0)
        {
            ReplaceLegacyFonts();
        }

        if (_tmpFont != null && _foundTmpTexts.Count > 0)
        {
            ReplaceTmpFonts();
        }

        Debug.Log("[FontReplacer] 모든 폰트 변경 완료!");
    }

    private string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        int depth = 0;
        while (parent != null && depth < 2)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }

        if (parent != null)
        {
            path = ".../" + path;
        }

        return path;
    }
}
