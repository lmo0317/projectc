using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 버프 선택 카드 UI 컴포넌트
/// </summary>
public class BuffOptionCard : MonoBehaviour
{
    [Header("UI References")]
    public Image IconImage;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI EffectText;
    public Button SelectButton;

    private int _buffType;
    private Action<int> _onSelected;

    private void Awake()
    {
        if (SelectButton != null)
        {
            SelectButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        _onSelected?.Invoke(_buffType);
    }

    /// <summary>
    /// 카드 정보 설정
    /// </summary>
    public void Setup(int buffType, int currentLevel, Action<int> onSelected)
    {
        _buffType = buffType;
        _onSelected = onSelected;

        var buffData = BuffDataHelper.GetBuffData((BuffType)buffType);

        // 아이콘 설정
        if (IconImage != null)
        {
            var icon = buffData.GetIcon();
            if (icon != null)
            {
                IconImage.sprite = icon;
                IconImage.color = Color.white;
            }
            else
            {
                // 아이콘이 없으면 색상으로 대체
                IconImage.sprite = null;
                IconImage.color = buffData.Color;
            }
        }

        // 버프 이름
        if (NameText != null)
        {
            NameText.text = buffData.Name;
        }

        // 레벨 표시
        if (LevelText != null)
        {
            if (currentLevel == 0)
            {
                LevelText.text = "NEW";
                LevelText.color = Color.green;
            }
            else if (currentLevel >= PlayerBuffs.MaxLevel)
            {
                LevelText.text = "MAX";
                LevelText.color = Color.yellow;
            }
            else
            {
                LevelText.text = $"Lv.{currentLevel} -> Lv.{currentLevel + 1}";
                LevelText.color = Color.white;
            }
        }

        // 효과 설명
        if (EffectText != null)
        {
            int nextLevel = Mathf.Min(currentLevel + 1, PlayerBuffs.MaxLevel);
            EffectText.text = buffData.GetEffectDescription(nextLevel);
        }
    }
}

/// <summary>
/// 버프 데이터 헬퍼 (정적 데이터)
/// </summary>
public static class BuffDataHelper
{
    public struct BuffData
    {
        public string Name;
        public Color Color;
        public string IconPath;  // Resources 폴더 내 아이콘 경로
        public string[] LevelEffects;

        public string GetEffectDescription(int level)
        {
            if (level <= 0 || level > LevelEffects.Length)
                return "";
            return LevelEffects[level - 1];
        }

        public Sprite GetIcon()
        {
            if (string.IsNullOrEmpty(IconPath))
                return null;
            return Resources.Load<Sprite>(IconPath);
        }
    }

    private static readonly BuffData[] BuffDatas = new BuffData[]
    {
        // Damage
        new BuffData
        {
            Name = "데미지 증가",
            Color = new Color(1f, 0.3f, 0.3f), // 빨강
            IconPath = "Icons/Buffs/Damage",
            LevelEffects = new[] { "+10%", "+20%", "+35%", "+50%", "+75%" }
        },
        // Speed
        new BuffData
        {
            Name = "이동 속도",
            Color = new Color(0.3f, 0.8f, 1f), // 하늘색
            IconPath = "Icons/Buffs/Speed",
            LevelEffects = new[] { "+10%", "+20%", "+30%", "+40%", "+50%" }
        },
        // FireRate
        new BuffData
        {
            Name = "공격 속도",
            Color = new Color(1f, 0.8f, 0.3f), // 주황
            IconPath = "Icons/Buffs/FireRate",
            LevelEffects = new[] { "+15%", "+30%", "+45%", "+60%", "+80%" }
        },
        // MissileCount
        new BuffData
        {
            Name = "미사일 추가",
            Color = new Color(0.8f, 0.3f, 1f), // 보라
            IconPath = "Icons/Buffs/Missile",
            LevelEffects = new[] { "+1", "+2", "+3", "+4", "+6" }
        },
        // Magnet
        new BuffData
        {
            Name = "자석 범위",
            Color = new Color(0.3f, 1f, 0.5f), // 초록
            IconPath = "Icons/Buffs/Magnet",
            LevelEffects = new[] { "3m", "5m", "7m", "10m", "15m" }
        },
        // HealthRegen
        new BuffData
        {
            Name = "체력 재생",
            Color = new Color(1f, 0.5f, 0.5f), // 분홍
            IconPath = "Icons/Buffs/HealthRegen",
            LevelEffects = new[] { "1/s", "2/s", "3/s", "5/s", "8/s" }
        },
        // MaxHealth
        new BuffData
        {
            Name = "최대 체력",
            Color = new Color(1f, 0.3f, 0.6f), // 핫핑크
            IconPath = "Icons/Buffs/MaxHealth",
            LevelEffects = new[] { "+20", "+40", "+70", "+100", "+150" }
        },
        // Critical
        new BuffData
        {
            Name = "치명타",
            Color = new Color(1f, 1f, 0.3f), // 노랑
            IconPath = "Icons/Buffs/Critical",
            LevelEffects = new[] { "5% (2x)", "10% (2x)", "15% (2.5x)", "20% (2.5x)", "30% (3x)" }
        }
    };

    public static BuffData GetBuffData(BuffType buffType)
    {
        int index = (int)buffType;
        if (index >= 0 && index < BuffDatas.Length)
        {
            return BuffDatas[index];
        }
        return new BuffData { Name = "Unknown", Color = Color.gray, IconPath = null, LevelEffects = new string[0] };
    }
}
