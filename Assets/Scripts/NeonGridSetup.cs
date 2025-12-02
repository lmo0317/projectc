using UnityEngine;

/// <summary>
/// 네온 그리드 바닥 설정 헬퍼 스크립트
/// Editor에서 실행하여 자동으로 네온 그리드 바닥을 생성합니다.
/// </summary>
public class NeonGridSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("GameObject/3D Object/Neon Grid Floor")]
    public static void CreateNeonGridFloor()
    {
        // 1. Plane 생성
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "NeonGridFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10); // 100x100 유닛

        // 2. Material 생성
        Shader neonShader = Shader.Find("Custom/NeonGrid");
        if (neonShader == null)
        {
            Debug.LogError("NeonGrid shader not found! Make sure NeonGrid.shader is in Assets/Shaders/");
            return;
        }

        Material neonMaterial = new Material(neonShader);
        neonMaterial.name = "NeonGridMaterial";

        // 3. Material 속성 설정
        neonMaterial.SetColor("_GridColor", new Color(0f, 1f, 1f, 1f)); // 사이안
        neonMaterial.SetColor("_BackgroundColor", new Color(0f, 0f, 0.1f, 1f)); // 어두운 파랑
        neonMaterial.SetFloat("_GridSize", 1.0f); // 1 유닛 간격
        neonMaterial.SetFloat("_LineWidth", 0.05f); // 라인 두께
        neonMaterial.SetFloat("_GlowIntensity", 2.0f); // 글로우 강도
        neonMaterial.SetFloat("_FadeDistance", 50.0f); // 50 유닛 거리에서 페이드
        neonMaterial.SetFloat("_EmissionStrength", 3.0f); // Emission 강도
        neonMaterial.SetFloat("_Transparency", 0.3f); // 투명도 (0.3 = 희미함)

        // 4. Material을 Assets에 저장
        string materialPath = "Assets/Materials/NeonGridMaterial.mat";
        UnityEditor.AssetDatabase.CreateAsset(neonMaterial, materialPath);
        UnityEditor.AssetDatabase.SaveAssets();

        // 5. Plane에 Material 적용
        MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
        Material loadedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        renderer.material = loadedMaterial;

        // 6. Selection에 추가
        UnityEditor.Selection.activeGameObject = floor;

        Debug.Log("Neon Grid Floor created successfully!");
        Debug.Log("Material saved to: " + materialPath);
    }

    [UnityEditor.MenuItem("Tools/Setup Neon Grid Material")]
    public static void SetupNeonGridMaterial()
    {
        // Material만 생성
        Shader neonShader = Shader.Find("Custom/NeonGrid");
        if (neonShader == null)
        {
            Debug.LogError("NeonGrid shader not found!");
            return;
        }

        Material neonMaterial = new Material(neonShader);
        neonMaterial.name = "NeonGridMaterial";

        // 기본 설정
        neonMaterial.SetColor("_GridColor", new Color(0f, 1f, 1f, 1f));
        neonMaterial.SetColor("_BackgroundColor", new Color(0f, 0f, 0.1f, 1f));
        neonMaterial.SetFloat("_GridSize", 1.0f);
        neonMaterial.SetFloat("_LineWidth", 0.05f);
        neonMaterial.SetFloat("_GlowIntensity", 2.0f);
        neonMaterial.SetFloat("_FadeDistance", 50.0f);
        neonMaterial.SetFloat("_EmissionStrength", 3.0f);
        neonMaterial.SetFloat("_Transparency", 0.3f);

        string materialPath = "Assets/Materials/NeonGridMaterial.mat";
        UnityEditor.AssetDatabase.CreateAsset(neonMaterial, materialPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log("Neon Grid Material created at: " + materialPath);
    }
#endif
}
