using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 숫자 팝업 UI 관리 (오브젝트 풀링)
/// </summary>
public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Settings")]
    public GameObject DamageTextPrefab;  // TextMeshPro 프리팹
    public int PoolSize = 20;            // 풀 크기
    public float FloatSpeed = 1.5f;      // 위로 떠오르는 속도
    public float FadeTime = 0.8f;        // 페이드아웃 시간
    public float ScaleStart = 0.5f;      // 시작 크기
    public float ScaleEnd = 1.2f;        // 최대 크기

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Camera mainCamera;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        InitializePool();
    }

    private void InitializePool()
    {
        if (DamageTextPrefab == null)
        {
            Debug.LogWarning("[DamagePopupManager] DamageTextPrefab is not assigned!");
            return;
        }

        for (int i = 0; i < PoolSize; i++)
        {
            var obj = Instantiate(DamageTextPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 데미지 숫자 표시
    /// </summary>
    public void ShowDamage(Vector3 worldPosition, float damage)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("[DamagePopupManager] Pool exhausted!");
            return;
        }

        var obj = pool.Dequeue();
        obj.SetActive(true);

        // 위치 설정 (월드 좌표)
        obj.transform.position = worldPosition + Vector3.up * 0.5f;

        // 텍스트 설정
        var tmp = obj.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = damage.ToString("F0");
            tmp.color = GetDamageColor(damage);
        }

        // 애니메이션 시작
        StartCoroutine(AnimatePopup(obj, tmp));
    }

    private Color GetDamageColor(float damage)
    {
        // 데미지 양에 따른 색상 (기본: 노란색, 높으면 빨간색)
        if (damage >= 50f)
            return Color.red;
        else if (damage >= 25f)
            return new Color(1f, 0.5f, 0f); // 주황색
        else
            return Color.yellow;
    }

    private IEnumerator AnimatePopup(GameObject obj, TextMeshPro tmp)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Color startColor = tmp != null ? tmp.color : Color.white;

        while (elapsed < FadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeTime;

            // 위로 이동
            obj.transform.position = startPos + Vector3.up * (FloatSpeed * t);

            // 크기 변화 (커졌다가 작아짐)
            float scale = Mathf.Lerp(ScaleStart, ScaleEnd, Mathf.Sin(t * Mathf.PI));
            obj.transform.localScale = Vector3.one * scale;

            // 페이드아웃
            if (tmp != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                tmp.color = c;
            }

            // 카메라 방향 바라보기 (빌보드)
            if (mainCamera != null)
            {
                obj.transform.rotation = mainCamera.transform.rotation;
            }

            yield return null;
        }

        // 풀로 반환
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
