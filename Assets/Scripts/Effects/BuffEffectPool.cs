using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 버프 획득 이펙트 파티클 풀링 관리
/// </summary>
public class BuffEffectPool : MonoBehaviour
{
    public static BuffEffectPool Instance { get; private set; }

    [Header("Settings")]
    public GameObject BuffEffectPrefab;  // 파티클 시스템 프리팹
    public int PoolSize = 10;            // 풀 크기

    [Header("Fallback Effect (프리팹 없을 때)")]
    public bool CreateFallbackEffect = true;

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializePool()
    {
        if (BuffEffectPrefab == null)
        {
            if (CreateFallbackEffect)
            {
                Debug.Log("[BuffEffectPool] 프리팹 없음 - 기본 이펙트 생성");
                BuffEffectPrefab = CreateDefaultBuffEffect();
            }
            else
            {
                Debug.LogWarning("[BuffEffectPool] BuffEffectPrefab is not assigned!");
                return;
            }
        }

        for (int i = 0; i < PoolSize; i++)
        {
            var obj = Instantiate(BuffEffectPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 기본 버프 이펙트 파티클 시스템 생성
    /// </summary>
    private GameObject CreateDefaultBuffEffect()
    {
        var effectObj = new GameObject("BuffEffect");
        effectObj.transform.SetParent(transform);

        var ps = effectObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.8f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.9f, 0.3f, 1f); // 금색
        main.maxParticles = 30;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
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
        renderer.material.color = new Color(1f, 0.9f, 0.3f, 1f);

        effectObj.SetActive(false);
        return effectObj;
    }

    /// <summary>
    /// 버프 획득 이펙트 재생
    /// </summary>
    public void PlayEffect(Vector3 position, BuffType buffType)
    {
        GameObject obj = null;

        // 유효한 오브젝트를 찾을 때까지 풀에서 꺼냄
        while (pool.Count > 0)
        {
            obj = pool.Dequeue();
            if (obj != null) break;
        }

        // 유효한 오브젝트가 없으면 새로 생성
        if (obj == null)
        {
            if (BuffEffectPrefab == null) return;
            obj = Instantiate(BuffEffectPrefab, transform);
        }

        obj.transform.position = position;
        obj.SetActive(true);

        // 버프 타입에 따라 색상 설정
        var ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = GetBuffColor(buffType);

            ps.Clear();
            ps.Play();

            // 파티클 완료 후 풀로 반환
            float duration = main.duration + main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(obj, duration));
        }
        else
        {
            StartCoroutine(ReturnToPool(obj, 1f));
        }

        Debug.Log($"[BuffEffectPool] 버프 이펙트 재생: {buffType} at {position}");
    }

    /// <summary>
    /// 버프 타입별 색상 반환
    /// </summary>
    private Color GetBuffColor(BuffType buffType)
    {
        return buffType switch
        {
            BuffType.Damage => new Color(1f, 0.3f, 0.3f),      // 빨강
            BuffType.Speed => new Color(0.3f, 0.8f, 1f),       // 하늘색
            BuffType.FireRate => new Color(1f, 0.8f, 0.3f),    // 주황
            BuffType.MissileCount => new Color(0.8f, 0.3f, 1f), // 보라
            BuffType.Magnet => new Color(0.3f, 1f, 0.5f),      // 초록
            BuffType.HealthRegen => new Color(1f, 0.5f, 0.5f), // 분홍
            BuffType.MaxHealth => new Color(1f, 0.3f, 0.6f),   // 핫핑크
            BuffType.Critical => new Color(1f, 1f, 0.3f),      // 노랑
            _ => new Color(1f, 0.9f, 0.3f)                      // 기본 금색
        };
    }

    private IEnumerator ReturnToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj == null)
        {
            if (BuffEffectPrefab != null)
            {
                var newObj = Instantiate(BuffEffectPrefab, transform);
                newObj.SetActive(false);
                pool.Enqueue(newObj);
            }
            yield break;
        }

        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
