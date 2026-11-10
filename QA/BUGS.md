# 버그 목록

> M0 기준 정리에서 발견한 결함을 추적한다. 상태 변경과 검증 결과는 같은 ID 아래에 누적한다.

## BUG-001 — Edit Mode에서 미니맵 RenderTexture 파괴 경고

| 항목 | 내용 |
|---|---|
| 상태 | Closed (수정 확인) |
| 심각도 | Minor |
| 우선순위 | P2 |
| 발견 단계 | M0 기반 정리 |
| 영향 범위 | Unity Editor, `MinimapController`가 포함된 씬에서 Play Mode 종료 |
| 관련 코드 | `Assets/Scripts/UI/MinimapController.cs:61-69` |

### 사전 조건

- `[ExecuteAlways]`가 적용된 `MinimapController`가 씬에 존재한다.
- `Build()`가 실행되어 런타임 생성 `RenderTexture`가 연결돼 있다.
- Unity Console이 열려 있다.

### 재현 절차

1. `MinimapController`가 있는 던전 씬을 연다.
2. Play Mode에 진입한다.
3. Play Mode를 정지한다.
4. Console을 확인한다.

### 기대 결과

- 미니맵 카메라의 `targetTexture`가 해제된다.
- 임시 `RenderTexture`가 Edit Mode와 Play Mode 각각에 맞는 방식으로 정리된다.
- Console에 리소스 파괴 관련 경고가 발생하지 않는다.

### 실제 결과

- (수정 전) `OnDestroy()`가 실행 환경과 관계없이 `Destroy(renderTexture)`를 호출했다.
- Play Mode 정지 과정에서 Edit Mode의 `OnDestroy()`가 실행되며 다음 경고가 발생한다.

```text
Destroy may not be called from edit mode! Use DestroyImmediate instead.
Destroying an object in edit mode destroys it permanently.
UnityEngine.Object:Destroy (UnityEngine.Object)
MinimapController:OnDestroy () (at Assets/Scripts/UI/MinimapController.cs:67)
```

### 원인 분석

- 클래스에 `[ExecuteAlways]`가 적용되어 `OnDestroy()`가 Edit Mode에서도 호출될 수 있다.
- 수정 전 구현은 `Application.isPlaying` 분기 없이 `Destroy(renderTexture)`만 사용했다.
- `RenderTexture.Release()`는 GPU 리소스를 해제하지만 Unity 오브젝트 자체 파괴를 대신하지 않는다.

### 수정 후보

```csharp
if (Application.isPlaying)
{
    Destroy(renderTexture);
}
else
{
    DestroyImmediate(renderTexture);
}
```

### 검증 항목

- Edit Mode에서 씬 재로드 및 오브젝트 제거 시 경고가 없는지 확인한다.
- Play Mode 종료 시 미니맵 카메라와 `RenderTexture`가 남지 않는지 확인한다.
- Play Mode 진입과 종료를 3회 반복해 고아 카메라 또는 GPU 리소스 누적이 없는지 확인한다.

### 비고

- `fix/BUG-001-minimap-destroy` 브랜치에서 Play/Edit Mode별 파괴 방식을 분리했다.
- 사용자 재확인 결과 BUG-001 경고는 사라지고 BUG-002 패키지 오류만 남았다.

### 수정 및 검증 기록

| 날짜 | 단계 | 내용 | 커밋 |
|---|---|---|---|
| 2026-11-03 | 등록 | 정적 분석으로 등록 | 80f58ef |
| 2026-11-03 | 재현 확인 | Play Mode 정지 시 Console 경고 재현, 로그 첨부 | b8cfb8d |
| 2026-11-03 | 수정 | `Application.isPlaying` 분기로 `Destroy` / `DestroyImmediate` 분리 | eea623e |
| 2026-11-03 | 수정 확인 | 사용자 재확인: Play 진입→정지 시 BUG-001 경고 사라짐 | 217bcbe |

| 검증 항목 | 결과 |
|---|---|
| Play Mode 종료 시 경고 없음 | 통과 (사용자 확인) |
| Edit Mode 씬 재로드·오브젝트 제거 시 경고 없음 | 통과 (사용자 확인, 2026-11-03) |
| Play 진입·종료 3회 반복 시 고아 카메라/RenderTexture 누적 없음 | 통과 (사용자 확인, 2026-11-03) |

---

## BUG-002 — Git 실행 파일 미탐지로 Unity MCP 패키지 해석 실패

| 항목 | 내용 |
|---|---|
| 상태 | Accepted (Won't Fix) |
| 심각도 | Minor |
| 우선순위 | P2 |
| 발견 단계 | M0 기반 정리 |
| 영향 범위 | Git이 PATH에 등록되지 않은 개발 환경의 Unity Package Manager |
| 관련 설정 | `Packages/manifest.json`의 `com.coplaydev.unity-mcp` Git URL 의존성 |

### 사전 조건

- `com.coplaydev.unity-mcp`가 Git URL 패키지로 등록돼 있다.
- Unity와 Unity Hub가 실행하는 환경의 PATH에서 `git` 실행 파일을 찾을 수 없다.

### 재현 절차

1. Git이 설치되지 않았거나 PATH에서 탐지되지 않는 환경에서 프로젝트를 연다.
2. Unity Package Manager가 프로젝트 의존성을 해석할 때까지 기다린다.
3. Package Manager 오류 창 또는 Console을 확인한다.

### 기대 결과

- 프로젝트 패키지 의존성이 정상적으로 해석된다.
- 개발용 MCP 패키지 문제 때문에 프로젝트 로드가 오류 상태가 되지 않는다.

### 실제 결과

Unity가 Git URL 의존성을 가져오지 못해 다음 오류를 표시한다.

```text
An error occurred while resolving packages:
  Project has invalid dependencies:
    com.coplaydev.unity-mcp: No 'git' executable was found. Please install Git on your system then restart Unity and Unity Hub
```

### 원인 분석

- `com.coplaydev.unity-mcp`가 로컬 또는 Registry 패키지가 아니라 Git URL로 선언돼 있다.
- Unity Package Manager가 해당 패키지를 내려받으려면 외부 `git` 실행 파일이 필요하다.
- 게임 런타임 기능이 아니라 개발 도구 의존성 문제이므로 게임 플레이 결함과 분리해 추적한다.

### 수정 후보

- Unity와 Unity Hub를 실행하는 환경의 PATH에 Git을 등록하고 두 프로그램을 재시작한다.
- 팀 공용 개발 환경 안내에 Git 설치 및 PATH 확인 절차를 추가한다.
- MCP가 필수 개발 의존성이 아니라면 프로젝트 필수 패키지와 분리하는 방안을 검토한다.

### 검증 항목

- 새 터미널에서 `git --version`이 정상 출력되는지 확인한다.
- Unity와 Unity Hub 재시작 후 Package Manager 오류가 사라지는지 확인한다.
- MCP 패키지가 없어도 게임 코드 컴파일과 Play가 가능한지 별도로 확인한다.

### 처리 결정

- MCP는 지속적으로 업데이트되는 개발 도구이며, Git 연결이 준비된 환경에서는 바로 사용할 수 있다.
- 게임 런타임 결함이 아니므로 현재 프로젝트에서 패키지 구조를 변경하지 않고 알려진 개발 환경 제약으로 수용한다.

---

## BUG-003 — 탈출 절차 중 적이 행동하지 않고 생환 확정

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 원정 한 바퀴 |
| 영향 범위 | 3층 탈출 절차, 생환·사망·MIA 결과 판정 |
| 관련 코드 | `ExpeditionManager.ResolveExitAfterTick()`, `Tick.Advance()` |

### 사전 조건

- 플레이어가 3층 탈출구에 도달해 있다.
- 탈출 절차 Tick 동안 행동할 수 있는 적이 주변에 있다.
- 원정이 생환·사망·MIA 중 어느 결과로도 아직 종료되지 않았다.

### 재현 절차

1. 3층 탈출구 칸에서 탈출 절차를 시작한다.
2. 탈출 비용이 적용되는 동안 적의 행동 횟수와 플레이어 HP를 관찰한다.
3. 다음 프레임의 원정 결과를 확인한다.

### 기대 결과

- 탈출 절차가 소비한 Tick만큼 적도 자신의 시간축에 따라 행동한다.
- 절차 완료 시점에 플레이어 생존 여부와 MIA 조건을 확인한다.
- 완료 전에 플레이어가 사망하면 `Dead`, MIA 임계값에 도달하면 `MIA`, 둘 다 아니면 `Escaped`가 된다.

### 실제 결과 (수정 전)

- 탈출 비용을 한 번 적용한 뒤 다음 프레임에 바로 `Escaped`를 확정했다.
- 탈출 절차 동안 적이 행동하거나 플레이어가 사망할 기회가 결과 판정에 반영되지 않았다.

### 원인 분석

- `ResolveExitAfterTick()`가 `yield return null` 한 번 뒤 생존 및 플레이어 차례 복귀를 확인하지 않고 생환을 확정했다.
- 기존 정수 `Tick.Advance(amount)`는 `amount`만큼 한 번에 증가한 뒤 `OnTick`을 한 번만 발행해, Tick 구독자가 소비 시간만큼 행동할 수 없었다.
- 탈출 진행 플래그가 세워진 동안 사망·MIA 처리도 함께 차단될 수 있었다.

### 수정 내용

- 탈출 절차 기본값을 30Tick에서 10Tick으로 조정했다. (`d958cf9`)
- 큐 기반 경로는 적 턴 처리 후 플레이어 차례 복귀 또는 사망·MIA 확정까지 완료 판정을 보류한다. (`ba39534`)
- 정수 Tick 경로는 비용을 1Tick씩 발행해 구독자가 매 Tick 행동할 수 있게 했다. (`ba39534`)
- 절차 중 `Dead`와 `MIA`가 `Escaped`보다 우선하도록 결과 가드를 수정했다. (`ba39534`)

### 검증 항목

- 적이 있는 상태에서 10Tick 탈출 시 적 행동이 시간축에 맞게 발생하는지 확인한다.
- 탈출 절차 중 HP가 0이 되면 `Dead`로 종료되는지 확인한다.
- 탈출 완료 Tick이 1,440에 도달하면 `MIA`로 종료되는지 확인한다.
- 생존하고 MIA가 아닌 경우에만 `Escaped`로 종료되는지 확인한다.
- 1,439/1,440/1,441 경계값에서 완료 시점 판정 결과를 기록한다.

### 수정 및 검증 기록

| 날짜 | 단계 | 내용 | 커밋 |
|---|---|---|---|
| 2026-11-03 | 등록 | 탈출 절차 중 적 미행동 명세 불일치 등록 | 문서 커밋 |
| 2026-11-03 | 비용 수정 | 탈출 절차 기본값 30 → 10Tick | d958cf9 |
| 2026-11-03 | 로직 수정 | 적 행동 처리 후 완료 판정, 사망·MIA 우선 | ba39534 |
| 2026-11-03 | 회귀 검증 | Unity Play 확인 필요 | 미검증 |
| 2026-11-05 | 회귀 검증 | 탈출 체류 중 Enemy 행동, 공격·대기 Tick 반영 및 구역 이탈 초기화 확인 | 사용자 확인 |

---

## BUG-004 — InDungeon 씬이 컴파일 제외 폴더의 스크립트를 참조

| 항목 | 내용 |
|---|---|
| 상태 | Closed (수정 완료) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 원정 한 바퀴 |
| 영향 범위 | `InDungeon` 씬 로드 및 `DungeonRunManager` 기반 런타임 구성 |
| 관련 코드 | `_to_delete`의 `DungeonRunManager`·`GridMover`·`Tick`, `Assets/Scripts/MapGen/DungeonRunManager.cs` |

### 재현 절차

1. `_to_delete` 폴더에 원본 스크립트가 있고 `InDungeon` 씬이 해당 GUID를 참조하는 상태로 프로젝트를 연다.
2. Unity가 스크립트를 컴파일하고 `InDungeon` 씬을 로드하도록 한다.
3. Console의 타입 누락 오류와 씬 컴포넌트 연결 상태를 확인한다.

### 기대 결과

- 씬이 참조하는 모든 런타임 스크립트가 `Assets` 아래에서 컴파일된다.
- `DungeonRunManager`, `GridMover`, `Tick` 타입과 직렬화된 컴포넌트 연결이 유지된다.

### 실제 결과 (수정 전)

- `InDungeon` 씬은 `_to_delete` 폴더에 남아 있던 스크립트 GUID를 계속 참조했다.
- 해당 폴더가 Unity 컴파일 대상에서 제외되어 타입을 찾지 못하고 런타임 구성이 동작하지 않았다.

### 원인 분석

- 스크립트를 정리 폴더로 옮긴 뒤 씬의 직렬화 참조를 함께 갱신하지 않았다.
- 파일과 `.meta`를 분리하거나 새 GUID로 교체하면 씬 참조가 끊어질 수 있는 구조였다.

### 수정 내용

- 원본 `.meta`와 GUID를 보존한 채 런타임 스크립트를 `Assets` 아래 컴파일 경로로 이동했다.
- 기존 씬의 직렬화 참조를 재설정하지 않고도 동일 타입을 다시 해석하도록 복구했다. (`e47ec10`)

### 검증 결과

- Unity 컴파일에서 관련 타입 누락 오류가 해소됐다.
- `InDungeon`에서 기존 GUID 기반 컴포넌트 참조가 유지됨을 확인했다.

### 수정 및 검증 기록

| 날짜 | 단계 | 내용 | 커밋 |
|---|---|---|---|
| 2026-11-03 | 원인 확인 | 씬이 `_to_delete`의 컴파일 제외 스크립트 GUID를 참조 | 기록 목적 등록 |
| 2026-11-03 | 수정 | GUID 보존 이동으로 컴파일 및 씬 참조 복구 | e47ec10 |
| 2026-11-03 | 종료 | 타입 오류 해소 확인, Closed 처리 | 문서 커밋 |

---

## BUG-005 — 런타임 Enemy 생성 중 체력바 초기화 NullReferenceException

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 원정 한 바퀴 |
| 영향 범위 | `DungeonRunManager`의 층별 Enemy 런타임 생성 |
| 관련 코드 | `ProceduralHealthBar.SetFill()`, `EnemyTurnActor.TryInitialize()` |

### 재현 절차

1. `InDungeon`에 진입해 `DungeonRunManager`가 Enemy를 생성하도록 한다.
2. 비활성 상태로 생성된 Enemy에 `EnemyTurnActor.ConfigureRuntime()`이 호출될 때까지 기다린다.
3. Console을 확인한다.

### 기대 결과

- Enemy의 Tilemap 참조와 체력바가 초기화된다.
- Enemy가 활성화되어 TickManager에 등록된다.

### 실제 결과 (수정 전)

```text
NullReferenceException: Object reference not set to an instance of an object
ProceduralHealthBar.SetFill (System.Single ratio01) (at Assets/Scripts/UI/ProceduralHealthBar.cs:76)
EnemyTurnActor.TryInitialize () (at Assets/Scripts/Enemy/EnemyTurnActor.cs:129)
DungeonRunManager.CreateRuntimeEnemy () (at Assets/Scripts/MapGen/DungeonRunManager.cs:227)
```

### 원인 분석

- Enemy를 안전하게 구성하기 위해 GameObject를 비활성 상태로 만든 뒤 컴포넌트를 추가했다.
- 비활성 GameObject의 `ProceduralHealthBar.Awake()`보다 `EnemyTurnActor`의 초기 `SetFill()` 호출이 먼저 실행됐다.
- `fillTransform`과 `fillRenderer`가 아직 생성되지 않아 NullReferenceException이 발생했다.

### 수정 내용

- `ProceduralHealthBar.Build()`을 중복 호출 가능한 형태로 변경했다.
- `SetFill()`이 내부 뷰를 먼저 지연 초기화하므로 Awake 호출 순서와 무관하게 동작한다. (`6c2b0b0`)

### 검증 항목

- InDungeon 진입 시 Enemy 3개가 오류 없이 생성되는지 확인한다.
- Enemy 체력바가 표시되고 TickManager 행동 로그가 발생하는지 확인한다.
- 플레이어 공격 후 체력바가 정상 갱신되는지 확인한다.

### 검증 결과

- InDungeon 진입 및 F4 배치에서 Enemy가 NRE 없이 생성되고 TickManager 행동 로그가 출력됨을 확인했다.
- Player 공격에 따라 체력바가 갱신되고 Enemy 처치까지 정상 처리됨을 확인했다.

---

## BUG-006 — 플레이어 피격 시 전역 Tick이 Enemy 공격 비용만큼 추가 경과

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 Play 검증 |
| 영향 범위 | 전역 시계, MIA 판정, 탈출 완료 Tick |
| 관련 코드 | `TickManager.OnTimeAdvanced`, `DungeonClock.HandleTimeAdvanced()` |

### 재현 절차

1. Enemy와 인접해 공격받을 수 있는 상태를 만든다.
2. 플레이어 행동 전후의 누적 Tick을 기록한다.
3. Enemy가 플레이어를 공격한 직후 누적 Tick을 다시 확인한다.

### 기대 결과

- 전역 시계는 플레이어가 선택한 행동 비용으로만 증가한다.
- Enemy 공격은 스케줄러의 같은 시간축 안에서 처리되며 전역 시계를 추가로 밀지 않는다.

### 실제 결과 (수정 전)

- 플레이어가 피격될 때마다 Enemy 공격 비용 5Tick이 전역 시계에 추가 반영됐다.
- 플레이어 공격 5Tick 사이의 Enemy 행동 로그가 1+4 또는 2+3처럼 보여 공격 비용이 분할된 것으로 오인될 수 있었다.

### 원인 및 수정

- `DungeonClock`이 모든 액터의 `NextActionTime` 최댓값을 전역 시간으로 사용해, Enemy의 비용이 Player보다 앞설 때 전역 시계도 함께 전진했다.
- Tick 이벤트에 행동 액터를 함께 전달하고, `DungeonClock`은 `PlayerTurnActor`의 절대 시각만 반영하도록 변경했다. (`ccafab9`)
- 로그의 대괄호 값은 전역 Tick으로 통일하고 액터 예약 시각은 `actorTime`으로 분리했다. 따라서 Player 공격은 `/Attack/5` 한 줄이며, 후속 Enemy 행동은 같은 전역 Tick에서 별도 표시된다. (`ccafab9`)

### 검증 결과

- 플레이어 행동으로 1Tick 진행한 뒤 Enemy가 남은 4Tick을 같은 시간축에서 처리하고 공격함을 확인했다.
- Enemy 공격은 전역 Tick을 추가로 증가시키지 않았고, Player 공격은 정확히 5Tick 증가했다.

---

## BUG-007 — 탈출 체류 중 공격·피격 Tick 이중 계산

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 Play 검증 |
| 영향 범위 | 3층 탈출 체류 진행도, 생환·MIA 경계값 |
| 관련 코드 | `DungeonStairs`, `DungeonClock.HandleTimeAdvanced()` |

### 재현 절차

1. 3층 탈출 구역 안에서 진행도를 0으로 시작한다.
2. 공격 1회 또는 Space 대기를 실행하고 Enemy에게 피격된다.
3. 전역 Tick과 탈출 진행도의 증가량을 비교한다.

### 기대 결과

- 탈출 진행도는 구역 안에서 플레이어가 소비한 Tick만큼만 증가한다.
- Enemy 행동·피격은 진행도와 전역 시계를 추가로 증가시키지 않는다.

### 실제 결과 (수정 전)

- 플레이어 행동 비용에 Enemy 행동 비용까지 전역 시계에 반영되어 탈출 진행도가 이중으로 증가했다.

### 원인 및 수정

- 탈출 진행도가 `DungeonClock.ElapsedTicks`의 차이를 사용하고 있어 BUG-006의 전역 시간 오염이 그대로 전파됐다.
- 전역 시계를 Player 행동 기준으로 분리해 탈출 진행도도 Player가 소비한 Tick만 반영되도록 수정했다. (`ccafab9`)

### 검증 결과

- 탈출 구역에서 Space는 1Tick, Player 공격은 5Tick만 진행도에 반영됐다.
- Enemy 공격은 진행도를 추가하지 않았고, 구역 이탈 후 진행도가 초기화됨을 확인했다.

---

## BUG-008 — Enemy 인접 상태에서 Space 대기 입력 누락

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 Play 검증 |
| 영향 범위 | 대기 입력, Enemy 인접 전투 |
| 관련 코드 | `PlayerTurnActor.Update()` |

### 재현 절차

1. 플레이어를 Enemy와 인접시킨다.
2. Enemy 행동 처리 직후 또는 처리 중 Space를 한 번 누른다.
3. 다음 Player 턴의 행동과 누적 Tick을 확인한다.

### 기대 결과

- Enemy 인접 여부와 관계없이 Space 1회가 대기 1회로 처리되고 전역 시계가 1Tick 증가한다.

### 실제 결과 (수정 전)

- `isMyTurn == false`인 프레임에 입력 검사가 끝나 Space 입력이 조용히 버려졌다.

### 원인 및 수정

- Space 감지가 Player 턴 가드 뒤에 있어 Enemy 연속 행동 중 누른 키를 다음 Player 턴으로 넘기지 못했다.
- Space 입력을 먼저 감지해 bool로 1회 예약하고, 다음 Player 턴에 대기 1Tick으로 실행하도록 변경했다. 연타가 여러 턴으로 누적되지는 않는다. (`0f094e1`)

### 검증 결과

- Enemy 인접 상태에서 Space 대기가 정상 실행되고 1Tick만 소비됨을 확인했다.
- Enemy는 이전 공격 후 정확히 5Tick째 다시 공격해 입력 예약이 스케줄을 깨지 않음을 확인했다.

---

## BUG-009 — 층 이동 시 플레이어 HP가 최대치로 회복

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 Play 검증 |
| 영향 범위 | 1→2→3층 원정의 누적 피해와 전투 위험 |
| 관련 코드 | `ExpeditionSession`, `PlayerTurnActor`, `DungeonSceneBootstrapper` |
| Jira | DUN-25 |

### 재현 절차

1. Enemy 공격으로 Player HP를 최대치 미만으로 낮춘다.
2. 계단을 사용해 다음 층으로 이동한다.
3. 새 층에서 Player HP를 확인한다.

### 기대 결과

- 같은 원정의 층 이동이므로 감소한 현재 HP가 유지된다.

### 실제 결과

- `InDungeon` 씬이 다시 로드되면서 Player HP가 최대치로 회복된다.

### 원인 분석

- `ExpeditionSession`은 현재 층과 누적 Tick은 보존하지만 Player 현재 HP는 보존하지 않는다.
- 층마다 새로 생성되는 `PlayerTurnActor`가 직렬화 기본값 `currentHP=10`으로 초기화된다.

### 수정 내용

- `ExpeditionSession`에 현재 HP와 초기화 여부를 보존한다.
- 첫 출격에서는 최대 HP를 기록하고, 같은 원정의 층 재진입에서는 저장된 현재 HP를 복원한다. (`fe84d70`)
- 피격 및 QA HP 변경 시 세션 HP를 즉시 갱신한다. (`fe84d70`)

### 회귀 검증 항목

- 1→2층과 2→3층 이동에서 현재 HP가 동일하게 유지되는지 확인한다.
- 새 원정을 시작할 때만 HP가 최대치로 초기화되는지 확인한다.
- 사망·MIA·생환 결과 처리에는 저장 HP가 남지 않는지 확인한다.

### 검증 결과

- 감소한 HP가 1→2→3층 이동 후에도 유지되고, 새 원정에서만 최대 HP로 초기화됨을 확인했다.

---

## BUG-010 — MIA 확정 대기 중 마우스 자동 경로가 계속 실행

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-05) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M1 경계값 검증 |
| 영향 범위 | MIA 경계 1,440Tick, 마우스 자동 이동, 결과 확정 전 상태 |
| 관련 코드 | `PlayerTurnActor.queuedAutoPath`, `ExpeditionManager.ResolveMIAAfterScheduledActions()` |
| Jira | DUN-26 |

### 재현 절차

1. 누적 Tick을 1,439로 설정한다.
2. 현재 위치에서 여러 칸 떨어진 바닥을 마우스로 클릭한다.
3. 첫 이동으로 1,440Tick에 도달한 뒤 Player 위치와 결과 전환 전 움직임을 확인한다.

### 기대 결과

- MIA가 발생한 행동 1칸까지만 반영된다.
- 결과 확정 대기 중 입력과 남은 자동 경로가 취소되고 추가 이동하지 않는다.

### 실제 결과

- MIA 판정이 예약된 뒤에도 클릭한 칸을 향한 자동 경로가 계속 실행된다.

### 원인 분석

- `HandleMIA()`는 결과 판정 코루틴만 시작하고 Player 입력이나 `queuedAutoPath`를 잠그지 않는다.
- `ResolveMIAAfterScheduledActions()`가 Player 차례 복귀를 기다리는 동안 `PlayerTurnActor.Update()`가 남은 경로를 계속 소비할 수 있다.

### 수정 내용

- MIA 판정 시작 시 Player의 예약 Space와 자동 경로를 즉시 취소하고 결과 확정까지 새 입력을 잠근다. (`fe84d70`)
- 이미 시작된 한 칸 연출과 같은 Tick의 Enemy 행동은 완료시켜 `Dead > MIA` 우선순위를 유지한다. (`fe84d70`)

### 회귀 검증 항목

- 1,439Tick에서 먼 칸 클릭 시 첫 한 칸 뒤 추가 이동 없이 MIA가 확정되는지 확인한다.
- 방향키·Space 입력도 MIA 확정 대기 중 무시되는지 확인한다.
- 같은 Tick의 Enemy 공격으로 사망하면 여전히 Dead > MIA가 유지되는지 확인한다.

### 검증 결과

- 1,439Tick에서 먼 칸 클릭 후 첫 한 칸으로 1,440에 도달하면 남은 자동 경로가 중단되고 MIA가 확정됨을 확인했다.
- 같은 Tick에 Enemy 공격으로 사망할 경우 Dead > MIA 우선순위가 유지됨을 확인했다.

---

## BUG-011 — 인벤토리 패널 중복 표시

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M2 1차 Play 검증 |
| 영향 범위 | 인벤토리 UI, 원정 가방 |
| 관련 코드 | `DungeonSceneBootstrapper`, `InventoryPanel`, `M2BagPanel` |
| Jira | DUN-30 |

### 실제 결과

- `I` 입력 시 기존 `InventoryPanel`과 임시 `M2BagPanel`이 함께 열려 검은 UI가 중첩됐다.

### 수정 내용

- 기존 `InventoryPanel`을 단일 화면으로 사용하고 임시 `M2BagPanel`의 런타임 생성을 제거했다.
- 기존 화면에 `LootRuntimeState` 가방 데이터와 `PlayerEquipment` 장비 데이터를 연결했다.

### 회귀 검증 항목

- `I` 입력 시 인벤토리가 하나만 표시되는지 확인한다.
- 가방 용량 6칸, 획득품, 기본 장비 이름과 대체 아이콘이 같은 화면에 표시되는지 확인한다.

---

## BUG-012 — 인벤토리 중 월드 입력 처리

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M2 1차 Play 검증 |
| 영향 범위 | 이동, 공격, 상호작용, 마우스 자동이동 |
| 관련 코드 | `PlayerTurnActor`, `DungeonChest`, `DungeonStairs`, `GroundLoot`, `InventoryPanel` |
| Jira | DUN-31 |

### 실제 결과

- 인벤토리를 연 상태에서도 방향키 이동과 마우스 월드 입력이 처리됐다.

### 수정 내용

- 인벤토리를 시간 정지가 아닌 입력 레이어로 분리해 월드 입력만 차단했다.
- 버리기 1Tick·장비 교체 2Tick 같은 인벤토리 내부 행동은 정상적으로 시간과 Enemy 행동을 진행한다.
- 내부 행동 중 사망하면 인벤토리를 닫고 결과 처리를 이어간다.

### 회귀 검증 항목

- 인벤토리 중 이동·공격·상자·계단·바닥 아이템·월드 클릭이 처리되지 않는지 확인한다.
- 버리기와 장비 교체 후 인벤토리가 유지되고 Enemy가 정상 행동하는지 확인한다.

---

## BUG-013 — 기본 장비를 인벤토리에서 식별할 수 없음

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Minor |
| 우선순위 | P2 |
| 발견 단계 | M2 1차 Play 검증 |
| 영향 범위 | 기본 장비 지급 확인, 장비 교체 UX |
| 관련 코드 | `InventoryPanel`, `PlayerEquipment`, `LootRuntimeState` |
| Jira | DUN-32 |

### 실제 결과

- 기본 검·방패·갑옷은 전투 효과와 로그가 있으나 인벤토리에서 이름을 식별하기 어려웠다.

### 수정 내용

- 장비 슬롯 라벨에 현재 장착 아이템 이름을 표시하고, 아이콘이 없으면 기존 대체 색상을 유지했다.
- M2 전리품 장착 시 동일 슬롯과 전투 보너스가 갱신되도록 데이터 연결을 추가했다.

### 회귀 검증 항목

- 기본 검·방패·갑옷의 이름과 슬롯을 식별할 수 있는지 확인한다.
- 전리품 장착 성공 시 2Tick이 적용되고 이전 장비가 가방으로 돌아오는지 확인한다.

---

## BUG-014 — 장비 해제 기능 없음

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M2 2차 Play 검증 |
| 영향 범위 | 기본 장비·전리품 장비, 가방, 장비 교체 |
| 관련 코드 | `InventoryPanel`, `PlayerEquipment`, `LootRuntimeState` |
| Jira | DUN-33 |

### 실제 결과

- 장착과 교체는 가능하지만 장착 슬롯에서 장비를 해제할 수 없었다.

### 수정 내용

- 무기·방패·방어구 슬롯에 `해제` 버튼을 추가했다.
- 해제 성공 시 장비를 가방으로 돌리고 2Tick을 적용한다.
- 가방이 가득 차면 해제를 거부하고 Tick을 소비하지 않는다.
- 기본 장비를 해제한 상태도 층 이동 중 유지해 기본 장비가 중복 생성되지 않게 했다.

---

## BUG-015 — 휴식 회복 후 HP바 간헐 미갱신

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Minor |
| 우선순위 | P1 |
| 발견 단계 | M2 2차 Play 검증 |
| 영향 범위 | Space 연속 휴식, Player HP 표시 |
| 관련 코드 | `PlayerTurnActor`, `ProceduralHealthBar` |
| Jira | DUN-34 |

### 실제 결과

- 회복 로직과 로그는 정상이나 간헐적으로 머리 위 HP바가 회복 결과를 즉시 반영하지 않았다.

### 수정 내용

- 회복·피격 후 즉시 갱신에 더해 `LateUpdate`에서 최종 HP 값을 한 번 더 반영하도록 보강했다.

---

## BUG-016 — 키보드 이동 후 마우스 경로선 잔존

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Minor |
| 우선순위 | P1 |
| 발견 단계 | M2 2차 Play 검증 |
| 영향 범위 | PC 마우스 선택, 키보드 이동 |
| 관련 코드 | `PlayerTurnActor` |
| Jira | DUN-35 |

### 실제 결과

- 마우스로 목적지를 선택해 경로선을 표시한 뒤 키보드로 이동해도 이전 선택과 경로선이 남았다.

### 수정 내용

- 키보드 방향 입력이 들어오면 예약 자동 경로와 함께 마우스 선택·경로선을 제거한다.

---

## BUG-017 — 주변 아이템 목록과 인벤토리 중첩

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M2 재검증 |
| 영향 범위 | 주변 아이템 선택 목록, 인벤토리 입력 레이어 |
| 관련 코드 | `GroundLoot`, `InventoryPanel` |
| Jira | DUN-36 |

### 재현 절차

1. 주변에 바닥 아이템을 여러 개 둔다.
2. `E`로 주변 아이템 목록을 연다.
3. 목록이 열린 상태에서 `I`를 누른다.

### 실제 결과

- 주변 아이템 목록이 유지된 채 인벤토리가 함께 열려 두 UI가 중첩됐다.

### 수정 내용

- 주변 아이템 목록과 인벤토리를 상호 배타 모달로 처리한다.
- 목록이 열린 상태에서 `I`를 누르면 목록을 먼저 닫고 인벤토리만 연다.

---

## BUG-018 — BaseCamp Stash에서 장착 전리품 확인·해제 불가

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M2 마감 Play |
| 영향 범위 | 생환 장비 보존, BaseCamp Stash |
| 관련 코드 | `StashPanel`, `LootRuntimeState` |
| Jira | DUN-37 |

### 실제 결과

- 생환 후 장착한 전리품은 보존되지만 BaseCamp Stash 화면에서 확인하거나 해제할 수 없었다.

### 수정 내용

- Stash 상단에 장착 전리품의 슬롯·이름·등급을 표시한다.
- BaseCamp에서 해제하면 원정 Tick 없이 무제한 Stash로 이동한다.

---

## BUG-019 — 가방이 가득 찬 상태에서 바닥 아이템이 E 상자 개봉을 차단

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 검증 완료, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M2 마감 Play |
| 영향 범위 | 가방 6/6, 바닥 아이템, 상자 키보드 상호작용 |
| 관련 코드 | `GroundLoot`, `DungeonChest` |
| Jira | DUN-38 |

### 실제 결과

- 가방이 가득 차 바닥 아이템 획득이 실패하면 해당 아이템이 E 입력을 계속 선점해 인접 상자를 열 수 없었다.
- 마우스로 상자를 직접 지정하면 개봉할 수 있어 입력 방식에 따라 결과가 달랐다.

### 수정 내용

- 바닥 아이템과 상자가 동시에 상호작용 범위에 있으면 `주변 상호작용` 목록을 표시한다.
- 사용자가 아이템 줍기 또는 상자 열기를 직접 선택하며, 가방이 가득 차도 상자 선택은 정상 처리한다.
## BUG-020 — 다른 층 문이 E 입력을 선점해 현재 층 문이 바뀌지 않음

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 확인, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3-A 문 Play 검증 |
| 관련 코드 | `DungeonDoor` |

- **재현:** 2층에서 문 옆 `E` 입력 시 OpenDoor/CloseDoor 로그는 출력되지만 현재 보이는 문 상태가 변하지 않는다.
- **원인:** 세 층의 문이 모두 활성 `Update()`를 실행하고 동일한 로컬 셀 좌표로 인접 판정해 다른 층 문이 입력을 먼저 선점했다.
- **수정:** Player의 현재 Floor Tilemap과 문 소유 Tilemap이 같은 경우에만 입력을 처리한다.

## BUG-021 — 문 점유 셀에 상자 생성

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 확인, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3-A 문 Play 검증 |
| 관련 코드 | `DungeonRunManager`, `DungeonDoor` |

- **재현:** 층 생성 후 문 통로에 상자가 배치되어 문 이동·상호작용을 방해한다.
- **원인:** 상자 후보 셀에서 문 점유 셀을 제외하지 않았다.
- **수정:** `DungeonDoor.IsDoorCell`을 추가하고 상자 생성 후보에서 제외한다.

## BUG-022 — Enemy 문 개방 직후 Player 재닫기로 추격 봉쇄 가능

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 확인, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3-A 문 Play 검증 |
| 관련 코드 | `DungeonDoor`, `EnemyTurnActor` |

- **재현:** Enemy가 `/OpenDoor/2`로 문을 연 직후 Player가 반대편에서 1Tick으로 다시 닫는 행동을 반복한다.
- **실제:** Enemy가 문 열기만 반복하고 이동하지 못해 추격을 봉쇄할 수 있다.
- **수정:** Enemy가 문 칸 또는 문 인접 칸에 있으면 Player 문 닫기를 실패·0Tick 처리한다.

## BUG-023 — 같은 방 경보 대상 Enemy의 추적 행동 불명확

| 항목 | 내용 |
|---|---|
| 상태 | Closed (첨부 로그 확인, 2026-11-06) |
| 심각도 | Major 후보 |
| 우선순위 | P1 |
| 발견 단계 | M3-A 집단 경보 Play 검증 |
| 관련 코드 | `EnemyTurnActor`, `DungeonRoomRegistry` |

- **재현:** 한 Enemy가 Player를 발견해 같은 방 Enemy에 경보 표시가 발생한 뒤 행동을 관찰한다.
- **실제:** 경보 표시는 보이지만 경보받은 Enemy가 Player 발견 지점으로 추적하지 않는 것으로 관찰됐다.
- **조사 보강:** `[RoomAlert] 발신 → 수신 / Room ID / 마지막 목격 위치` 로그를 추가해 경보 대상과 경로 상태를 재검증한다.
- **검증:** 같은 Room ID의 대상만 1회 경보를 받고 마지막 목격 위치를 기준으로 Search한 뒤 종료하는 로그를 확인했다. 연쇄 경보 및 다른 Room 전파는 없었다.

## BUG-024 — Space 장기 휴식 중 회복·HP UI 갱신 정지 및 Tick 무한 소비

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 확인, 2026-11-06) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3-A 자동 휴식 Play 검증 |
| 관련 코드 | `PlayerTurnActor` |

- **재현:** HP 2에서 Space를 계속 눌러 자동 휴식한다.
- **기대:** 5Tick마다 HP와 체력바가 1씩 증가하고, 40Tick 뒤 HP 10에 도달하면 자동 대기가 멈춘다.
- **실제:** `Wait/1` 로그와 전역 Tick은 계속 증가하지만 일부 회복 및 체력바 갱신이 실행되지 않고 최대 HP에서도 대기가 이어진다.
- **원인:** 5번째 대기 뒤 시작한 회복 코루틴이 `IsReadyForInput`을 기다리는 동안, `Update`의 자동 대기가 돌아온 플레이어 턴을 같은 프레임에 다시 소비해 회복 처리 시점을 계속 놓쳤다.
- **수정:** 5번째 대기 전에 회복을 예약하고 다음 `OnTurnStart`에서 입력 처리보다 먼저 확정한다. 최대 HP 또는 추적 상태에서는 Space 장기 휴식을 새로 시작하지 않는다.
- **회귀 검증:** HP 2에서 Space를 유지했을 때 5Tick마다 HP와 체력바가 함께 증가하고, 40Tick 뒤 HP 10에서 자동 대기가 정지함을 확인했다.

## BUG-025 — 먼 거리 추적 중 문이 닫히면 마지막 목격 위치 도달 전에 무작위 수색 전환

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play·첨부 로그 확인, 2026-11-06) |
| 심각도 | Major 후보 |
| 우선순위 | P1 |
| 발견 단계 | M3-A 추적·Search Play 검증 |
| 관련 코드 | `EnemyTurnActor.OnTurnStart` |

- **재현:** Enemy가 멀리서 Player를 발견한 상태에서 거리를 벌리고 문을 닫아 시야를 차단한다.
- **기대 후보:** Enemy가 마지막 목격 위치까지 이동하며 경로상의 닫힌 문을 열고, 해당 위치를 확인한 뒤 주변을 수색한다.
- **실제:** 마지막 목격 위치 도달 여부와 무관하게 미인식 행동이 3회 누적되면 `isTracing`을 해제하고 무작위 `TryWanderStep` 수색으로 전환한다. 방향이 어긋나면 문을 열지 않고 추적을 포기한 것처럼 보인다.
- **로그 근거:** Tick 64에 최초 경보, Tick 67~68에 미인식 3회 수색 시작, Tick 68 문 닫힘, Tick 69~70 수색 종료. Room ID 전파 자체는 정상이다.
- **원인:** 추적 기억의 만료 조건이 마지막 목격 위치 도달이 아니라 고정 3회 행동이며, Search 단계가 마지막 목격 위치 경로를 우선하지 않고 즉시 무작위 이동한다.
- **기획 확정:** `마지막 목격 위치까지 추적 → 도착 후 Search 3회`로 변경한다.
- **수정:** 시야를 잃은 행동 횟수로 추적을 만료하지 않는다. 마지막 목격 셀에 도착할 때까지 기존 추적 경로와 Enemy 문 개방을 유지하고, 도착한 턴부터 Search 3회를 수행한다.
- **회귀 검증:** 먼 거리에서 시야를 끊고 문을 닫아도 Enemy가 마지막 목격 위치를 향해 이동했다. 경로상 문이 수색 범위 안에 있으면 `OpenDoor/2`로 개방하고, 도착 후 Search 3회 뒤 종료했다.

## BUG-026 — 탈출 구역 진입 직후 공격 Tick이 체류 진행도에서 누락

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play·경계 로그 확인, 2026-11-09) |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3 원정 CSV 수동 검증 |
| 관련 코드 | `DungeonStairs`, `TickManager` |
| Jira | DUN-41 |

- **재현:** 3층 탈출 구역 진입 직후 Player가 공격 2회를 연속 실행해 10Tick을 소비한다.
- **기대:** 공격 5Tick×2가 탈출 체류 10Tick으로 인정되어 생환한다.
- **실제:** `DungeonStairs.Update` 실행 순서에 따라 첫 공격 완료 후 탈출 시작 Tick이 저장되어 5Tick이 누락될 수 있다.
- **수정:** Player 행동 비용을 적용하기 전, 탈출 구역 안의 첫 비이동 행동이면 체류 시작 시각을 먼저 확정한다.
- **추가 보호:** F4/QA 디버그 맵, F5 가방 채우기, QA Tick·Player HP 강제 설정을 사용한 원정은 M3 CSV에 행을 저장하지 않는다.

## BUG-027 — 일반 출격이 기본 고정 Seed를 재사용해 측정 표본과 맵 다양성 왜곡

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play·Random CSV 확인, 2026-11-09) |
| Jira | DUN-43 |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3 목표 Tick 측정 준비 |
| 관련 코드 | `M3MeasurementAutomation`, `DungeonRunManager`, `ExpeditionDebugCommands` |

- **재현:** F6에서 Seed를 별도로 변경하지 않고 일반 출격을 여러 번 시작한다.
- **기대:** 일반 출격은 원정마다 새로운 Seed와 맵을 사용하고, Fixed Seed는 결함 재현·비교 시에만 명시적으로 선택한다.
- **실제:** PlayerPrefs 기본값 `20261106`을 모든 출격에서 재사용해 같은 맵이 반복되고, 맵 학습 효과가 목표 Tick 표본에 섞였다.
- **원인:** 설정 Seed와 이번 원정의 실제 Seed가 분리되지 않았고 Seed 모드 상태가 없었다.
- **수정:** 기본 Random/명시적 Fixed 모드를 분리하고, 원정 시작 때 실제 Run Seed를 한 번만 확정해 1~3층에서 유지한다. CSV v3에 모드와 Seed 반복 차수를 기록하고 Fixed 표본은 목표 Tick 자동 분석에서 제외한다.
- **회귀 범위:** TC-M3-01·21·24·27·30·31~36.

## BUG-028 — 문과 상자가 동시에 인접하면 문이 E 입력을 선점

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 확인, 2026-11-09) |
| Jira | DUN-42 |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M3 Random 원정 Play 검증 |
| 관련 코드 | `DungeonInteractionMenu`, `DungeonDoor`, `DungeonChest`, `GroundLoot` |

- **재현:** Player가 문과 미개봉 상자 모두에 인접한 위치에서 E를 누르고 상자 열기를 시도한다.
- **기대:** 선택 UI에서 문과 상자를 모두 보여주고 사용자가 선택한 대상 하나만 처리한다.
- **실제:** 상자 안내는 표시되지만 문 `Update()`가 같은 프레임의 상호작용 게이트를 먼저 점유해 문 열기·닫기만 실행된다.
- **원인:** 문과 상자가 각자 E 입력을 폴링하며 실행 순서에 따라 `LootInteractionGate`를 경쟁했다.
- **수정:** 우선 실행되는 중앙 선택 메뉴가 문+상자 동시 인접 상황을 감지해 E를 한 번만 점유한다. 선택한 문 또는 상자의 기존 실행 API만 호출하고 다른 대상은 처리하지 않는다.
- **회귀 범위:** 문 단독 1Tick, 상자 단독 5Tick, 문 닫기 Actor 차단, 주변 아이템 선택 UI.

## BUG-029 — 탈출 완료 Tick에 Space 유지 시 생환이 지연되고 Tick이 계속 경과

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play·경계 로그 확인, 2026-11-09) |
| Jira | DUN-44 |
| 심각도 | Critical 후보 |
| 우선순위 | P0 |
| 발견 단계 | M3 방 수 축소 후 Random 원정 Play 검증 |
| 관련 코드 | `DungeonStairs`, `TickManager`, `PlayerTurnActor` |

- **재현:** 3층 탈출 구역에 진입해 Space를 누른 채 10Tick 이상 계속 대기한다.
- **기대:** 예약된 Enemy 행동과 사망·MIA 판정을 처리한 뒤 정확히 체류 10Tick에서 입력을 중단하고 생환한다.
- **실제:** Player `Update()`가 탈출구 `Update()`보다 먼저 다음 대기 Tick을 소비해 `IsReadyForInput` 조건이 계속 거짓이 된다. Space를 놓을 때까지 Tick이 흐르고 생환이 지연된다.
- **로그 근거:** 385Tick에 탈출 구역 진입 후 395Tick에 완료되어야 하지만, Space 유지로 403Tick까지 `Wait/1`이 반복된 뒤 입력을 놓자 생환했다.
- **영향:** 마감 근처에서 정상 생환할 원정이 1,440Tick을 넘어 MIA로 잘못 판정될 수 있으며 원정 Tick 측정값도 오염된다.
- **수정:** 프레임 `Update()` 순서가 아니라 Player 행동과 해당 Tick에 예약된 Enemy 행동이 모두 끝난 직후 탈출 완료를 판정한다. 기존 탈출구 `Update()`도 같은 공통 완료 판정을 사용한다.
- **회귀 범위:** Space 유지 완료, 1,439/1,440 경계, 완료 Tick 사망 우선순위, 탈출 구역 이탈 초기화.

## BUG-030 — Enemy 턴 중 연속 공격 입력이 데미지만 적용하고 Player 공격 Tick을 누락

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play·첨부 로그 확인, 2026-11-09) |
| Jira | DUN-45 |
| 심각도 | Critical 후보 |
| 우선순위 | P0 |
| 발견 단계 | M3 탈출 구역 전투 Play 검증 |
| 관련 코드 | `PlayerTurnActor.AttackEnemy`, `TickManager.CompleteTurn` |

- **재현:** Enemy와 인접한 상태에서 첫 공격 뒤 Enemy 예약 행동·연출이 끝나기 전에 같은 방향 공격을 연속 입력한다.
- **기대:** 정상 Player 턴마다 데미지 1회와 `Attack/5` 1행이 함께 처리된다. Enemy 3타 처치에는 Player 공격 로그 3행과 총 15Tick이 필요하다.
- **실제:** Enemy는 3타 피해로 죽지만 Player `Attack/5`는 1행만 남고 탈출 체류도 5Tick만 증가한다.
- **로그 근거:** 수정 전 원정은 `damage_dealt=12`인데 `attack_ticks=5`만 기록됐다. 수정 후 Seed `-1542686671`은 3타 처치에 `Player/Attack/5` 3행, `attack_ticks=15`, `damage_dealt=12`로 일치했다.
- **원인:** `AttackEnemy()`가 다른 행동과 달리 행동 시작 전에 `isMyTurn=false`로 닫지 않았다. Enemy 턴 중 추가 입력이 데미지를 먼저 적용하고, 이후 `CompleteTurn()`은 현재 스케줄 액터 불일치로 무시될 수 있었다.
- **수정:** 공격 진입 시 Player 턴·애니메이션·사망·대상·TickManager를 검증하고 즉시 `isMyTurn=false`로 전환한 뒤 데미지와 5Tick을 처리한다.
- **회귀 범위:** 방향키 연타, 마우스 연타, 탈출 구역 3타 전투, Enemy 사망 후 반격 없음, 공격 1회당 로그·CSV 5Tick 일치.
- **2026-11-10 기준 변경:** 위 3타·15Tick·12피해는 수정 당시 증거로 보존한다. 전투 단위 재조정 후 회귀 기대값은 기본 장비 5타·25Tick·총 200피해이며 TC-M3-40·TC-M4-81~82에서 재검증한다.

## BUG-031 — 보스 광역 예고가 전역 5Tick 전에 발동

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 재검증 완료, 2026-11-10) |
| Jira | 미등록 |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M4 5층 보스 Play 검증 |
| 관련 코드 | `EnemyTurnActor.RunBossTurn`, `TickManager` |

- **재현:** 추격 후 보스와 교전해 두 번째 단일 공격의 광역 예고를 본 뒤 Player가 1칸 이동한다.
- **기대:** 예고가 생성된 전역 시각으로부터 5Tick 뒤에 광역이 발동한다.
- **실제:** 보스 개인 시간축이 추격 중 전역 시계보다 뒤처진 경우, 밀린 행동을 따라잡으면서 1Tick 이동 직후 광역이 발동한다.
- **원인:** 광역을 전역 Tick이 아니라 보스의 다음 행동 1회로만 예약했다.
- **수정:** 예고 행동 완료 시 보스의 다음 행동 시각을 `현재 전역 Tick + 5` 이상으로 보정한다.
- **회귀 범위:** 추격 거리별 예고 후 1~4Tick 미발동, 5Tick 발동, 보스 사망 시 예약 광역 취소.
- **검증:** 거리와 행동 비용을 달리해도 예고 후 정확히 전역 5Tick에 발동하며, 보스 사망 시 예약 광역이 취소됨을 확인했다.

## BUG-032 — 보스에게 사망하면 결과 처리 전에 스케줄러가 멈춤

| 항목 | 내용 |
|---|---|
| 상태 | Closed (사용자 Play 재검증 완료, 2026-11-10) |
| Jira | 미등록 |
| 심각도 | Critical |
| 우선순위 | P0 |
| 발견 단계 | M4 5층 보스 Play 검증 |
| 관련 코드 | `PlayerTurnActor.Update`, `TickManager.UnregisterActor`, `ExpeditionManager.CompleteDeath` |

- **재현:** 5층 보스의 단일·광역 공격으로 Player HP가 0이 되게 한다.
- **기대:** Dead 결과를 확정하고 BaseCamp 결과 화면으로 전환한다.
- **실제:** 입력과 진행이 멈추고 결과 화면으로 전환되지 않는다.
- **원인:** 사망 처리에서 `enabled=false`를 먼저 적용해 Player가 스케줄에서 해제됐다. Player가 현재 액터이면 남은 보스 AI만 동기적으로 계속 턴을 완료해 `CompleteDeath()` 호출까지 복귀하지 못한다.
- **수정:** 사망 결과 요청을 한 번만 표시한 뒤 Player를 비활성화하기 전에 `CompleteDeath()`를 호출한다.
- **회귀 범위:** 단일 사망, 광역 사망, 광역과 보스 사망 동시 판정, Dead 결과 화면·재출격.
- **검증:** 보스 단일·광역 사망 후 진행 정지 없이 Dead 결과 화면으로 전환됨을 확인했다.

## BUG-033 — M4 자동화가 M3 층 범위와 Tick 목표를 적용해 거짓 실패·오판을 생성

| 항목 | 내용 |
|---|---|
| 상태 | Closed (자동화 재실행 PASS, 2026-11-10) |
| Jira | 미등록 |
| 심각도 | Major |
| 우선순위 | P1 |
| 발견 단계 | M4 전투 단위 재조정 후 자동화 |
| 관련 코드 | `M3QaAutomationWindow.RunCoreRegression`, `AnalyzeRunCsv` |

- **재현:** M4 빌드에서 M3 QA 자동화를 실행하고 5층 생환 CSV를 분석한다.
- **기대:** 현재 원정 범위 1~5층을 Pass로 판정하고, 3층 생환만 기존 300~450Tick 목표와 비교하며 5층 생환은 별도로 집계한다.
- **실제:** `FinalFloor == 3`을 요구해 핵심 회귀가 실패하고, 5층 생환 632·676Tick을 3층 목표 300~450과 비교해 0/2로 표시한다.
- **원인:** M4에서 최종 층이 5층으로 확장됐지만 M3 자동화의 고정 기대값과 CSV 분석 범위를 갱신하지 않았다.
- **수정:** 핵심 회귀 기대값을 1~5층으로 변경하고 Random 생환을 `reached_floor` 3·5로 나눠 각각 집계한다. M4 5층 목표 범위는 미확정으로 명시한다.
- **회귀 범위:** 1~5층 핵심 회귀 Pass, 3층 목표 비교, 5층 별도 평균, Fixed Seed 분석 제외.
- **검증:** 자동화 종합 PASS, 핵심 회귀 9/9, Run Seed 1,000개 × 5층 실패 0. Random CSV는 3층 생환 0건과 5층 생환 2건 평균 654Tick을 분리해 표시했다.
