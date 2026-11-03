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
| Edit Mode 씬 재로드·오브젝트 제거 시 경고 없음 | 미기록 → 다음 회귀 때 확인 |
| Play 진입·종료 3회 반복 시 고아 카메라/RenderTexture 누적 없음 | 미기록 → 다음 회귀 때 확인 |

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
