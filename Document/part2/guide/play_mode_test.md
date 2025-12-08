# Unity Netcode for Entities - Multiplayer Play Mode 테스트 가이드

## 📋 개요

Unity Netcode for Entities를 사용한 멀티플레이어 기능을 **Multiplayer Play Mode**로 테스트하는 방법을 설명합니다.

**Multiplayer Play Mode**는 Unity Editor에서 여러 개의 가상 플레이어를 동시에 실행하여 멀티플레이어 게임을 테스트할 수 있는 기능입니다.

---

## 🎯 테스트 목표

- 2명의 클라이언트가 서버에 연결
- 각 클라이언트에서 상대방 플레이어가 보이는지 확인
- 네트워크 동기화 확인 (움직임, 상태 등)

---

## ✅ 사전 준비

### 1. Scene을 Build Settings에 추가

**필수!** Multiplayer Play Mode는 Build Settings에 등록된 Scene만 사용할 수 있습니다.

1. 테스트할 Scene 열기 (예: `NetworkTest.unity`)
2. `File → Build Settings` 열기
3. `Add Open Scenes` 버튼 클릭
4. Scene이 목록에 추가되었는지 확인

### 2. Run in Background 활성화

**필수!** 창 전환 시 연결이 끊어지지 않도록 설정합니다.

1. `Edit → Project Settings → Player` 열기
2. `Resolution and Presentation` 섹션 찾기
3. `Run in Background` ✅ 체크

### 3. SimpleNetworkBootstrap GameObject 제거

**중요!** `SimpleNetworkBootstrap`은 `ClientServerBootstrap`을 상속받은 순수 C# 클래스로, Unity가 자동으로 실행합니다.

1. Hierarchy 창에서 `SimpleNetworkBootstrap` 컴포넌트가 부착된 GameObject 찾기
2. **있으면 삭제** (GameObject에 부착하면 안 됨!)

---

## 🔧 Multiplayer Play Mode 설정

### 1. Multiplayer Play Mode 창 열기

```
Window → Multiplayer → Multiplayer Play Mode
```

### 2. Virtual Player 추가

- **Main Editor Player**: 기본적으로 존재 (Player 1)
- **Player 2 추가**: Virtual Players 섹션에서 Player 2 활성화
  - Player 2 왼쪽 체크박스 ✅ 클릭
  - "Active" 상태로 변경

### 3. 각 Player의 PlayMode Type 설정 (매우 중요!)

**이 설정이 핵심입니다!** 각 Unity 창마다 별도로 설정해야 합니다.

#### **Main Editor (Player 1) 설정:**

1. Main Editor Unity 창에서 작업
2. `Window → Multiplayer → PlayMode Tools` 열기
3. **PlayMode Type**: `Client & Server` 선택
4. **Server Emulation**: `Client Hosted Server` (기본값)
5. **Instantiation Frequency**: `2` (2개 클라이언트 테스트용)

#### **Player 2 설정:**

1. Multiplayer Play Mode 창에서 **Play All** 클릭 (Player 2 창이 열림)
2. **Player 2 Unity 창으로 전환** (Alt+Tab 또는 작업 표시줄)
3. Player 2 창에서 `Window → Multiplayer → PlayMode Tools` 열기
4. **PlayMode Type**: `Client` 선택 ⚠️ (Server 아님!)
5. **Instantiation Frequency**: `2` (동일하게 설정)

**주의:** Main Editor는 `Client & Server`, Player 2는 `Client`만 선택해야 포트 충돌이 발생하지 않습니다!

---

## 🚀 테스트 실행

### 1. Play All 실행

1. **Multiplayer Play Mode 창**에서 `Play All` 버튼 클릭
2. 또는 각 Player 체크박스 선택 후 Unity Play 버튼 클릭

### 2. 연결 상태 확인

각 Unity 창에서 `Window → Multiplayer → PlayMode Tools` 열기:

#### **Main Editor (Server + Client):**

```
ServerWorld [server]: [IPC:127.0.0.1:7979] [UDP:0.0.0.0:7979]
  2 Clients
  4 Ghosts (Player 2개 × 각 2개 컴포넌트)

ClientWorld [Client]: [UDP:127.0.0.1:7979] [Connected]
  4 Ghosts
```

#### **Player 2 (Client Only):**

```
ClientWorld [Client]: [UDP:127.0.0.1:7979] [Connected]
  4 Ghosts
```

### 3. Console 로그 확인

**Main Editor Console:**
```
[SimpleNetworkBootstrap] Initialized with AutoConnectPort=7979
[PlayerSpawnSystem] Found PlayerPrefab: Entity(...)
[PlayerSpawnSystem] Client connected: NetworkId = 1
[Server] Player spawned for NetworkId 1 at (...)
[PlayerSpawnSystem] Client connected: NetworkId = 2
[Server] Player spawned for NetworkId 2 at (...)
```

**Player 2 Console:**
- 포트 충돌 에러 **없음** ✅
- 연결 성공 메시지만 표시

---

## ✅ 테스트 성공 기준

### 시각적 확인

- ✅ **Main Editor**: Scene View/Game View에 플레이어 **2개** 보임
- ✅ **Player 2**: Scene View/Game View에 플레이어 **2개** 보임
- ✅ 각 플레이어가 다른 위치에 스폰됨 (예: x=-2, x=2)

### 네트워크 상태

- ✅ **ServerWorld**: 2 Clients, 4 Ghosts
- ✅ **ClientWorld (Main Editor)**: Connected, 4 Ghosts
- ✅ **ClientWorld (Player 2)**: Connected, 4 Ghosts

### Console 확인

- ✅ Main Editor: PlayerSpawnSystem 로그 2회 출력 (NetworkId 1, 2)
- ✅ Player 2: **포트 충돌 에러 없음**

---

## ❌ 문제 해결

### 문제 1: "Failed to bind UDP socket... port 7979"

**증상:** Player 2 Console에 포트 충돌 에러 발생

**원인:** Player 2도 `Client & Server` 모드로 설정되어 서버를 시작하려고 시도

**해결:**
1. Player 2 Unity 창 전환
2. `Window → Multiplayer → PlayMode Tools` 열기
3. **PlayMode Type을 `Client`로 변경** (Client & Server 아님!)
4. Play 재시작

### 문제 2: 플레이어가 1개만 보임

**증상:** 각 클라이언트에서 자기 자신만 보임

**원인:** Scene이 Build Settings에 없음

**해결:**
1. Scene 열기
2. `File → Build Settings → Add Open Scenes`
3. Play 재시작

### 문제 3: "0 Ghosts" 표시

**증상:** PlayMode Tools에서 Ghost 개수가 0으로 표시

**원인:** SubScene이 로드되지 않음 또는 Prefab에 GhostAuthoringComponent가 없음

**해결:**
1. Hierarchy에서 SubScene 확인 (NetworkTestSubscene)
2. SubScene Inspector에서 `Auto Load Scene` ✅ 확인
3. Player Prefab에 `GhostAuthoringComponent` 추가 확인

### 문제 4: "'SimpleNetworkBootstrap' is missing the class attribute 'ExtensionOfNativeClass'"

**증상:** Console에 ExtensionOfNativeClass 에러 발생

**원인:** SimpleNetworkBootstrap이 GameObject에 부착됨

**해결:**
1. Hierarchy에서 SimpleNetworkBootstrap 컴포넌트 제거
2. `ClientServerBootstrap`은 자동으로 실행되므로 GameObject 부착 불필요

---

## 📝 주요 개념 정리

### ClientServerBootstrap

- Unity Netcode for Entities의 기본 부트스트랩 클래스
- `Initialize()` 메서드를 오버라이드하여 네트워크 설정
- `AutoConnectPort` 설정으로 자동 연결 활성화
- **GameObject에 부착하지 않음** (순수 C# 클래스)

### PlayMode Type

- **Client & Server**: 하나의 Unity 인스턴스에서 서버와 클라이언트 모두 실행
- **Client**: 클라이언트만 실행 (서버 없음)
- **Server**: 서버만 실행 (클라이언트 없음)

### Ghost

- 네트워크로 동기화되는 Entity
- `[GhostComponent]` 어트리뷰트로 컴포넌트 마킹
- `GhostAuthoringComponent`를 Prefab에 추가하여 Ghost로 등록

### NetworkId

- 각 클라이언트에게 할당되는 고유 ID
- 서버에서 1부터 순차적으로 할당 (NetworkId 1, 2, 3...)
- `GhostOwner` 컴포넌트로 소유권 설정

---

## 🔍 네트워크 디버깅 팁

### Entities Hierarchy 창 활용

```
Window → Entities → Entities Hierarchy
```

- **ServerWorld [server]**: 서버에서 관리하는 모든 Entity 확인
- **ClientWorld [Client]**: 클라이언트에서 보이는 모든 Entity 확인
- Player Entity에 어떤 컴포넌트가 있는지 확인 가능

### PlayMode Tools 창 버튼

- **DC All**: 모든 클라이언트 연결 끊기
- **Reconnect All**: 모든 클라이언트 재연결
- **Client Reconnect**: 특정 클라이언트 재연결
- **Log Relevancy**: 네트워크 관련성 로그 출력

### Network Emulation

PlayMode Tools 창의 Network Emulation 섹션:
- **RTT Delay**: 왕복 지연 시간 시뮬레이션 (ms)
- **Packet Drop**: 패킷 손실률 시뮬레이션 (%)
- 실제 네트워크 환경을 시뮬레이션하여 테스트 가능

---

## 📚 관련 파일

### 핵심 코드

- `Assets/Scripts/Network/SimpleNetworkBootstrap.cs` - 네트워크 초기화
- `Assets/Scripts/Systems/Network/PlayerSpawnSystem.cs` - 플레이어 스폰 로직
- `Assets/Scripts/Components/PlayerTag.cs` - 플레이어 식별 태그
- `Assets/Scripts/Authoring/PlayerAuthoring.cs` - Player Prefab Baker

### 설정 파일

- `ProjectSettings/EditorBuildSettings.asset` - Scene 목록
- `ProjectSettings/ProjectSettings.asset` - Run in Background 설정

---

## ✨ 추가 테스트 시나리오

### 3명 이상의 클라이언트 테스트

1. Multiplayer Play Mode 창에서 Player 3, Player 4 활성화
2. 각 Player를 `Client` 모드로 설정
3. `Instantiation Frequency`를 클라이언트 수만큼 증가 (예: 4)
4. PlayerSpawnSystem의 스폰 위치 로직 조정 필요

### 네트워크 지연 테스트

1. PlayMode Tools에서 `RTT Delay` 설정 (예: 100ms)
2. `Packet Drop` 설정 (예: 5%)
3. 움직임이 지연되거나 끊기는지 확인
4. Prediction/Interpolation 동작 확인

### 재연결 테스트

1. PlayMode Tools에서 `Client DC` 버튼으로 클라이언트 연결 끊기
2. `Client Reconnect` 버튼으로 재연결
3. 플레이어 상태가 유지되는지 확인

---

## 🎓 학습한 교훈

### 1. ClientServerBootstrap은 GameObject에 부착하지 않음

- MonoBehaviour가 아닌 순수 C# 클래스
- Unity가 자동으로 찾아서 `Initialize()` 호출
- GameObject 부착 시 "ExtensionOfNativeClass" 에러 발생

### 2. 각 Player마다 PlayMode Type 별도 설정 필요

- Multiplayer Play Mode는 각 Player가 **별도 Unity 인스턴스**
- 각 인스턴스마다 PlayMode Tools 창에서 개별 설정
- Main Editor: `Client & Server`, Clone: `Client`로 분리

### 3. AutoConnectPort로 포트 충돌 해결

- `AutoConnectPort = 7979` 설정
- Server는 자동으로 해당 포트에서 Listen
- Client는 자동으로 `127.0.0.1:7979`로 Connect
- Client 전용 모드는 서버를 시작하지 않아 충돌 없음

### 4. Build Settings Scene 등록 필수

- Multiplayer Play Mode는 Scene을 동적으로 로드
- Build Settings에 없는 Scene은 로드 불가
- 테스트 전에 반드시 Scene 등록 확인

---

## 📖 참고 자료

- [Unity Netcode for Entities Documentation](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
- [PlayMode Tool window | Netcode for Entities](https://docs.unity3d.com/Packages/com.unity.netcode@1.9/manual/playmode-tool.html)
- [Connecting server and clients | Netcode for Entities](https://docs.unity3d.com/Packages/com.unity.netcode@1.4/manual/network-connection.html)
- [Multiplayer Play Mode Documentation](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@latest)

---

**작성일:** 2025-12-08
**테스트 환경:** Unity 6000.1.7f1, Netcode for Entities 1.4.1, Multiplayer Play Mode 1.3.2
