# Phase 3 실행 계획 (Part 1): 몬스터 컴포넌트 및 Prefab 구현

## 프로젝트 개요

**Phase**: Phase 3 - 몬스터 스폰 시스템 구현
**목표**: 플레이어 주변에 주기적으로 몬스터가 스폰되는 ECS 기반 시스템 구현
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수

## Phase 3 개발 사항 요약

이 단계에서는 몬스터가 주기적으로 플레이어 주변에 스폰되는 시스템을 ECS 구조로 구현합니다.

### 구현 범위 (Part 1)
1. 몬스터 ECS 컴포넌트 및 Authoring 구현 (TASK-010)
2. 몬스터 Prefab 생성 및 시각적 설정 (TASK-011)
3. 스폰 매니저 컴포넌트 및 Authoring 구현 (TASK-012)

---

## TASK-010: 몬스터 ECS 컴포넌트 및 Authoring 구현

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 몬스터 엔티티를 위한 ECS 컴포넌트와 Authoring 클래스 구현

### 10.1 몬스터 컴포넌트 정의

`Assets/Scripts/Components/` 폴더에 다음 컴포넌트 작성:

**EnemyTag.cs**
```csharp
using Unity.Entities;

public struct EnemyTag : IComponentData
{
}
```

**EnemyHealth.cs**
```csharp
using Unity.Entities;

public struct EnemyHealth : IComponentData
{
    public float Value;
}
```

**EnemySpeed.cs**
```csharp
using Unity.Entities;

public struct EnemySpeed : IComponentData
{
    public float Value;
}
```

### 10.2 Authoring 클래스 구현

`Assets/Scripts/Authoring/EnemyAuthoring.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float Health = 100f;
    public float Speed = 3f;

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            // Renderable | Dynamic: 렌더링되면서 움직이는 Entity
            var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new EnemyHealth { Value = authoring.Health });
            AddComponent(entity, new EnemySpeed { Value = authoring.Speed });
        }
    }
}
```

**완료 조건**:
- [ ] EnemyTag.cs 작성 완료
- [ ] EnemyHealth.cs 작성 완료
- [ ] EnemySpeed.cs 작성 완료
- [ ] EnemyAuthoring.cs 작성 완료
- [ ] 컴파일 에러 없음

**예상 작업량**: 1-2시간

---

## TASK-011: 몬스터 Prefab 생성

**카테고리**: 환경 설정
**우선순위**: P0 (최우선)
**설명**: 몬스터 Entity Prefab을 생성하고 시각적 표현을 설정

### 11.1 몬스터 GameObject 생성

1. Hierarchy → 3D Object → Cube 생성
2. 이름: "Enemy"
3. Transform 설정:
   - Position: (0, 0, 0)
   - Scale: (0.5, 0.5, 0.5)

### 11.2 Material 생성 및 적용

1. `Assets/Materials/` 폴더에 Material 생성: "EnemyMaterial"
2. Albedo 색상: 빨간색 (#FF0000)
3. Material을 Enemy에 적용

### 11.3 EnemyAuthoring 컴포넌트 추가

1. Enemy GameObject 선택
2. Add Component → EnemyAuthoring
3. Inspector 설정:
   - Health: 100
   - Speed: 3

### 11.4 Prefab 저장

1. `Assets/Prefabs/` 폴더에 Enemy를 드래그하여 Prefab 생성
2. Hierarchy에서 Enemy GameObject 삭제

**완료 조건**:
- [ ] Enemy GameObject 생성 완료
- [ ] EnemyMaterial 생성 (빨간색)
- [ ] EnemyAuthoring 컴포넌트 추가
- [ ] Prefab 저장됨 (Assets/Prefabs/Enemy.prefab)

**예상 작업량**: 1시간

---

## TASK-012: 스폰 매니저 구현

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 몬스터 스폰을 관리하는 컴포넌트 구현

### 12.1 스폰 매니저 컴포넌트

`Assets/Scripts/Components/EnemySpawnConfig.cs` 작성:

```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct EnemySpawnConfig : IComponentData
{
    public float SpawnInterval;      // 스폰 간격 (초)
    public float TimeSinceLastSpawn; // 마지막 스폰 후 경과 시간
    public Entity EnemyPrefab;       // 몬스터 Prefab Entity 참조
    public float SpawnRadius;        // 플레이어로부터 스폰 반경
    public int MaxEnemies;           // 최대 몬스터 수
    public Random RandomGenerator;   // 랜덤 생성기
}
```

### 12.2 스폰 매니저 Authoring

`Assets/Scripts/Authoring/EnemySpawnAuthoring.cs` 작성:

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemySpawnAuthoring : MonoBehaviour
{
    public float SpawnInterval = 2f;
    public GameObject EnemyPrefab;
    public float SpawnRadius = 10f;
    public int MaxEnemies = 50;

    class Baker : Baker<EnemySpawnAuthoring>
    {
        public override void Bake(EnemySpawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var enemyPrefabEntity = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemySpawnConfig
            {
                SpawnInterval = authoring.SpawnInterval,
                TimeSinceLastSpawn = 0f,
                EnemyPrefab = enemyPrefabEntity,
                SpawnRadius = authoring.SpawnRadius,
                MaxEnemies = authoring.MaxEnemies,
                RandomGenerator = new Random((uint)System.DateTime.Now.Ticks)
            });
        }
    }
}
```

### 12.3 GameObject 생성

1. Hierarchy → Create Empty
2. 이름: "EnemySpawnManager"
3. Add Component → EnemySpawnAuthoring
4. Inspector 설정:
   - Spawn Interval: 2
   - Enemy Prefab: Assets/Prefabs/Enemy 드래그
   - Spawn Radius: 10
   - Max Enemies: 50

**완료 조건**:
- [ ] EnemySpawnConfig.cs 작성 완료
- [ ] EnemySpawnAuthoring.cs 작성 완료
- [ ] EnemySpawnManager GameObject 생성 완료

**예상 작업량**: 2시간

---

## Part 1 검증

- [ ] 모든 컴포넌트 파일 생성됨
- [ ] 모든 Authoring 파일 생성됨
- [ ] Enemy Prefab 생성됨
- [ ] EnemySpawnManager GameObject 생성됨
- [ ] Console 에러 없음

**다음**: Part 2에서 스폰 시스템 구현 및 테스트 진행

---

**문서 버전**: 1.0
**작성일**: 2025-11-28
