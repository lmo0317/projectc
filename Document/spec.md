# PRD: Unity DOTS 기반 3D 서바이벌 슈터 게임

## 프로젝트 개요
뱀파이어 서바이벌, 탕탕 특공대 스타일의 3D 서바이벌 슈터 게임을 Unity DOTS(Data-Oriented Technology Stack)를 활용하여 개발합니다. ECS(Entity Component System) 아키텍처, Burst Compiler, Job System을 통해 고성능 최적화를 달성하는 것이 핵심 목표입니다.

## 기술 스택
- **엔진**: Unity (2022.3 LTS 이상 권장)
- **아키텍처**: DOTS (ECS, Burst Compiler, Job System)
- **렌더링**: URP (Universal Render Pipeline)
- **물리**: Unity Physics (DOTS 호환)

## 핵심 게임플레이
1. 플레이어는 화면 중앙에 배치되며 DPAD로 이동
2. 몬스터는 주기적으로 플레이어 주변에 스폰
3. 플레이어는 자동으로 총을 발사하며 몬스터를 피해 생존

## 필수 지침
- 정의된 내용 외에 다른 내용이 추가 되지 않도록 유의해주고 복잡한 구현보다 쉽고 간결한 구현을 선택해줘.
- SOLID 원칙을 잘 지키는 결과를 낼 수 있도록 계획 해줘
- 단계별 구현이 끝날 때마다 프로젝트에 문제가 없는지 확인하고 문제가 있다면 해결 해줘
- 너가 한일은 최대한 간결하게 정리 하고 내가 해야될 추가 작업이 있으면 step by step으로 하나씩 설명 해줘

---

## Phase 1: 프로젝트 초기 설정 및 플레이어 이동 시스템

### 이번 단계 개발 사항 오버뷰
Unity DOTS 프로젝트의 기초 환경을 구축하고, ECS 기반의 플레이어 엔티티와 기본 이동 시스템을 구현합니다. 이 단계에서는 플레이어가 DPAD 입력을 통해 3D 공간에서 이동할 수 있는 최소 기능을 완성합니다.

### 개발 기능 체크리스트
- [ ] Unity 프로젝트 생성 및 DOTS 패키지 설치
  - [ ] Entities 패키지
  - [ ] Burst Compiler
  - [ ] Collections
  - [ ] Mathematics
  - [ ] Unity Physics
- [ ] 프로젝트 폴더 구조 설정
  - [ ] `/Scripts/Components` - ECS 컴포넌트
  - [ ] `/Scripts/Systems` - ECS 시스템
  - [ ] `/Scripts/Authoring` - MonoBehaviour 변환 클래스
  - [ ] `/Prefabs` - 게임 오브젝트 프리팹
  - [ ] `/Scenes` - 씬 파일
- [ ] 플레이어 ECS 컴포넌트 구현
  - [ ] `PlayerTag` (IComponentData) - 플레이어 식별 태그
  - [ ] `MovementSpeed` (IComponentData) - 이동 속도 데이터
  - [ ] `PlayerInput` (IComponentData) - 입력 데이터 저장
- [ ] 플레이어 Authoring 클래스 구현
  - [ ] `PlayerAuthoring` (MonoBehaviour → Entity 변환)
  - [ ] Inspector에서 이동 속도 설정 가능
- [ ] 입력 시스템 구현
  - [ ] `PlayerInputSystem` (SystemBase)
  - [ ] DPAD 입력 감지 (WASD 또는 Arrow Keys)
  - [ ] 입력값을 `PlayerInput` 컴포넌트에 저장
- [ ] 플레이어 이동 시스템 구현
  - [ ] `PlayerMovementSystem` (ISystem with Burst)
  - [ ] `PlayerInput`과 `MovementSpeed`를 읽어 Transform 업데이트
  - [ ] IJobEntity 활용하여 Job System으로 구현
- [ ] 기본 씬 설정
  - [ ] 평면(Ground) 생성
  - [ ] 플레이어 엔티티 배치 (화면 중앙)
  - [ ] 카메라 설정 (Top-down 또는 Isometric 뷰)
- [ ] 간단한 UI DPAD 표시 (선택사항)
  - [ ] 키보드 입력 가이드 UI

### 작업 완료 후 검증 체크리스트
- [ ] 프로젝트가 에러 없이 실행됨
- [ ] DOTS 패키지가 정상적으로 설치되고 작동함
- [ ] 플레이어 엔티티가 씬에 생성됨
- [ ] WASD 또는 Arrow Key 입력 시 플레이어가 상하좌우로 이동함
- [ ] 이동 속도가 Inspector에서 설정한 값대로 작동함
- [ ] Burst Compiler가 활성화되어 시스템이 컴파일됨
- [ ] Entity Debugger에서 플레이어 엔티티와 컴포넌트 확인 가능
- [ ] 성능 프로파일러에서 Job System이 작동하는지 확인
- [ ] 플레이어가 Ground 평면 위에서만 이동함 (경계 설정 선택사항)

---

## Phase 2: 자동 발사 시스템 구현

### 이번 단계 개발 사항 오버뷰
플레이어가 자동으로 총알을 발사하는 시스템을 ECS 구조로 구현합니다. 총알은 Entity로 생성되며, 일정 간격으로 발사되고 직선으로 이동합니다. 오브젝트 풀링 패턴을 고려하여 성능을 최적화합니다.

### 개발 기능 체크리스트
- [ ] 총알 ECS 컴포넌트 구현
  - [ ] `BulletTag` (IComponentData) - 총알 식별 태그
  - [ ] `BulletSpeed` (IComponentData) - 총알 이동 속도
  - [ ] `BulletLifetime` (IComponentData) - 총알 생존 시간
  - [ ] `BulletDirection` (IComponentData) - 발사 방향
- [ ] 총알 Authoring 및 Prefab 설정
  - [ ] `BulletAuthoring` 클래스 구현
  - [ ] 총알 Entity Prefab 생성 (시각적 표현: 간단한 Sphere 또는 Capsule)
- [ ] 플레이어 발사 컴포넌트 추가
  - [ ] `AutoShootConfig` (IComponentData)
    - 발사 간격 (Fire Rate)
    - 다음 발사까지 남은 시간
    - 총알 Prefab Entity 참조
- [ ] 자동 발사 시스템 구현
  - [ ] `AutoShootSystem` (ISystem with Burst)
  - [ ] 타이머 기반으로 발사 간격 관리
  - [ ] `EntityCommandBuffer`를 사용하여 총알 Entity 생성
  - [ ] 플레이어 이동 방향 또는 고정 방향으로 발사 (초기에는 위쪽 고정)
- [ ] 총알 이동 시스템 구현
  - [ ] `BulletMovementSystem` (ISystem with Burst)
  - [ ] IJobEntity 활용하여 총알 위치 업데이트
  - [ ] 방향과 속도를 곱해 Transform 이동
- [ ] 총알 생명주기 관리 시스템
  - [ ] `BulletLifetimeSystem` (ISystem with Burst)
  - [ ] Lifetime 감소 및 0 이하 시 Entity 삭제
  - [ ] EntityCommandBuffer 활용
- [ ] 총알 시각적 표현
  - [ ] Hybrid Renderer를 통한 메시 렌더링
  - [ ] 간단한 Material 적용

### 작업 완료 후 검증 체크리스트
- [ ] 게임 시작 시 플레이어가 자동으로 총알을 발사함
- [ ] 설정한 발사 간격대로 총알이 생성됨
- [ ] 총알이 지정된 방향으로 일정 속도로 이동함
- [ ] 총알이 생존 시간이 끝나면 자동으로 삭제됨
- [ ] Entity Debugger에서 총알 Entity 생성/삭제 확인 가능
- [ ] 성능 프로파일러에서 총알 시스템이 Burst로 컴파일되었는지 확인
- [ ] 다수의 총알(100개 이상)이 생성되어도 프레임 드랍 없이 작동함
- [ ] 총알이 화면에 시각적으로 표시됨
- [ ] 플레이어가 이동하면서도 총알이 정상적으로 발사됨

---

## Phase 3: 몬스터 스폰 시스템 구현

### 이번 단계 개발 사항 오버뷰
몬스터가 주기적으로 플레이어 주변에 스폰되는 시스템을 구현합니다. 몬스터는 Entity로 생성되며, 스폰 위치와 주기를 관리하는 시스템을 ECS로 구축합니다.

### 개발 기능 체크리스트
- [ ] 몬스터 ECS 컴포넌트 구현
  - [ ] `EnemyTag` (IComponentData) - 몬스터 식별 태그
  - [ ] `EnemyHealth` (IComponentData) - 체력 (Phase 4에서 활용)
  - [ ] `EnemySpeed` (IComponentData) - 이동 속도 (Phase 4에서 활용)
- [ ] 몬스터 Authoring 및 Prefab 설정
  - [ ] `EnemyAuthoring` 클래스 구현
  - [ ] 몬스터 Entity Prefab 생성 (시각적 표현: Cube 또는 간단한 모델)
- [ ] 스폰 매니저 컴포넌트 구현
  - [ ] `EnemySpawnConfig` (IComponentData)
    - 스폰 간격
    - 다음 스폰까지 남은 시간
    - 몬스터 Prefab Entity 참조
    - 스폰 반경 (플레이어로부터의 거리)
    - 동시 존재 가능한 최대 몬스터 수 (선택사항)
- [ ] 스폰 매니저 Authoring 설정
  - [ ] `EnemySpawnAuthoring` 클래스
  - [ ] 싱글톤 Entity로 구성 (하나의 스폰 매니저)
- [ ] 몬스터 스폰 시스템 구현
  - [ ] `EnemySpawnSystem` (ISystem with Burst)
  - [ ] 타이머 기반 스폰 주기 관리
  - [ ] 플레이어 위치 참조 (PlayerTag 쿼리)
  - [ ] 플레이어 주변 랜덤 위치에 몬스터 스폰
  - [ ] Unity.Mathematics.Random을 사용한 랜덤 위치 계산
  - [ ] EntityCommandBuffer로 몬스터 Entity 생성
- [ ] 몬스터 시각적 표현
  - [ ] Hybrid Renderer를 통한 메시 렌더링
  - [ ] 플레이어와 구분되는 색상/형태 적용

### 작업 완료 후 검증 체크리스트
- [ ] 게임 시작 후 설정된 시간 간격으로 몬스터가 스폰됨
- [ ] 몬스터가 플레이어 주변의 랜덤 위치에 생성됨
- [ ] 스폰 반경이 설정값대로 작동함 (플레이어에게 너무 가깝거나 멀지 않음)
- [ ] Entity Debugger에서 몬스터 Entity 확인 가능
- [ ] 다수의 몬스터(50개 이상)가 생성되어도 성능 저하 없음
- [ ] 몬스터가 화면에 시각적으로 표시됨
- [ ] 최대 몬스터 수 제한이 있는 경우 정상 작동함
- [ ] 플레이어가 이동해도 스폰 위치가 플레이어 기준으로 계산됨

---

## Phase 4: 몬스터 AI - 플레이어 추적 시스템

### 이번 단계 개발 사항 오버뷰
스폰된 몬스터가 플레이어를 향해 이동하는 AI 시스템을 구현합니다. 몬스터는 플레이어의 위치를 추적하며 일정 속도로 접근합니다.

### 개발 기능 체크리스트
- [ ] 몬스터 추적 시스템 구현
  - [ ] `EnemyChaseSystem` (ISystem with Burst)
  - [ ] 플레이어 위치 쿼리 (PlayerTag + Transform)
  - [ ] 각 몬스터에서 플레이어로의 방향 벡터 계산
  - [ ] 방향을 정규화하고 속도를 곱해 이동
  - [ ] IJobEntity 활용하여 병렬 처리
- [ ] 몬스터 회전 시스템 (선택사항)
  - [ ] 몬스터가 이동 방향을 바라보도록 Rotation 업데이트
- [ ] 성능 최적화
  - [ ] Burst Compile 적용 확인
  - [ ] Job System 병렬화 확인
  - [ ] 불필요한 쿼리 최소화

### 작업 완료 후 검증 체크리스트
- [ ] 스폰된 몬스터가 플레이어를 향해 이동함
- [ ] 플레이어가 이동하면 몬스터도 새로운 위치를 추적함
- [ ] 몬스터 이동 속도가 설정값대로 작동함
- [ ] 다수의 몬스터(100개 이상)가 동시에 추적해도 성능 저하 없음
- [ ] Entity Debugger에서 몬스터의 Transform 변화 확인 가능
- [ ] 몬스터가 플레이어에게 겹쳐져도 계속 추적 시도함 (충돌은 Phase 5에서 처리)
- [ ] 성능 프로파일러에서 Chase System이 Burst로 최적화되었는지 확인

---

## Phase 5: 충돌 감지 및 데미지 시스템

### 이번 단계 개발 사항 오버뷰
총알과 몬스터, 몬스터와 플레이어 간의 충돌을 감지하고 처리하는 시스템을 구현합니다. Unity Physics for DOTS를 활용하여 ECS 기반 물리 충돌을 처리합니다.

### 개발 기능 체크리스트
- [ ] Unity Physics 컴포넌트 설정
  - [ ] 플레이어 Entity에 `PhysicsCollider` 추가 (Sphere 또는 Capsule)
  - [ ] 몬스터 Entity에 `PhysicsCollider` 추가
  - [ ] 총알 Entity에 `PhysicsCollider` 추가 (작은 Sphere)
  - [ ] 각 Entity에 적절한 Collision Filter 설정
- [ ] 데미지 컴포넌트 추가
  - [ ] `DamageValue` (IComponentData) - 총알이 입히는 데미지
  - [ ] `PlayerHealth` (IComponentData) - 플레이어 체력
- [ ] 총알-몬스터 충돌 처리 시스템
  - [ ] `BulletHitSystem` (ISystem)
  - [ ] ITriggerEventsJob 또는 Physics Trigger 이벤트 활용
  - [ ] 충돌 시 몬스터 체력 감소
  - [ ] 체력이 0 이하면 몬스터 Entity 삭제
  - [ ] 총알 Entity 삭제
  - [ ] EntityCommandBuffer 활용
- [ ] 몬스터-플레이어 충돌 처리 시스템
  - [ ] `PlayerDamageSystem` (ISystem)
  - [ ] 충돌 시 플레이어 체력 감소
  - [ ] 체력이 0 이하면 게임 오버 로직 (Phase 6에서 확장)
- [ ] 충돌 필터 최적화
  - [ ] 총알은 플레이어와 충돌하지 않도록 설정
  - [ ] 몬스터는 서로 충돌하지 않도록 설정 (선택사항)

### 작업 완료 후 검증 체크리스트
- [ ] 총알이 몬스터에 맞으면 몬스터가 삭제됨
- [ ] 총알이 몬스터에 맞으면 총알도 삭제됨
- [ ] 몬스터 체력이 설정값대로 작동함 (1발 이상 필요 시)
- [ ] 몬스터가 플레이어와 충돌하면 플레이어 체력이 감소함
- [ ] 플레이어 체력이 0이 되면 로그 또는 Debug 메시지 출력 (게임 오버)
- [ ] 총알이 플레이어와 충돌하지 않음
- [ ] 다수의 충돌(100+ 동시)이 발생해도 성능 저하 없음
- [ ] Physics Debugger에서 충돌 이벤트 확인 가능
- [ ] Entity Debugger에서 체력 값 변화 확인 가능

---

## Phase 6: UI 시스템 - 체력 표시 및 게임 오버

### 이번 단계 개발 사항 오버뷰
플레이어 체력, 생존 시간, 처치한 몬스터 수 등을 표시하는 UI를 구현합니다. 게임 오버 시 재시작 옵션을 제공합니다.

### 개발 기능 체크리스트
- [ ] UI 컴포넌트 추가
  - [ ] `GameStats` (IComponentData) - 게임 통계 저장
    - 생존 시간
    - 처치한 몬스터 수
    - 현재 웨이브 (선택사항)
- [ ] Managed Component 또는 Singleton Entity 활용
  - [ ] `GameStatsManaged` (IComponentData, managed)
  - [ ] UI 업데이트를 위한 데이터 브릿지
- [ ] UI Canvas 설정
  - [ ] TextMeshPro를 사용한 체력 표시
  - [ ] 생존 시간 표시 (초 단위)
  - [ ] 처치 수 카운터
- [ ] UI 업데이트 시스템
  - [ ] `UIUpdateSystem` (SystemBase, MainThread)
  - [ ] ECS 데이터를 읽어 UI Text 업데이트
  - [ ] 매 프레임 또는 값 변경 시에만 업데이트
- [ ] 게임 오버 UI
  - [ ] 게임 오버 패널 (Canvas)
  - [ ] 최종 통계 표시
  - [ ] 재시작 버튼
- [ ] 게임 오버 시스템
  - [ ] `GameOverSystem` (SystemBase)
  - [ ] 플레이어 체력 0 감지
  - [ ] 게임 일시정지 또는 Entity 스폰 중지
  - [ ] 게임 오버 UI 활성화
- [ ] 재시작 로직
  - [ ] 씬 재로드 또는 Entity 초기화
  - [ ] 모든 몬스터/총알 Entity 삭제
  - [ ] 플레이어 체력 및 통계 초기화

### 작업 완료 후 검증 체크리스트
- [ ] 게임 실행 시 플레이어 체력이 UI에 표시됨
- [ ] 생존 시간이 초 단위로 증가함
- [ ] 몬스터를 처치하면 카운터가 증가함
- [ ] 플레이어 체력이 0이 되면 게임 오버 UI가 표시됨
- [ ] 게임 오버 시 최종 통계가 정확하게 표시됨
- [ ] 재시작 버튼 클릭 시 게임이 초기 상태로 돌아감
- [ ] UI 업데이트로 인한 성능 저하 없음
- [ ] UI가 게임플레이를 방해하지 않음 (적절한 배치)

---

## Phase 7: 게임 밸런싱 및 추가 기능

### 이번 단계 개발 사항 오버뷰
게임의 난이도를 조정하고 추가 기능(웨이브 시스템, 파워업, 멀티 총알 등)을 구현하여 완성도를 높입니다.

### 개발 기능 체크리스트
- [ ] 웨이브 시스템 구현 (선택사항)
  - [ ] `WaveConfig` (IComponentData)
  - [ ] 시간 또는 처치 수에 따라 웨이브 증가
  - [ ] 웨이브마다 몬스터 스폰 속도/수 증가
  - [ ] UI에 현재 웨이브 표시
- [ ] 추가 총알 패턴 (선택사항)
  - [ ] 멀티 방향 발사
  - [ ] 총알 타입 다양화 (관통, 폭발 등)
- [ ] 파워업 시스템 (선택사항)
  - [ ] 랜덤 아이템 드랍 Entity
  - [ ] 획득 시 능력치 향상 (속도, 발사 속도, 데미지)
- [ ] 사운드 및 이펙트 (선택사항)
  - [ ] 총알 발사 사운드
  - [ ] 몬스터 사망 이펙트
  - [ ] 피격 이펙트
- [ ] 난이도 조정
  - [ ] 플레이어 체력, 이동 속도
  - [ ] 몬스터 체력, 이동 속도, 스폰 주기
  - [ ] 총알 데미지, 발사 속도
- [ ] 코드 리팩토링 및 최적화
  - [ ] 중복 코드 제거
  - [ ] System 실행 순서 최적화
  - [ ] Job 병합 및 최적화

### 작업 완료 후 검증 체크리스트
- [ ] 게임이 적절한 난이도로 플레이 가능함
- [ ] 초반에는 쉽고 시간이 지날수록 어려워짐
- [ ] 추가 기능이 게임플레이에 자연스럽게 통합됨
- [ ] 웨이브 시스템이 정상 작동함 (구현 시)
- [ ] 파워업이 정상적으로 적용됨 (구현 시)
- [ ] 사운드 및 이펙트가 적절한 타이밍에 재생됨 (구현 시)
- [ ] 장시간 플레이해도 성능 저하 없음 (5분 이상 테스트)
- [ ] 코드가 명확하고 유지보수 가능한 구조임
- [ ] Burst Compiler와 Job System이 최대한 활용됨

---

## 추가 권장 사항

### 버전 관리
- Git을 사용하여 각 Phase 완료 후 커밋
- 태그를 활용하여 각 Phase 구분

### 테스트
- 각 Phase 완료 후 독립적으로 실행 가능한지 확인
- Entity Debugger와 Profiler를 활용한 성능 검증

### 문서화
- 각 System과 Component에 주석 추가
- README 파일에 프로젝트 설정 및 실행 방법 기록

### 성능 목표
- 60 FPS 이상 유지 (플레이어 1명, 몬스터 100개, 총알 200개 동시 존재 시)
- Burst Compiler 적용률 90% 이상
- Job System 병렬화율 최대화

---

이 PRD는 점진적으로 기능을 구축하며 각 단계에서 실행 가능한 결과물을 만들 수 있도록 설계되었습니다. Claude Code 에이전트는 각 Phase를 독립적으로 수행하며, 이전 Phase의 결과물을 기반으로 작업을 이어갈 수 있습니다.

---

## ⚠️ Claude Code 작업 시 필수 준수 사항 (매우 중요!)

### 1. API 사용 전 검증 원칙
**절대 API를 추측해서 사용하지 말 것!**

- ❌ **금지**: 메서드나 프로퍼티 이름을 추측해서 코드 작성
- ❌ **금지**: "이 정도면 있을 것 같다"는 가정으로 API 사용
- ✅ **필수**: Unity/Netcode 공식 문서를 먼저 확인
- ✅ **필수**: WebSearch 도구를 활용하여 정확한 API 사용법 조사
- ✅ **필수**: 확실하지 않으면 사용자에게 질문

**나쁜 예시들 (실제 발생한 오류):**
```csharp
// ❌ 존재하지 않는 메서드 호출
NetDebug.SuppressTickBatchingWarning();

// ❌ 인스턴스 프로퍼티를 static처럼 사용
NetDebug.SuppressApplicationRunInBackgroundWarning = true;

// ❌ 존재하지 않는 프로퍼티 접근
var prefab = Prefab.Value;  // Prefab에는 Value 프로퍼티 없음

// ❌ 제거된 API 사용
GameObjectConversionSettings settings = ...;  // Unity Entities 1.x에서 제거됨
```

### 2. 작업 완료 선언 기준
**"완료했습니다"라고 말하기 전에 반드시 확인할 것!**

다음을 **모두** 확인한 후에만 완료 선언:
- [ ] 작성한 코드에 컴파일 에러 가능성이 없는가?
- [ ] 사용한 모든 API가 실제로 존재하고 올바르게 사용했는가?
- [ ] 타입이 맞는가? (static vs instance, 메서드 vs 프로퍼티)
- [ ] 런타임 에러 가능성은 없는가?
- [ ] 이전에 비슷한 에러를 낸 적이 있다면, 같은 실수를 반복하지 않았는가?

**절대 하지 말아야 할 것:**
- ❌ 코드를 작성하고 검증 없이 "완료했습니다" 선언
- ❌ 에러가 발생할 것 같지만 "아마 괜찮을 것"이라고 가정
- ❌ 사용자가 테스트하면서 에러를 발견하게 만드는 것

### 3. 에러 발생 시 대응 프로토콜

**사용자가 같은 문제를 2번 이상 보고하면, 이전 해결책이 틀렸다는 의미!**

에러 발생 시 절차:
1. **에러 메시지 정확히 분석**
   - 단순히 증상만 보지 말고 **근본 원인** 파악
   - 왜 이 에러가 발생했는지 이해

2. **올바른 해결책 조사**
   - Unity/Netcode 공식 문서 확인
   - 같은 문제를 겪은 사례 검색 (WebSearch 활용)
   - 비슷한 메서드/프로퍼티가 있는지 확인

3. **한 번에 완전히 수정**
   - 임시방편 금지 - **근본적인 해결**만 시도
   - 수정 후 다른 부작용이 없는지 검토
   - 같은 에러가 다시 발생하지 않도록 보장

4. **반복 실패 시 방향 전환**
   - 같은 방법으로 2번 이상 실패했다면 **완전히 다른 접근법** 고려
   - "좀 더 수정해보면 될 것 같다"는 생각 금지
   - 원점으로 돌아가서 다시 생각

### 4. Unity Netcode for Entities 특수 사항

**경고(Warning)와 에러(Error) 구분:**
- **Warning**: 기능에 영향 없으면 무시 가능 (사용자에게 설명 후)
  - 예: "Tick Batching Warning" - Editor에서만 나타나는 정상적인 경고
  - 예: "Application.runInBackground" - `Application.runInBackground = true`로 간단히 해결

- **Error**: 반드시 수정 필요
  - 컴파일 에러는 절대 무시 불가
  - API 사용 오류는 반드시 수정

**Sub Scene과 Baking 시스템:**
- Unity Entities 1.x는 GameObjectConversionSettings를 사용하지 않음
- Entity Prefab은 Sub Scene에 배치하고 Baking으로 변환
- `[GhostComponent]` 특성을 반드시 추가하여 네트워크 동기화 설정

### 5. 작업 품질 원칙

**"빠른 실패"보다 "정확한 성공"을 우선시**

- ✅ **올바름**: 느리더라도 한 번에 정확하게 완료
- ❌ **금지**: 빠르게 여러 번 시도하면서 사용자 시간 낭비

**사용자 시간과 토큰 존중:**
- 잘못된 방법으로 여러 번 시도하는 것은 시간과 토큰 낭비
- 불확실하면 먼저 조사하고, 확신이 서면 구현
- "일단 해보고 안 되면 고치자"는 접근 금지

### 6. 패턴 학습 및 반복 방지

**이전 실수에서 배우기:**
- 같은 타입의 에러를 2번 이상 반복하지 않기
- 예: static vs instance 혼동 → 다음부터는 반드시 확인
- 예: API 추측 실패 → 다음부터는 반드시 문서 확인

**자주 발생하는 실수 패턴:**
1. **타입 혼동**: static 메서드를 instance처럼, 또는 그 반대
2. **API 추측**: 존재하지 않는 메서드/프로퍼티 사용
3. **Deprecated API**: 옛날 버전 문서를 보고 제거된 API 사용
4. **구조체 복사**: RefRW<T>.ValueRW를 변수에 할당 (값 복사 발생)

---

## Unity Netcode for Entities 관련 추가 지침

### GhostComponent 설정
모든 네트워크 동기화 컴포넌트는 `[GhostComponent]` 특성 필요:
```csharp
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerTag : IComponentData { }
```

### Entity Prefab 설정 방법
1. Sub Scene에 GameObject로 Prefab 배치
2. Authoring 컴포넌트 추가하여 Entity로 변환
3. Unity가 자동으로 Baking하여 Entity Prefab 생성
4. 시스템에서 `typeof(PlayerTag), typeof(Prefab)` 쿼리로 찾기

### 네트워크 틱 레이트 설정
```csharp
var tickRateEntity = world.EntityManager.CreateEntity(typeof(ClientServerTickRate));
world.EntityManager.SetComponentData(tickRateEntity, new ClientServerTickRate
{
    SimulationTickRate = 20,
    NetworkTickRate = 20,
    MaxSimulationStepsPerFrame = 8,
    TargetFrameRateMode = ClientServerTickRate.FrameRateMode.Sleep
});
```

### 일반적인 경고 처리
- **"Server Tick Batching"**: Editor에서 정상적인 경고, 무시 가능
- **"Application.runInBackground"**: `Application.runInBackground = true` 추가
- **경고 억제 API**: NetDebug에는 경고 억제 메서드가 없음 - 억제하려 하지 말 것

---

**마지막 당부:**
이 문서의 지침을 무시하고 작업하면 사용자의 시간과 토큰을 낭비하게 됩니다.
불확실한 것이 있으면 **추측하지 말고 조사하거나 질문**하세요.
"완료했습니다"는 **정말로 완료했을 때만** 말하세요.
