using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 피격 이펙트 파티클 풀링 관리
/// </summary>
public class HitEffectPool : MonoBehaviour
{
    public static HitEffectPool Instance { get; private set; }

    [Header("Settings")]
    public GameObject HitEffectPrefab;  // 파티클 시스템 프리팹
    public int PoolSize = 30;           // 풀 크기

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        if (HitEffectPrefab == null)
        {
            Debug.LogWarning("[HitEffectPool] HitEffectPrefab is not assigned!");
            return;
        }

        for (int i = 0; i < PoolSize; i++)
        {
            var obj = Instantiate(HitEffectPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 피격 이펙트 재생
    /// </summary>
    public void PlayEffect(Vector3 position)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("[HitEffectPool] Pool exhausted!");
            return;
        }

        var obj = pool.Dequeue();
        obj.transform.position = position;
        obj.SetActive(true);

        // 파티클 시스템 재생
        var ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Play();

            // 파티클 완료 후 풀로 반환
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(obj, duration));
        }
        else
        {
            // 파티클이 없으면 1초 후 반환
            StartCoroutine(ReturnToPool(obj, 1f));
        }
    }

    private System.Collections.IEnumerator ReturnToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 오브젝트가 이미 파괴되었으면 새로 생성해서 풀에 추가
        if (obj == null)
        {
            if (HitEffectPrefab != null)
            {
                var newObj = Instantiate(HitEffectPrefab, transform);
                newObj.SetActive(false);
                pool.Enqueue(newObj);
            }
            yield break;
        }

        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
