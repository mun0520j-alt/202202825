# 코드 리뷰 & 기술부채 로그 (Code Review Log)

> `DEV_WORKFLOW_RULES.md` 규칙 2-2에 따라 작성. 코드 작업 → `DEV_LOG.md` 기록 이후, 사용자와의 코드 리뷰 세션마다 새 항목을 맨 위에 추가한다(최신이 위).
> 각 항목에는 리뷰 대상(어떤 커밋/기능), 피드백 내용, 발견된 기술부채, 후속 조치 여부를 남긴다.

---

## 항목 작성 템플릿 (참고용, 실제 리뷰 시 복사해서 사용)

```
## YYYY-MM-DD — 리뷰 대상: (예: TickManager 초안)

**피드백**:
-

**발견된 기술부채**:
-

**후속 조치**:
- [ ] (조치 항목)
```

---

## 2026-08-27 — 리뷰 대상: Tick 시스템 코어 (`TickCost`/`ITurnActor`/`TickManager`/`DungeonClock`) + 검증 하네스(`TickQueueTestActor`/`TickQueueTestBootstrapper`)

**정적 리뷰(코드 구조)**:
- `TickCost.cs`: 승인 — 상수만 들고 로직 없음, 이동배율 확장 여지도 주석으로 명시됨
- `ITurnActor.cs`: 초안 이름(`ITickActor`)을 `ITurnActor`로 변경 — "턴을 갖는 액터"라는 의미가 더 직관적
- `TickManager.cs`: 워치독을 `Time.realtimeSinceStartup` 기준 5초 경고에서 `Time.deltaTime` 누적 기준 10초 `LogError`(버그 리포트)로 변경 — 일시정지 중 오작동 방지, 심각도 격상
- **[발견/수정] `CompleteTurn → 다음 액터 호출`이 재귀 호출로 구현되어 있었음** — AI처럼 `OnTurnStart` 안에서 즉시 `CompleteTurn`을 부르는 액터가 연쇄로 이어지면 재귀 깊이가 액터 수만큼 쌓여 극단적인 경우 `StackOverflowException` 위험. `isAdvancingSchedule` 가드 + `AdvanceSchedule()` 반복문으로 리팩터링해서 해결 — 실행 스택 깊이가 항상 일정하게 유지됨

**동적 리뷰(Test Scene 실행 결과)**:
- `TickQueueTestBootstrapper`로 Fast(0.2)/Medium(1)/Slow(2) tick 비용 액터 3개 등록 후 실행
- Fast 액터가 Medium/Slow보다 훨씬 자주 차례를 받음 확인 — 큐 스케줄링 정상
- `DungeonClock`이 tick 누적에 맞춰 실시계(06:01 → 06:49) 정확히 갱신 확인 — Manager→Clock 이벤트 흐름 정상
- 20턴째 의도적으로 `CompleteTurn` 생략 → 10초 뒤 정확히 `LogError` 버그 리포트 발생 확인 — 워치독 정상
- 재귀→반복문 수정 후 재실행 대기 중(결과는 이전과 동일해야 함 — 스택 사용 방식만 변경, 로직 동일)

**발견된 기술부채**:
- (해결됨) 재귀 호출로 인한 잠재적 StackOverflow — 위 수정으로 해소
- `FindEarliestActor()`가 매번 전체 리스트를 선형 탐색(O(n)) — 지금 규모(수십 개 액터)에서는 문제없지만, 나중에 액터 수가 크게 늘면 우선순위 큐(힙)로 교체 고려 가능. 지금은 조치 안 함(과설계 방지)

**후속 조치**:
- [x] 재귀 → 반복문 리팩터링 (`TickManager.AdvanceSchedule`)
- [x] 재귀 수정 후 재실행 결과 확인 — 스택 트레이스가 `AdvanceSchedule → BeginSchedule → Start` 한 겹으로 평탄화됨, 순서/시계값/워치독 발동 결과는 수정 전과 동일함을 사용자가 확인
- [ ] Enemy 여러 마리 실제로 붙을 때 `FindEarliestActor()` 성능 재검토

**결론**: Tick 시스템 코어(4파일) + 검증 하네스(2파일) 1차 리뷰 통과. 다음 단계는 플레이어 이동을 Test Scene의 임시 맵 제네레이터에 연결하는 작업.
