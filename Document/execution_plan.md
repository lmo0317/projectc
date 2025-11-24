# Phase 1 실행 계획: 프로젝트 초기 설정 및 플레이어 이동 시스템

## 프로젝트 개요

**Phase**: Phase 1 - 프로젝트 초기 설정 및 플레이어 이동 시스템
**목표**: Unity DOTS 환경을 구축하고 ECS 기반 플레이어 엔티티와 DPAD 이동 시스템 구현
**핵심 원칙**:
- 정의된 내용만 구현 (추가 기능 배제)
- 쉽고 간결한 구현 선택
- SOLID 원칙 준수
- 단계별 검증 필수

## Phase 1 개발 사항 요약

이 단계에서는 플레이어가 DPAD(WASD/Arrow Keys) 입력을 통해 3D 공간에서 이동할 수 있는 최소 기능을 완성합니다.

### 구현 범위
1. DOTS 패키지 설치 및 프로젝트 구조 설정
2. 플레이어 ECS 컴포넌트 및 Authoring 구현
3. 입력 및 이동 시스템 구현
4. 기본 씬 설정 및 검증

---

## 태스크 분해

### **TASK-001: DOTS 환경 구축 및 프로젝트 구조 설정**

**카테고리**: 시스템/인프라
**우선순위**: P0 (최우선)
**설명**: Unity DOTS 개발 환경 완전 설정 및 프로젝트 폴더 구조 구축

**구현 내용**:

#### 1.1 DOTS 패키지 설치
- Unity Package Manager를 통한 필수 패키지 설치:
  - **Entities** - ECS 핵심 패키지
  - **Burst Compiler** - 고성능 코드 컴파일러
  - **Collections** - 네이티브 컬렉션 (NativeArray 등)
  - **Mathematics** - 수학 라이브러리 (float3, quaternion 등)
  - **Unity Physics** - DOTS 물리 엔진
- 각 패키지 설치 후 콘솔 에러 확인
- 설치 완료 후 Unity 재시작

#### 1.2 프로젝트 폴더 구조 생성
Assets 하위에 다음 폴더 생성:
```
Assets/
├── Scripts/
│   ├── Components/      # ECS IComponentData 정의
│   ├── Systems/         # ECS SystemBase/ISystem 정의
│   └── Authoring/       # MonoBehaviour → Entity 변환 클래스
├── Prefabs/             # 게임 오브젝트 프리팹
└── Scenes/              # 씬 파일 (기존 사용)
```

#### 1.3 기본 검증
- 테스트용 빈 SystemBase 스크립트 작성 및 컴파일 확인
- Entity Debugger 열기 (Window → Entities → Hierarchy)
- 에러 없이 프로젝트 실행 확인

**의존성**: 없음

**완료 조건**:
- [ ] 5개 DOTS 패키지 모두 설치 완료
- [ ] Package Manager에서 설치된 패키지 확인 가능
- [ ] 프로젝트 컴파일 에러 없음
- [ ] Scripts 폴더 구조 생성 완료
- [ ] Entity Debugger가 정상적으로 열림
- [ ] 테스트 System이 컴파일되고 실행됨

**예상 작업량**: 2-3시간

---

### **TASK-002: 플레이어 ECS 컴포넌트 및 Authoring 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 플레이어 엔티티를 위한 ECS 컴포넌트와 GameObject → Entity 변환 Authoring 클래스 구현

**구현 내용**:

#### 2.1 플레이어 컴포넌트 정의
`Assets/Scripts/Components/` 폴더에 다음 컴포넌트 작성:

**PlayerTag.cs** (플레이어 식별용)
```csharp
using Unity.Entities;

public struct PlayerTag : IComponentData
{
}
```

**MovementSpeed.cs** (이동 속도 데이터)
```csharp
using Unity.Entities;

public struct MovementSpeed : IComponentData
{
    public float Value;
}
```

**PlayerInput.cs** (입력 데이터 저장)
```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct PlayerInput : IComponentData
{
    public float2 Movement; // DPAD 입력 (x, y)
}
```

#### 2.2 Authoring 클래스 구현
`Assets/Scripts/Authoring/PlayerAuthoring.cs` 작성:

```csharp
using Unity.Entities;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerTag());
            AddComponent(entity, new MovementSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new PlayerInput { Movement = float2.zero });
        }
    }
}
```

#### 2.3 검증
- 각 스크립트 컴파일 에러 없이 작성
- SOLID 원칙: 단일 책임 원칙(SRP) - 각 컴포넌트는 하나의 데이터만 담당
- 간결한 구조 유지

**의존성**: TASK-001

**완료 조건**:
- [ ] PlayerTag, MovementSpeed, PlayerInput 컴포넌트 작성 완료
- [ ] PlayerAuthoring 클래스 작성 완료
- [ ] 모든 스크립트 컴파일 에러 없음
- [ ] Authoring 클래스의 Baker가 정상 작동함
- [ ] Inspector에서 MoveSpeed 파라미터 노출 확인

**예상 작업량**: 2-3시간

---

### **TASK-003: 입력 및 이동 시스템 구현**

**카테고리**: 게임플레이
**우선순위**: P0 (최우선)
**설명**: 플레이어 입력을 처리하고 이동을 적용하는 ECS 시스템 구현

**구현 내용**:

#### 3.1 입력 시스템 구현
`Assets/Scripts/Systems/PlayerInputSystem.cs` 작성:

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // WASD 또는 Arrow Keys 입력 읽기
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down

        float2 input = new float2(horizontal, vertical);

        // 모든 PlayerInput 컴포넌트에 입력값 저장
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref PlayerInput playerInput) =>
            {
                playerInput.Movement = input;
            })
            .Schedule();
    }
}
```

**설계 원칙**:
- 입력 처리만 담당 (단일 책임)
- MainThread에서 실행 (Input.GetAxis는 메인 스레드 전용)
- InitializationSystemGroup에서 실행하여 이동 전 입력 수집

#### 3.2 이동 시스템 구현
`Assets/Scripts/Systems/PlayerMovementSystem.cs` 작성:

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // IJobEntity를 사용한 병렬 처리
        new PlayerMovementJob
        {
            DeltaTime = deltaTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct PlayerMovementJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in PlayerInput input, in MovementSpeed speed)
    {
        if (math.lengthsq(input.Movement) > 0.01f)
        {
            // 입력 방향 정규화
            float2 direction = math.normalize(input.Movement);

            // 3D 공간 이동 (XZ 평면)
            float3 movement = new float3(direction.x, 0, direction.y) * speed.Value * DeltaTime;

            // Transform 위치 업데이트
            transform.Position += movement;
        }
    }
}
```

**설계 원칙**:
- Burst Compile 적용으로 고성능 처리
- IJobEntity를 통한 병렬 처리
- SimulationSystemGroup에서 실행하여 물리/이동 처리
- SOLID의 개방-폐쇄 원칙(OCP): 확장 가능한 구조

#### 3.3 시스템 검증
- 두 시스템 모두 컴파일 에러 없이 작성
- Burst Compiler 적용 확인
- 의존성 관리: 입력 → 이동 순서 보장

**의존성**: TASK-002

**완료 조건**:
- [ ] PlayerInputSystem 작성 완료
- [ ] PlayerMovementSystem 작성 완료
- [ ] 두 시스템 모두 컴파일 에러 없음
- [ ] Burst Compile 속성 적용됨
- [ ] IJobEntity를 사용한 병렬 처리 구현
- [ ] 시스템 실행 순서가 올바름 (입력 → 이동)

**예상 작업량**: 4-5시간

---

### **TASK-004: 기본 씬 설정 및 통합 테스트**

**카테고리**: 환경 설정
**우선순위**: P0 (최우선)
**설명**: 게임 씬을 구성하고 전체 시스템을 통합 테스트하여 Phase 1 완료 검증

**구현 내용**:

#### 4.1 씬 구성
기존 `Assets/Scenes/SampleScene.unity`를 수정:

**Ground 평면 생성**:
1. GameObject → 3D Object → Plane 생성
2. 이름: "Ground"
3. Transform: Position (0, 0, 0), Scale (2, 1, 2) - 20x20 크기
4. Material: 기본 또는 간단한 색상 적용

**플레이어 GameObject 생성**:
1. GameObject → 3D Object → Capsule 생성
2. 이름: "Player"
3. Transform: Position (0, 1, 0) - Ground 위 1유닛
4. PlayerAuthoring 컴포넌트 추가
5. Inspector에서 MoveSpeed 설정 (기본값: 5)

**카메라 설정**:
1. Main Camera 선택
2. Transform:
   - Position: (0, 15, -10) - 탑다운 뷰
   - Rotation: (45, 0, 0) - 아래를 바라봄
3. 또는 Isometric 뷰:
   - Position: (10, 10, 10)
   - Rotation: (30, -45, 0)

#### 4.2 입력 설정 확인
- Edit → Project Settings → Input Manager
- Horizontal 축: A/D, Left/Right Arrow 확인
- Vertical 축: W/S, Up/Down Arrow 확인
- (Unity 기본 설정이므로 일반적으로 수정 불필요)

#### 4.3 통합 테스트
**Play 모드 실행 테스트**:
1. Unity Editor에서 Play 버튼 클릭
2. WASD 키 입력하여 플레이어 이동 확인
3. Arrow Keys로도 이동 확인
4. 대각선 이동 테스트 (W+A, W+D 등)

**Entity Debugger 검증**:
1. Window → Entities → Hierarchy 열기
2. Play 모드에서 Player 엔티티 확인
3. 컴포넌트 목록 확인:
   - PlayerTag
   - MovementSpeed
   - PlayerInput
   - LocalTransform
4. 입력 시 PlayerInput.Movement 값 변화 확인

**성능 검증**:
1. Window → Analysis → Profiler 열기
2. Play 모드에서 CPU Usage 확인
3. PlayerMovementSystem이 Burst로 컴파일되었는지 확인
4. Job System이 작동하는지 확인 (Profiler → Jobs)

#### 4.4 문제 해결 체크리스트
만약 문제 발생 시:
- [ ] 플레이어가 이동하지 않음 → Input Manager 설정 확인
- [ ] 컴파일 에러 → 패키지 버전 확인, Unity 재시작
- [ ] Entity가 생성되지 않음 → Authoring Baker 확인
- [ ] 성능 문제 → Burst Compile 적용 여부 확인

**의존성**: TASK-003

**완료 조건**:
- [ ] Ground 평면 생성됨
- [ ] Player GameObject 생성 및 PlayerAuthoring 부착됨
- [ ] 카메라가 플레이어를 볼 수 있게 배치됨
- [ ] Play 모드에서 WASD로 플레이어 이동 가능
- [ ] Arrow Keys로도 이동 가능
- [ ] 대각선 이동 정상 작동
- [ ] Entity Debugger에서 Player 엔티티 및 컴포넌트 확인 가능
- [ ] PlayerInput.Movement 값이 입력에 따라 변화함
- [ ] Profiler에서 Burst 컴파일 확인
- [ ] 프레임률 60 FPS 이상 유지
- [ ] 콘솔에 에러 없음

**예상 작업량**: 3-4시간

---

## Phase 1 전체 검증 체크리스트

### 필수 검증 항목 (SPEC.md 기준)
- [ ] 프로젝트가 에러 없이 실행됨
- [ ] DOTS 패키지가 정상적으로 설치되고 작동함
- [ ] 플레이어 엔티티가 씬에 생성됨
- [ ] WASD 또는 Arrow Key 입력 시 플레이어가 상하좌우로 이동함
- [ ] 이동 속도가 Inspector에서 설정한 값대로 작동함
- [ ] Burst Compiler가 활성화되어 시스템이 컴파일됨
- [ ] Entity Debugger에서 플레이어 엔티티와 컴포넌트 확인 가능
- [ ] 성능 프로파일러에서 Job System이 작동하는지 확인
- [ ] 플레이어가 Ground 평면 위에서만 이동함

### 추가 검증 (안정성)
- [ ] Unity 재시작 후에도 정상 작동
- [ ] 장시간 플레이(5분) 시 메모리 누수 없음
- [ ] 빌드 테스트 (Standalone Windows)

---

## 작업 순서 및 일정

### 권장 진행 순서
1. **Day 1 (2-3시간)**: TASK-001 완료
   - DOTS 패키지 설치
   - 프로젝트 구조 설정
   - 기본 검증

2. **Day 1-2 (2-3시간)**: TASK-002 완료
   - 컴포넌트 작성
   - Authoring 작성
   - 컴파일 확인

3. **Day 2-3 (4-5시간)**: TASK-003 완료
   - 입력 시스템 구현
   - 이동 시스템 구현
   - Burst 적용

4. **Day 3 (3-4시간)**: TASK-004 완료
   - 씬 설정
   - 통합 테스트
   - 최종 검증

### 총 예상 소요 시간
- **필수 작업**: 11-15시간 (약 2-3일)
- **검증 및 문제 해결**: 추가 2-3시간
- **전체**: 13-18시간

---

## 의존성 다이어그램

```
TASK-001 (DOTS 환경 구축)
    ↓
TASK-002 (컴포넌트/Authoring)
    ↓
TASK-003 (입력/이동 시스템)
    ↓
TASK-004 (씬 설정 및 검증)
```

---

## SOLID 원칙 준수 확인

### Single Responsibility Principle (단일 책임)
- **PlayerTag**: 플레이어 식별만 담당
- **MovementSpeed**: 속도 데이터만 저장
- **PlayerInput**: 입력 데이터만 저장
- **PlayerInputSystem**: 입력 수집만 담당
- **PlayerMovementSystem**: 이동 처리만 담당

### Open/Closed Principle (개방/폐쇄)
- IJobEntity 구조로 확장 가능
- 새로운 컴포넌트 추가 시 기존 코드 수정 불필요

### Liskov Substitution Principle (리스코프 치환)
- IComponentData, ISystem 인터페이스를 올바르게 구현

### Interface Segregation Principle (인터페이스 분리)
- 각 컴포넌트는 필요한 데이터만 포함
- 불필요한 의존성 없음

### Dependency Inversion Principle (의존성 역전)
- 시스템은 구체적 구현이 아닌 컴포넌트 인터페이스에 의존

---

## 문제 해결 가이드

### 일반적인 문제

**1. DOTS 패키지 설치 오류**
- Unity 버전 확인 (2022.3 LTS 이상 권장)
- Package Manager 캐시 삭제: `Window → Package Manager → 톱니바퀴 → Clear Cache`
- Unity 재시작

**2. Burst Compile 오류**
- Managed Type 사용 확인 (string, class 등 불가)
- NativeArray 등 네이티브 타입만 사용
- [BurstCompile] 속성 제거 후 재시도

**3. Entity가 생성되지 않음**
- Baking 설정 확인: GameObject에 Authoring 컴포넌트 부착 확인
- Entity Conversion 설정: SubScene 없이도 작동해야 함
- 콘솔 에러 메시지 확인

**4. 입력이 작동하지 않음**
- Input Manager 설정 확인
- PlayerInputSystem이 실행되는지 확인 (Debugger)
- Input.GetAxis 대신 Unity Input System 사용 고려 (Phase 1에서는 Legacy 사용)

---

## 다음 단계 (Phase 2 준비)

Phase 1 완료 후:
1. **코드 리뷰**: 모든 스크립트 검토
2. **Git 커밋**: "Phase 1 완료: DOTS 환경 및 플레이어 이동 시스템"
3. **문서화**: 주요 결정 사항 README에 기록
4. **Phase 2 준비**: 자동 발사 시스템 설계 검토

---

## 참고 사항

### 코드 스타일
- C# Naming Convention 준수
- 명확한 변수명 사용
- 간결한 주석 (필요시에만)

### Unity 설정
- Auto Save 활성화 권장
- Version Control 활성화 (Git)
- Scene 변경 시 저장 확인

### 성능 고려사항
- 이 단계에서는 플레이어 1명만 처리하므로 성능 문제 없음
- Burst 적용으로 향후 확장성 확보

---

**Phase 1 시작 준비 완료!**
TASK-001부터 순차적으로 진행하세요.
