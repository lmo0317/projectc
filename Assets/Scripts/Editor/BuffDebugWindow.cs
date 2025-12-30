using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.NetCode;

/// <summary>
/// 버프 디버그 에디터 윈도우
/// 게임 실행 중 버프 레벨을 조정하고 실시간으로 상태를 확인
/// </summary>
public class BuffDebugWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private bool _autoRefresh = true;
    private double _lastRefreshTime;
    private const float RefreshInterval = 0.5f;

    // 캐시된 데이터
    private PlayerBuffs _cachedBuffs;
    private StatModifiers _cachedModifiers;
    private PlayerStarPoints _cachedStarPoints;
    private bool _hasPlayerData;

    // 버프 이름 및 설명
    private static readonly string[] BuffNames = {
        "Damage (데미지)",
        "Speed (이동속도)",
        "FireRate (공격속도)",
        "MissileCount (미사일)",
        "Magnet (자석)",
        "HealthRegen (체력재생)",
        "MaxHealth (최대체력)",
        "Critical (치명타)"
    };

    private static readonly string[] BuffDescriptions = {
        "+10% / +20% / +35% / +50% / +75%",
        "+10% / +20% / +30% / +40% / +50%",
        "-15% / -30% / -45% / -60% / -80%",
        "+1 / +2 / +3 / +4 / +6",
        "3m / 5m / 7m / 10m / 15m",
        "1/s / 2/s / 3/s / 5/s / 8/s",
        "+20 / +40 / +70 / +100 / +150",
        "5%/2x / 10%/2x / 15%/2.5x / 20%/2.5x / 30%/3x"
    };

    [MenuItem("Tools/Buff Debug Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<BuffDebugWindow>("버프 디버그");
        window.minSize = new Vector2(400, 500);
    }

    private void OnInspectorUpdate()
    {
        // 자동 새로고침
        if (_autoRefresh && Application.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // 헤더
        EditorGUILayout.Space(10);
        GUILayout.Label("버프 디버그 윈도우", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 게임 실행 상태 체크
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("게임이 실행 중이 아닙니다.\nPlay 버튼을 눌러 게임을 시작하세요.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // 자동 새로고침 토글
        _autoRefresh = EditorGUILayout.Toggle("자동 새로고침", _autoRefresh);

        if (!_autoRefresh)
        {
            if (GUILayout.Button("수동 새로고침"))
            {
                RefreshData();
            }
        }
        else
        {
            // 자동 새로고침
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshData();
            }
        }

        EditorGUILayout.Space(10);

        // 플레이어 데이터 없음
        if (!_hasPlayerData)
        {
            EditorGUILayout.HelpBox("플레이어 엔티티를 찾을 수 없습니다.\n서버 월드가 활성화되어 있는지 확인하세요.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        // Star 포인트 정보
        DrawStarPointsSection();

        EditorGUILayout.Space(10);

        // 버프 컨트롤
        DrawBuffControlSection();

        EditorGUILayout.Space(10);

        // 현재 스탯 수정치
        DrawStatModifiersSection();

        EditorGUILayout.Space(10);

        // 전체 초기화 버튼
        DrawResetSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawStarPointsSection()
    {
        EditorGUILayout.LabelField("Star 포인트", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField($"현재 포인트: {_cachedStarPoints.CurrentPoints}");
        EditorGUILayout.LabelField($"총 수집량: {_cachedStarPoints.TotalCollected}");
        EditorGUILayout.LabelField($"다음 버프까지: {_cachedStarPoints.NextBuffThreshold - _cachedStarPoints.CurrentPoints}");
        EditorGUILayout.LabelField($"버프 선택 횟수: {_cachedStarPoints.BuffSelectionCount}");

        EditorGUI.indentLevel--;
    }

    private void DrawBuffControlSection()
    {
        EditorGUILayout.LabelField("버프 레벨 조정", EditorStyles.boldLabel);

        for (int i = 0; i < 8; i++)
        {
            var buffType = (BuffType)i;
            int currentLevel = _cachedBuffs.GetLevel(buffType);

            EditorGUILayout.BeginHorizontal();

            // 버프 이름 및 현재 레벨
            EditorGUILayout.LabelField($"{BuffNames[i]}", GUILayout.Width(150));

            // 레벨 표시 (색상으로 강조)
            var originalColor = GUI.color;
            GUI.color = currentLevel > 0 ? Color.green : Color.gray;
            EditorGUILayout.LabelField($"Lv.{currentLevel}", GUILayout.Width(40));
            GUI.color = originalColor;

            // 레벨 다운 버튼
            GUI.enabled = currentLevel > 0;
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                SetBuffLevel(buffType, currentLevel - 1);
            }

            // 레벨 업 버튼
            GUI.enabled = currentLevel < PlayerBuffs.MaxLevel;
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                SetBuffLevel(buffType, currentLevel + 1);
            }

            // MAX 버튼
            GUI.enabled = currentLevel < PlayerBuffs.MaxLevel;
            if (GUILayout.Button("MAX", GUILayout.Width(40)))
            {
                SetBuffLevel(buffType, PlayerBuffs.MaxLevel);
            }

            GUI.enabled = true;

            // 효과 설명
            EditorGUILayout.LabelField(BuffDescriptions[i], EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawStatModifiersSection()
    {
        EditorGUILayout.LabelField("현재 적용된 스탯 수정치", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField($"데미지 배율: {_cachedModifiers.DamageMultiplier:F2}x");
        EditorGUILayout.LabelField($"이동속도 배율: {_cachedModifiers.SpeedMultiplier:F2}x");
        EditorGUILayout.LabelField($"공격속도 배율: {_cachedModifiers.FireRateMultiplier:F2}x");
        EditorGUILayout.LabelField($"추가 미사일: +{_cachedModifiers.BonusMissileCount}발");
        EditorGUILayout.LabelField($"자석 범위: {_cachedModifiers.MagnetRange:F1}m");
        EditorGUILayout.LabelField($"체력 재생: {_cachedModifiers.HealthRegenPerSecond:F1}/s");
        EditorGUILayout.LabelField($"추가 최대체력: +{_cachedModifiers.BonusMaxHealth:F0}");
        EditorGUILayout.LabelField($"치명타 확률: {_cachedModifiers.CriticalChance:F1}%");
        EditorGUILayout.LabelField($"치명타 배율: {_cachedModifiers.CriticalMultiplier:F1}x");

        EditorGUI.indentLevel--;
    }

    private void DrawResetSection()
    {
        EditorGUILayout.BeginHorizontal();

        // 모든 버프 초기화
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("모든 버프 초기화", GUILayout.Height(30)))
        {
            ResetAllBuffs();
        }

        // 모든 버프 MAX
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("모든 버프 MAX", GUILayout.Height(30)))
        {
            MaxAllBuffs();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Star 포인트 추가
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+10 포인트"))
        {
            AddStarPoints(10);
        }
        if (GUILayout.Button("+50 포인트"))
        {
            AddStarPoints(50);
        }
        if (GUILayout.Button("+100 포인트"))
        {
            AddStarPoints(100);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void RefreshData()
    {
        _hasPlayerData = false;

        // Server World 찾기
        World serverWorld = null;
        foreach (var world in World.All)
        {
            if (world.IsServer() || world.Name.Contains("Server"))
            {
                serverWorld = world;
                break;
            }
        }

        // Server World가 없으면 기본 World 사용
        if (serverWorld == null)
        {
            serverWorld = World.DefaultGameObjectInjectionWorld;
        }

        if (serverWorld == null || !serverWorld.IsCreated)
            return;

        var entityManager = serverWorld.EntityManager;

        // PlayerTag가 있는 엔티티 찾기
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerTag>(),
            ComponentType.ReadOnly<PlayerBuffs>(),
            ComponentType.ReadOnly<StatModifiers>(),
            ComponentType.ReadOnly<PlayerStarPoints>()
        );

        if (query.CalculateEntityCount() == 0)
            return;

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length > 0)
        {
            var entity = entities[0];
            _cachedBuffs = entityManager.GetComponentData<PlayerBuffs>(entity);
            _cachedModifiers = entityManager.GetComponentData<StatModifiers>(entity);
            _cachedStarPoints = entityManager.GetComponentData<PlayerStarPoints>(entity);
            _hasPlayerData = true;
        }
        entities.Dispose();
    }

    private void SetBuffLevel(BuffType buffType, int level)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null)
            return;

        var entityManager = serverWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<PlayerBuffs>(),
            ComponentType.ReadOnly<PlayerTag>()
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length > 0)
        {
            var entity = entities[0];
            var buffs = entityManager.GetComponentData<PlayerBuffs>(entity);
            buffs.SetLevel(buffType, level);
            entityManager.SetComponentData(entity, buffs);
            Debug.Log($"[BuffDebug] {buffType} → Lv.{level}");
        }
        entities.Dispose();
        RefreshData();
    }

    private void ResetAllBuffs()
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null)
            return;

        var entityManager = serverWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<PlayerBuffs>(),
            ComponentType.ReadOnly<PlayerTag>()
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length > 0)
        {
            var entity = entities[0];
            entityManager.SetComponentData(entity, new PlayerBuffs());
            Debug.Log("[BuffDebug] 모든 버프 초기화됨");
        }
        entities.Dispose();
        RefreshData();
    }

    private void MaxAllBuffs()
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null)
            return;

        var entityManager = serverWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<PlayerBuffs>(),
            ComponentType.ReadOnly<PlayerTag>()
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length > 0)
        {
            var entity = entities[0];
            var buffs = new PlayerBuffs
            {
                DamageLevel = PlayerBuffs.MaxLevel,
                SpeedLevel = PlayerBuffs.MaxLevel,
                FireRateLevel = PlayerBuffs.MaxLevel,
                MissileCountLevel = PlayerBuffs.MaxLevel,
                MagnetLevel = PlayerBuffs.MaxLevel,
                HealthRegenLevel = PlayerBuffs.MaxLevel,
                MaxHealthLevel = PlayerBuffs.MaxLevel,
                CriticalLevel = PlayerBuffs.MaxLevel
            };
            entityManager.SetComponentData(entity, buffs);
            Debug.Log("[BuffDebug] 모든 버프 MAX 설정됨");
        }
        entities.Dispose();
        RefreshData();
    }

    private void AddStarPoints(int amount)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null)
            return;

        var entityManager = serverWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<PlayerStarPoints>(),
            ComponentType.ReadOnly<PlayerTag>()
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length > 0)
        {
            var entity = entities[0];
            var starPoints = entityManager.GetComponentData<PlayerStarPoints>(entity);
            starPoints.CurrentPoints += amount;
            starPoints.TotalCollected += amount;
            entityManager.SetComponentData(entity, starPoints);
            Debug.Log($"[BuffDebug] +{amount} 포인트 추가 (현재: {starPoints.CurrentPoints})");
        }
        entities.Dispose();
        RefreshData();
    }

    private World GetServerWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsServer() || world.Name.Contains("Server"))
            {
                return world;
            }
        }
        return World.DefaultGameObjectInjectionWorld;
    }
}
