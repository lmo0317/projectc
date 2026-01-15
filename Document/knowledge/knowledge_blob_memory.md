# Unity DOTS BlobHeap 완전 가이드

## 📚 목차

1. [BlobHeap이란?](#blobheap이란)
2. [전체 메모리 구조](#전체-메모리-구조)
3. [BlobHeap의 특징](#blobheap의-특징)
4. [BlobHeap 내부 구조](#blobheap-내부-구조)
5. [BlobHeap과 다른 메모리의 상호 작용](#blobheap과-다른-메모리의-상호-작용)
6. [BlobHeap의 생명주기](#blobheap의-생명주기)
7. [코드로 보는 BlobHeap 사용](#코드로-보는-blobheap-사용)
8. [BlobHeap의 메모리 최적화](#blobheap의-메모리-최적화)
9. [실전 사용 예시](#실전-사용-예시)
10. [요약](#요약)

---

## BlobHeap이란?

**BlobHeap**은 Unity DOTS에서 **Blob Asset 전용 메모리 영역**입니다.

### 핵심 개념

```
💡 BlobHeap = Blob Asset만을 위한 특별한 창고

일반 메모리와 분리된 별도의 공간에서:
- 대용량 데이터 저장
- 읽기 전용(Immutable) 데이터 관리
- 효율적인 메모리 할당/해제
```

### 왜 별도의 메모리 공간이 필요한가?

```
🎯 문제: ECS Chunk는 작음 (16KB 제한)
📦 Mesh, Texture, 테이블 데이터는 큼 (수 MB ~ 수 GB)
💡 해결: 별도의 BlobHeap에 대용량 데이터 저장
```

---

## 전체 메모리 구조

```
┌─────────────────────────────────────────────────────────────┐
│                 Unity DOTS 메모리 맵                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  🏛️ 시스템 메모리 (System Memory)                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Unity Editor, OS, 다른 프로세스                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ────────────────────────────────────────────────────────   │
│                                                             │
│  🎮 Unity 프로세스 메모리                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                                       │   │
│  │  📦 Managed Heap (.NET/C#)                          │   │
│  │  ┌─────────────────────────────────────────────┐    │   │
│  │  │ C# Objects, Strings, Classes                │    │   │
│  │  │ - Garbage Collection 관리                   │    │   │
│  │  │ - 속도: 느림                                 │    │   │
│  │  └─────────────────────────────────────────────┘    │   │
│  │                                                       │   │
│  │  🧱 Native Heap (Unmanaged)                         │   │
│  │  ┌─────────────────────────────────────────────┐    │   │
│  │  │ Unity Engine, Physics, Graphics             │    │   │
│  │  └─────────────────────────────────────────────┘    │   │
│  │                                                       │   │
│  │  🔷 ECS Chunk Memory (Archetype Chunk)              │   │
│  │  ┌─────────────────────────────────────────────┐    │   │
│  │  │ Entity의 컴포넌트 데이터 저장                │    │   │
│  │  │                                             │    │   │
│  │  │ Chunk_0:                                    │    │   │
│  │  │ ┌─────────────────────────────────────┐    │    │   │
│  │  │ │ Entity_1000: [Transform][Health]    │    │    │   │
│  │  │ │ Entity_1001: [Transform][Health]    │    │    │   │
│  │  │ │ Entity_1002: [Transform][Health]    │    │    │   │
│  │  │ │ ... (16KB 크기)                      │    │    │   │
│  │  │ └─────────────────────────────────────┘    │    │   │
│  │  │                                             │    │   │
│  │  │ Chunk_1:                                    │    │   │
│  │  │ ┌─────────────────────────────────────┐    │    │   │
│  │  │ │ Entity_2000: [Transform][Physics]   │    │    │   │
│  │  │ │ Entity_2001: [Transform][Physics]   │    │    │   │
│  │  │ └─────────────────────────────────────┘    │    │   │
│  │  └─────────────────────────────────────────────┘    │   │
│  │                                                       │   │
│  │  💎 BLOB HEAP (BlobAsset 전용) ← 이게 핵심!          │   │
│  │  ┌─────────────────────────────────────────────┐    │   │
│  │  │                                             │    │   │
│  │  │ BlobAsset_0: SphereCollider                 │    │   │
│  │  │ ├─ Geometry: { Center, Radius }             │    │   │
│  │  │ ├─ Filter: { BelongsTo, CollidesWith }      │    │   │
│  │  │ └─ Material: { CollisionResponse }          │    │   │
│  │  │           (128 bytes)                        │    │   │
│  │  │                                             │    │   │
│  │  │ BlobAsset_1: BoxCollider                    │    │   │
│  │  │ ├─ Geometry: { Center, Size, Orientation }  │    │   │
│  │  │ └─ ... (256 bytes)                          │    │   │
│  │  │                                             │    │   │
│  │  │ BlobAsset_2: MeshData                       │    │   │
│  │  │ ├─ Vertices: [v1, v2, ..., v50000]          │    │   │
│  │  │ ├─ Indices: [i1, i2, ..., i150000]         │    │   │
│  │  │ └─ ... (수 MB)                              │    │   │
│  │  │                                             │    │   │
│  │  │ BlobAsset_3: GameBalanceData               │    │   │
│  │  │ └─ ... (수 KB ~ 수 MB)                      │    │   │
│  │  │                                             │    │   │
│  │  └─────────────────────────────────────────────┘    │   │
│  │                                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## BlobHeap의 특징

### 1. 다른 메모리와의 비교

| 특징 | Managed Heap | ECS Chunk Memory | **BlobHeap** |
|-----|-------------|------------------|-------------|
| **관리 방식** | Garbage Collection | Archetype Manager | 수동 (Dispose) |
| **데이터 타입** | C# Classes/Objects | IComponentData 구조체 | Blob Asset 구조체 |
| **변경 가능성** | ✅ 가능 | ✅ 가능 | ❌ 불변 (Immutable) |
| **데이터 크기** | 가변 | 제한적 (~16KB/Chunk) | 매우 큼 (GB까지) |
| **할당 속도** | 느림 | 빠름 | 중간 |
| **접근 속도** | 느림 (Indirect) | 빠름 (Sequential) | 매우 빠름 (Cache-friendly) |
| **메모리 위치** | Heap | Heap | 별도 Heap |
| **주요 용도** | 일반 객체 | Entity 데이터 | 대용량 불변 데이터 |

### 2. BlobHeap만의 장점

```
✅ 1. 불변성 (Immutability)
   - 한 번 생성되면 변경 불가
   - 여러 스레드가 동시에 읽어도 안전
   - 데이터 경합(Race Condition) 없음

✅ 2. 캐시 친화적 (Cache-Friendly)
   - 데이터가 연속적으로 배치
   - CPU 캐시 적중률 높음
   - 메모리 프리패칭 최적화

✅ 3. 메모리 효율성
   - 데이터 조각화(Fragmentation) 최소화
   - 불필요한 복사 없음
   - 여러 Entity가 공유 가능

✅ 4. 대용량 데이터 지원
   - 수 MB ~ 수 GB 데이터 저장 가능
   - Chunk의 16KB 제한 없음
```

---

## BlobHeap 내부 구조

```
┌─────────────────────────────────────────────────────────────┐
│                    BlobHeap 내부 구조                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  BlobAllocator (할당자)                               │ │
│  │  - 메모리 블록 관리                                    │ │
│  │  - 할당/해제 요청 처리                                 │ │
│  └───────────────────────────────────────────────────────┘ │
│                        ↓                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  Memory Blocks (메모리 블록)                          │ │
│  │                                                       │ │
│  │  Block_0 (16 MB)                                     │ │
│  │  ┌──────────────────────────────────────────────┐   │ │
│  │  │ BlobAsset_0 (128 bytes)                      │   │ │
│  │  │ ↓                                            │   │ │
│  │  │ BlobAsset_1 (256 bytes)                      │   │ │
│  │  │ ↓                                            │   │ │
│  │  │ BlobAsset_2 (5 MB)                           │   │ │
│  │  │ ↓                                            │   │ │
│  │  │ Free Space (10 MB)                           │   │ │
│  │  └──────────────────────────────────────────────┘   │ │
│  │                                                       │ │
│  │  Block_1 (16 MB)  ← Block_0가 꽉 차면 새로 할당      │ │
│  │  ┌──────────────────────────────────────────────┐   │ │
│  │  │ BlobAsset_100 (2 MB)                         │   │ │
│  │  │ ↓                                            │   │ │
│  │  │ Free Space (14 MB)                           │   │ │
│  │  └──────────────────────────────────────────────┘   │ │
│  └───────────────────────────────────────────────────────┘ │
│                        ↓                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  Free List (빈 공간 관리)                             │ │
│  │  - 해제된 BlobAsset의 공간을 추적                    │ │
│  │  - 재할당에 사용                                     │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## BlobHeap과 다른 메모리의 상호 작용

### 1. 데이터 흐름

```
🔄 데이터 생성 → 저장 → 사용 과정

1️⃣ Baker 실행 (Editor, Import Time)
   ↓
   Temporary Memory (임시 메모리)
   └── BlobBuilder가 임시로 데이터 생성

2️⃣ BlobAsset 생성
   ↓
   BlobHeap
   └── 데이터를 복사해서 BlobHeap에 저장

3️⃣ BlobAssetReference 생성
   ↓
   ECS Chunk Memory
   └── Entity의 컴포넌트에 Reference (8 bytes) 저장

4️⃣ 런타임 사용
   ↓
   System이 참조를 통해 BlobHeap의 데이터 읽기
```

### 2. 실제 메모리 배치 예시

```
🎮 100개의 고블린이 있는 게임

// 1️⃣ ECS Chunk Memory (Entity 컴포넌트)
Chunk_0 (16 KB):
┌──────────────────────────────────────────┐
│ Entity_1000:                             │
│  ├─ LocalTransform (Position: 10,0,5)    │  32 bytes
│  ├─ EnemyHealth: 100                     │   4 bytes
│  ├─ EnemySpeed: 3                        │   4 bytes
│  └─ PhysicsCollider                      │   8 bytes
│     └─ Value: BlobAssetReference         │
│        └─ Ptr: 0x7F8A2B1000 (BlobHeap)   │ ← BlobHeap을 가리킴
│                                           │
│ Entity_1001:                             │
│  ├─ LocalTransform (Position: 15,0,8)    │  32 bytes
│  ├─ EnemyHealth: 100                     │   4 bytes
│  └─ PhysicsCollider                      │   8 bytes
│     └─ Value: BlobAssetReference         │
│        └─ Ptr: 0x7F8A2B1000 ← 같은 주소! │
│                                           │
│ ... (더 많은 Entity)                       │
└──────────────────────────────────────────┘

// 2️⃣ BlobHeap (공유 데이터)
BlobHeap:
┌──────────────────────────────────────────┐
│ BlobAsset_0 @ 0x7F8A2B1000               │
│ └─ SphereCollider:                      │
│    ├─ Geometry:                         │
│    │  ├─ Center: (0, 0, 0)              │
│    │  └─ Radius: 1.0                    │
│    ├─ Filter:                           │
│    │  ├─ BelongsTo: 4                   │
│    │  └─ CollidesWith: 3                │
│    └─ Material:                          │
│       └─ CollisionResponse: None         │
│          (총 128 bytes)                  │
│                                           │
│ BlobAsset_1 @ 0x7F8A2B1080               │
│ └─ GameBalanceData:                     │
│    ├─ Enemies[100]: ...                  │
│    └─ LevelExp[100]: ...                │
│          (총 50 KB)                      │
└──────────────────────────────────────────┘
```

---

## BlobHeap의 생명주기

### SubScene과 BlobHeap의 저장/로딩 과정

```
┌─────────────────────────────────────────────────────────────┐
│          SubScene & BlobAsset 생명주기                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1️⃣ Editor에서 작업                                         │
│     └── GameObject 배치, Collider 설정                      │
│                                                             │
│  2️⃣ SubScene 저장 (Ctrl+S)                                 │
│     └── Baker 실행 → GameObject → Entity 변환               │
│     └── .entities 파일에 저장                               │
│         ├── Entity의 컴포넌트 데이터                        │
│         └── BlobAsset 데이터도 함께 저장!                   │
│                                                             │
│  3️⃣ 게임 실행 (Play Mode 또는 Build)                       │
│     └── .entities 파일을 읽어서 로드                        │
│     └── BlobAsset도 메모리에 복원                           │
│     └── Entity이 생성되어 게임에서 실행                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 상세 생명주기

```
┌─────────────────────────────────────────────────────────────┐
│                  BlobHeap 생명주기                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1️⃣ Editor - SubScene 저장                                 │
│      ↓                                                      │
│  Baker.Bake() 실행                                          │
│      ↓                                                      │
│  BlobBuilder로 임시 데이터 생성 (Allocator.Temp)            │
│      ↓                                                      │
│  BlobAssetReference.Create() 호출                           │
│      ↓                                                      │
│  ┌──────────────────────────────────────────────┐          │
│  │ 💾 BlobHeap에 메모리 할당                    │          │
│  │ - 데이터를 BlobHeap으로 복사                 │          │
│  │ - BlobAssetReference 반환                    │          │
│  └──────────────────────────────────────────────┘          │
│      ↓                                                      │
│  .entities 파일에 직렬화 (Serialize)                       │
│                                                             │
│  2️⃣ Runtime - 게임 실행 (Play Mode 또는 Build)            │
│      ↓                                                      │
│  SubScene 로딩                                              │
│      ↓                                                      │
│  .entities 파일에서 읽기                                    │
│      ↓                                                      │
│  ┌──────────────────────────────────────────────┐          │
│  │ 📦 BlobHeap에 메모리 재할당                   │          │
│  │ - 직렬화된 데이터를 BlobHeap에 복원           │          │
│  │ - 각 BlobAssetReference에 포인터 연결        │          │
│  └──────────────────────────────────────────────┘          │
│      ↓                                                      │
│  System에서 BlobAsset 사용                                  │
│      ↓                                                      │
│  SubScene 언로드 또는 게임 종료                             │
│      ↓                                                      │
│  ┌──────────────────────────────────────────────┐          │
│  │ 🗑️ BlobAsset.Dispose()                       │          │
│  │ - BlobHeap 메모리 해제                        │          │
│  │ - 포인터 무효화                               │          │
│  └──────────────────────────────────────────────┘          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 코드로 보는 BlobHeap 사용

### 1. Baker에서 BlobAsset 생성

```csharp
class Baker : Baker<GameBalanceAuthoring>
{
    public override void Bake(GameBalanceAuthoring authoring)
    {
        // 1. BlobBuilder 생성 (Temp Allocator - 임시 메모리)
        var builder = new BlobAssetBuilder(Allocator.Temp);

        // 2. 임시 메모리에서 데이터 구조화
        ref var root = ref builder.ConstructRoot<MyGameData>();
        root.Enemies = builder.Allocate(ref root.Enemies, 100);

        // 데이터 채우기...
        for (int i = 0; i < 100; i++)
        {
            root.Enemies[i] = new EnemyStats { Health = 100, Damage = 10 };
        }

        // 3. BlobAssetReference 생성
        // 🔥 여기서 BlobHeap에 할당됨!
        var blobRef = builder.CreateBlobAssetReference<MyGameData>(
            Allocator.Persistent  // BlobHeap에 영구 저장
        );

        // 4. Entity에 Reference 추가
        // 실제 데이터는 BlobHeap에, 참조만 Chunk에 저장
        AddComponent(blobRef);

        // 5. Temp 메모리 해제 (Builder는 더 이상 필요 없음)
        builder.Dispose();
    }
}
```

**내부 메모리 동작:**
```
1. builder.ConstructRoot()
   → Temp Allocator에 임시 메모리 할당

2. builder.CreateBlobAssetReference()
   → BlobHeap에 메모리 할당
   → Temp 데이터를 BlobHeap으로 복사
   → BlobAssetReference 반환

3. builder.Dispose()
   → Temp 메모리 해제
   → 이제 BlobHeap의 데이터만 남음
```

### 2. System에서 BlobAsset 사용

```csharp
public partial struct GameSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. BlobAssetReference 가져오기
        var gameData = SystemAPI.GetSingleton<GameDataBlob>();

        // 2. Reference를 통해 BlobHeap의 데이터 접근
        // 🚀 매우 빠름 (직접 메모리 접근)
        var blobData = gameData.Data.Value;

        // 3. 데이터 읽기
        foreach (var enemy in blobData.Enemies)
        {
            // BlobHeap에서 직접 읽기
            var health = enemy.BaseHealth;

            // 데이터 사용...
        }
    }

    public void OnDestroy(ref SystemState state)
    {
        // 4. 게임 종료 시 BlobHeap에서 해제
        if (SystemAPI.HasSingleton<GameDataBlob>())
        {
            var blob = SystemAPI.GetSingleton<GameDataBlob>();
            blob.Data.Dispose();  // 🔥 BlobHeap 메모리 해제
        }
    }
}
```

### 3. BlobAssetReference 사용법

```csharp
// BlobAssetReference를 통한 데이터 접근

// 1. Reference 가져오기
var blobRef = SystemAPI.GetSingleton<GameDataBlob>().Data;

// 2. .Value로 실제 Blob 데이터 접근
var blobData = blobRef.Value;  // ← ref readonly로 반환

// 3. 데이터 읽기 (수정 불가!)
foreach (var enemy in blobData.Enemies)
{
    Debug.Log($"Enemy Health: {enemy.Health}");
}

// 4. IsValid로 유효성 확인
if (blobRef.IsCreated)
{
    // BlobAsset이 유효함
}

// 5. 메모리 사용량 확인
var size = blobRef.m_length;  // BlobAsset 크기 (bytes)
```

---

## BlobHeap의 메모리 최적화

### 1. 메모리 정렬 (Alignment)

```
BlobHeap은 메모리 정렬을 최적화하여 성능 향상:

❌ 정렬 안 함:
[Data1: 3 bytes][Data2: 7 bytes][Data3: 5 bytes]
↓
CPU가 비효율적으로 읽음 (Cache miss)

✅ 정렬 함:
[Data1: 3 bytes][Pad: 5 bytes] → 8 bytes align
[Data2: 7 bytes][Pad: 1 byte]  → 8 bytes align
[Data3: 5 bytes][Pad: 3 bytes] → 8 bytes align
↓
CPU가 효율적으로 읽음 (Cache hit 증가)
```

### 2. 메모리 풀 (Memory Pool)

```
BlobHeap은 메모리 풀을 사용하여 할당/해제 최적화:

Free List (해제된 공간):
├── Block_0: 16 KB (BlobAsset_50이 해제됨)
├── Block_1: 32 KB (BlobAsset_75이 해제됨)
└── Block_2: 128 KB (BlobAsset_100이 해제됨)

새로운 BlobAsset 할당 시:
1. 적절한 크기의 Free Block 탐색
2. 재사용 (빠름!)
3. 없으면 새로운 Block 할당
```

### 3. 메모리 공유 효과

```
📊 메모리 절약 효과

❌ BlobAsset을 사용하지 않으면:
Entity_1000: [Collider 데이터 복사] 100 KB
Entity_1001: [Collider 데이터 복사] 100 KB
Entity_1002: [Collider 데이터 복사] 100 KB
...
Entity_2000: [Collider 데이터 복사] 100 KB

총 메모리: 1000개 × 100 KB = 100 MB 😱

✅ BlobAsset을 사용하면:
[BlobHeap] BlobAsset_5000 (100 KB) ← 실제 데이터

Entity_1000: [Reference] 8 bytes
Entity_1001: [Reference] 8 bytes
Entity_1002: [Reference] 8 bytes
...
Entity_2000: [Reference] 8 bytes

총 메모리: 100 KB + (1000개 × 8 bytes) = 100.008 KB ✅

절약율: 99.9%
```

---

## 실전 사용 예시

### 예시 1: 메모리 프로파일링

```csharp
#if UNITY_EDITOR
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class BlobHeapProfiler : MonoBehaviour
{
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;

        // 모든 PhysicsCollider의 BlobAsset 확인
        var query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<PhysicsCollider>()
        );

        var colliders = query.ToComponentDataArray<PhysicsCollider>(
            Unity.Collections.Allocator.Temp
        );

        ulong totalBlobMemory = 0;
        var uniqueBlobs = new System.Collections.Generic.HashSet<ulong>();

        foreach (var collider in colliders)
        {
            var ptr = collider.Value.m_ptr;
            uniqueBlobs.Add(ptr);

            // 각 BlobAsset의 크기 추정
            totalBlobMemory += EstimateBlobSize(collider.Value);
        }

        Debug.Log($"=== BlobHeap 사용 현황 ===");
        Debug.Log($"총 PhysicsCollider: {colliders.Length}");
        Debug.Log($"고유 BlobAsset: {uniqueBlobs.Count}");
        Debug.Log($"BlobHeap 총 메모리: {totalBlobMemory / 1024.0:F2} KB");
        Debug.Log($"평균 BlobAsset 크기: {totalBlobMemory / uniqueBlobs.Count} bytes");
        Debug.Log($"메모리 절약율: {(1 - (double)uniqueBlobs.Count / colliders.Length) * 100:F1}%");

        colliders.Dispose();
    }

    ulong EstimateBlobSize(Unity.Physics.Collider collider)
    {
        // Collider 타입에 따른 크기 추정
        switch (collider.Type)
        {
            case Unity.Physics.ColliderType.Sphere:
                return 128; // 예상 크기
            case Unity.Physics.ColliderType.Box:
                return 256;
            case Unity.Physics.ColliderType.Capsule:
                return 192;
            case Unity.Physics.ColliderType.Mesh:
                return 1024 * 100; // 100 KB
            default:
                return 128;
        }
    }
}
#endif
```

**출력 예시:**
```
=== BlobHeap 사용 현황 ===
총 PhysicsCollider: 1000
고유 BlobAsset: 3
BlobHeap 총 메모리: 172.50 KB
평균 BlobAsset 크기: 57500 bytes
메모리 절약율: 99.7%
```

### 예시 2: BlobAssetReference 비교

```csharp
// 같은 BlobAsset을 참조하는지 확인

public partial class BlobAssetDebugger : MonoBehaviour
{
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<PhysicsCollider>()
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var colliders = query.ToComponentDataArray<PhysicsCollider>(Unity.Collections.Allocator.Temp);

        Debug.Log($"총 {colliders.Length}개의 PhysicsCollider 발견");

        // BlobAsset의 메모리 주소 확인
        for (int i = 0; i < colliders.Length; i++)
        {
            var colliderRef = colliders[i].Value;

            Debug.Log($"Entity_{entities[i].Index}:");
            Debug.Log($"  BlobAssetPtr: {colliderRef.m_ptr}");
            Debug.Log($"  Collider Type: {colliderRef.Value.Type}");

            // 같은 BlobAsset을 참조하는지 확인
            for (int j = i + 1; j < colliders.Length; j++)
            {
                if (colliderRef.m_ptr == colliders[j].Value.m_ptr)
                {
                    Debug.Log($"  ⭐ Entity_{entities[j].Index}와 같은 BlobAsset 공유!");
                }
            }
        }

        entities.Dispose();
        colliders.Dispose();
    }
}
```

**출력 예시:**
```
총 100개의 PhysicsCollider 발견
Entity_1000:
  BlobAssetPtr: 1234567890
  Collider Type: Sphere
  ⭐ Entity_1001와 같은 BlobAsset 공유!
  ⭐ Entity_1002와 같은 BlobAsset 공유!
  ...
Entity_1001:
  BlobAssetPtr: 1234567890  ← 같은 주소!
  Collider Type: Sphere
```

### 예시 3: .entities 파일 구조 확인

```
📁 GameSceneSpaceSubscene.entities (Binary 파일)

┌─────────────────────────────────────────────────────────────┐
│                   .entities 파일 내부                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Header                                                      │
│  ├─ Scene GUID                                              │
│  └─ Entity Count: 100                                       │
│                                                             │
│  ─────────────────────────────────────────────────────      │
│                                                             │
│  Entity_1000 (Goblin_01)                                    │
│  ├─ Entity Version                                         │
│  ├─ Components:                                             │
│  │  ├─ LocalTransform                                      │
│  │  │  ├─ Position: (10, 0, 5)                            │
│  │  │  ├─ Rotation: (0, 0, 0, 1)                          │
│  │  │  └─ Scale: (1, 1, 1)                                │
│  │  ├─ EnemyHealth: 100                                    │
│  │  ├─ EnemySpeed: 3                                       │
│  │  └─ PhysicsCollider                                     │
│  │     └─ BlobAssetReference                              │
│  │        └─ BlobAssetIndex: 0  ← BlobAsset을 가리키는 인덱스 │
│                                                             │
│  Entity_1001 (Goblin_02)                                    │
│  ├─ LocalTransform: (15, 0, 8), ...                        │
│  ├─ EnemyHealth: 100                                       │
│  ├─ EnemySpeed: 3                                           │
│  └─ PhysicsCollider                                        │
│     └─ BlobAssetReference                                  │
│        └─ BlobAssetIndex: 0  ← 같은 BlobAsset 참조!         │
│                                                             │
│  ─────────────────────────────────────────────────────      │
│                                                             │
│  BlobAsset Section (실제 Blob 데이터 저장소)                │
│  ├─ BlobAsset_0:                                            │
│  │  ├─ Type: Unity.Physics.SphereCollider                 │
│  │  ├─ Size: 128 bytes                                    │
│  │  └─ Data:                                               │
│  │     ├─ Geometry: { Center: (0,0,0), Radius: 1.0 }      │
│  │     ├─ Filter: { BelongsTo: 4, CollidesWith: 3 }       │
│  │     └─ Material: { CollisionResponse: None }           │
│  │                                                           │
│  └─ (다른 BlobAsset들...)                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## BlobHeap 사용 가이드

### 1. 언제 BlobHeap을 사용해야 하는가?

```
✅ BlobAsset (BlobHeap) 사용 적합:
- 대용량 데이터 (수 KB ~ 수 GB)
- 읽기 전용 데이터 (설정, 테이블, 스탯)
- 여러 Entity가 공유하는 데이터
- 메시, 텍스처, 애니메이션 커브

❌ 일반 ComponentData 사용 적합:
- 각 Entity마다 다른 데이터 (위치, 회전, 현재 체력)
- 자주 변경되는 데이터 (쿨다운, 임시 버프)
- 크기가 작은 데이터 (16KB 이하)
```

### 2. BlobHeap vs 다른 메모리 사용 사례

```
🎯 언제 어느 메모리를 사용할까?

1️⃣ Managed Heap (.NET/C#)
   사용: 일반 C# 클래스, 문자열, 컬렉션
   예시: List<int>, Dictionary<string, int>, string

2️⃣ Native Heap (NativeArray)
   사용: 임시 계산용 데이터, 버퍼
   예시: NativeArray<float3>, NativeList<int>

3️⃣ ECS Chunk Memory
   사용: Entity의 컴포넌트 데이터
   예시: LocalTransform, EnemyHealth, PhysicsVelocity

4️⃣ BlobHeap ← 이거!
   사용: 대용량 불변 데이터, 공유 데이터
   예시: MeshData, GameBalanceData, Physics.Collider
```

### 3. 주의사항

```
⚠️ BlobHeap 사용 시 주의사항:

1️⃣ 수동 메모리 관리
   - Dispose() 호출 필수
   - 잊으면 메모리 누수

2️⃣ 불변성
   - 한 번 생성하면 수정 불가
   - 수정하려면 새로운 BlobAsset 생성

3️⃣ 참조 유효성
   - Dispose 후 참조하면 크래시
   - IsCreated로 유효성 확인

4️⃣ 스레드 안전성
   - 읽기는 안전
   - 쓰기는 불가 (불변이므로)
```

---

## 요약

```
🎯 BlobHeap 핵심 포인트:

1️⃣ 별도 메모리 공간
   - ECS Chunk, Managed Heap과 분리
   - Blob Asset 전용 힙

2️⃣ 불변성 (Immutable)
   - 한번 생성하면 수정 불가
   - 스레드 안전성 보장

3️⃣ 메모리 효율성
   - 여러 Entity가 데이터 공유
   - 메모리 절약 (99%+ 절약 가능)

4️⃣ 성능 최적화
   - 캐시 친화적 (연속 메모리)
   - 직접 메모리 접근 (빠름)

5️⃣ 생명주기
   - 생성: Baker 또는 런타임
   - 저장: .entities 파일에 직렬화
   - 로딩: SubScene 로드 시 복원
   - 해제: Dispose() 또는 SubScene 언로드

6️⃣ 사용처
   - 대용량 불변 데이터
   - 메시, 텍스처, 테이블
   - 여러 Entity가 공유하는 데이터
```

---

## 관련 문서

- [Blob Asset 완전 가이드](./knowledge_blob_asset.md)
- [SubScene 완전 가이드](./knowledge_subscene.md)
- [Unity.Entities 문서](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual)

---

**마지막 업데이트**: 2026-01-15
**Unity 버전**: 6000.1.7f1
**Entities 패키지**: 1.3.x 이상
