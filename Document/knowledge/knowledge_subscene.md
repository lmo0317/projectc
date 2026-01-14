# Unity DOTS SubScene 완전 가이드

## 📚 목차

1. [SubScene이란?](#subscene이란)
2. [SubScene vs 일반 Scene](#subscene-vs-일반-scene)
3. [SubScene 작동 원리](#subscene-작동-원리)
4. [SubScene 생성 및 사용 방법](#subscene-생성-및-사용-방법)
5. [Entity 생성: SubScene vs 일반 Scene](#entity-생성-subscene-vs-일반-scene)
6. [실전 사용 예시](#실전-사용-예시)
7. [주의사항 및 모범 사례](#주의사항-및-모범-사례)
8. [자주 묻는 질문](#자주-묻는-질문)

---

## SubScene이란?

**SubScene**은 Unity DOTS (Data-Oriented Technology Stack)에서 **GameObject를 Entity로 자동 변환**하기 위해 사용하는 특수한 Scene 컴포넌트입니다.

> **📖 관련 문서**: 대용량 불변 데이터(메시, 테이블, 스탯 등)를 관리하는 방법은 [Blob Asset 완전 가이드](./knowledge_blob_asset.md)를 참조하세요. SubScene과 Blob Asset을 함께 사용하면 메모리 효율을 극대화할 수 있습니다.

### 핵심 개념

```
일반 GameObject → (Baker) → Entity → ECS World에서 실행
```

- **Authoring**: Unity Editor에서 GameObject로 작업 (디자이넸 친화적)
- **Baking**: SubScene이 빌드될 때 GameObject → Entity로 변환
- **Runtime**: 변환된 Entity만 메모리에 로드되어 고성능 실행

---

## SubScene vs 일반 Scene

| 특징 | 일반 Scene | SubScene |
|------|-----------|----------|
| **GameObject 저장** | ✅ 직접 저장 | ❌ 저장 안 함 (Entity로만 변환) |
| **Entity 생성** | 수동으로 스크립트로 생성 | 자동으로 GameObject 변환 |
| **Editor에서 편집** | GameObject로 직접 편집 | GameObject로 직접 편집 |
| **Runtime 성능** | 변환 오버헤드 있음 | 미리 변환되어 빠름 |
| **Streaming** | 불가능 | ✅ 가능 (대형 월드 로딩) |
| **주요 용도** | UI, 간단한 오브젝트 | 게임 월드, 수천 개의 엔티티 |

---

## SubScene 작동 원리

**중요:** SubScene은 **빌드 타임과 런타임 모두 수행**하는 프로세스입니다. 두 단계가 연속적으로 작동합니다.

```
┌─────────────────────────────────────────────────────────────┐
│                    SubScene 작동 원리                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1️⃣ 빌드 타임 (Editor)           2️⃣ 런타임 (게임 실행)      │
│  ──────────────────            ────────────────────         │
│  GameObject 작업                .entities 파일 로드          │
│       ↓                              ↓                       │
│  Baker 실행                      Entity 생성                 │
│       ↓                              ↓                       │
│  Entity 변환                      ECS World에 추가            │
│       ↓                              ↓                       │
│  .entities 저장                   System이 실행               │
│  (디스크에 보관)                  (게임 플레이)               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 1. 빌드 타임 (Baking) - **데이터 변환 단계**

**실행 시점:**
- Unity Editor에서 SubScene 저장 시 (Ctrl+S)
- Play 버튼 클릭 시
- Asset Import 시
- Build 실행 시

**생성되는 파일:**

```
📁 Assets/Scenes/GameSceneSpace/
├── GameSceneSpaceSubscene.unity          (Authoring Scene - GameObject)
│   ├── Monster_A (GameObject)
│   ├── Monster_B (GameObject)
│   └── Monster_C (GameObject)
│
└── GameSceneSpaceSubscene.entities       ← 새로 생성됨! (Binary Entity Header)
    (Entity 데이터만 포함된 바이너리 파일)
```

**.entities 파일 구조:**

```csharp
// 내부적으로 이런 데이터가 저장됨 (개념적 표현)
[Entity Header]
{
    EntityCount: 3
    Entities:
    [
        {
            EntityID: 1000
            Components:
            [
                { Type: LocalTransform, Data: { Position: (0,0,0), Rotation: (0,0,0,1), Scale: 1 } }
                { Type: MonsterComponent, Data: { Health: 100, MoveSpeed: 5f } }
            ]
        },
        {
            EntityID: 1001
            Components:
            [
                { Type: LocalTransform, Data: { Position: (5,0,0), ... } }
                { Type: MonsterComponent, Data: { Health: 100, MoveSpeed: 5f } }
            ]
        },
        {
            EntityID: 1002
            Components:
            [
                { Type: LocalTransform, Data: { Position: (10,0,0), ... } }
                { Type: MonsterComponent, Data: { Health: 100, MoveSpeed: 5f } }
            ]
        }
    ]
}
```

**빌드 프로세스 상세:**

```
1. Unity Editor가 SubScene 스캔
   ↓
2. 각 GameObject의 Authoring Component 발견
   - MonsterAuthoring (MonoBehaviour)
   - ItemAuthoring (MonoBehaviour)
   ↓
3. Baker.Bake() 메서드 호출
   - GetEntity()로 Entity 식별자 생성
   - AddComponent()로 컴포넌트 데이터 추가
   ↓
4. Entity Serialization
   - 모든 Entity와 Component를 바이너리로 변환
   - .entities 파일에 기록
   ↓
5. 완료: .entities 파일이 디스크에 저장됨
```

**실제 파일 예시:**

```
📦 GameSceneSpaceSubscene.entities (실제 바이너리 파일)
크기: 12.5 KB
형식: Unity Entity Binary Format
내용: Entity metadata, Component data, Scene references
```

---

### 2. 런타임 (Loading) - **데이터 실행 단계**

**실행 시점:**
- 게임이 시작될 때 (MainScene 로드)
- SubScene 컴포넌트가 발견될 때
- Scene Streaming으로 SubScene을 불러올 때

**로딩 프로세스 상세:**

```
1. MainScene.unity 로드
   ↓
2. SubSceneAuthoring Component 발견
   - "Auto Load Scene"이 체크되어 있으면 자동 로드
   ↓
3. .entities 파일 스캔
   - 디스크에서 GameSceneSpaceSubscene.entities 읽기
   ↓
4. Entity Deserialization
   - 바이너리 데이터를 메모리의 Entity로 변환
   - 각 Entity의 Component 복원
   ↓
5. ECS World에 Entity 추가
   - DefaultWorld 또는 지정된 World에 삽입
   ↓
6. System 실행 시작
   - MonsterSystem.OnUpdate()가 Entity를 찾아 처리
```

**메모리 상태 변화:**

```
빌드 타임 (Editor):
┌─────────────────────────────────────┐
│ Unity Editor 메모리                  │
│ ├── GameObject_A (MonsterAuthoring)  │
│ ├── GameObject_B (MonsterAuthoring)  │
│ └── GameObject_C (MonsterAuthoring)  │
└─────────────────────────────────────┘
              ↓ Baking
              ↓
디스크 저장:
┌─────────────────────────────────────┐
│ GameSceneSpaceSubscene.entities      │
│ (Binary Entity Data)                 │
└─────────────────────────────────────┘
              ↓ 게임 시작
              ↓
런타임 (Game):
┌─────────────────────────────────────┐
│ ECS World 메모리                     │
│ ├── Entity_1000                      │
│ │   ├── LocalTransform               │
│ │   └── MonsterComponent             │
│ ├── Entity_1001                      │
│ │   ├── LocalTransform               │
│ │   └── MonsterComponent             │
│ └── Entity_1002                      │
│     ├── LocalTransform               │
│     └── MonsterComponent             │
└─────────────────────────────────────┘
```

**런타임 코드 동작 예시:**

```csharp
// 1. 게임 시작 - MainScene.unity 로드
// Unity가 자동으로 SubScene을 감지

// 2. SubScene 로드 요청
SceneSystem.LoadScene(GetEntityQuery(typeof(SubSceneComponent)));

// 3. .entities 파일에서 Entity 생성
// 내부적으로 Unity가 다음 작업 수행:
/*
- GameSceneSpaceSubscene.entities 파일 열기
- 바이너리 데이터 읽기
- Entity_1000, 1001, 1002 생성
- LocalTransform, MonsterComponent 추가
- DefaultWorld.EntityManager에 등록
*/

// 4. System이 Entity를 처리
public partial struct MonsterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // .entities에서 로드된 Entity들이 자동으로 여기서 처리됨!
        foreach (var (transform, monster) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<MonsterComponent>>())
        {
            // 몬스터 이동 로직
            transform.ValueRW.Position += new float3(0, 0, 1) * monster.ValueRW.MoveSpeed * SystemAPI.Time.DeltaTime;
        }
    }
}
```

---

### 3. 빌드 타임과 런타임의 상호작용

**비유로 이해하기:**

```
🍳 요리 레시피 비유

1️⃣ 빌드 타임 = 식당 주방 (준비 단계)
   신선한 재료(GameObject) → 손질 & 레시피 적용(Baker)
   → 냉동 보관(.entities 파일)

2️⃣ 런타임 = 서빙 시간 (실행 단계)
   냉동 보관된 재료(.entities) → 꺼내서 조리(Entity 로드)
   → 접시에 서빙(ECS World)
```

**실제 파일 사용 예시:**

```csharp
// ============================================
// 빌드 타임에 생성되는 파일
// ============================================
📁 Assets/Scenes/GameSceneSpace/
├── GameSceneSpaceSubscene.unity        ← Authoring용 (Editor)
│   (GameObject 작업용)
│
└── GameSceneSpaceSubscene.entities    ← Runtime용 (Binary)
    (Entity 데이터)

// ============================================
// 런타임에 이 파일이 어떻게 사용되는가
// ============================================

// 단계 1: Editor에서 작업
// 1. GameSceneSpaceSubscene.unity 열기
// 2. GameObject 배치 + MonsterAuthoring 추가
// 3. 저장 (Ctrl+S)
// → 자동으로 GameSceneSpaceSubscene.entities 생성됨!

// 단계 2: 게임 빌드
// Build Settings → Build
// → .entities 파일이 게임 데이터에 포함됨

// 단계 3: 런타임 로딩
// 게임 실행
// → MainScene.unity 로드
// → SubScene 컴포넌트가 .entities 파일을 찾음
// → Entity를 메모리에 생성
// → System이 실행됨
```

---

### 4. 실제 프로젝트에서의 작동 순서

**개발자 작업 흐름:**

```
Day 1: SubScene 생성
1. Hierarchy → 우클릭 → "Sub Scene (DOTS)"
2. 이름: "GameSceneSpaceSubscene"
3. 더블클릭하여 SubScene 열기
4. GameObject 배치 및 작업
   - Monster_A 생성
   - MonsterAuthoring 스크립트 부착
   - Health: 100, MoveSpeed: 5 설정
5. 저장 (Ctrl+S)
   → Unity가 자동으로 Baking 실행
   → GameSceneSpaceSubscene.entities 생성됨!

Day 2: Play 버튼으로 테스트
1. MainScene.unity 열기
2. Play 버튼 클릭 (▶)
   → SubScene 로딩 시작
   → .entities 파일에서 Entity 생성
   → MonsterSystem이 실행되며 몬스터들이 움직임!

Day 3: 빌드 및 배포
1. File → Build Settings → Build
2. 생성된 게임 실행
   → .entities 파일이 번들로 포함됨
   → 플레이어의 컴퓨터에서 Entity 로드
   → 게임 플레이!
```

---

### 5. 중요 개념 정리

| 단계 | 시점 | 파일 | 메모리 상태 | 작업 |
|------|------|------|-----------|------|
| **빌드 타임** | Editor 작업 중 | `.unity` 파일 존재 | GameObject 존재 | Baker가 Entity로 변환 |
| **Baking 완료** | 저장/빌드 시 | `.entities` 파일 생성 | Entity 데이터가 파일로 저장 | 디스크에 보관 |
| **런타임** | 게임 실행 중 | `.entities` 파일 로드 | Entity만 존재 | GameObject 없음! |

**핵심 포인트:**
- GameObject는 **Editor에서만 존재** (개발자 편의성)
- `.entities` 파일은 **중간 결과물** (디스크에 저장)
- 런타임에는 **Entity만 로드** (고성능 실행)

---

## SubScene 생성 및 사용 방법

### 1. SubScene 생성

```csharp
// 방법 1: Hierarchy에서 우클릭
// GameObject → Sub Scene (DOTS)

// 방법 2: 코드로 생성
using Unity.Scenes;
using UnityEngine;

public class CreateSubScene : MonoBehaviour
{
    void Start()
    {
        // SubScene 자동 생성은 Editor 전용 기능
        // Runtime에는 Scene의 Entity를 로드
    }
}
```

### 2. SubScene 구조

```
MainScene.unity (기본 Scene)
├── SubScene (GameObject)
│   ├── GameSceneSpaceSubscene (SceneAsset)
│   └── SubSceneAuthoring (Component)
├── Camera
└── Light
```

### 3. SubScene 설정

```
Inspector → SubSceneAuthoring Component

┌─────────────────────────────────────┐
│ SubScene                            │
├─────────────────────────────────────┤
│ Scene: GameSceneSpaceSubscene       │
│ Authoring Scene: GameSceneSpace...  │
│ □ Auto Load Scene                  │
│ □ Open Scene In Prefab Mode        │
└─────────────────────────────────────┘
```

---

## Entity 생성: SubScene vs 일반 Scene

### SubScene에서 Entity 생성 (권장)

**장점:**
- ✅ 디자이너가 Unity Editor에서 직접 배치
- ✅ 수천 개의 Entity를 자동으로 변환
- ✅ Scene Streaming으로 메모리 최적화
- ✅ Prefab 인스턴스 자동 지원

**예시:**

```csharp
// 1. Authoring Component (GameObject에 부착)
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MonsterAuthoring : MonoBehaviour
{
    public int Health = 100;
    public float MoveSpeed = 5f;

    // Baker가 자동으로 호출됨
    class Baker : Baker<MonsterAuthoring>
    {
        public override void Bake(MonsterAuthoring authoring)
        {
            // Entity 생성 및 컴포넌트 추가
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new MonsterComponent
            {
                Health = authoring.Health,
                MoveSpeed = authoring.MoveSpeed,
                Position = float3.zero
            });
        }
    }
}

// 2. Component Data (IComponentData)
public struct MonsterComponent : IComponentData
{
    public int Health;
    public float MoveSpeed;
    public float3 Position;
}

// 3. 사용 방법
/*
1. Unity Editor에서 GameObject 생성
2. MonsterAuthoring 스크립트 부착
3. SubScene 안에 배치
4. Unity가 자동으로 Entity로 변환!
*/
```

### 일반 Scene에서 Entity 생성 (런타임)

**장점:**
- ✅ 동적으로 생성 필요한 경우 (총알, 이펙트)
- ✅ 조건부 생성 (로직에 따라 결정)
- ✅ 임시 오브젝트 (생성 후 삭제)

**예시:**

```csharp
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public class MonsterSpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 프리팹에서 Entity 생성 (런타임)
        var prefab = SystemAPI.GetSingleton<MonsterPrefab>();

        // 매 프레임 Entity 생성 (비추천 - 예시)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var entity = EntityManager.Instantiate(prefab.Value);

            // 위치 설정
            EntityManager.SetComponentData(entity, new LocalTransform
            {
                Position = new float3(0, 0, 0),
                Rotation = quaternion.identity,
                Scale = 1f
            });
        }
    }
}
```

---

## 실전 사용 예시

### 예시 1: 맵에 배치된 몬스터

```
📁 Assets/Scenes/GameSceneSpace/
    └── GameSceneSpaceSubscene.unity
        ├── Monster_A (GameObject + MonsterAuthoring)
        ├── Monster_B (GameObject + MonsterAuthoring)
        └── Monster_C (GameObject + MonsterAuthoring)
```

**Baking 결과:**
```
Runtime Entity World
├── Entity_A (MonsterComponent + LocalTransform)
├── Entity_B (MonsterComponent + LocalTransform)
└── Entity_C (MonsterComponent + LocalTransform)
```

### 예시 2: Scene Streaming (대형 월드)

```csharp
using Unity.Entities;
using Unity.Scenes;

public class SceneStreamingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 플레이어 위치에 따라 SubScene 로드/언로드
        var playerPos = SystemAPI.GetSingleton<PlayerPosition>();

        Entity sceneEntity = /* SubScene의 Entity */;

        if (ShouldLoadScene(playerPos))
        {
            // Scene 로드 요청
            EntityManager.AddComponentData(sceneEntity, new RequestSceneLoaded());
        }
        else
        {
            // Scene 언로드 요청
            EntityManager.AddComponentData(sceneEntity, new RequestSceneUnloaded());
        }
    }

    bool ShouldLoadScene(float3 playerPos)
    {
        // 거리 계산 등 로직
        return true;
    }
}
```

---

## 주의사항 및 모범 사례

### ✅ 권장 사항

1. **정적/반정적 오브젝트는 SubScene 사용**
   - 지형, 건물, 배치된 몬스터
   - 자동차, 아이템 박스 등

2. **Baker에서 GetEntity() 플래그 올바르게 설정**
   ```csharp
   // 움직이는 오브젝트
   var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

   // 고정된 오브젝트
   var entity = GetEntity(TransformUsageFlags.Renderable);
   ```

3. **대형 월드는 여러 SubScene으로 분리**
   - 각 구역을 별도 SubScene으로 관리
   - 메모리 효율성 향상

### ❌ 피해야 할 실수

1. **런타임에 SubScene의 GameObject 접근 시도**
   ```csharp
   // ❌ SubScene 내부의 GameObject는 런타임에 존재하지 않음!
   var obj = GameObject.Find("Monster"); // null 반환

   // ✅ Entity를 통해 접근
   var query = GetEntityQuery(typeof(MonsterComponent));
   ```

2. **SubScene에 MonoBehaviour 로직 의존**
   ```csharp
   // ❌ MonoBehaviour의 Update()는 런타임에 실행되지 않을 수 있음
   public class MonsterController : MonoBehaviour
   {
       void Update() { /* 실행 안 됨! */ }
   }

   // ✅ ISystem으로 구현
   public partial struct MonsterSystem : ISystem
   {
       public void OnUpdate(ref SystemState state) { /* ECS 방식 */ }
   }
   ```

3. **TransformUsageFlags 누락**
   ```csharp
   // ❌ Transform이 필요한데 플래그 없음
   var entity = GetEntity(TransformUsageFlags.None);

   // ✅ 필요한 플래그 모두 지정
   var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);
   ```

---

## 자주 묻는 질문

### Q1: SubScene과 일반 Scene을 같이 써도 되나요?

**A:** 네, 가능합니다! 일반적으로 다음과 같이 구분합니다:

- **일반 Scene**: UI, 카메라, 라이트, 게임 매니저
- **SubScene**: 게임 월드, 몬스터, 아이템 (ECS로 처리)

```
MainScene.unity
├── Canvas (UI)
├── Main Camera
├── Game Manager (MonoBehaviour)
└── SubScene (Game World)
    └── Entity들로 변환됨
```

### Q2: SubScene에 있는 GameObject를 런타임에 수정할 수 있나요?

**A:** 직접 수정은 불가능합니다. SubScene은 빌드 타임에만 존재하고, 런타임에는 Entity로만 변환되어 로드됩니다.

대신:
1. Entity 컴포넌트를 런타임에 수정
2. 필요하면 새로운 SubScene을 로드

### Q3: SubScene을 언제 사용하면 되나요?

**A:** 다음과 같은 상황에서 사용하세요:

- ✅ 수백 개 이상의 게임 오브젝트
- ✅ 지형, 건물 같은 정적/반정적 환경
- ✅ 맵에 미리 배치된 몬스터, 아이템
- ✅ Scene Streaming이 필요한 대형 월드

**사용하지 않는 경우:**
- ❌ UI (UGUI는 GameObject 기반)
- ❌ 동적으로 생성되는 일시적 오브젝트 (총알, 폭발 효과)
- ❌ 간단한 프로토타입

### Q4: SubScene 빌드는 언제 실행되나요?

**A:** 다음과 같은 타이밍에 자동으로 실행됩니다:

1. Unity Editor에서 Play 버튼 클릭 시
2. SubScene이 포함된 Scene 저장 시
3. Build 실행 시
4. SubScene의 Asset Import 시

**수동으로 빌드:** `Assets → Run Baking (Entities)` 또는 Ctrl+Shift+B

### Q5: SubScene의 Entity를 코드로 생성할 수 있나요?

**A:** 네, 가능합니다! 하지만 **SubScene을 사용하는 이유가 사라집니다.**

SubScene의 핵심 목적은 "디자이너가 Editor에서 직접 배치하는 것"이므로, 코드로 생성해야 한다면 일반 Scene에서 `EntityManager.Instantiate()`를 사용하는 것이 더 적합합니다.

---

## 요약

### SubScene 핵심 포인트

1. **목적**: GameObject → Entity 자동 변환 (Authoring → Runtime)
2. **장점**: Editor 친화적, 대규모 Entity 처리, Scene Streaming 지원
3. **사용처**: 맵 배치, 몬스터, 아이템 등 정적/반정적 오브젝트
4. **주의**: 런타임에 GameObject는 존재하지 않음, Baker 필수

### 결정 트리

```
Entity가 필요한가?
├── YES
│   ├── 디자이너가 Editor에서 배치해야 하나?
│   │   ├── YES → SubScene 사용 ✅
│   │   └── NO → EntityManager.Instantiate() 사용
│   └── 수천 개 이상의 Entity가 필요한가?
│       ├── YES → SubScene 사용 ✅
│       └── NO → 일반 방식으로 생성
└── NO → MonoBehaviour 사용
```

---

## 참고 자료

- [Unity Entities Package 문서](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual)
- [Unity Scenes Package 문서](https://docs.unity3d.com/Packages/com.unity.scenes@latest)
- [DOTS Samples](https://github.com/Unity-Technologies/EntityComponentSystemSamples)
- **Blob Asset 가이드**: [Blob Asset 완전 가이드](./knowledge_blob_asset.md) (SubScene과 함께 사용되는 대용량 데이터 관리)

---

**마지막 업데이트**: 2026-01-14
**Unity 버전**: 6000.1.7f1
**Entities 패키지**: 1.3.x 이상
