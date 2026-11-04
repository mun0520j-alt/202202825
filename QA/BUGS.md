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
| 상태 | Fixed (검증 대기) |
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
| 상태 | Fixed (검증 대기) |
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
