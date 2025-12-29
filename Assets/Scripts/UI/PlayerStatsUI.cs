using TMPro;
using UnityEngine;

/// <summary>
/// 플레이어 능력치 HUD 표시
/// 기본 수치 + 버프 추가 수치를 함께 표시
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    public static PlayerStatsUI Instance { get; private set; }

    [Header("Stats Panel")]
    public GameObject StatsPanel;

    [Header("Stat Texts")]
    public TextMeshProUGUI DamageText;
    public TextMeshProUGUI SpeedText;
    public TextMeshProUGUI FireRateText;
    public TextMeshProUGUI MissileText;
    public TextMeshProUGUI CriticalText;
    public TextMeshProUGUI MagnetText;
    public TextMeshProUGUI RegenText;
    public TextMeshProUGUI MaxHealthText;

    [Header("Base Stats (기본 수치)")]
    public float BaseDamage = 10f;          // 기본 데미지
    public float BaseSpeed = 5f;            // 기본 이동 속도
    public float BaseFireRate = 0.5f;       // 기본 발사 간격 (초)
    public int BaseMissileCount = 1;        // 기본 미사일 개수
    public float BaseMaxHealth = 100f;      // 기본 최대 체력

    // 현재 스탯 값 캐싱
    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _fireRateMultiplier = 1f;
    private int _bonusMissile = 0;
    private float _critChance = 0f;
    private float _critMultiplier = 1f;
    private float _magnetRange = 0f;
    private float _healthRegen = 0f;
    private float _bonusMaxHealth = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 기본 스탯 설정 (게임 시작 시 호출)
    /// </summary>
    public void SetBaseStats(float damage, float speed, float fireRate, int missileCount, float maxHealth)
    {
        BaseDamage = damage;
        BaseSpeed = speed;
        BaseFireRate = fireRate;
        BaseMissileCount = missileCount;
        BaseMaxHealth = maxHealth;
        RefreshUI();
    }

    /// <summary>
    /// 모든 스탯 업데이트
    /// </summary>
    public void UpdateStats(float damageMultiplier, float speedMultiplier, float fireRateMultiplier,
                           int bonusMissile, float critChance, float critMultiplier,
                           float magnetRange, float healthRegen, float bonusMaxHealth = 0f)
    {
        _damageMultiplier = damageMultiplier;
        _speedMultiplier = speedMultiplier;
        _fireRateMultiplier = fireRateMultiplier;
        _bonusMissile = bonusMissile;
        _critChance = critChance;
        _critMultiplier = critMultiplier;
        _magnetRange = magnetRange;
        _healthRegen = healthRegen;
        _bonusMaxHealth = bonusMaxHealth;

        RefreshUI();
    }

    /// <summary>
    /// UI 갱신
    /// </summary>
    private void RefreshUI()
    {
        // 데미지: 기본값 x 배율
        if (DamageText != null)
        {
            float finalDamage = BaseDamage * _damageMultiplier;
            bool isBuffed = _damageMultiplier > 1f;

            if (isBuffed)
            {
                int bonusPercent = Mathf.RoundToInt((_damageMultiplier - 1f) * 100f);
                DamageText.text = $"DMG: {finalDamage:F0} <color=#4CFF4C>(+{bonusPercent}%)</color>";
            }
            else
            {
                DamageText.text = $"DMG: {finalDamage:F0}";
            }
        }

        // 이동 속도
        if (SpeedText != null)
        {
            float finalSpeed = BaseSpeed * _speedMultiplier;
            bool isBuffed = _speedMultiplier > 1f;

            if (isBuffed)
            {
                int bonusPercent = Mathf.RoundToInt((_speedMultiplier - 1f) * 100f);
                SpeedText.text = $"SPD: {finalSpeed:F1} <color=#4CFF4C>(+{bonusPercent}%)</color>";
            }
            else
            {
                SpeedText.text = $"SPD: {finalSpeed:F1}";
            }
        }

        // 공격 속도 (초당 발사 횟수로 표시)
        if (FireRateText != null)
        {
            float actualFireRate = BaseFireRate * _fireRateMultiplier;
            float shotsPerSecond = 1f / actualFireRate;
            bool isBuffed = _fireRateMultiplier < 1f;

            if (isBuffed)
            {
                int bonusPercent = Mathf.RoundToInt((1f - _fireRateMultiplier) * 100f);
                FireRateText.text = $"ATK: {shotsPerSecond:F1}/s <color=#4CFF4C>(+{bonusPercent}%)</color>";
            }
            else
            {
                FireRateText.text = $"ATK: {shotsPerSecond:F1}/s";
            }
        }

        // 미사일 개수
        if (MissileText != null)
        {
            int totalMissiles = BaseMissileCount + _bonusMissile;
            bool isBuffed = _bonusMissile > 0;

            if (isBuffed)
            {
                MissileText.text = $"Missile: {totalMissiles} <color=#4CFF4C>(+{_bonusMissile})</color>";
            }
            else
            {
                MissileText.text = $"Missile: {totalMissiles}";
            }
        }

        // 치명타 (기본값 없음, 버프로만 획득)
        if (CriticalText != null)
        {
            bool isBuffed = _critChance > 0f;

            if (isBuffed)
            {
                CriticalText.text = $"CRIT: <color=#FFFF4C>{_critChance:F0}%</color> (x{_critMultiplier:F1})";
            }
            else
            {
                CriticalText.text = "CRIT: 0%";
            }
        }

        // 자석 범위 (기본값 없음, 버프로만 획득)
        if (MagnetText != null)
        {
            bool isBuffed = _magnetRange > 0f;

            if (isBuffed)
            {
                MagnetText.text = $"Magnet: <color=#4CFF4C>{_magnetRange:F0}m</color>";
            }
            else
            {
                MagnetText.text = "Magnet: 0m";
            }
        }

        // 체력 재생 (기본값 없음, 버프로만 획득)
        if (RegenText != null)
        {
            bool isBuffed = _healthRegen > 0f;

            if (isBuffed)
            {
                RegenText.text = $"Regen: <color=#FF8080>{_healthRegen:F0}/s</color>";
            }
            else
            {
                RegenText.text = "Regen: 0/s";
            }
        }

        // 최대 체력
        if (MaxHealthText != null)
        {
            float totalMaxHealth = BaseMaxHealth + _bonusMaxHealth;
            bool isBuffed = _bonusMaxHealth > 0f;

            if (isBuffed)
            {
                MaxHealthText.text = $"HP: {totalMaxHealth:F0} <color=#FF6699>(+{_bonusMaxHealth:F0})</color>";
            }
            else
            {
                MaxHealthText.text = $"HP: {totalMaxHealth:F0}";
            }
        }
    }

    /// <summary>
    /// 스탯 패널 표시/숨김
    /// </summary>
    public void SetPanelVisible(bool visible)
    {
        if (StatsPanel != null)
        {
            StatsPanel.SetActive(visible);
        }
    }
}
