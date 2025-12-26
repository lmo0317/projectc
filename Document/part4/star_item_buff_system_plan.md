# 스타 아이템 & 버프 선택 시스템 계획서

## 개요

Enemy를 처치하면 별(Star) 아이템을 드롭하고, 플레이어가 수집하면 포인트가 증가합니다.
일정 포인트 도달 시 3가지 랜덤 버프 중 1개를 선택할 수 있으며, 동일 버프 재선택 시 레벨업됩니다.

---

## 1. 시스템 구성도

```
┌─────────────────────────────────────────────────────────────────────┐
│                         전체 시스템 흐름                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [Enemy 사망] ──→ [Star 아이템 스폰] ──→ [플레이어 수집]              │
│                                              │                      │
│                                              ▼                      │
│                                    [포인트 증가]                     │
│                                              │                      │
│                                              ▼                      │
│                              ┌─────────────────────────┐            │
│                              │ 포인트 >= 필요량?        │            │
│                              └─────────────────────────┘            │
│                                       │ YES                         │
│                                       ▼                             │
│                         ┌──────────────────────────┐                │
│                         │   버프 선택 UI 표시       │                │
│                         │  (3개 랜덤 버프 옵션)     │                │
│                         └──────────────────────────┘                │
│                                       │                             │
│                                       ▼                             │
│                         ┌──────────────────────────┐                │
│                         │   플레이어 버프 선택      │                │
│                         └──────────────────────────┘                │
│                                       │                             │
│                          ┌────────────┴────────────┐                │
│                          ▼                         ▼                │
│                   [신규 버프 획득]           [기존 버프 레벨업]       │
│                    (레벨 1)                  (레벨 +1)              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. 컴포넌트 설계

### 2.1 Star 아이템 컴포넌트

```csharp
// Assets/Scripts/Components/Items/StarTag.cs
public struct StarTag : IComponentData { }

// Assets/Scripts/Components/Items/StarValue.cs
public struct StarValue : IComponentData
{
    public int Value;  // 포인트 값 (기본 1)
}

// Assets/Scripts/Components/Items/MagnetTarget.cs
// 자석 효과 대상 (나중에 자석 버프 구현 시 사용)
public struct MagnetTarget : IComponentData
{
    public Entity TargetPlayer;
    public float AttractionSpeed;
}
```

### 2.2 플레이어 포인트 컴포넌트

```csharp
// Assets/Scripts/Components/PlayerStarPoints.cs
public struct PlayerStarPoints : IComponentData
{
    [GhostField] public int CurrentPoints;      // 현재 포인트
    [GhostField] public int TotalCollected;     // 총 수집량
    [GhostField] public int NextBuffThreshold;  // 다음 버프 필요 포인트
    [GhostField] public int BuffSelectionCount; // 버프 선택 횟수
}
```

### 2.3 버프 시스템 컴포넌트

```csharp
// Assets/Scripts/Components/Buffs/PlayerBuffs.cs
public struct PlayerBuffs : IComponentData
{
    // 각 버프의 레벨 (0 = 미획득, 1~5 = 레벨)
    [GhostField] public int DamageLevel;           // 데미지 증가
    [GhostField] public int SpeedLevel;            // 이동 속도 증가
    [GhostField] public int FireRateLevel;         // 공격 속도 증가
    [GhostField] public int MissileCountLevel;     // 미사일 개수 증가
    [GhostField] public int MagnetLevel;           // 자석 효과
    [GhostField] public int HealthRegenLevel;      // 체력 재생
    [GhostField] public int MaxHealthLevel;        // 최대 체력 증가
    [GhostField] public int CriticalLevel;         // 치명타 확률
}

// Assets/Scripts/Components/Buffs/BuffSelectionRequest.cs
// 버프 선택 UI 표시 요청
public struct BuffSelectionRequest : IComponentData
{
    public bool IsActive;
    public int Option1;  // 버프 타입 인덱스
    public int Option2;
    public int Option3;
}
```

---

## 3. 버프 종류 및 효과

### 3.1 버프 타입 정의

```csharp
// Assets/Scripts/Buffs/BuffType.cs
public enum BuffType
{
    Damage = 0,        // 데미지 증가
    Speed = 1,         // 이동 속도 증가
    FireRate = 2,      // 공격 속도 증가
    MissileCount = 3,  // 미사일 개수 증가
    Magnet = 4,        // 자석 효과 (아이템 흡수)
    HealthRegen = 5,   // 체력 재생
    MaxHealth = 6,     // 최대 체력 증가
    Critical = 7,      // 치명타 확률
}
```

### 3.2 레벨별 효과 수치

| 버프 타입 | Lv1 | Lv2 | Lv3 | Lv4 | Lv5 (MAX) |
|-----------|-----|-----|-----|-----|-----------|
| **데미지 증가** | +10% | +20% | +35% | +50% | +75% |
| **이동 속도** | +10% | +20% | +30% | +40% | +50% |
| **공격 속도** | +15% | +30% | +45% | +60% | +80% |
| **미사일 개수** | +1 | +2 | +3 | +4 | +6 |
| **자석 효과** | 범위 3 | 범위 5 | 범위 7 | 범위 10 | 범위 15 |
| **체력 재생** | 1/s | 2/s | 3/s | 5/s | 8/s |
| **최대 체력** | +20 | +40 | +70 | +100 | +150 |
| **치명타** | 5% (2x) | 10% (2x) | 15% (2.5x) | 20% (2.5x) | 30% (3x) |

---

## 4. 시스템 설계

### 4.1 Star 스폰 시스템

```csharp
// Assets/Scripts/Systems/Items/StarSpawnSystem.cs
// 위치: BulletHitSystem에서 Enemy 사망 시 호출

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BulletHitSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StarSpawnSystem : ISystem
{
    // Enemy 사망 위치에 Star Entity 생성
    // - StarTag, StarValue 컴포넌트 추가
    // - 네트워크 동기화 (Ghost)
}
```

### 4.2 Star 수집 시스템

```csharp
// Assets/Scripts/Systems/Items/StarCollectSystem.cs

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct StarCollectSystem : ISystem
{
    // 플레이어와 Star 거리 체크
    // 수집 시:
    //   1. PlayerStarPoints.CurrentPoints 증가
    //   2. Star Entity 삭제
    //   3. 수집 이펙트 RPC 전송
    //   4. 포인트 >= NextBuffThreshold 체크
}
```

### 4.3 자석 효과 시스템

```csharp
// Assets/Scripts/Systems/Items/MagnetSystem.cs

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(StarCollectSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct MagnetSystem : ISystem
{
    // 자석 버프 보유 플레이어 주변의 Star를
    // 플레이어 방향으로 끌어당김
    // - MagnetLevel에 따라 범위/속도 결정
}
```

### 4.4 버프 선택 시스템

```csharp
// Assets/Scripts/Systems/Buffs/BuffSelectionSystem.cs

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct BuffSelectionSystem : ISystem
{
    // 포인트 도달 시:
    //   1. 게임 일시정지 (Time.timeScale = 0 또는 별도 플래그)
    //   2. 8개 버프 중 랜덤 3개 선택
    //   3. BuffSelectionRequest 컴포넌트 활성화
    //   4. 선택 대기
}
```

### 4.5 버프 적용 시스템

```csharp
// Assets/Scripts/Systems/Buffs/BuffApplySystem.cs

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct BuffApplySystem : ISystem
{
    // 버프 선택 RPC 수신 시:
    //   1. 해당 버프 레벨 +1
    //   2. 실제 스탯에 효과 적용
    //   3. NextBuffThreshold 증가
    //   4. 게임 재개
}
```

---

## 5. UI 설계

### 5.1 버프 선택 UI

```
┌─────────────────────────────────────────────────────────────┐
│                     레벨 업!                                 │
│                  버프를 선택하세요                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   ⚔️ 데미지  │  │   🏃 속도   │  │   🚀 미사일 │         │
│  │   증가      │  │   증가      │  │   +1       │         │
│  │             │  │             │  │             │         │
│  │  Lv.1→Lv.2  │  │  신규 획득  │  │  Lv.2→Lv.3  │         │
│  │  +10%→+20%  │  │   +10%     │  │  +2→+3개   │         │
│  │             │  │             │  │             │         │
│  │   [선택]    │  │   [선택]    │  │   [선택]    │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 현재 버프 표시 (HUD)

```
┌────────────────────────────┐
│ ⚔️ Lv.2  🏃 Lv.1  🚀 Lv.3   │  ← 화면 상단에 아이콘으로 표시
└────────────────────────────┘
```

### 5.3 UI 컴포넌트

```csharp
// Assets/Scripts/UI/BuffSelectionUI.cs (MonoBehaviour)
public class BuffSelectionUI : MonoBehaviour
{
    public GameObject Panel;
    public BuffOptionCard[] OptionCards;  // 3개

    public void Show(BuffType[] options, int[] currentLevels);
    public void Hide();
    public event Action<int> OnBuffSelected;  // 선택된 옵션 인덱스
}

// Assets/Scripts/UI/BuffOptionCard.cs
public class BuffOptionCard : MonoBehaviour
{
    public Image IconImage;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI EffectText;
    public Button SelectButton;
}
```

---

## 6. 네트워크 동기화

### 6.1 RPC 정의

```csharp
// Star 수집 이펙트 (Server → Client)
public struct StarCollectRpc : IRpcCommand
{
    public float3 Position;
    public int Value;
}

// 버프 선택 UI 표시 요청 (Server → Client)
public struct ShowBuffSelectionRpc : IRpcCommand
{
    public int Option1BuffType;
    public int Option1CurrentLevel;
    public int Option2BuffType;
    public int Option2CurrentLevel;
    public int Option3BuffType;
    public int Option3CurrentLevel;
}

// 버프 선택 결과 (Client → Server)
public struct BuffSelectedRpc : IRpcCommand
{
    public int SelectedOptionIndex;  // 0, 1, 2
}

// 버프 적용 알림 (Server → Client)
public struct BuffAppliedRpc : IRpcCommand
{
    public int BuffType;
    public int NewLevel;
}
```

### 6.2 Ghost 컴포넌트

| 컴포넌트 | GhostMode | 설명 |
|---------|-----------|------|
| `PlayerStarPoints` | AllPredicted | 포인트 동기화 |
| `PlayerBuffs` | AllPredicted | 버프 레벨 동기화 |
| `StarTag` | Server | 서버에서만 관리 |

---

## 7. 구현 순서

### Phase 1: 기본 Star 시스템 (1단계)
1. [ ] Star 프리팹 생성 (시각적 모델 + Collider)
2. [ ] StarTag, StarValue 컴포넌트 구현
3. [ ] StarAuthoring 구현
4. [ ] BulletHitSystem 수정 - Enemy 사망 시 Star 스폰
5. [ ] StarCollectSystem 구현 - 수집 로직
6. [ ] PlayerStarPoints 컴포넌트 구현
7. [ ] UI에 포인트 표시 추가

### Phase 2: 버프 시스템 (2단계)
1. [ ] BuffType enum 및 버프 데이터 정의
2. [ ] PlayerBuffs 컴포넌트 구현
3. [ ] BuffSelectionSystem 구현 - 랜덤 3개 선택
4. [ ] BuffApplySystem 구현 - 버프 효과 적용
5. [ ] 기존 시스템 수정:
   - [ ] AutoShootSystem - 데미지/발사속도/미사일개수 버프 적용
   - [ ] PlayerMovementSystem - 이동속도 버프 적용
   - [ ] PlayerAuthoring - 최대체력 버프 적용

### Phase 3: 버프 선택 UI (3단계)
1. [ ] BuffSelectionUI 프리팹 생성
2. [ ] BuffOptionCard 컴포넌트 구현
3. [ ] 버프 아이콘 에셋 준비
4. [ ] UIManager에 버프 선택 UI 연동
5. [ ] 게임 일시정지/재개 로직

### Phase 4: 자석 효과 & 폴리싱 (4단계)
1. [ ] MagnetSystem 구현
2. [ ] Star 수집 이펙트
3. [ ] 버프 획득 이펙트
4. [ ] HUD 버프 아이콘 표시
5. [ ] 사운드 효과

### Phase 5: 네트워크 통합 (5단계)
1. [ ] RPC 구현 및 테스트
2. [ ] 멀티플레이어 동기화 검증
3. [ ] 버프 선택 시 게임 일시정지 동기화

---

## 8. 파일 구조

```
Assets/Scripts/
├── Components/
│   ├── Items/
│   │   ├── StarTag.cs
│   │   ├── StarValue.cs
│   │   └── MagnetTarget.cs
│   ├── Buffs/
│   │   ├── PlayerBuffs.cs
│   │   └── BuffSelectionRequest.cs
│   └── PlayerStarPoints.cs
│
├── Systems/
│   ├── Items/
│   │   ├── StarSpawnSystem.cs
│   │   ├── StarCollectSystem.cs
│   │   └── MagnetSystem.cs
│   └── Buffs/
│       ├── BuffSelectionSystem.cs
│       └── BuffApplySystem.cs
│
├── Authoring/
│   └── StarAuthoring.cs
│
├── Buffs/
│   ├── BuffType.cs
│   └── BuffDataConfig.cs (ScriptableObject)
│
├── UI/
│   ├── BuffSelectionUI.cs
│   └── BuffOptionCard.cs
│
└── Network/
    ├── StarCollectRpc.cs
    ├── ShowBuffSelectionRpc.cs
    ├── BuffSelectedRpc.cs
    └── BuffAppliedRpc.cs
```

---

## 9. 밸런싱 고려사항

### 포인트 필요량 (레벨업 곡선)

| 버프 선택 횟수 | 필요 포인트 | 누적 포인트 |
|---------------|------------|------------|
| 1차 | 10 | 10 |
| 2차 | 15 | 25 |
| 3차 | 20 | 45 |
| 4차 | 30 | 75 |
| 5차 | 40 | 115 |
| 6차+ | +15씩 증가 | ... |

### Star 드롭률

- 일반 Enemy: 1개 (100%)
- 엘리트 Enemy (추후): 3개
- 보스 (추후): 10개

---

## 10. 버프 효과 아키텍처 (확장 가능한 설계)

### 10.1 설계 원칙

버프 시스템은 다음 원칙을 따릅니다:

1. **단일 책임 원칙 (SRP)**: 각 버프 효과는 하나의 책임만 가짐
2. **개방-폐쇄 원칙 (OCP)**: 새 버프 추가 시 기존 코드 수정 없이 확장 가능
3. **데이터 주도 설계**: 버프 수치는 코드가 아닌 데이터로 관리
4. **ECS 친화적**: Unity DOTS 패턴과 자연스럽게 통합

### 10.2 아키텍처 개요

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         버프 시스템 아키텍처                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐                                                        │
│  │ BuffDefinition  │ ← ScriptableObject (버프 메타데이터 정의)               │
│  │ - BuffType      │                                                        │
│  │ - TargetStat    │                                                        │
│  │ - LevelValues[] │                                                        │
│  └────────┬────────┘                                                        │
│           │                                                                 │
│           ▼                                                                 │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐       │
│  │ BuffRegistry    │ ──→ │ IBuffEffect     │ ←── │ BuffEffectBase  │       │
│  │ (Singleton)     │     │ (Interface)     │     │ (Abstract)      │       │
│  └────────┬────────┘     └─────────────────┘     └────────┬────────┘       │
│           │                       ▲                       │                 │
│           │           ┌───────────┼───────────┐           │                 │
│           │           │           │           │           │                 │
│           │    ┌──────┴──┐ ┌──────┴──┐ ┌──────┴──┐        │                 │
│           │    │ Damage  │ │ Speed   │ │ Magnet  │ ...    │ (구체 구현체)    │
│           │    │ Effect  │ │ Effect  │ │ Effect  │        │                 │
│           │    └─────────┘ └─────────┘ └─────────┘        │                 │
│           │                                               │                 │
│           ▼                                               ▼                 │
│  ┌─────────────────────────────────────────────────────────────────┐       │
│  │                    BuffModifierSystem (ECS)                      │       │
│  │  - 매 프레임 활성 버프 순회                                        │       │
│  │  - IBuffEffect.CalculateModifier() 호출                          │       │
│  │  - 최종 스탯 = 기본값 × (1 + 합산 배율) + 가산 보너스               │       │
│  └─────────────────────────────────────────────────────────────────┘       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 10.3 핵심 인터페이스 및 기본 클래스

#### 10.3.1 버프 효과 인터페이스

```csharp
// Assets/Scripts/Buffs/Core/IBuffEffect.cs
public interface IBuffEffect
{
    BuffType BuffType { get; }
    StatType TargetStat { get; }
    StatModifier CalculateModifier(int level, in BuffContext context);
    void OnApply(int level, ref EntityCommandBuffer ecb, Entity target);
    void OnLevelUp(int oldLevel, int newLevel, ref EntityCommandBuffer ecb, Entity target);
}
```

#### 10.3.2 스탯 수정자 구조체

```csharp
// Assets/Scripts/Buffs/Core/StatModifier.cs
public struct StatModifier
{
    public float Additive;        // 가산 보너스 (예: MaxHealth +50)
    public float Multiplicative;  // 승산 배율 (0.1 = 10% 증가)
    public float FinalMultiplier; // 최종 승산 (예: 치명타 2배)

    public static StatModifier None => new StatModifier
    { Additive = 0f, Multiplicative = 0f, FinalMultiplier = 1f };

    public static StatModifier Percent(float percent) => new StatModifier
    { Additive = 0f, Multiplicative = percent / 100f, FinalMultiplier = 1f };

    public static StatModifier Flat(float value) => new StatModifier
    { Additive = value, Multiplicative = 0f, FinalMultiplier = 1f };
}
```

#### 10.3.3 스탯 타입 열거형

```csharp
// Assets/Scripts/Buffs/Core/StatType.cs
public enum StatType
{
    None = 0,
    // 공격 관련
    Damage = 1, FireRate = 2, MissileCount = 3,
    CriticalChance = 4, CriticalMultiplier = 5,
    // 이동 관련
    MovementSpeed = 10,
    // 생존 관련
    MaxHealth = 20, HealthRegen = 21,
    // 유틸리티
    MagnetRange = 30, MagnetSpeed = 31,
}
```

---

### 10.4 추상 기본 클래스

```csharp
// Assets/Scripts/Buffs/Core/BuffEffectBase.cs
public abstract class BuffEffectBase : IBuffEffect
{
    public abstract BuffType BuffType { get; }
    public abstract StatType TargetStat { get; }
    protected abstract float[] LevelValues { get; }
    protected virtual ModifierType ModifierType => ModifierType.Multiplicative;

    public virtual StatModifier CalculateModifier(int level, in BuffContext context)
    {
        if (level <= 0 || level > LevelValues.Length)
            return StatModifier.None;

        float value = LevelValues[level - 1];
        return ModifierType switch
        {
            ModifierType.Additive => StatModifier.Flat(value),
            ModifierType.Multiplicative => StatModifier.Percent(value),
            _ => StatModifier.None
        };
    }

    public virtual void OnApply(int level, ref EntityCommandBuffer ecb, Entity target) { }
    public virtual void OnLevelUp(int oldLevel, int newLevel,
                                   ref EntityCommandBuffer ecb, Entity target) { }
}

public enum ModifierType { Additive, Multiplicative }
```

---

### 10.5 구체적인 버프 효과 구현 예시

#### 데미지 버프 (승산 방식)

```csharp
// Assets/Scripts/Buffs/Effects/DamageBuffEffect.cs
public class DamageBuffEffect : BuffEffectBase
{
    public override BuffType BuffType => BuffType.Damage;
    public override StatType TargetStat => StatType.Damage;
    protected override float[] LevelValues => new[] { 10f, 20f, 35f, 50f, 75f };
    protected override ModifierType ModifierType => ModifierType.Multiplicative;
}
```

#### 미사일 개수 버프 (가산 방식)

```csharp
// Assets/Scripts/Buffs/Effects/MissileCountBuffEffect.cs
public class MissileCountBuffEffect : BuffEffectBase
{
    public override BuffType BuffType => BuffType.MissileCount;
    public override StatType TargetStat => StatType.MissileCount;
    protected override float[] LevelValues => new[] { 1f, 2f, 3f, 4f, 6f };
    protected override ModifierType ModifierType => ModifierType.Additive;
}
```

#### 치명타 버프 (복합 효과 - 확률 + 배율)

```csharp
// Assets/Scripts/Buffs/Effects/CriticalBuffEffect.cs
public class CriticalBuffEffect : IBuffEffect
{
    public BuffType BuffType => BuffType.Critical;
    public StatType TargetStat => StatType.CriticalChance;

    private static readonly float[] ChanceValues = { 5f, 10f, 15f, 20f, 30f };
    private static readonly float[] MultiplierValues = { 2.0f, 2.0f, 2.5f, 2.5f, 3.0f };

    public StatModifier CalculateModifier(int level, in BuffContext context)
    {
        if (level <= 0 || level > ChanceValues.Length)
            return StatModifier.None;

        return new StatModifier
        {
            Additive = ChanceValues[level - 1],
            Multiplicative = 0f,
            FinalMultiplier = MultiplierValues[level - 1]
        };
    }

    public void OnApply(int level, ref EntityCommandBuffer ecb, Entity target) { }
    public void OnLevelUp(int oldLevel, int newLevel,
                          ref EntityCommandBuffer ecb, Entity target) { }
}
```

#### 최대 체력 버프 (즉시 효과 포함)

```csharp
// Assets/Scripts/Buffs/Effects/MaxHealthBuffEffect.cs
public class MaxHealthBuffEffect : BuffEffectBase
{
    public override BuffType BuffType => BuffType.MaxHealth;
    public override StatType TargetStat => StatType.MaxHealth;
    protected override float[] LevelValues => new[] { 20f, 40f, 70f, 100f, 150f };
    protected override ModifierType ModifierType => ModifierType.Additive;

    public override void OnLevelUp(int oldLevel, int newLevel,
                                    ref EntityCommandBuffer ecb, Entity target)
    {
        // 최대 체력 증가 시 현재 체력도 증가분만큼 회복
        float oldBonus = oldLevel > 0 ? LevelValues[oldLevel - 1] : 0f;
        float healthGain = LevelValues[newLevel - 1] - oldBonus;
        ecb.AddComponent(target, new HealRequest { Amount = healthGain });
    }
}
```

---

### 10.6 버프 레지스트리 (중앙 관리)

```csharp
// Assets/Scripts/Buffs/Core/BuffRegistry.cs
public class BuffRegistry
{
    private static BuffRegistry _instance;
    public static BuffRegistry Instance => _instance ??= new BuffRegistry();

    private readonly Dictionary<BuffType, IBuffEffect> _effects = new();
    private readonly Dictionary<StatType, List<BuffType>> _statToBuffs = new();

    private BuffRegistry()
    {
        Register(new DamageBuffEffect());
        Register(new SpeedBuffEffect());
        Register(new FireRateBuffEffect());
        Register(new MissileCountBuffEffect());
        Register(new MagnetBuffEffect());
        Register(new HealthRegenBuffEffect());
        Register(new MaxHealthBuffEffect());
        Register(new CriticalBuffEffect());
    }

    public void Register(IBuffEffect effect)
    {
        _effects[effect.BuffType] = effect;
        if (!_statToBuffs.TryGetValue(effect.TargetStat, out var list))
        {
            list = new List<BuffType>();
            _statToBuffs[effect.TargetStat] = list;
        }
        list.Add(effect.BuffType);
    }

    public IBuffEffect GetEffect(BuffType type) =>
        _effects.TryGetValue(type, out var effect) ? effect : null;
}
```

---

### 10.7 스탯 계산 시스템 (ECS 통합)

#### 스탯 수정자 컴포넌트

```csharp
// Assets/Scripts/Components/Buffs/StatModifiers.cs
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct StatModifiers : IComponentData
{
    [GhostField] public float DamageMultiplier;      // 1.0 = 100%
    [GhostField] public float FireRateMultiplier;    // 0.8 = 20% 빠름
    [GhostField] public int BonusMissileCount;
    [GhostField] public float SpeedMultiplier;
    [GhostField] public float BonusMaxHealth;
    [GhostField] public float HealthRegenPerSecond;
    [GhostField] public float CriticalChance;        // 0~100
    [GhostField] public float CriticalMultiplier;    // 2.0 = 2배
    [GhostField] public float MagnetRange;

    public static StatModifiers Default => new StatModifiers
    {
        DamageMultiplier = 1f, FireRateMultiplier = 1f, BonusMissileCount = 0,
        SpeedMultiplier = 1f, BonusMaxHealth = 0f, HealthRegenPerSecond = 0f,
        CriticalChance = 0f, CriticalMultiplier = 1f, MagnetRange = 0f
    };
}
```

#### 스탯 계산 시스템

```csharp
// Assets/Scripts/Systems/Buffs/StatCalculationSystem.cs
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(AutoShootSystem))]
[UpdateBefore(typeof(PlayerMovementSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct StatCalculationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (buffs, modifiers) in
                 SystemAPI.Query<RefRO<PlayerBuffLevels>, RefRW<StatModifiers>>()
                     .WithAll<PlayerTag>())
        {
            var levels = buffs.ValueRO;
            ref var stats = ref modifiers.ValueRW;

            stats.DamageMultiplier = 1f + GetDamageMultiplier(levels.DamageLevel);
            stats.FireRateMultiplier = 1f - GetFireRateReduction(levels.FireRateLevel);
            stats.BonusMissileCount = GetMissileBonus(levels.MissileCountLevel);
            stats.SpeedMultiplier = 1f + GetSpeedMultiplier(levels.SpeedLevel);
            stats.BonusMaxHealth = GetMaxHealthBonus(levels.MaxHealthLevel);
            stats.HealthRegenPerSecond = GetHealthRegen(levels.HealthRegenLevel);
            stats.MagnetRange = GetMagnetRange(levels.MagnetLevel);

            var (chance, mult) = GetCriticalStats(levels.CriticalLevel);
            stats.CriticalChance = chance;
            stats.CriticalMultiplier = mult;
        }
    }

    // Burst 호환 인라인 함수들
    private static float GetDamageMultiplier(int level) => level switch
    { 1 => 0.10f, 2 => 0.20f, 3 => 0.35f, 4 => 0.50f, 5 => 0.75f, _ => 0f };

    private static float GetFireRateReduction(int level) => level switch
    { 1 => 0.15f, 2 => 0.30f, 3 => 0.45f, 4 => 0.60f, 5 => 0.80f, _ => 0f };

    private static int GetMissileBonus(int level) => level switch
    { 1 => 1, 2 => 2, 3 => 3, 4 => 4, 5 => 6, _ => 0 };

    private static float GetSpeedMultiplier(int level) => level switch
    { 1 => 0.10f, 2 => 0.20f, 3 => 0.30f, 4 => 0.40f, 5 => 0.50f, _ => 0f };

    private static float GetMaxHealthBonus(int level) => level switch
    { 1 => 20f, 2 => 40f, 3 => 70f, 4 => 100f, 5 => 150f, _ => 0f };

    private static float GetHealthRegen(int level) => level switch
    { 1 => 1f, 2 => 2f, 3 => 3f, 4 => 5f, 5 => 8f, _ => 0f };

    private static float GetMagnetRange(int level) => level switch
    { 1 => 3f, 2 => 5f, 3 => 7f, 4 => 10f, 5 => 15f, _ => 0f };

    private static (float, float) GetCriticalStats(int level) => level switch
    { 1 => (5f, 2.0f), 2 => (10f, 2.0f), 3 => (15f, 2.5f),
      4 => (20f, 2.5f), 5 => (30f, 3.0f), _ => (0f, 1f) };
}
```

---

### 10.8 기존 시스템에 버프 적용 예시

#### AutoShootSystem 수정

```csharp
foreach (var (shootConfig, transform, modifiers) in
         SystemAPI.Query<RefRW<AutoShootConfig>, RefRO<LocalTransform>, RefRO<StatModifiers>>()
             .WithAll<PlayerTag>().WithDisabled<PlayerDead>())
{
    float effectiveFireRate = shootConfig.ValueRO.BaseFireRate * modifiers.ValueRO.FireRateMultiplier;
    shootConfig.ValueRW.TimeSinceLastShot += deltaTime;

    if (shootConfig.ValueRW.TimeSinceLastShot >= effectiveFireRate)
    {
        shootConfig.ValueRW.TimeSinceLastShot = 0f;
        int missileCount = shootConfig.ValueRO.BaseMissileCount + modifiers.ValueRO.BonusMissileCount;
        for (int i = 0; i < missileCount; i++) { /* 미사일 발사 */ }
    }
}
```

#### BulletHitSystem 데미지 계산

```csharp
float finalDamage = baseDamage * modifiers.DamageMultiplier;

if (modifiers.CriticalChance > 0f && random.NextFloat(0f, 100f) < modifiers.CriticalChance)
{
    finalDamage *= modifiers.CriticalMultiplier;
    // 치명타 이펙트 RPC 전송
}
enemyHealth.ValueRW.Value -= finalDamage;
```

---

### 10.9 버프 데이터 설정 (ScriptableObject)

```csharp
// Assets/Scripts/Buffs/Data/BuffDefinitionSO.cs
[CreateAssetMenu(fileName = "BuffDefinition", menuName = "Buffs/Buff Definition")]
public class BuffDefinitionSO : ScriptableObject
{
    public BuffType BuffType;
    public StatType TargetStat;
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public float[] LevelValues = new float[5];
    public ModifierType ModifierType = ModifierType.Multiplicative;
}

// Assets/Scripts/Buffs/Data/BuffDatabaseSO.cs
[CreateAssetMenu(fileName = "BuffDatabase", menuName = "Buffs/Buff Database")]
public class BuffDatabaseSO : ScriptableObject
{
    public BuffDefinitionSO[] Buffs;
    private Dictionary<BuffType, BuffDefinitionSO> _lookup;

    public void Initialize() => _lookup = Buffs.ToDictionary(b => b.BuffType);
    public BuffDefinitionSO GetDefinition(BuffType type) =>
        _lookup.TryGetValue(type, out var def) ? def : null;
}
```

---

### 10.10 확장 가이드: 새 버프 추가 방법

| Step | 작업 | 설명 |
|------|-----|------|
| 1 | BuffType enum 추가 | `Shield = 8` |
| 2 | StatType 추가 (필요시) | `ShieldAmount = 40` |
| 3 | 효과 클래스 구현 | `ShieldBuffEffect : BuffEffectBase` |
| 4 | BuffRegistry 등록 | `Register(new ShieldBuffEffect())` |
| 5 | PlayerBuffLevels 필드 추가 | `[GhostField] public int ShieldLevel` |
| 6 | StatModifiers 필드 추가 | `[GhostField] public float ShieldAmount` |
| 7 | 해당 시스템 수정 | `ShieldDamageSystem`에서 처리 |

---

### 10.11 시스템 실행 순서

```
Frame Start
    │
    ▼
┌────────────────────────────────────┐
│  BuffLevelChangeSystem             │ ← RPC 처리, 레벨 업데이트
└────────────────────────────────────┘
    │
    ▼
┌────────────────────────────────────┐
│  StatCalculationSystem             │ ← 버프 레벨 → StatModifiers
└────────────────────────────────────┘
    │
    ▼
┌────────────────────────────────────┐
│  HealthRegenSystem                 │ ← 체력 재생 적용
└────────────────────────────────────┘
    │
    ▼
┌────────────────────────────────────┐
│  Game Logic Systems                │ ← 버프 적용된 스탯 사용
│  - AutoShootSystem                 │
│  - PlayerMovementSystem            │
│  - MagnetSystem                    │
│  - BulletHitSystem                 │
└────────────────────────────────────┘
    │
    ▼
Frame End
```

---

### 10.12 장점 요약

| 원칙 | 구현 방법 | 이점 |
|-----|----------|-----|
| **단일 책임 (SRP)** | 각 버프 = 독립된 클래스 | 버프 수정 시 다른 버프 영향 없음 |
| **개방-폐쇄 (OCP)** | IBuffEffect + BuffRegistry | 새 버프 추가 시 기존 코드 수정 불필요 |
| **의존성 역전 (DIP)** | 인터페이스 의존 | 구체 구현 교체 용이 |
| **데이터 주도** | ScriptableObject 분리 | 밸런싱 시 코드 수정 불필요 |
| **ECS 호환** | StatModifiers + Burst | 고성능 + 네트워크 동기화 |

---

## 11. 확장 가능성

- **버프 타입 추가**: BuffType enum에 추가만 하면 됨
- **시너지 효과**: 특정 버프 조합 시 추가 효과
- **희귀 버프**: 낮은 확률로 등장하는 강력한 버프
- **저주 버프**: 단점이 있지만 강력한 효과
- **무한 레벨**: 레벨 6+ 시 효과 증가폭 감소하며 무한 성장
