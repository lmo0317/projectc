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

        string damageText = damage.ToString("F0");
        Color damageColor = GetDamageColor(damage);

        // 텍스트 설정 - 여러 방식으로 찾기 시도
        // 1. 3D TextMeshPro (루트)
        var tmp3D = obj.GetComponent<TextMeshPro>();
        if (tmp3D != null)
        {
            tmp3D.text = damageText;
            tmp3D.color = damageColor;
            StartCoroutine(AnimatePopup3D(obj, tmp3D));
            return;
        }

        // 2. 3D TextMeshPro (자식)
        tmp3D = obj.GetComponentInChildren<TextMeshPro>();
        if (tmp3D != null)
        {
            tmp3D.text = damageText;
            tmp3D.color = damageColor;
            StartCoroutine(AnimatePopup3D(obj, tmp3D));
            return;
        }

        // 3. UI TextMeshProUGUI (루트 또는 자식)
        var tmpUI = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpUI != null)
        {
            tmpUI.text = damageText;
            tmpUI.color = damageColor;
            StartCoroutine(AnimatePopupUI(obj, tmpUI));
            return;
        }

        // 4. 일반 TMP_Text (베이스 클래스)
        var tmpBase = obj.GetComponentInChildren<TMP_Text>();
        if (tmpBase != null)
        {
            tmpBase.text = damageText;
            tmpBase.color = damageColor;
            StartCoroutine(AnimatePopupBase(obj, tmpBase));
            return;
        }

        Debug.LogWarning($"[DamagePopupManager] No TextMeshPro component found on {obj.name}!");
        StartCoroutine(AnimatePopupSimple(obj));
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

    private IEnumerator AnimatePopup3D(GameObject obj, TextMeshPro tmp)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Color startColor = tmp.color;

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
            Color c = startColor;
            c.a = 1f - t;
            tmp.color = c;

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

    private IEnumerator AnimatePopupUI(GameObject obj, TextMeshProUGUI tmp)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Color startColor = tmp.color;

        while (elapsed < FadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeTime;

            // 위로 이동
            obj.transform.position = startPos + Vector3.up * (FloatSpeed * t);

            // 크기 변화
            float scale = Mathf.Lerp(ScaleStart, ScaleEnd, Mathf.Sin(t * Mathf.PI));
            obj.transform.localScale = Vector3.one * scale;

            // 페이드아웃
            Color c = startColor;
            c.a = 1f - t;
            tmp.color = c;

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

    private IEnumerator AnimatePopupBase(GameObject obj, TMP_Text tmp)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Color startColor = tmp.color;

        while (elapsed < FadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeTime;

            // 위로 이동
            obj.transform.position = startPos + Vector3.up * (FloatSpeed * t);

            // 크기 변화
            float scale = Mathf.Lerp(ScaleStart, ScaleEnd, Mathf.Sin(t * Mathf.PI));
            obj.transform.localScale = Vector3.one * scale;

            // 페이드아웃
            Color c = startColor;
            c.a = 1f - t;
            tmp.color = c;

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

    private IEnumerator AnimatePopupSimple(GameObject obj)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;

        while (elapsed < FadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeTime;

            // 위로 이동
            obj.transform.position = startPos + Vector3.up * (FloatSpeed * t);

            // 크기 변화
            float scale = Mathf.Lerp(ScaleStart, ScaleEnd, Mathf.Sin(t * Mathf.PI));
            obj.transform.localScale = Vector3.one * scale;

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
