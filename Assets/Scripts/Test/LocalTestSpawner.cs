using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// NetCode 없이 로컬에서 Enemy를 스폰하여 성능 테스트하는 컴포넌트
/// 사용법: 빈 GameObject에 붙이고 EnemyPrefab 할당 후 Play
/// </summary>
public class LocalTestSpawner : MonoBehaviour
{
    [Header("Enemy 설정")]
    public GameObject EnemyPrefab; // Inspector에서 Enemy Prefab 할당
    public int SpawnCount = 1000;
    public float SpawnRadius = 50f;
    public float EnemySpeed = 3f;
    public float EnemyHealth = 100f;

    [Header("플레이어 시뮬레이션 (이 오브젝트 위치 사용)")]
    public bool UseThisAsPlayer = true;

    [Header("디버그")]
    public bool ShowGizmos = true;
    public Color EnemyGizmoColor = Color.red;
    public Color PlayerGizmoColor = Color.green;

    private EntityManager _entityManager;
    private bool _spawned = false;
    private Entity _playerEntity;
    private EntityQuery _enemyQuery;
    private Entity _prefabEntity;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("DefaultWorld가 없습니다!");
            return;
        }

        _entityManager = world.EntityManager;
        _enemyQuery = _entityManager.CreateEntityQuery(typeof(EnemyTag), typeof(LocalTransform));
    }

    void Update()
    {
        if (_entityManager == default) return;

        // 플레이어 Entity 위치 업데이트
        if (_playerEntity != Entity.Null && UseThisAsPlayer)
        {
            _entityManager.SetComponentData(_playerEntity, LocalTransform.FromPosition(transform.position));
        }

        if (_spawned) return;

        // 첫 프레임에서 스폰
        SpawnTestPlayer();
        SpawnEnemies();
        _spawned = true;
    }

    void SpawnTestPlayer()
    {
        if (!UseThisAsPlayer) return;

        // 테스트용 플레이어 Entity 생성
        var playerArchetype = _entityManager.CreateArchetype(
            typeof(LocalTransform),
            typeof(LocalTestPlayerTag)
        );

        _playerEntity = _entityManager.CreateEntity(playerArchetype);
        _entityManager.SetComponentData(_playerEntity, LocalTransform.FromPosition(transform.position));

        Debug.Log("[LocalTestSpawner] 테스트 플레이어 생성 완료");
    }

    void SpawnEnemies()
    {
        Debug.Log($"[LocalTestSpawner] {SpawnCount}개의 Enemy 스폰 시작...");

        float3 center = (float3)transform.position;

        // Prefab이 있으면 GameObject로 스폰 (렌더링 포함)
        if (EnemyPrefab != null)
        {
            SpawnFromGameObjectPrefab(center);
            return;
        }

        // Prefab이 없으면 기본 Archetype으로 스폰 (렌더링 없음)
        SpawnFromArchetype(center);
    }

    void SpawnFromGameObjectPrefab(float3 center)
    {
        Debug.Log($"[LocalTestSpawner] GameObject Prefab으로 스폰 중...");

        // 부모 오브젝트 생성 (정리용)
        var parent = new GameObject("SpawnedEnemies");

        for (int i = 0; i < SpawnCount; i++)
        {
            // 나선형으로 위치 설정
            float angle = i * 0.1f;
            float radius = 5f + (i * 0.05f) % SpawnRadius;
            Vector3 pos = (Vector3)center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            // GameObject Instantiate (Baker가 자동으로 Entity로 변환)
            var instance = Instantiate(EnemyPrefab, pos, Quaternion.identity, parent.transform);
            instance.name = $"Enemy_{i}";
        }

        Debug.Log($"[LocalTestSpawner] {SpawnCount}개의 Enemy 스폰 완료! (GameObject Prefab)");
    }

    Entity GetPrefabEntity()
    {
        // 방법 1: LocalTestEnemyPrefabReference에서 Prefab Entity 찾기
        var refQuery = _entityManager.CreateEntityQuery(typeof(LocalTestEnemyPrefabReference));
        if (refQuery.CalculateEntityCount() > 0)
        {
            var entities = refQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var prefabRef = _entityManager.GetComponentData<LocalTestEnemyPrefabReference>(entities[0]);
            entities.Dispose();

            if (prefabRef.Prefab != Entity.Null)
            {
                Debug.Log("[LocalTestSpawner] LocalTestEnemyPrefabReference에서 Prefab 찾음");
                return prefabRef.Prefab;
            }
        }

        // 방법 2: EnemyTag + Prefab 컴포넌트로 찾기
        var prefabQuery = _entityManager.CreateEntityQuery(
            typeof(EnemyTag),
            typeof(Prefab)
        );

        if (prefabQuery.CalculateEntityCount() > 0)
        {
            var prefabs = prefabQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var result = prefabs[0];
            prefabs.Dispose();
            Debug.Log("[LocalTestSpawner] EnemyTag+Prefab에서 Prefab 찾음");
            return result;
        }

        return Entity.Null;
    }

    void SpawnFromPrefabEntity(float3 center)
    {
        Debug.Log($"[LocalTestSpawner] Prefab Entity로 스폰 중...");

        for (int i = 0; i < SpawnCount; i++)
        {
            // Prefab Entity 복제
            var entity = _entityManager.Instantiate(_prefabEntity);

            // 나선형으로 위치 설정
            float angle = i * 0.1f;
            float radius = 5f + (i * 0.05f) % SpawnRadius;
            float3 pos = center + new float3(
                math.cos(angle) * radius,
                0f,
                math.sin(angle) * radius
            );

            _entityManager.SetComponentData(entity, LocalTransform.FromPosition(pos));

            // 속도와 체력 덮어쓰기
            if (_entityManager.HasComponent<EnemySpeed>(entity))
                _entityManager.SetComponentData(entity, new EnemySpeed { Value = EnemySpeed });
            if (_entityManager.HasComponent<EnemyHealth>(entity))
                _entityManager.SetComponentData(entity, new EnemyHealth { Value = EnemyHealth });
        }

        Debug.Log($"[LocalTestSpawner] {SpawnCount}개의 Enemy 스폰 완료! (Prefab Entity)");
    }

    void SpawnFromArchetype(float3 center)
    {
        Debug.Log($"[LocalTestSpawner] Archetype으로 스폰 중 (렌더링 없음)...");

        var archetype = _entityManager.CreateArchetype(
            typeof(LocalTransform),
            typeof(EnemyTag),
            typeof(EnemySpeed),
            typeof(EnemyHealth)
        );

        for (int i = 0; i < SpawnCount; i++)
        {
            var entity = _entityManager.CreateEntity(archetype);

            float angle = i * 0.1f;
            float radius = 5f + (i * 0.05f) % SpawnRadius;
            float3 pos = center + new float3(
                math.cos(angle) * radius,
                0f,
                math.sin(angle) * radius
            );

            _entityManager.SetComponentData(entity, LocalTransform.FromPosition(pos));
            _entityManager.SetComponentData(entity, new EnemySpeed { Value = EnemySpeed });
            _entityManager.SetComponentData(entity, new EnemyHealth { Value = EnemyHealth });
        }

        Debug.Log($"[LocalTestSpawner] {SpawnCount}개의 Enemy 스폰 완료! (Archetype)");
    }

    void OnDestroy()
    {
        // 정리
        if (_entityManager != default && _playerEntity != Entity.Null)
        {
            if (_entityManager.Exists(_playerEntity))
            {
                _entityManager.DestroyEntity(_playerEntity);
            }
        }
    }

    void OnGUI()
    {
        if (_entityManager == default) return;

        int enemyCount = _enemyQuery.CalculateEntityCount();

        // 화면 좌상단에 정보 표시
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 400, 30), $"Enemy Count: {enemyCount}", style);
        GUI.Label(new Rect(10, 40, 400, 30), $"Player Pos: {transform.position}", style);
        GUI.Label(new Rect(10, 70, 400, 30), $"FPS: {1f / Time.deltaTime:F1}", style);
    }

    void OnDrawGizmos()
    {
        if (!ShowGizmos) return;
        if (!Application.isPlaying) return;
        if (_entityManager == default) return;

        // 플레이어 위치 표시
        Gizmos.color = PlayerGizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Enemy 위치 표시 (최대 500개만 - 성능)
        Gizmos.color = EnemyGizmoColor;

        var enemies = _enemyQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        int drawCount = Mathf.Min(enemies.Length, 500);

        for (int i = 0; i < drawCount; i++)
        {
            if (_entityManager.Exists(enemies[i]))
            {
                var pos = _entityManager.GetComponentData<LocalTransform>(enemies[i]).Position;
                Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, pos.z), Vector3.one * 0.5f);
            }
        }

        enemies.Dispose();
    }
}
