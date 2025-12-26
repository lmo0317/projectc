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

## 10. 확장 가능성

- **버프 타입 추가**: BuffType enum에 추가만 하면 됨
- **시너지 효과**: 특정 버프 조합 시 추가 효과
- **희귀 버프**: 낮은 확률로 등장하는 강력한 버프
- **저주 버프**: 단점이 있지만 강력한 효과
- **무한 레벨**: 레벨 6+ 시 효과 증가폭 감소하며 무한 성장
