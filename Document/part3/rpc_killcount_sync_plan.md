# RPC를 이용한 GameStats 동기화 구현 계획

**작성일**: 2025-12-11
**목표**: Server의 KillCount를 RPC로 Client에 동기화
**참고**: NetcodeSamples 03_RPC 패턴

---

## 분석: Time vs KillCount

| 데이터 | 특성 | 적합한 방법 |
|---|---|---|
| **SurvivalTime** | 지속적으로 증가 (매 프레임) | ❌ RPC 부적합 (너무 자주 변경) |
| | | ✅ **Client 로컬 계산** (이미 작동 중) |
| **KillCount** | 이벤트 발생 시만 증가 | ✅ **RPC 적합** (킬 이벤트만 전송) |

## 최종 전략

1. **SurvivalTime**: Client에서 독립적으로 계산 (현재 방식 유지)
2. **KillCount**: Server에서 RPC로 Client에 전송

---

## 구현 계획

### 1단계: RPC 컴포넌트 정의

**파일**: `Assets/Scripts/Components/Network/KillCountRpc.cs` (새로 생성)

```csharp
using Unity.NetCode;

/// <summary>
/// Enemy 처치 시 Server에서 Client로 전송하는 RPC
/// </summary>
public struct KillCountRpc : IRpcCommand
{
    public int IncrementAmount;  // 증가량 (보통 1)
}
```

**이유**:
- NetcodeSamples 03_RPC 패턴 따름
- `IRpcCommand` 인터페이스 구현
- Unmanaged 타입만 사용 (int)

---

### 2단계: Server - Enemy 죽을 때 RPC 전송

**파일**: `Assets/Scripts/Systems/BulletHitSystem.cs` (수정)

**기존 코드**:
```csharp
if (enemyHealth.ValueRO.Value <= 0)
{
    ecb.DestroyEntity(enemyEntity);
    // GameStats.KillCount++ (이건 Server만 증가)
}
```

**수정 후**:
```csharp
if (enemyHealth.ValueRO.Value <= 0)
{
    ecb.DestroyEntity(enemyEntity);

    // RPC 생성: 모든 Client에게 KillCount +1 브로드캐스트
    var rpcEntity = ecb.CreateEntity();
    ecb.AddComponent(rpcEntity, new KillCountRpc
    {
        IncrementAmount = 1
    });
    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
    // TargetConnection 생략 → 모든 Client에게 브로드캐스트
}
```

**변경점**:
- GameStats 직접 수정 제거 (Server도 RPC를 통해 업데이트)
- RPC Entity 생성하여 모든 Client에 브로드캐스트

---

### 3단계: Client - RPC 수신 및 GameStats 업데이트

**파일**: `Assets/Scripts/Systems/Network/ReceiveKillCountRpcSystem.cs` (새로 생성)

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// KillCount RPC를 수신하여 GameStats를 업데이트하는 시스템
/// Client와 Server 양쪽에서 실행됨 (Server도 자신의 RPC를 받음)
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ReceiveKillCountRpcSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();  // 연결된 후에만 실행
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // RPC 수신 처리
        foreach (var (receiveRequest, rpcData, entity) in
                 SystemAPI.Query<ReceiveRpcCommandRequest, KillCountRpc>()
                     .WithEntityAccess())
        {
            Debug.Log($"[{World.Name}] Received KillCount RPC: +{rpcData.IncrementAmount}");

            // GameStats 업데이트
            foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
            {
                stats.ValueRW.KillCount += rpcData.IncrementAmount;
                Debug.Log($"[{World.Name}] Updated KillCount: {stats.ValueRW.KillCount}");
                break;  // 싱글톤
            }

            // RPC Entity 삭제 (필수!)
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
```

**핵심 포인트**:
1. **Client + Server 양쪽 실행**: Server도 자신이 보낸 RPC를 받아서 GameStats 업데이트
2. **RequireForUpdate<NetworkId>()**: 연결된 후에만 실행
3. **RPC Entity 삭제**: `ecb.DestroyEntity(entity)` 필수
4. **GameStats 싱글톤**: 하나만 있으므로 break

---

### 4단계: GameStats 초기화 수정

**파일**: `Assets/Scripts/Systems/GameStatsSystem.cs` (수정)

**기존**:
```csharp
// Server와 Client 각자 독립적으로 SurvivalTime + KillCount 관리
```

**수정 후**:
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct GameStatsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // SurvivalTime만 업데이트 (KillCount는 RPC로 관리)
        foreach (var stats in SystemAPI.Query<RefRW<GameStats>>())
        {
            stats.ValueRW.SurvivalTime += deltaTime;
            break;  // 싱글톤
        }
    }
}
```

**변경점**:
- KillCount 업데이트 로직 제거
- SurvivalTime만 로컬에서 계산

---

### 5단계: UI 업데이트 (변경 없음)

**파일**: `Assets/Scripts/Systems/UIUpdateSystem.cs`

현재 코드 그대로 유지:
```csharp
// GameStats에서 KillCount 읽어서 UI 업데이트
uiManager.UpdateKillCount(stats.ValueRO.KillCount);
```

**이유**: GameStats는 RPC로 업데이트되므로 UI는 그냥 읽기만 하면 됨

---

## 작동 흐름도

```
[ServerWorld: Enemy 죽음]
    ↓
BulletHitSystem: Enemy 체력 0 감지
    ↓
RPC Entity 생성 (KillCountRpc)
    ↓
SendRpcCommandRequest 추가 (브로드캐스트)
    ↓
NetCode가 모든 Client + Server로 RPC 전송
    ↓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    ↓
[ClientWorld: RPC 수신]
    ↓
ReceiveKillCountRpcSystem 실행
    ↓
ReceiveRpcCommandRequest 쿼리로 RPC 찾기
    ↓
GameStats.KillCount += 1
    ↓
RPC Entity 삭제
    ↓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    ↓
[ServerWorld: RPC 수신] (자기가 보낸 것)
    ↓
ReceiveKillCountRpcSystem 실행
    ↓
GameStats.KillCount += 1
    ↓
RPC Entity 삭제
    ↓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    ↓
[UI 업데이트]
    ↓
UIUpdateSystem: GameStats 읽기
    ↓
uiManager.UpdateKillCount(...) 호출
    ↓
화면에 KillCount 표시 ✅
```

---

## 파일 구조

```
Assets/Scripts/
├── Components/
│   ├── GameStats.cs (기존 - 변경 없음)
│   └── Network/
│       └── KillCountRpc.cs (새로 생성) ⭐
├── Systems/
│   ├── BulletHitSystem.cs (수정) ⭐
│   ├── GameStatsSystem.cs (수정) ⭐
│   ├── UIUpdateSystem.cs (변경 없음)
│   └── Network/
│       └── ReceiveKillCountRpcSystem.cs (새로 생성) ⭐
```

---

## 체크리스트

### 구현 전 확인사항
- [ ] NetworkStreamInGame 태그가 connection에 있는지 확인
- [ ] GoInGameSystem이 실행 중인지 확인
- [ ] Client와 Server가 연결되어 있는지 확인

### 구현 단계
1. [ ] `KillCountRpc.cs` 생성
2. [ ] `BulletHitSystem.cs` 수정 (RPC 전송)
3. [ ] `ReceiveKillCountRpcSystem.cs` 생성 (RPC 수신)
4. [ ] `GameStatsSystem.cs` 수정 (KillCount 업데이트 제거)
5. [ ] Unity Editor에서 테스트

### 테스트 방법
1. [ ] Play 모드 시작
2. [ ] Enemy 처치
3. [ ] 콘솔 로그 확인:
   - `[ServerWorld] Received KillCount RPC: +1`
   - `[ClientWorld] Received KillCount RPC: +1`
4. [ ] UI에서 KillCount 증가 확인
5. [ ] Game Over UI에서 최종 KillCount 확인

---

## 장점

1. **대역폭 효율적**: 킬 이벤트 발생 시에만 전송 (Ghost보다 효율적)
2. **명확한 이벤트**: "+1 증가" 이벤트가 명확함
3. **Server Authoritative**: Server가 진실의 원천
4. **간단한 구현**: RPC 3파일만 수정/추가
5. **NetcodeSamples 패턴**: 공식 샘플과 동일한 구조

---

## 참고 자료

- **NetcodeSamples**: `03_RPC/RPC.cs`
- **Unity DOTS 스킬**: `.claude/skills/unity-dots-ecs/skill.md` (RPC 섹션)
- **Knowledge Base**: `Document/knowledge/knowledge_netcode.md` (RPC 섹션)

---

## 주의사항

### RPC 필수 규칙
1. ✅ **RPC Entity는 반드시 삭제**: `ecb.DestroyEntity(entity)`
2. ✅ **Unmanaged 타입만 사용**: int, float (string X, List X)
3. ✅ **연결 확인 필수**: `RequireForUpdate<NetworkId>()`
4. ✅ **TargetConnection 생략 시 브로드캐스트**: 모든 Client에게 전송
5. ✅ **Server도 자신의 RPC를 받음**: Server + Client 양쪽 실행

### 디버깅 팁
- RPC가 전송 안 되면: NetworkStreamInGame 태그 확인
- RPC가 수신 안 되면: WorldSystemFilter 확인
- 경고 발생 시: RPC Entity 삭제 여부 확인

---

**상태**: 계획 완료, 구현 대기 중
**다음 단계**: 구현 시작
