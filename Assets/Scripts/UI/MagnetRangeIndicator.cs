using UnityEngine;

/// <summary>
/// 자석 범위를 시각적으로 표시하는 원형 인디케이터
/// </summary>
public class MagnetRangeIndicator : MonoBehaviour
{
    public static MagnetRangeIndicator Instance { get; private set; }

    [Header("Settings")]
    public Color RangeColor = new Color(0.3f, 1f, 0.5f, 0.25f);  // 초록색, 알파 0.25
    public int Segments = 64;  // 원의 세그먼트 수

    private GameObject rangeObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private float currentRange = 0f;

    private void Awake()
    {
        Instance = this;
        CreateRangeIndicator();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CreateRangeIndicator()
    {
        rangeObject = new GameObject("MagnetRangeCircle");
        rangeObject.transform.SetParent(transform);
        rangeObject.transform.localPosition = Vector3.zero;
        // 바닥에 평평하게 표시 (Y축 회전)
        rangeObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        meshFilter = rangeObject.AddComponent<MeshFilter>();
        meshRenderer = rangeObject.AddComponent<MeshRenderer>();

        // 반투명 머티리얼 생성
        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = RangeColor;
        meshRenderer.material = material;

        // 초기에는 숨김
        rangeObject.SetActive(false);
    }

    /// <summary>
    /// 자석 범위 업데이트
    /// </summary>
    public void UpdateRange(float range, Vector3 position)
    {
        if (range <= 0f)
        {
            // 범위가 0이면 숨김
            if (rangeObject != null && rangeObject.activeSelf)
            {
                rangeObject.SetActive(false);
            }
            currentRange = 0f;
            return;
        }

        // 위치 업데이트 (플레이어 위치)
        transform.position = position;

        // 범위가 변경되었으면 메시 재생성
        if (!Mathf.Approximately(range, currentRange))
        {
            currentRange = range;
            CreateCircleMesh(range);
        }

        // 표시
        if (rangeObject != null && !rangeObject.activeSelf)
        {
            rangeObject.SetActive(true);
        }
    }

    /// <summary>
    /// 원형 메시 생성
    /// </summary>
    private void CreateCircleMesh(float radius)
    {
        if (meshFilter == null) return;

        var mesh = new Mesh();
        mesh.name = "MagnetRangeMesh";

        // 버텍스 생성 (중심 + 외곽)
        var vertices = new Vector3[Segments + 1];
        var triangles = new int[Segments * 3];
        var colors = new Color[Segments + 1];

        // 중심점
        vertices[0] = Vector3.zero;
        colors[0] = new Color(RangeColor.r, RangeColor.g, RangeColor.b, RangeColor.a * 0.6f);  // 중심은 조금 더 진하게

        // 외곽 버텍스
        float angleStep = 360f / Segments;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            colors[i + 1] = new Color(RangeColor.r, RangeColor.g, RangeColor.b, RangeColor.a * 0.4f);  // 외곽은 더 투명하게
        }

        // 삼각형 생성
        for (int i = 0; i < Segments; i++)
        {
            int nextIndex = (i + 1) % Segments + 1;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = nextIndex;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }

    /// <summary>
    /// 색상 변경
    /// </summary>
    public void SetColor(Color color)
    {
        RangeColor = color;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
        // 메시 색상도 업데이트
        if (currentRange > 0f)
        {
            CreateCircleMesh(currentRange);
        }
    }
}
