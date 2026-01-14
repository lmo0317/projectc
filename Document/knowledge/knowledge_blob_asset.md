# Unity DOTS Blob Asset 완전 가이드

## 📚 목차

1. [Blob Asset이란?](#blob-asset이란)
2. [Blob Asset vs 일반 Component Data](#blob-asset-vs-일반-component-data)
3. [Blob Asset 작동 원리](#blob-asset-작동-원리)
4. [Blob Asset 생성 및 사용 방법](#blob-asset-생성-및-사용-방법)
5. [SubScene과 Blob Asset의 관계](#subscene과-blob-asset의-관계)
6. [실전 사용 예시](#실전-사용-예시)
7. [주의사항 및 모범 사례](#주의사항-및-모범-사례)
8. [자주 묻는 질문](#자주-묻는-질문)

---

## Blob Asset이란?

**Blob Asset**은 Unity DOTS에서 **대용량 불변(Immutable) 데이터**를 효율적으로 저장하고 관리하기 위한 메모리 구조입니다.

### 핵심 개념

```
Blob Asset = 불변 대용량 데이터를 위한 메모리 풀
```

**"Blob"이 의미하는 것:**
- **Binary Large Object**: 대용량 바이너리 데이터
- **Immutable**: 한 번 생성되면 변경 불가능
- **Shared**: 여러 Entity가 같은 데이터를 참조

### 사용처

| 데이터 유형 | 예시 | Blob Asset 사용 이유 |
|-----------|------|---------------------|
| **메시 데이터** | Vertex, Index, UV | 수만 개의 꼭짓점 데이터 |
| **텍스처 데이터** | 픽셀 배열 | 대용량 이미지 데이터 |
| **게임 테이블** | 아이템, 몬스터 stats | 수천 개의 데이터 행 |
| **AI 경로 데이터** | 그래프, 네비게이션 | 복잡한 구조체 배열 |
| **애니메이션 커브** | 키프레임 데이터 | 시간별 값 배열 |
| **사운드 데이터** | 오디오 샘플 | 대용량 PCM 데이터 |

---

## Blob Asset vs 일반 Component Data

### 데이터 크기와 변경 가능성 비교

| 특징 | 일반 Component Data (IComponentData) | Blob Asset |
|------|-----------------------------------|------------|
| **데이터 크기** | 작음 (기본적으로 16KB 이하) | 큼 (수 MB ~ 수 GB) |
| **변경 가능성** | ✅ 가변 (Mutable) | ❌ 불변 (Immutable) |
| **메모리 위치** | Chunk 내부 | 별도 Blob 힙 |
| **복사 비용** | Entity마다 복사 | 참조만 공유 |
| **ARC 수집** | Chunk 파괴 시 자동 | 수동으로 Dispose 필요 |
| **주요 사용처** | 위치, 회전, 체력 등 | 메시, 테이블, 커브 등 |

### 메모리 구조 비교

**일반 Component Data:**
```
┌─────────────────────────────────────────────────────────────┐
│ Archetype Chunk                                             │
├─────────────────────────────────────────────────────────────┤
│ Entity_1000: [LocalTransform][Health][EnemyData]            │
│ Entity_1001: [LocalTransform][Health][EnemyData]            │
│ Entity_1002: [LocalTransform][Health][EnemyData]            │
│                                                             │
│ ⚠️ 각 Entity마다 데이터가 복사됨                            │
└─────────────────────────────────────────────────────────────┘
```

**Blob Asset (공유 데이터):**
```
┌─────────────────────────────────────────────────────────────┐
│ Blob Heap (별도 메모리 공간)                                │
├─────────────────────────────────────────────────────────────┤
│ BlobAsset_5000:                                             │
│ {                                                           │
│   VertexData: [v1, v2, v3, ..., v50000]                    │
│   IndexData:  [i1, i2, i3, ..., i150000]                   │
│ }                                                           │
└─────────────────────────────────────────────────────────────┘
                              ↑ 참조
┌─────────────────────────────────────────────────────────────┐
│ Archetype Chunk                                             │
├─────────────────────────────────────────────────────────────┤
│ Entity_1000: [LocalTransform][BlobReference(5000)]          │
│ Entity_1001: [LocalTransform][BlobReference(5000)]          │
│ Entity_1002: [LocalTransform][BlobReference(5000)]          │
│                                                             │
│ ✅ 모든 Entity이 같은 Blob Asset를 참조 (메모리 절약)        │
└─────────────────────────────────────────────────────────────┘
```

---

## Blob Asset 작동 원리

### 1. Blob Asset의 생명주기

```
┌─────────────────────────────────────────────────────────────┐
│                  Blob Asset 생명주기                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1️⃣ 빌드 타임 (Baker)       2️⃣ 런타임 (System)            │
│  ──────────────────         ────────────────────           │
│  Blob 데이터 정의             BlobAssetReference 생성        │
│       ↓                             ↓                       │
│  BlobBuilder로 생성         BlobAsset 참조                  │
│       ↓                             ↓                       │
│  BlobAsset 반환         System에서 데이터 읽기              │
│       ↓                             ↓                       │
│  BlobAssetReference    완료 후 Blob.Dispose()              │
│  (컴포넌트에 저장)              (메모리 해제)                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2. 메모리 할당 과정

**빌드 타임 (Baker):**

```csharp
// 1. BlobBuilder 생성 (할당자와 함께)
var builder = new BlobAssetBuilder(Allocator.Temp);

// 2. 루트 구조체 설정
ref var root = ref builder.ConstructRoot<MyGameData>();

// 3. 배열 할당
root.Enemies = builder.Allocate(ref root.EnemyArray, 1000);

// 4. 데이터 채우기
for (int i = 0; i < 1000; i++)
{
    root.Enemies[i] = new EnemyData { Health = 100, Damage = 10 };
}

// 5. BlobAsset 생성 및 Reference 반환
var blobAsset = builder.CreateBlobAssetReference<MyGameData>(Allocator.Persistent);
```

**런타임 (System):**

```csharp
// 1. BlobAssetReference에서 데이터 접근
public partial struct EnemySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var gameData = SystemAPI.GetSingleton<GameDataBlob>();

        // 2. 참조로 데이터 읽기 (복사 없음)
        foreach (var enemy in gameData.Data.Value.Enemies)
        {
            // enemy 데이터 읽기
        }
    }
}

// 3. 사용 완료 후 메모리 해제
public void OnDestroy(ref SystemState state)
{
    var gameData = SystemAPI.GetSingleton<GameDataBlob>();
    gameData.Data.Dispose(); // Blob Asset 메모리 해제
}
```

### 3. 불변성 (Immutability) 보장

**왜 불변이어야 하는가?**

```
🔒 불변성의 이유

1️⃣ 메모리 안정성
   - 여러 스레드가 동시에 읽어도 안전
   - 데이터 변경으로 인한 경합 조건(Condition Race) 방지

2️⃣ 메모리 최적화
   - 변경 가능성이 없으므로 메모리 레이아웃 최적화 가능
   - 데이터 압축 및 재배치 가능

3️⃣ 캐시 친화적
   - 읽기 전용이므로 CPU 캐시에 유리
   - 메모리 프리패칭 최적화
```

**시도하면 안 되는 것:**

```csharp
// ❌ 컴파일 에러: Blob 데이터는 수정 불가
public partial struct ModifySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var blob = SystemAPI.GetSingleton<MyDataBlob>();
        blob.Value.DataArray[0] = newValue; // 컴파일 에러!
        // ref readonly로 반환되므로 수정 불가
    }
}

// ✅ 올바른 방법: 새로운 Blob Asset 생성
public partial struct ModifySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. 기존 데이터 읽기
        var oldBlob = SystemAPI.GetSingleton<MyDataBlob>();

        // 2. 새로운 BlobBuilder로 복사
        var builder = new BlobAssetBuilder(Allocator.Temp);
        ref var newRoot = ref builder.ConstructRoot<MyData>();

        // 3. 데이터 복사 및 수정
        for (int i = 0; i < oldBlob.Value.DataArray.Length; i++)
        {
            newRoot.DataArray[i] = oldBlob.Value.DataArray[i];
        }
        newRoot.DataArray[0] = newValue; // 수정

        // 4. 새로운 Blob Asset 생성
        var newBlob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);

        // 5. 기존 Blob 해제 및 교체
        oldBlob.Data.Dispose();
        SystemAPI.GetSingletonRW<MyDataBlob>().Data = newBlob;
    }
}
```

---

## Blob Asset 생성 및 사용 방법

### 1. 기본 Blob 데이터 구조 정의

```csharp
using Unity.Entities;
using Unity.Mathematics;

// 1. Blob 안에 들어갈 데이터 구조체 (IComponentData가 아님!)
public struct EnemyStats
{
    public float Health;
    public float Damage;
    public float MoveSpeed;
}

// 2. Blob 루트 데이터 구조체
public struct GameBalanceData : IComponentData
{
    public BlobArray<EnemyStats> Enemies;  // 배열
    public BlobArray<float> LevelExp;      // 또 다른 배열
    public float GlobalDamageMultiplier;   // 단일 값
    public int MaxLevel;                   // 단일 값
}
```

### 2. Baker에서 Blob Asset 생성

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// 1. Authoring Component (GameObject에 부착)
public class GameBalanceAuthoring : MonoBehaviour
{
    [Header("적 데이터")]
    public EnemyStatsConfig[] enemies;

    [Header("레벨 경험치 테이블")]
    public float[] levelExp;

    [Header("전역 설정")]
    public float globalDamageMultiplier = 1.0f;
    public int maxLevel = 100;

    // 직렬화를 위한 중간 구조체
    [System.Serializable]
    public struct EnemyStatsConfig
    {
        public string name;
        public float health;
        public float damage;
        public float moveSpeed;
    }

    // 2. Baker: GameObject → Entity 변환
    class Baker : Baker<GameBalanceAuthoring>
    {
        public override void Bake(GameBalanceAuthoring authoring)
        {
            // 1. BlobBuilder 생성
            var builder = new BlobAssetBuilder(Allocator.Temp);

            // 2. 루트 구조체 생성
            ref var root = ref builder.ConstructRoot<GameBalanceData>();

            // 3. 배열 할당
            root.Enemies = builder.Allocate(
                ref root.Enemies,
                authoring.enemies.Length
            );
            root.LevelExp = builder.Allocate(
                ref root.LevelExp,
                authoring.levelExp.Length
            );

            // 4. 데이터 채우기
            for (int i = 0; i < authoring.enemies.Length; i++)
            {
                root.Enemies[i] = new EnemyStats
                {
                    Health = authoring.enemies[i].health,
                    Damage = authoring.enemies[i].damage,
                    MoveSpeed = authoring.enemies[i].moveSpeed
                };
            }

            for (int i = 0; i < authoring.levelExp.Length; i++)
            {
                root.LevelExp[i] = authoring.levelExp[i];
            }

            // 5. 단일 값 설정
            root.GlobalDamageMultiplier = authoring.globalDamageMultiplier;
            root.MaxLevel = authoring.maxLevel;

            // 6. BlobAssetReference 생성 및 컴포넌트에 추가
            var blobRef = builder.CreateBlobAssetReference<GameBalanceData>(Allocator.Persistent);
            AddComponent(blobRef);
        }
    }
}
```

### 3. System에서 Blob Asset 사용

```csharp
using Unity.Entities;
using Unity.Mathematics;

// Blob Asset을 참조하는 컴포넌트
public struct GameBalanceData : IComponentData
{
    public BlobAssetReference<GameBalanceDataBlob> Data;
}

// Blob 데이터 구조체 (IComponentData가 아님!)
public struct GameBalanceDataBlob
{
    public BlobArray<EnemyStats> Enemies;
    public BlobArray<float> LevelExp;
    public float GlobalDamageMultiplier;
    public int MaxLevel;
}

// System에서 사용
public partial struct EnemySpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 게임 데이터가 있는지 확인
        if (!SystemAPI.HasSingleton<GameBalanceData>())
        {
            state.Enabled = false;
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        // 1. Blob Asset 참조 가져오기
        var gameData = SystemAPI.GetSingleton<GameBalanceData>();

        // 2. Blob 데이터 읽기 (복사 없이 직접 참조)
        var blobData = gameData.Data.Value;

        // 3. 배열 데이터 사용
        foreach (var enemyStats in blobData.Enemies)
        {
            // 적 스폰 로직
            var enemy = EntityManager.CreateEntity();
            EntityManager.SetComponentData(enemy, new Health
            {
                Value = enemyStats.Health * blobData.GlobalDamageMultiplier
            });
        }

        // 4. 레벨 경험치 테이블 사용
        float currentLevelExp = blobData.LevelExp[5]; // 레벨 6 경험치
    }

    public void OnDestroy(ref SystemState state)
    {
        // System이 파괴될 때 Blob Asset 메모리 해제
        if (SystemAPI.HasSingleton<GameBalanceData>())
        {
            var gameData = SystemAPI.GetSingleton<GameBalanceData>();
            gameData.Data.Dispose();
        }
    }
}
```

### 4. Blob Asset과 함께 사용하는 특수 타입

#### BlobArray<T>

```csharp
public struct MyData
{
    public BlobArray<float> Numbers;  // 1차원 배열
    public BlobArray<float3> Positions;  // float3 배열
}

// 사용법
for (int i = 0; i < data.Numbers.Length; i++)
{
    float value = data.Numbers[i];
}
```

#### BlobString

```csharp
public struct EnemyData
{
    public BlobString Name;  // 문자열
    public int Health;
}

// 사용법
string enemyName = data.Name.ToString(); // C# 문자열로 변환
```

#### BlobPtr<T>

```csharp
public struct Node
{
    public int Value;
    public BlobPtr<Node> Next;  // 다른 노드를 가리키는 포인터
}

public struct Graph
{
    public BlobArray<Node> Nodes;
}
```

---

## SubScene과 Blob Asset의 관계

### 1. 데이터 관리의 분담

```
┌─────────────────────────────────────────────────────────────┐
│             SubScene + Blob Asset 협업 모델                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  SubScene 담당:                                             │
│  • Entity 구조 정의 (몇 개의 몬스터, 어디에 위치)            │
│  • 컴포넌트 데이터 (각 몬스터의 개별 속성)                   │
│  • 빌드 타임 변환 (GameObject → Entity)                     │
│                                                             │
│  Blob Asset 담당:                                          │
│  • 대용량 공유 데이터 (모든 몬스터의 공통 스탯 테이블)        │
│  • 불변 데이터 (메시, 텍스처, 애니메이션)                    │
│  • 메모리 효율적 저장 (한 번만 저장, 여러 번 참조)          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2. 실제 사용 시나리오

**시나리오: RPG 게임의 몬스터 시스템**

```
📁 SubScene (GameSceneSpace.unity)
├── Goblin_01 (Entity)
│   ├── LocalTransform (Position: 0, 0, 0)
│   └── MonsterComponent
│       ├── EnemyType: Goblin           # 개별 데이터
│       ├── Level: 5                    # 개별 데이터
│       └── StatsBlobReference: 1000    # Blob Asset 참조
│
├── Goblin_02 (Entity)
│   ├── LocalTransform (Position: 5, 0, 0)
│   └── MonsterComponent
│       ├── EnemyType: Goblin           # 개별 데이터
│       ├── Level: 7                    # 개별 데이터
│       └── StatsBlobReference: 1000    # 같은 Blob Asset 참조
│
└── Orc_01 (Entity)
    ├── LocalTransform (Position: 10, 0, 0)
    └── MonsterComponent
        ├── EnemyType: Orc              # 개별 데이터
        ├── Level: 10                   # 개별 데이터
        └── StatsBlobReference: 1001    # 다른 Blob Asset 참조


📦 Blob Asset Heap
├── BlobAsset_1000 (GoblinStats)
│   ├── BaseHealth: 100
│   ├── BaseDamage: 10
│   ├── BaseSpeed: 3.5f
│   └── DropTable: [Gold, Sword, Potion]
│
└── BlobAsset_1001 (OrcStats)
    ├── BaseHealth: 200
    ├── BaseDamage: 20
    ├── BaseSpeed: 2.0f
    └── DropTable: [Gold, Axe, Shield]
```

### 3. SubScene과 Blob Asset의 데이터 흐름

**빌드 타임:**

```
1. Unity Editor 작업
   └── GameBalanceAuthoring (GameObject)에 데이터 입력
       ├── enemies 배열 (100개의 적 데이터)
       └── levelExp 배열 (100개의 레벨 데이터)

2. Baker 실행
   └── BlobBuilder로 Blob Asset 생성
       ├── 모든 데이터를 Blob에 저장
       └── BlobAssetReference 생성

3. SubScene 빌드
   └── Entity + BlobAssetReference 포함하여 저장
```

**런타임:**

```
1. SubScene 로드
   └── Entity 생성
       └── BlobAssetReference는 그대로 보존

2. System 실행
   └── BlobAssetReference를 통해 Blob Asset 접근
       └── 데이터 읽기 (수천 개의 Entity가 같은 데이터 참조)
```

### 4. 함께 사용할 때의 이점

| 측면 | SubScene만 사용 | SubScene + Blob Asset |
|-----|----------------|---------------------|
| **메모리 사용** | 각 Entity마다 데이터 복사 | 데이터 공유로 메모리 절약 |
| **데이터 수정** | 각 Entity별로 수정 가능 | 공유 데이터는 불변 (안전) |
| **로딩 속도** | 데이터가 분산되어 느림 | Blob Asset 한 번만 로드 |
| **데이터 일관성** | Entity별로 다를 수 있음 | 모든 Entity가 같은 데이터 참조 |
| **대용량 데이터** | 성능 저하 | Blob Heap으로 최적화 |

### 5. 코드 예시: SubScene + Blob Asset

**Authoring Component:**

```csharp
public class MonsterAuthoring : MonoBehaviour
{
    public EnemyType Type;
    public int Level;

    class Baker : Baker<MonsterAuthoring>
    {
        public override void Bake(MonsterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 개별 데이터는 컴포넌트에 직접 저장
            AddComponent(entity, new MonsterComponent
            {
                Type = authoring.Type,
                Level = authoring.Level
            });

            // 공유 데이터는 Blob Asset 참조
            // (GameBalanceSystem에서 BlobAssetReference를 가져옴)
        }
    }
}

public struct MonsterComponent : IComponentData
{
    public EnemyType Type;
    public int Level;
    // BlobAssetReference는 별도 Singleton으로 관리
}
```

**System:**

```csharp
public partial struct MonsterStatSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. Blob Asset 참조 가져오기 (전역 공유 데이터)
        var gameBalance = SystemAPI.GetSingleton<GameBalanceBlob>();
        var statsTable = gameBalance.Data.Value.EnemyStats;

        // 2. 각 몬스터 Entity 처리
        foreach (var (monster, transform) in
                 SystemAPI.Query<RefRO<MonsterComponent>, RefRW<LocalTransform>>())
        {
            // 3. Blob에서 기본 스탯 가져오기
            var baseStats = statsTable[(int)monster.ValueRO.Type];

            // 4. 개별 데이터(레벨)와 결합
            float actualHealth = baseStats.BaseHealth * (1 + monster.ValueRO.Level * 0.1f);

            // 5. Entity별로 다르게 적용
            transform.ValueRW.Scale = actualHealth / 100f; // 체력에 따라 크기 조절
        }
    }
}
```

---

## 실전 사용 예시

### 예시 1: 게임 밸런스 테이블

```csharp
// 1. 데이터 구조
public struct GameBalanceBlob
{
    public BlobArray<CharacterStats> Characters;
    public BlobArray<WeaponStats> Weapons;
    public BlobArray<float> LevelExpTable;
    public BlobArray<DropRate> GlobalDropRates;
}

public struct CharacterStats
{
    public float BaseHealth;
    public float BaseDamage;
    public float BaseSpeed;
    public BlobString Name;
}

// 2. Baker에서 생성
class GameBalanceBaker : Baker<GameBalanceAuthoring>
{
    public override void Bake(GameBalanceAuthoring authoring)
    {
        var builder = new BlobAssetBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<GameBalanceBlob>();

        // 캐릭터 데이터 로드
        var characterData = Resources.LoadAll<CharacterData>("Characters");
        root.Characters = builder.Allocate(ref root.Characters, characterData.Length);

        for (int i = 0; i < characterData.Length; i++)
        {
            root.Characters[i] = new CharacterStats
            {
                BaseHealth = characterData[i].Health,
                BaseDamage = characterData[i].Damage,
                BaseSpeed = characterData[i].Speed,
                Name = builder.Allocate(characterData[i].Name)  // 문자열 할당
            };
        }

        AddComponent(builder.CreateBlobAssetReference<GameBalanceBlob>(Allocator.Persistent));
    }
}

// 3. System에서 사용
public partial struct CharacterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var balanceData = SystemAPI.GetSingleton<GameBalanceBlob>();

        foreach (var (character, health) in
                 SystemAPI.Query<RefRO<CharacterComponent>, RefRW<Health>>())
        {
            var stats = balanceData.Data.Value.Characters[character.ValueRO.CharacterId];
            health.ValueRO.Value = stats.BaseHealth;
        }
    }
}
```

### 예시 2: 메시 데이터 (정점/인덱스 버퍼)

```csharp
// 1. 메시 데이터 구조
public struct MeshDataBlob
{
    public BlobArray<float3> Vertices;
    public BlobArray<int> Indices;
    public BlobArray<float3> Normals;
    public BlobArray<float2> UVs;
    public int VertexCount;
    public int TriangleCount;
}

// 2. Baker에서 Unity 메시를 Blob으로 변환
class MeshDataBaker : Baker<MeshAuthoring>
{
    public override void Bake(MeshAuthoring authoring)
    {
        var mesh = authoring.GetComponent<MeshFilter>().sharedMesh;

        var builder = new BlobAssetBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<MeshDataBlob>();

        // 정점 데이터 복사
        var vertices = mesh.vertices;
        root.Vertices = builder.Allocate(ref root.Vertices, vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            root.Vertices[i] = vertices[i];
        }

        // 인덱스 데이터 복사
        var indices = mesh.triangles;
        root.Indices = builder.Allocate(ref root.Indices, indices.Length);
        for (int i = 0; i < indices.Length; i++)
        {
            root.Indices[i] = indices[i];
        }

        root.VertexCount = vertices.Length;
        root.TriangleCount = indices.Length / 3;

        AddComponent(builder.CreateBlobAssetReference<MeshDataBlob>(Allocator.Persistent));
    }
}

// 3. System에서 메시 데이터 사용 (커스텀 렌더링 등)
public partial struct CustomRenderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (meshData, transform) in
                 SystemAPI.Query<RefRO<MeshDataComponent>, RefRO<LocalTransform>>())
        {
            var mesh = meshData.ValueRO.Data.Value;

            // 메시 데이터로 렌더링
            for (int i = 0; i < mesh.TriangleCount; i++)
            {
                int i0 = mesh.Indices[i * 3 + 0];
                int i1 = mesh.Indices[i * 3 + 1];
                int i2 = mesh.Indices[i * 3 + 2];

                float3 v0 = mesh.Vertices[i0];
                float3 v1 = mesh.Vertices[i1];
                float3 v2 = mesh.Vertices[i2];

                // 렌더링 로직...
            }
        }
    }
}
```

### 예시 3: AI 경로 데이터 (그래프)

```csharp
// 1. 그래프 노드 구조
public struct Node
{
    public float3 Position;
    public BlobArray<int> Neighbors;  // 연결된 노드 인덱스
    public float Cost;  // 이동 비용
}

// 2. 그래프 데이터
public struct NavigationGraphBlob
{
    public BlobArray<Node> Nodes;
    public int NodeCount;
}

// 3. Baker에서 그래프 생성
class NavGraphBaker : Baker<NavGraphAuthoring>
{
    public override void Bake(NavGraphAuthoring authoring)
    {
        var nodes = authoring.GetComponentsInChildren<NodeAuthoring>();

        var builder = new BlobAssetBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<NavigationGraphBlob>();

        root.Nodes = builder.Allocate(ref root.Nodes, nodes.Length);
        root.NodeCount = nodes.Length;

        for (int i = 0; i < nodes.Length; i++)
        {
            root.Nodes[i].Position = nodes[i].transform.position;
            root.Nodes[i].Cost = nodes[i].MovementCost;

            // 이웃 노드 할당
            var neighbors = nodes[i].Neighbors;
            root.Nodes[i].Neighbors = builder.Allocate(
                ref root.Nodes[i].Neighbors,
                neighbors.Length
            );

            for (int j = 0; j < neighbors.Length; j++)
            {
                root.Nodes[i].Neighbors[j] = neighbors[j].NodeIndex;
            }
        }

        AddComponent(builder.CreateBlobAssetReference<NavigationGraphBlob>(Allocator.Persistent));
    }
}

// 4. System에서 경로 탐색 (A* 알고리즘)
public partial struct PathFindingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var navGraph = SystemAPI.GetSingleton<NavigationGraphBlob>();
        var graph = navGraph.Data.Value;

        foreach (var (agent, target) in
                 SystemAPI.Query<RefRW<AgentComponent>, RefRO<TargetComponent>>())
        {
            // A* 알고리즘으로 경로 탐색
            var path = FindPath(
                graph.Nodes,
                agent.ValueRO.CurrentNode,
                target.ValueRO.TargetNode
            );

            agent.ValueRW.Path = path;
        }
    }

    NativeList<int> FindPath(BlobArray<Node> nodes, int start, int goal)
    {
        // A* 구현 (Blob 데이터로부터 읽기)
        var path = new NativeList<int>(Allocator.Temp);

        // ... 경로 탐색 로직 ...

        return path;
    }
}
```

---

## 주의사항 및 모범 사례

### ✅ 권장 사항

#### 1. Blob Asset 사용 기준

```csharp
// ✅ Blob Asset에 적합한 데이터
- 배열 크기가 100개 이상인 데이터
- 읽기 전용 데이터 (설정, 테이블, 스탯)
- 여러 Entity가 공유하는 데이터
- 메시, 텍스처 같은 대용량 리소스 데이터

// ❌ Blob Asset에 부적합한 데이터
- 각 Entity마다 다른 데이터 (위치, 회전, 현재 체력)
- 자주 변경되는 데이터 (쿨다운, 임시 버프)
- 크기가 작은 데이터 (10개 미만의 배열)
```

#### 2. 메모리 관리

```csharp
public partial class GameManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Singleton으로 Blob Asset 관리
        if (!HasSingleton<GameBalanceBlob>())
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new GameBalanceBlob());
        }
    }

    protected override void OnDestroy()
    {
        // 게임 종료 시 반드시 해제
        if (HasSingleton<GameBalanceBlob>())
        {
            var blob = GetSingleton<GameBalanceBlob>();
            blob.Data.Dispose(); // 메모리 누수 방지
        }
    }
}
```

#### 3. 데이터 구조 최적화

```csharp
// ❌ 비효율적: 구조체 내에 포인터나 참조 타입
public struct BadBlobData
{
    public GameObject EnemyPrefab;  // ❌ GameObject는 사용 불가
    public Texture2D Icon;          // ❌ Texture2D는 사용 불가
    public List<int> Items;         // ❌ List는 사용 불가
}

// ✅ 효율적: Blob 전용 타입만 사용
public struct GoodBlobData
{
    public Entity EnemyPrefabRef;   // ✅ Entity 참조
    public BlobString IconPath;     // ✅ BlobString
    public BlobArray<int> Items;    // ✅ BlobArray
}
```

#### 4. 빌더 할당자 선택

```csharp
// ✅ 빌드 타임에는 Temp (빠름)
var builder = new BlobAssetBuilder(Allocator.Temp);
// ... 데이터 채우기 ...
var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);

// ⚠️ 런타임에 생성하면 느림
// 런타임에 Blob Asset을 생성해야 한다면, TempJob 사용
var builder = new BlobAssetBuilder(Allocator.TempJob);
// ... 데이터 채우기 ...
var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);
builder.Dispose(); // TempJob 사용 후 해제 필요
```

### ❌ 피해야 할 실수

#### 1. Blob 데이터 수정 시도

```csharp
// ❌ 컴파일 에러: Blob 데이터는 수정 불가
public void OnUpdate(ref SystemState state)
{
    var blob = SystemAPI.GetSingleton<MyBlob>();
    blob.Value.DataArray[0] = newValue; // 에러!
}

// ✅ 올바른 방법: 새로운 Blob 생성
public void UpdateBlobData(ref SystemState state)
{
    var oldBlob = SystemAPI.GetSingletonRW<MyBlob>();

    var builder = new BlobAssetBuilder(Allocator.Temp);
    ref var newRoot = ref builder.ConstructRoot<MyData>();

    // 데이터 복사 및 수정
    for (int i = 0; i < oldBlob.ValueRO.Data.Value.DataArray.Length; i++)
    {
        newRoot.DataArray[i] = oldBlob.ValueRO.Data.Value.DataArray[i];
    }
    newRoot.DataArray[0] = newValue;

    // 새로운 Blob 생성
    var newBlob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);

    // 기존 Blob 해제
    oldBlob.ValueRW.Data.Dispose();
    oldBlob.ValueRW.Data = newBlob;
}
```

#### 2. 메모리 누수 (Dispose 누락)

```csharp
// ❌ 메모리 누수: Blob Asset 해제 안 함
public partial struct BadSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        // 아무것도 하지 않음 → 메모리 누수!
    }
}

// ✅ 올바른 해제
public partial struct GoodSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<MyBlob>())
        {
            var blob = SystemAPI.GetSingleton<MyBlob>();
            blob.Data.Dispose(); // 필수!
        }
    }
}
```

#### 3. 잘못된 데이터 타입 사용

```csharp
// ❌ Blob에서 사용 불가한 타입
public struct InvalidBlobData
{
    public string Text;         // ❌ string 대신 BlobString 사용
    public int[] Array;         // ❌ C# 배열 대신 BlobArray<int> 사용
    public List<int> List;      // ❌ List는 사용 불가
    public GameObject Obj;      // ❌ GameObject는 사용 불가
}

// ✅ 올바른 타입
public struct ValidBlobData
{
    public BlobString Text;     // ✅
    public BlobArray<int> Array; // ✅
    public Entity EntityRef;    // ✅
}
```

#### 4. 과도한 Blob Asset 생성

```csharp
// ❌ 매 프레임 Blob Asset 생성 (성능 저하)
public partial struct BadSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var builder = new BlobAssetBuilder(Allocator.Temp);
        // ... 데이터 채우기 ...
        var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);
        // 해제도 안 함 → 심각한 메모리 누수!
    }
}

// ✅ 빌드 타임에 한 번만 생성
class MyBaker : Baker<MyAuthoring>
{
    public override void Bake(MyAuthoring authoring)
    {
        var builder = new BlobAssetBuilder(Allocator.Temp);
        // ... 데이터 채우기 ...
        var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);
        AddComponent(blob);
    }
}

// 또는 런타임에 한 번만 생성
public partial struct GoodSystem : ISystem
{
    private bool initialized = false;

    public void OnUpdate(ref SystemState state)
    {
        if (!initialized)
        {
            var builder = new BlobAssetBuilder(Allocator.Temp);
            // ... 데이터 채우기 ...
            var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new MyComponent { Data = blob });

            initialized = true;
        }
    }
}
```

---

## 자주 묻는 질문

### Q1: Blob Asset과 NativeContainer의 차이는 무엇인가요?

**A:** 주요 차이점은 다음과 같습니다:

| 특징 | Blob Asset | NativeContainer |
|------|-----------|----------------|
| **변경 가능성** | ❌ 불변 (Immutable) | ✅ 가변 (Mutable) |
| **메모리 위치** | 별도 Blob 힙 | 일반 힙 |
| **사용 용도** | 대용량 읽기 전용 데이터 | 계산용 임시 데이터 |
| **수명 관리** | 수동 (Dispose) | 자동 (Using/Dispose) |
| **스레드 안전성** | 완전히 안전 | 필요시 동기화 |

**사용 가이드:**
```
데이터가 절대 변경되지 않나? → YES → Blob Asset
데이터를 자주 수정해야 하나? → YES → NativeContainer
```

### Q2: Blob Asset 내부에 포인터를 저장할 수 있나요?

**A:** 네, **BlobPtr<T>**를 사용하면 됩니다:

```csharp
public struct Node
{
    public int Value;
    public BlobPtr<Node> Next;  // 다른 노드를 가리킴
}

public struct LinkedList
{
    public BlobPtr<Node> Head;
    public int Count;
}

// 사용법
var current = linkedList.Head;
while (current.IsValid)  // 유효한 포인터인지 확인
{
    var node = current.Value;  // 역참조
    // node.Value 사용...

    current = node.Next;  // 다음 노드로 이동
}
```

### Q3: Blob Asset의 크기 제한이 있나요?

**A:** 이론적인 제한은 거의 없지만, 실제로는 다음을 고려해야 합니다:

- **권장 크기**: 수백 MB 이하
- **최대 크기**: 시스템 메모리에 따라 다름 (수 GB 가능)
- **성능 고려사항**:
  - 너무 큰 Blob Asset은 로딩 시간 증가
  - 여러 개의 작은 Blob Asset으로 분리 고려
  - 자주 접근하는 데이터는 앞쪽에 배치

### Q4: 런타임에 Blob Asset을 생성할 수 있나요?

**A:** 네, 가능하지만 **권장하지 않습니다**:

```csharp
// ⚠️ 가능하지만 느림 (빌드 타임이 좋음)
public void CreateBlobRuntime()
{
    var builder = new BlobAssetBuilder(Allocator.Temp);
    ref var root = ref builder.ConstructRoot<MyData>();

    // 데이터 채우기...

    var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);
}

// ✅ 빌드 타임에 생성 (권장)
class MyBaker : Baker<MyAuthoring>
{
    public override void Bake(MyAuthoring authoring)
    {
        var builder = new BlobAssetBuilder(Allocator.Temp);
        // ... 데이터 채우기 ...
        var blob = builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);
        AddComponent(blob);
    }
}
```

**런타임 생성이 필요한 경우:**
- 로딩 시간에 큰 영향을 주지 않음
- 데이터를 파일에서 로드해야 할 때
- 프로시저럴 생성된 데이터

### Q5: Blob Asset을 여러 Entity가 공유할 수 있나요?

**A:** 네, **Blob Asset의 핵심 목적**입니다:

```csharp
// 1. Singleton으로 Blob Asset 저장
public struct GameBalanceBlob : IComponentData
{
    public BlobAssetReference<BalanceData> Data;
}

// 2. 수천 개의 Entity가 같은 데이터 참조
public partial struct MonsterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 모든 몬스터가 같은 Blob Asset 참조
        var balanceData = SystemAPI.GetSingleton<GameBalanceBlob>();

        foreach (var (monster, health) in
                 SystemAPI.Query<RefRO<MonsterComponent>, RefRW<Health>>())
        {
            // 같은 Blob 데이터에서 기본 스탯 읽기
            var baseStats = balanceData.Data.Value.MonsterStats[monster.ValueRO.Type];
            health.ValueRW.Value = baseStats.BaseHealth;
        }
    }
}
```

### Q6: Blob Asset을 디스크에 저장했다가 다시 로드할 수 있나요?

**A:** 직접적인 방법은 없지만, 다음 방법들을 사용할 수 있습니다:

1. **SubScene 사용 (권장)**: Baker에서 생성한 Blob Asset은 SubScene에 자동 저장
2. **직렬화**: Blob 데이터를 파일로 저장 후 런타임에 다시 Blob로 변환
3. **Addressables**: Blob Asset 데이터를 JSON/Binary로 저장 후 로드

```csharp
// 방법 2: JSON으로 저장 후 다시 로드
// 저장
public void SaveBlobToJson(BlobAssetReference<MyData> blob)
{
    var data = blob.Value;
    var json = JsonUtility.ToJson(data);
    File.WriteAllText("data.json", json);
}

// 로드
public BlobAssetReference<MyData> LoadBlobFromJson()
{
    var json = File.ReadAllText("data.json");
    var data = JsonUtility.FromJson<MyData>(json);

    var builder = new BlobAssetBuilder(Allocator.Temp);
    ref var root = ref builder.ConstructRoot<MyData>();
    // ... 데이터 복사 ...
    return builder.CreateBlobAssetReference<MyData>(Allocator.Persistent);
}
```

---

## 요약

### Blob Asset 핵심 포인트

1. **목적**: 대용량 불변(Immutable) 데이터의 효율적 관리
2. **장점**: 메모리 공유, 스레드 안전, 캐시 친화적
3. **사용처**: 메시, 텍스처, 테이블, 그래프 등 읽기 전용 대용량 데이터
4. **주의**: 수동 메모리 관리(Dispose), 불변성, 런타임 생성 비용

### SubScene과 함께 사용할 때의 장점

```
SubScene + Blob Asset = 완벽한 데이터 관리

• SubScene: Entity 구조 및 개별 데이터
• Blob Asset: 대용량 공유 데이터
• 결과: 메모리 효율 + 성능 최적화
```

### 결정 트리

```
대용량 데이터(100+ 요소)가 있는가?
├── YES
│   ├── 데이터가 절대 변경되지 않는가?
│   │   ├── YES → Blob Asset 사용 ✅
│   │   └── NO → NativeArray/Stream 사용
│   └── 여러 Entity가 공유하는가?
│       ├── YES → Blob Asset 사용 ✅
│       └── NO → 일반 ComponentData 사용
└── NO
    ├── Entity마다 다른가? → ComponentData
    └── 자주 변경되는가? → ComponentData
```

### SubScene vs Blob Asset 비교

| 측면 | SubScene | Blob Asset |
|-----|----------|-----------|
| **주요 목적** | GameObject → Entity 변환 | 대용량 불변 데이터 저장 |
| **데이터 유형** | Entity + Component | 대용량 배열, 구조체 |
| **메모리 위치** | Chunk | 별도 Blob 힙 |
| **변경 가능성** | Entity마다 다름 | 전체 불변 |
| **수명 주기** | Scene 로드/언로드 | 수동 관리 |
| **함께 사용** | ✅ 권장 | ✅ 권장 |

---

## 참고 자료

- [Unity Entities Package 문서 - Blob Asset](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/blob_assets.html)
- **SubScene 가이드**: [SubScene 완전 가이드](./knowledge_subscene.md) (Blob Asset과 함께 사용)
- [Unity DOTS Samples - Blob Assets](https://github.com/Unity-Technologies/EntityComponentSystemSamples)
- [Unity Blog - Understanding Blob Assets](https://blog.unity.com/technology/data-oriented-design)

---

**마지막 업데이트**: 2026-01-14
**Unity 버전**: 6000.1.7f1
**Entities 패키지**: 1.3.x 이상
