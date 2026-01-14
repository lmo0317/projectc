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

### 1. 빌드 타임 (Baking)

```
Unity Editor (Authoring)
         ↓
    GameObject 작업
    (Prefab, Component)
         ↓
    ┌─────────────────────────┐
    │   SubScene Baking       │
    │  (IEntityData + Baker)  │
    └─────────────────────────┘
         ↓
    Entity 변환
    (Binary Entity Header)
         ↓
    .entities 파일 저장
```

### 2. 런타임 (Loading)

```
Game Start
    ↓
SubScene 로드 요청
    ↓
Entity Scene 생성
    (기존 GameObject 없음!)
    ↓
ECS World에 Entity 추가
    ↓
ISystem이 실행되며 처리
```

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

---

**마지막 업데이트**: 2026-01-14
**Unity 버전**: 6000.1.7f1
**Entities 패키지**: 1.3.x 이상
