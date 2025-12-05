# CLAUDE.md

이 파일은 이 저장소에서 작업할 때 Claude Code (claude.ai/code)에게 가이드를 제공합니다.

## 🌐 언어 설정 (중요!)

**모든 응답은 반드시 한국어로 작성해야 합니다.**

- ✅ **올바름**: 코드 설명, 에러 메시지, 가이드, 모든 커뮤니케이션을 한국어로 작성
- ❌ **금지**: 영어로 응답하지 마세요
- 📝 **예외**: 코드 내 주석은 한국어, 변수명/함수명은 영어 사용 (프로그래밍 관례)
- 💬 **커밋 메시지**: 반드시 한국어로 작성 (개발팀 규칙)

이 프로젝트의 개발팀은 한국어를 사용하므로, Claude의 모든 설명과 응답도 한국어로 제공되어야 합니다.

## 프로젝트 개요

초기 개발 단계의 Unity 6 (버전 6000.1.7f1) 게임 프로젝트입니다. 사용 기술:

- **언어:** C# 9.0 (.NET 4.7.1 / .NET Standard 2.1 타겟)
- **렌더링:** Universal Render Pipeline (URP) 17.1.0 (모바일/PC 품질 계층 분리)
- **입력:** 새로운 Unity Input System 1.14.0 (레거시 Input Manager 아님)
- **주요 플랫폼:** Windows Standalone 64-bit (iOS/Android/tvOS 설정됨)
- **저장소:** https://github.com/lmo0317/projectc.git

현재 코드베이스는 최소한의 상태로, Unity의 URP 템플릿을 기반으로 메카닉 개발(애니메이션, 점프, 타일, 3D 요소 등)에 집중하고 있습니다.

## 개발 환경 설정

Unity 프로젝트이므로 개발은 명령줄이 아닌 Unity Editor를 통해 이루어집니다.

**프로젝트 열기:**
1. Unity Hub 설치
2. Unity Hub를 통해 Unity 버전 6000.1.7f1 설치
3. 경로에서 프로젝트 추가: `C:\work\git\projectc`
4. Unity 6000.1.7f1로 열기

**IDE 설정:**
- 솔루션 파일: `projectc.sln`
- 지원 IDE: JetBrains Rider 3.0.36 또는 Visual Studio 2022
- C# 프로젝트 파일은 Unity가 자동 생성 (`.csproj` 파일을 수동으로 편집하지 말 것)

## 일반적인 개발 워크플로우

Unity 프로젝트이므로 대부분의 작업은 Unity Editor를 통해 수행됩니다:

**게임 실행:**
- Unity Editor 열기 → Play 버튼 클릭 (또는 Ctrl+P)
- 메인 씬: `Assets/Scenes/SampleScene.unity`

**빌드:**
- Unity Editor → File → Build Settings → Build
- 또는: File → Build and Run

**테스트:**
- Unity Test Framework 1.5.1 설치됨
- 접근 방법: Window → General → Test Runner
- 현재 프로젝트에 테스트 없음

**스크립트 편집:**
- Unity Editor에서 `.cs` 파일을 더블클릭하면 설정된 IDE에서 열림
- Unity가 필요 시 `.sln` 및 `.csproj` 자동 재생성

**버전 관리:**
- 표준 git 명령어 사용 가능
- 메인 브랜치: `master`
- 커밋 메시지는 한글로 작성 (개발팀 선호사항)

## 코드 아키텍처

### Unity ECS (Entity Component System) 아키텍처

이 프로젝트는 **Unity Entities 패키지**를 사용하여 데이터 지향 기술 스택(DOTS)을 구현합니다:
- **Entity**: 고유 식별자 (게임 오브젝트의 경량화 버전)
- **Component**: 데이터만 포함 (`IComponentData` 구조체)
- **System**: 로직 처리 (`ISystem` 구조체 또는 `SystemBase` 클래스)
- **Baker**: GameObject를 Entity로 변환하는 Authoring 클래스

**중요한 ECS 규칙:**

#### 1. TransformUsageFlags 설정 (Baker에서)
Entity의 Transform 동작을 지정할 때 Baker에서 `GetEntity()` 호출 시 적절한 플래그 조합을 사용해야 합니다:

```csharp
// ❌ 잘못된 예: 움직이기만 하고 렌더링 안 됨
var entity = GetEntity(TransformUsageFlags.Dynamic);

// ✅ 올바른 예: 움직이면서 렌더링됨
var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
```

**TransformUsageFlags 종류:**
- `Dynamic`: Entity의 Transform이 런타임에 변경됨 (움직임)
- `Renderable`: Entity가 화면에 렌더링됨 (보임)
- `Dynamic | Renderable`: 움직이면서 보이는 Entity (총알, 플레이어 등)

#### 2. RefRW<T> 컴포넌트 수정 방법
ECS에서 컴포넌트를 수정할 때는 **절대 로컬 변수로 복사하지 마세요**. C# 구조체는 값 타입이므로 복사본을 수정하면 원본은 변경되지 않습니다.

```csharp
// ❌ 잘못된 예: 로컬 복사본 생성 (변경사항 유실!)
foreach (var (shootConfig, ...) in SystemAPI.Query<RefRW<AutoShootConfig>, ...>())
{
    var config = shootConfig.ValueRW;  // 복사본 생성
    config.Timer += deltaTime;          // 복사본만 수정됨
    // 원본 컴포넌트는 변경되지 않음!
}

// ✅ 올바른 예: ValueRW를 통해 직접 수정
foreach (var (shootConfig, ...) in SystemAPI.Query<RefRW<AutoShootConfig>, ...>())
{
    shootConfig.ValueRW.Timer += deltaTime;  // 원본 직접 수정
    if (shootConfig.ValueRW.Timer >= shootConfig.ValueRW.Interval)
    {
        shootConfig.ValueRW.Timer = 0f;
        // 로직 처리...
    }
}
```

**핵심 원칙:**
- `RefRW<T>.ValueRW`는 컴포넌트의 **참조**를 제공하지만, 이를 변수에 할당하면 구조체가 **복사**됩니다
- 컴포넌트 필드에 접근할 때는 항상 `shootConfig.ValueRW.FieldName` 형태로 직접 접근
- 중간 변수를 만들지 말고 체인 방식으로 접근

#### 3. 기타 MonoBehaviour 컴포넌트
레거시 GameObject 기반 스크립트도 일부 사용 중:
- 동작은 GameObject에 부착된 MonoBehaviour 스크립트를 통해 정의
- 현재 프로젝트는 ECS로 전환 중 (`Assets/TutorialInfo/Scripts/`의 템플릿 파일만 존재)

### 디렉토리 구조

```
Assets/
├── Scenes/                    # 게임 씬 (.unity 파일)
├── Settings/                  # URP 렌더링 파이프라인 설정
├── TutorialInfo/             # 템플릿 파일 (제거 가능)
└── InputSystem_Actions.inputactions  # 중앙화된 입력 설정
```

**중요:** 커스텀 게임 스크립트는 `Assets/Scripts/`에 정리해야 합니다 (아직 생성되지 않음). Editor 전용 코드는 `Editor/` 하위 디렉토리에 배치하는 Unity 규칙을 따를 것.

### Input System 아키텍처

프로젝트는 Unity의 **새로운 Input System**을 사용 (액션 기반, 레거시 Input Manager 아님).

**입력 설정 파일:** `Assets/InputSystem_Actions.inputactions`

**액션 맵: "Player"**
- **Move** (Vector2) - WASD/왼쪽 스틱
- **Look** (Vector2) - 마우스 이동/오른쪽 스틱
- **Jump** (Button) - Space/South 버튼 (Xbox A, PS Cross)
- **Sprint** (Button) - Left Shift/왼쪽 스틱 누르기
- **Crouch** (Button) - Left Ctrl/오른쪽 스틱 누르기
- **Attack** (Button) - 마우스 왼쪽 버튼/West 버튼 (Xbox X, PS Square)
- **Interact** (Hold Button) - E/North 버튼 (Xbox Y, PS Triangle)
- **Previous** (Button) - Q/왼쪽 범퍼
- **Next** (Button) - R/오른쪽 범퍼

**지원 입력 장치:**
- 키보드 & 마우스
- 게임패드 (Xbox/PlayStation 레이아웃)
- 터치 (모바일)
- XR 컨트롤러

코드에서 입력 처리를 추가할 때는 PlayerInput 컴포넌트 또는 Input Action Asset을 통해 이 액션 이름들을 참조하세요.

### Universal Render Pipeline (URP) 설정

**렌더링 아키텍처:**
- Forward 렌더링 (deferred 아님)
- 두 개의 품질 계층과 별도 렌더 파이프라인 에셋:
  - **Mobile:** `Assets/Settings/Mobile_RPAsset.asset` + `Mobile_Renderer.asset`
  - **PC:** `Assets/Settings/PC_RPAsset.asset` + `PC_Renderer.asset` (기본값)
- `Assets/Settings/`의 Volume Profile을 통한 포스트 프로세싱

**품질 설정:**
- `ProjectSettings/QualitySettings.asset`에서 설정
- 현재 기본값: PC 품질 계층 (index 1)
- 변경 방법: Edit → Project Settings → Quality

### 씬 구조

**메인 씬:** `Assets/Scenes/SampleScene.unity`
- `ProjectSettings/EditorBuildSettings.asset`에서 기본 빌드 씬으로 설정됨
- 현재 12개의 GameObject 포함

## 프로젝트 규칙

### 코드 구성
- **Editor 스크립트:** `Assets/*/Editor/` 폴더에 배치 (Unity가 별도로 컴파일)
- **런타임 스크립트:** `Assets/Scripts/` 또는 기능별 폴더에 배치
- **설정/Config:** `Assets/Settings/`에 중앙화

### 네이밍
- **제품명:** "projecte" (ProjectSettings에 설정됨)
- **회사명:** "DefaultCompany" (Unity 기본값, 커스터마이징 안 됨)
- 커스텀 C# 네임스페이스 미정의 - 전역 네임스페이스 사용 중

### Assembly Definitions
현재 존재하지 않음. 대규모 프로젝트의 경우 Assembly Definition 파일(`.asmdef`)을 추가하여 코드를 구성하고 컴파일 시간을 개선하는 것을 고려하세요.

### 테스트
- Unity Test Framework 설치되어 있으나 테스트 미작성
- 테스트 추가 시 `Assets/Tests/`에 Editor 및 PlayMode 테스트 어셈블리를 분리하여 구성

## Unity 패키지 의존성

주요 패키지 (전체 목록은 `Packages/manifest.json` 참조):
- **Input System** 1.14.0 - 새로운 입력 시스템 (필수)
- **AI Navigation** 2.0.8 - NavMesh 경로 찾기
- **Visual Scripting** 1.9.7 - 노드 기반 스크립팅
- **Timeline** 1.8.7 - 시네마틱 시퀀싱
- **URP** 17.1.0 - 렌더링 파이프라인 (핵심 의존성)

## 플랫폼 설정

**빌드 타겟:**
- 주요: Windows Standalone (x86_64)
- 설정됨: iPhone, Android, tvOS

**그래픽스 API:**
- Windows: Direct3D 11
- Mobile: OpenGL ES 3.0+ / Vulkan

## 버전 관리 참고사항

- Git LFS가 대용량 바이너리 에셋에 대해 설정됨
- `.gitignore`는 Unity 모범 사례를 따름 (`Library/`, `Temp/`, `Logs/`, `UserSettings/` 무시)
- 솔루션 및 프로젝트 파일 (`.sln`, `.csproj`)은 gitignore됨 (Unity가 자동 생성)
- 현재 브랜치: `master`

## ⚠️ 에러 처리 및 품질 보증 규칙 (매우 중요!)

Claude Code는 다음 규칙을 **반드시** 준수해야 합니다:

### 1. 작업 완료 선언 전 검증
- ✅ **필수**: 코드 작성 후 반드시 컴파일 에러 가능성을 **깊이 있게** 검토
- ✅ **필수**: API 사용법이 확실하지 않으면 **추측하지 말고** 문서 확인 또는 사용자에게 질문
- ❌ **금지**: "완료했습니다"라고 말한 후 실제로 컴파일 에러가 발생하는 상황
- ❌ **금지**: 에러를 여러 번 반복해서 수정하는 상황 (한 번에 제대로 수정)

### 2. 에러 발생 시 대응
사용자가 컴파일 에러를 보고하면:

1. **에러 메시지 정확히 분석**
   - 에러의 근본 원인 파악 (API 잘못 사용? 타입 불일치? 접근 제한?)
   - 단순히 증상만 고치지 말고 **왜** 에러가 발생했는지 이해

2. **올바른 해결책 조사**
   - Unity/Netcode API 문서 확인
   - 비슷한 메서드/프로퍼티 검색
   - 필요 시 WebSearch 도구 사용

3. **한 번에 완전히 수정**
   - 임시방편이 아닌 **근본적인 해결**
   - 수정 후 다른 부작용 없는지 검토
   - 같은 에러가 다시 발생하지 않도록 보장

### 3. API 사용 시 주의사항
Unity Netcode for Entities 같은 특정 라이브러리 사용 시:

```csharp
// ❌ 잘못된 예: API 추측해서 사용
NetDebug.SuppressTickBatchingWarning();  // 존재하지 않는 메서드

// ❌ 잘못된 예: 인스턴스 멤버를 static처럼 사용
NetDebug.SuppressApplicationRunInBackgroundWarning = true;  // 인스턴스 프로퍼티

// ✅ 올바른 예: 불필요한 경고는 시스템 자체를 비활성화하거나 무시
// 경고가 기능에 영향 없으면 사용자에게 설명 후 무시 권장
```

### 4. 검증 체크리스트
코드 작성/수정 후 반드시 확인:

- [ ] 모든 타입이 올바른가? (static vs instance, 메서드 vs 프로퍼티)
- [ ] API 사용법이 공식 문서와 일치하는가?
- [ ] 컴파일 에러 가능성은 없는가?
- [ ] 런타임 에러 가능성은 없는가?
- [ ] 이전에 비슷한 에러를 낸 적이 있는가? (패턴 학습)

### 5. "완료" 선언 기준
다음 조건을 **모두** 만족할 때만 "완료했습니다"라고 말할 것:

- ✅ 컴파일 에러 없음을 확신
- ✅ 런타임 에러 가능성 최소화
- ✅ API 사용법이 올바름을 확인
- ✅ 사용자 요구사항을 완전히 충족

**원칙: 빠르게 여러 번 실패하는 것보다, 느리더라도 한 번에 정확하게 성공하는 것이 낫습니다.**
