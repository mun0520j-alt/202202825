# 버그 목록

> M0 기준 정리에서 발견한 결함을 추적한다. 상태 변경과 검증 결과는 같은 ID 아래에 누적한다.

## BUG-001 — Edit Mode에서 미니맵 RenderTexture 파괴 경고

| 항목 | 내용 |
|---|---|
| 상태 | Open |
| 심각도 | Minor |
| 우선순위 | P2 |
| 발견 단계 | M0 기반 정리 |
| 영향 범위 | Unity Editor, `MinimapController`가 포함된 씬 편집 및 리로드 |
| 관련 코드 | `Assets/Scripts/UI/MinimapController.cs:61-69` |

### 사전 조건

- `[ExecuteAlways]`가 적용된 `MinimapController`가 씬에 존재한다.
- `Build()`가 실행되어 런타임 생성 `RenderTexture`가 연결돼 있다.
- Unity가 Play Mode가 아닌 Edit Mode다.

### 재현 절차

1. `MinimapController`가 있는 던전 씬을 연다.
2. Inspector 또는 미니맵 재구성 메뉴로 미니맵을 생성한다.
3. Edit Mode에서 씬을 다시 불러오거나 `MinimapController` 오브젝트를 제거해 `OnDestroy()`를 실행한다.
4. Console을 확인한다.

### 기대 결과

- 미니맵 카메라의 `targetTexture`가 해제된다.
- 임시 `RenderTexture`가 Edit Mode와 Play Mode 각각에 맞는 방식으로 정리된다.
- Console에 리소스 파괴 관련 경고가 발생하지 않는다.

### 실제 결과

- `OnDestroy()`가 실행 환경과 관계없이 `Destroy(renderTexture)`를 호출한다.
- `Destroy`는 지연 파괴 방식이라 Edit Mode 오브젝트 정리에 적합하지 않으며 Unity 경고가 발생할 수 있다.

### 정적 분석 근거

- 클래스에 `[ExecuteAlways]`가 적용되어 `OnDestroy()`가 Edit Mode에서도 호출될 수 있다.
- 현재 구현은 `Application.isPlaying` 분기 없이 `Destroy(renderTexture)`만 사용한다.
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

- 현재 단계에서는 정적 분석으로 등록했으며 Unity Console 재현 로그는 아직 첨부하지 않았다.
- 수정은 별도 `fix/BUG-001-minimap-destroy` 브랜치에서 진행한다.
