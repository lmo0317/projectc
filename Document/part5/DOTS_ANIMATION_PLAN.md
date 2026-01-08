# Enemy Monster 애니메이션 구현 계획 (Idle + Move 전용)

## 개요

**목표**: Enemy Monster에 **Idle**과 **Move** 2개의 애니메이션만 적용
**대상**: 플레이어가 아닌 Enemy (구현 복잡도 최소화)
**방식**: Hybrid ECS (MonoBehaviour Animator + ECS)

---

## 1. 현재 Enemy 구조

### 기존 ECS 컴포넌트
- `EnemyTag`: Enemy 식별 태그
- `EnemyHealth`: 체력 (float Value)
- `EnemySpeed`: 이동 속도 (float Value, 기본 2~3)
- `LocalTransform`: 위치, 회전, 스케일

### 기존 시스템
- `EnemySpawnSystem`: Enemy 생성 (최대 50개)
- `EnemyChaseSystem`: 플레이어 추적 + Enemy 분리 AI
  - 이동: 플레이어 방향 + 다른 Enemy와 분리
  - 회전: Quaternion.Slerp로 부드러운 회전

### Enemy 프리팹
- **경로**: `Assets/Prefabs/Enemy.prefab`
- **현재 구조**: 정적 메시 (애니메이션 없음)
- **Transform 플래그**: `Renderable | Dynamic`

---

## 2. 구현 방식: Hybrid ECS

### 선택 이유
1. **빠른 프로토타입**: Unity Animator Controller 활용
2. **간단한 구조**: 애니메이션 2개만 (Idle, Move)
3. **적은 Enemy 수**: 최대 50개 (성능 문제 없음)

---

## 3. 구현 단계

### 단계 1: ECS 컴포넌트 정의 (3개)

#### `EnemyAnimationState.cs`
**경로**: `Assets/Scripts/Components/EnemyAnimationState.cs`

```csharp
using Unity.Entities;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct EnemyAnimationState : IComponentData
{
    [GhostField] public float MoveSpeed; // 0 = Idle, > 0 = Move
}
```

**설명**:
- `MoveSpeed`: 0이면 Idle, 0보다 크면 Move
- `GhostField`: 클라이언트 동기화 (애니메이션 표시용)

#### `EnemyAnimationReference.cs`
**경로**: `Assets/Scripts/Components/EnemyAnimationReference.cs`

```csharp
using Unity.Entities;

public class EnemyAnimationReference : IComponentData
{
    public EnemyAnimationController Controller;
}
```

#### `EnemyAnimationPrefabRef.cs`
**경로**: `Assets/Scripts/Components/EnemyAnimationPrefabRef.cs`

```csharp
using Unity.Entities;
using UnityEngine;

public class EnemyAnimationPrefabRef : IComponentData
{
    public GameObject Prefab;
}
```

---

### 단계 2: MonoBehaviour 컨트롤러

#### `EnemyAnimationController.cs`
**경로**: `Assets/Scripts/MonoBehaviours/EnemyAnimationController.cs`

```csharp
using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    private Animator animator;
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void UpdateAnimation(float moveSpeed)
    {
        if (animator != null)
        {
            animator.SetFloat(MoveSpeedHash, moveSpeed);
        }
    }
}
```

---

### 단계 3: ECS 시스템 구현 (5개)

#### 3-1. `UpdateEnemyAnimationStateSystem.cs`
**경로**: `Assets/Scripts/Systems/UpdateEnemyAnimationStateSystem.cs`

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemyChaseSystem))]
public partial struct UpdateEnemyAnimationStateSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (speed, animState) in
            SystemAPI.Query<RefRO<EnemySpeed>, RefRW<EnemyAnimationState>>()
                .WithAll<EnemyTag>())
        {
            // 이동 속도를 0~1로 정규화 (최대 속도 5 기준)
            float normalizedSpeed = math.saturate(speed.ValueRO.Value / 5f);
            animState.ValueRW.MoveSpeed = normalizedSpeed;
        }
    }
}
```

**역할**: EnemySpeed → MoveSpeed 변환 (0~1 정규화)

#### 3-2. `SpawnEnemyAnimationSystem.cs`
**경로**: `Assets/Scripts/Systems/SpawnEnemyAnimationSystem.cs`

```csharp
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class SpawnEnemyAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.IsServer())
            return;

        Entities
            .WithAll<EnemyTag>()
            .WithNone<EnemyAnimationReference>()
            .ForEach((Entity entity, in EnemyAnimationPrefabRef prefabRef, in LocalTransform transform) =>
            {
                if (prefabRef.Prefab == null)
                    return;

                var go = GameObject.Instantiate(prefabRef.Prefab);
                go.transform.position = transform.Position;
                go.transform.rotation = transform.Rotation;

                var animController = go.GetComponent<EnemyAnimationController>();
                if (animController != null)
                {
                    EntityManager.AddComponentObject(entity, new EnemyAnimationReference
                    {
                        Controller = animController
                    });
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();
    }
}
```

**역할**: GameObject 인스턴스 생성 (Client-only)

#### 3-3. `SyncEnemyAnimationTransformSystem.cs`
**경로**: `Assets/Scripts/Systems/SyncEnemyAnimationTransformSystem.cs`

```csharp
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(EnemyAnimationSystem))]
public partial class SyncEnemyAnimationTransformSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.IsServer())
            return;

        Entities
            .WithAll<EnemyTag>()
            .ForEach((in LocalTransform transform, in EnemyAnimationReference animRef) =>
            {
                if (animRef.Controller != null)
                {
                    var go = animRef.Controller.gameObject;
                    go.transform.position = transform.Position;
                    go.transform.rotation = transform.Rotation;
                }
            })
            .WithoutBurst()
            .Run();
    }
}
```

**역할**: Entity Transform → GameObject 동기화

#### 3-4. `EnemyAnimationSystem.cs`
**경로**: `Assets/Scripts/Systems/EnemyAnimationSystem.cs`

```csharp
using Unity.Entities;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class EnemyAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.IsServer())
            return;

        Entities
            .WithAll<EnemyTag>()
            .ForEach((in EnemyAnimationState animState, in EnemyAnimationReference animRef) =>
            {
                if (animRef.Controller != null)
                {
                    animRef.Controller.UpdateAnimation(animState.MoveSpeed);
                }
            })
            .WithoutBurst()
            .Run();
    }
}
```

**역할**: Animator 파라미터 업데이트

#### 3-5. `CleanupEnemyAnimationSystem.cs`
**경로**: `Assets/Scripts/Systems/CleanupEnemyAnimationSystem.cs`

```csharp
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CleanupEnemyAnimationSystem : SystemBase
{
    private EntityQuery deadEnemyQuery;

    protected override void OnCreate()
    {
        deadEnemyQuery = GetEntityQuery(
            ComponentType.ReadOnly<EnemyTag>(),
            ComponentType.ReadOnly<EnemyAnimationReference>(),
            ComponentType.Exclude<EnemyHealth>()
        );
    }

    protected override void OnUpdate()
    {
        if (World.IsServer())
            return;

        var entities = deadEnemyQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            if (EntityManager.HasComponent<EnemyAnimationReference>(entity))
            {
                var animRef = EntityManager.GetComponentObject<EnemyAnimationReference>(entity);
                if (animRef?.Controller != null)
                {
                    GameObject.Destroy(animRef.Controller.gameObject);
                }
                EntityManager.RemoveComponent<EnemyAnimationReference>(entity);
            }
        }

        entities.Dispose();
    }
}
```

**역할**: Enemy 사망 시 GameObject 정리

---

### 단계 4: EnemyAuthoring 수정

**경로**: `Assets/Scripts/Authoring/EnemyAuthoring.cs`

**변경사항**:
1. `AnimationPrefab` 필드 추가
2. Baker에서 `EnemyAnimationState`, `EnemyAnimationPrefabRef` 추가

```csharp
public class EnemyAuthoring : MonoBehaviour
{
    public float Health = 100f;
    public float Speed = 3f;
    public float ColliderRadius = 0.5f;
    public GameObject AnimationPrefab; // 신규

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            // 기존 컴포넌트
            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new EnemyHealth { Value = authoring.Health });
            AddComponent(entity, new EnemySpeed { Value = authoring.Speed });

            // 신규: 애니메이션 상태
            AddComponent(entity, new EnemyAnimationState { MoveSpeed = 0f });

            // 신규: 애니메이션 프리팹 참조
            if (authoring.AnimationPrefab != null)
            {
                AddComponentObject(entity, new EnemyAnimationPrefabRef
                {
                    Prefab = authoring.AnimationPrefab
                });
            }

            // 기존 Physics Collider... (유지)
        }
    }
}
```

---

## 4. Animator Controller 설정

### Animator Controller
**경로**: `Assets/Animations/EnemyAnimator.controller`

**파라미터**:
- `MoveSpeed` (Float): 0~1 범위

**상태 구성**:
```
Base Layer
├── Idle (MoveSpeed == 0)
└── Move (MoveSpeed > 0)
```

**Transition**:
- Idle → Move: `MoveSpeed > 0.1` (Has Exit Time: false)
- Move → Idle: `MoveSpeed < 0.1` (Has Exit Time: false)

### 애니메이션 클립 설정
- `Enemy_Idle.anim`: Loop Time 체크
- `Enemy_Move.anim`: Loop Time 체크
- **Root Motion**: 체크 해제 (ECS에서 이동 제어)

---

## 5. 애니메이션 프리팹 구조

### `EnemyAnimation.prefab`
**경로**: `Assets/Prefabs/EnemyAnimation.prefab`

**구조**:
```
EnemyAnimation (Root GameObject)
├── EnemyAnimationController (MonoBehaviour)
├── Animator
│   └── Controller: EnemyAnimator.controller
└── [Enemy 3D Model with SkinnedMeshRenderer]
```

---

## 6. 시스템 실행 순서

```
InitializationSystemGroup
└── SpawnEnemyAnimationSystem (GameObject 생성)

SimulationSystemGroup
├── EnemyChaseSystem (기존 - 이동 계산)
├── UpdateEnemyAnimationStateSystem (애니메이션 상태 계산)
└── CleanupEnemyAnimationSystem (GameObject 정리)

PresentationSystemGroup
├── SyncEnemyAnimationTransformSystem (Transform 동기화)
└── EnemyAnimationSystem (Animator 파라미터 업데이트)
```

---

## 7. 구현 체크리스트

### 코드 작성
- [ ] `EnemyAnimationState.cs` (IComponentData)
- [ ] `EnemyAnimationReference.cs` (Managed Component)
- [ ] `EnemyAnimationPrefabRef.cs` (Managed Component)
- [ ] `EnemyAnimationController.cs` (MonoBehaviour)
- [ ] `UpdateEnemyAnimationStateSystem.cs`
- [ ] `SpawnEnemyAnimationSystem.cs`
- [ ] `SyncEnemyAnimationTransformSystem.cs`
- [ ] `EnemyAnimationSystem.cs`
- [ ] `CleanupEnemyAnimationSystem.cs`
- [ ] `EnemyAuthoring.cs` 수정 (AnimationPrefab 필드 추가)

### 애니메이션 에셋
- [ ] Enemy 스켈레톤 메시 임포트
- [ ] `Enemy_Idle.anim` 설정 (Loop)
- [ ] `Enemy_Move.anim` 설정 (Loop)
- [ ] `EnemyAnimator.controller` 생성
- [ ] Animator 파라미터 `MoveSpeed` 추가
- [ ] Transition 설정 (Idle ↔ Move)

### 프리팹 설정
- [ ] `EnemyAnimation.prefab` 생성
- [ ] `EnemyAnimationController` 부착
- [ ] `Animator` 컴포넌트 설정
- [ ] `Enemy.prefab`에서 `AnimationPrefab` 할당

### 테스트
- [ ] Unity Editor 재생
- [ ] Enemy Idle 상태 확인
- [ ] Enemy 추적 시 Move 애니메이션 확인
- [ ] Transform 동기화 확인
- [ ] Enemy 사망 시 GameObject 정리 확인

---

## 8. 주요 파일 목록

### 신규 파일 (9개)
1. `Assets/Scripts/Components/EnemyAnimationState.cs`
2. `Assets/Scripts/Components/EnemyAnimationReference.cs`
3. `Assets/Scripts/Components/EnemyAnimationPrefabRef.cs`
4. `Assets/Scripts/MonoBehaviours/EnemyAnimationController.cs`
5. `Assets/Scripts/Systems/UpdateEnemyAnimationStateSystem.cs`
6. `Assets/Scripts/Systems/SpawnEnemyAnimationSystem.cs`
7. `Assets/Scripts/Systems/SyncEnemyAnimationTransformSystem.cs`
8. `Assets/Scripts/Systems/EnemyAnimationSystem.cs`
9. `Assets/Scripts/Systems/CleanupEnemyAnimationSystem.cs`

### 수정 파일 (2개)
1. `Assets/Scripts/Authoring/EnemyAuthoring.cs`
2. `Assets/Prefabs/Enemy.prefab`

### 애니메이션 에셋 (3개)
1. `Assets/Animations/EnemyAnimator.controller`
2. `Assets/Animations/Enemy_Idle.anim`
3. `Assets/Animations/Enemy_Move.anim`

### 프리팹 (1개)
1. `Assets/Prefabs/EnemyAnimation.prefab`

---

## 9. 핵심 포인트

### MoveSpeed 계산
- `EnemySpeed.Value` (실제 속도) → 0~1로 정규화
- 최대 속도 5 기준: `normalizedSpeed = saturate(speed / 5)`
- 0이면 Idle, 0보다 크면 Move

### Client-only 실행
- 모든 애니메이션 시스템은 클라이언트만 실행
- `if (World.IsServer()) return;` 체크

### GameObject 생명주기
- **생성**: `SpawnEnemyAnimationSystem` (InitializationSystemGroup)
- **동기화**: `SyncEnemyAnimationTransformSystem` (매 프레임)
- **정리**: `CleanupEnemyAnimationSystem` (Enemy 사망 시)

---

## 10. 성능 고려사항

### 최적화
- Animator 파라미터 해시 캐싱 (StringToHash 한 번만)
- Burst 컴파일 (`UpdateEnemyAnimationStateSystem`)
- Client-only 실행 (서버 부하 0)

### 예상 성능
- Enemy 최대 50개 (SpawnSystem 제한)
- CPU 부하: 낮음 (Animator 50개)
- 메모리: 경량 (GameObject 50개)

---

## 11. 주의사항

1. **TransformUsageFlags**: `Renderable | Dynamic` 유지
2. **GhostField**: `EnemyAnimationState.MoveSpeed` 동기화
3. **메모리 누수**: Enemy 사망 시 GameObject 정리 필수
4. **Root Motion**: 체크 해제 (ECS에서 이동 제어)

---

## 12. 향후 확장 가능성

### 추가 가능한 애니메이션
- **Attack**: 공격 모션 (트리거 파라미터)
- **Hit**: 피격 모션 (트리거 파라미터)
- **Death**: 사망 모션 (원샷, 루프 없음)

### Pure ECS 전환 시점
- Enemy 수가 100개 이상 필요할 때
- 성능 병목이 발생할 때
- 복잡한 애니메이션 블렌딩이 필요할 때

---

**이 계획은 Enemy Monster에 Idle과 Move 애니메이션만 적용하는 최소한의 구현입니다.**
