using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Phase 4 관련 컴포넌트를 쉽게 설정하는 에디터 도구
/// </summary>
public class Phase4SetupTool : EditorWindow
{
    [MenuItem("Tools/Phase 4 Setup/Create All Phase 4 Components")]
    public static void CreateAllComponents()
    {
        CreateBuffEffectPool();
        CreateBuffIconsUI();
        CreateGameSoundManager();
        Debug.Log("[Phase4SetupTool] 모든 Phase 4 컴포넌트가 생성되었습니다!");
    }

    [MenuItem("Tools/Phase 4 Setup/Create Buff Effect Pool")]
    public static void CreateBuffEffectPool()
    {
        // 기존 확인
        var existing = Object.FindObjectOfType<BuffEffectPool>();
        if (existing != null)
        {
            Debug.LogWarning("[Phase4SetupTool] BuffEffectPool이 이미 존재합니다!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var obj = new GameObject("BuffEffectPool");
        obj.AddComponent<BuffEffectPool>();

        Debug.Log("[Phase4SetupTool] BuffEffectPool 생성 완료!");
        Selection.activeGameObject = obj;
    }

    [MenuItem("Tools/Phase 4 Setup/Create Buff Icons UI")]
    public static void CreateBuffIconsUI()
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

        // 기존 확인
        var existing = Object.FindObjectOfType<BuffIconsUI>();
        if (existing != null)
        {
            Debug.LogWarning("[Phase4SetupTool] BuffIconsUI가 이미 존재합니다!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // BuffIconsUI 컨테이너 생성 (오른쪽 상단)
        var containerObj = new GameObject("BuffIconsUI");
        containerObj.transform.SetParent(canvas.transform, false);

        var rect = containerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);  // 우측 상단
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);      // 우측 상단 기준
        rect.anchoredPosition = new Vector2(-10, -10); // 약간 안쪽으로
        rect.sizeDelta = new Vector2(400, 50);

        var buffIconsUI = containerObj.AddComponent<BuffIconsUI>();
        buffIconsUI.IconsContainer = containerObj.transform;

        Debug.Log("[Phase4SetupTool] BuffIconsUI 생성 완료!");
        Selection.activeGameObject = containerObj;
    }

    [MenuItem("Tools/Phase 4 Setup/Create Game Sound Manager")]
    public static void CreateGameSoundManager()
    {
        // 기존 확인
        var existing = Object.FindObjectOfType<GameSoundManager>();
        if (existing != null)
        {
            Debug.LogWarning("[Phase4SetupTool] GameSoundManager가 이미 존재합니다!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var obj = new GameObject("GameSoundManager");
        var soundManager = obj.AddComponent<GameSoundManager>();

        // AudioSource 추가
        var audioSource = obj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        soundManager.SfxSource = audioSource;

        Debug.Log("[Phase4SetupTool] GameSoundManager 생성 완료!");
        Selection.activeGameObject = obj;
    }

    [MenuItem("Tools/Phase 4 Setup/Create Star Collect Effect Pool")]
    public static void CreateStarCollectEffectPool()
    {
        // 기존 확인
        var existing = Object.FindObjectOfType<StarCollectEffectPool>();
        if (existing != null)
        {
            Debug.LogWarning("[Phase4SetupTool] StarCollectEffectPool이 이미 존재합니다!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var obj = new GameObject("StarCollectEffectPool");
        var pool = obj.AddComponent<StarCollectEffectPool>();

        // 기본 파티클 시스템 프리팹 생성
        var effectPrefab = CreateStarCollectEffectPrefab();
        effectPrefab.transform.SetParent(obj.transform);
        effectPrefab.SetActive(false);
        pool.CollectEffectPrefab = effectPrefab;

        Debug.Log("[Phase4SetupTool] StarCollectEffectPool 생성 완료!");
        Selection.activeGameObject = obj;
    }

    /// <summary>
    /// Star 수집 이펙트 파티클 시스템 프리팹 생성
    /// </summary>
    private static GameObject CreateStarCollectEffectPrefab()
    {
        var effectObj = new GameObject("StarCollectEffect");

        var ps = effectObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.9f, 0.2f, 1f); // 노랑
        main.maxParticles = 20;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 15)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 1f, 0.5f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // 렌더러 설정
        var renderer = effectObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = new Color(1f, 0.9f, 0.2f, 1f);

        return effectObj;
    }
}
