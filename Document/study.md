# Unity DOTS 학습 노트

## 목차
1. [SystemGroup 실행 순서](#systemgroup-실행-순서)
2. [SystemBase vs ISystem](#systembase-vs-isystem)

---

## SystemGroup 실행 순서

Unity DOTS는 매 프레임마다 세 가지 주요 SystemGroup을 순서대로 실행합니다.

### 실행 순서

```
InitializationSystemGroup → SimulationSystemGroup → PresentationSystemGroup
```

### 1. InitializationSystemGroup
**역할**: 프레임 시작 시 데이터 수집 및 초기화

**주요 용도**:
- 입력 수집 (키보드, 마우스, 게임패드)
- 네트워크 메시지 수신
- 외부 데이터 읽기
- 프레임 초기 상태 설정

**예시**:
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 입력 수집만 담당
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        // PlayerInput 컴포넌트에 저장
    }
}
```

### 2. SimulationSystemGroup
**역할**: 게임 로직 및 물리 시뮬레이션 처리

**주요 용도**:
- 이동, 회전 등 Transform 변경
- 물리 시뮬레이션 (충돌, 중력)
- AI 로직
- 게임 규칙 적용
- 상태 업데이트

**예시**:
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 입력을 기반으로 이동 처리
        new PlayerMovementJob { DeltaTime = SystemAPI.Time.DeltaTime }
            .ScheduleParallel();
    }
}
```

### 3. PresentationSystemGroup
**역할**: 렌더링 준비 및 시각적 표현

**주요 용도**:
- 카메라 업데이트
- 애니메이션 적용
- VFX/파티클 업데이트
- UI 갱신
- 렌더링 데이터 준비

### 데이터 흐름

```
InitializationSystemGroup:  입력 수집
         ↓
SimulationSystemGroup:      입력 → 이동 계산
         ↓
PresentationSystemGroup:    이동 결과 → 렌더링
```

---

## SystemBase vs ISystem

Unity DOTS에서 시스템을 작성하는 두 가지 방식의 차이점입니다.

### 비교표

| 특징 | SystemBase | ISystem |
|------|-----------|---------|
| **타입** | Class (참조 타입) | Struct (값 타입) |
| **시대** | Legacy (레거시) | Modern (최신 권장) |
| **Burst 컴파일** | ❌ 불가능 | ✅ 가능 |
| **성능** | 보통 (GC 오버헤드) | 높음 (GC 없음) |
| **Unity API 접근** | ✅ 가능 | ❌ 불가능 |
| **Managed 타입** | ✅ 사용 가능 | ❌ 사용 불가 |
| **병렬 처리** | Entities.ForEach | IJobEntity |

### SystemBase (Class 기반)

**장점**:
- Unity API 사용 가능 (`Input.GetAxis`, `Debug.Log` 등)
- Managed 타입 사용 가능 (string, class)
- 작성이 간단함

**단점**:
- Burst 컴파일 불가 → 성능 낮음
- GC 오버헤드 발생
- 레거시 방식

**사용 시기**:
- Unity API가 필수인 경우 (Input.GetAxis 등)
- Managed 타입이 필요한 경우
- 성능이 크리티컬하지 않은 경우

### ISystem (Struct 기반)

**장점**:
- Burst 컴파일 가능 → 고성능
- GC 오버헤드 없음
- 병렬 처리 최적화
- Unity 권장 방식

**단점**:
- Unity API 사용 불가
- Managed 타입 사용 불가
- 순수 계산 로직만 가능

**사용 시기**:
- 순수 계산 로직 (이동, 회전, 충돌 등)
- 대량의 엔티티 처리
- 고성능이 요구되는 경우
- **대부분의 게임 로직 시스템**

### 선택 가이드

```
Unity API 필요? (Input, Debug, GameObject 등)
    ↓ YES
SystemBase 사용

    ↓ NO

순수 계산/로직만 수행?
    ↓ YES
ISystem 사용 (권장)
```

### 프로젝트 적용 예시

**PlayerInputSystem → SystemBase 선택 이유**:
- `Input.GetAxis()`는 Unity API이므로 SystemBase 필요

**PlayerMovementSystem → ISystem 선택 이유**:
- 순수 계산만 수행 → Burst 최적화 가능
- 1000개 엔티티 처리 시 약 8배 빠름

---

## 핵심 정리

### SystemGroup 순서
1. **InitializationSystemGroup**: 데이터 수집 (입력, 네트워크)
2. **SimulationSystemGroup**: 게임 로직 (이동, 물리)
3. **PresentationSystemGroup**: 렌더링 준비 (카메라, 애니메이션)

### System 타입 선택
- **SystemBase**: Unity API가 필요한 경우만 (입력, 디버그)
- **ISystem**: 나머지 모든 경우 (권장, 고성능)

### 프로젝트 적용
```
PlayerInputSystem (SystemBase)          → InitializationSystemGroup
    ↓ (PlayerInput 데이터)
PlayerMovementSystem (ISystem + Burst)  → SimulationSystemGroup
```

---

**작성일**: 2025-11-26
**프로젝트**: projectc (Unity DOTS Phase 1)
