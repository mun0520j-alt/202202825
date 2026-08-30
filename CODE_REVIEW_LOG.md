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

## 2026-08-28 — 리뷰 대상: Tick 폴더 재구성 + 플레이어 이동(`PlayerTurnActor`/`TilePathfinder`/`CameraFollow`/`DungeonSceneBootstrapper`)

**리뷰 방식**: 사용자가 코드를 먼저 읽고 자기 이해를 말하면, 그걸 기준으로 (1) 맞는 부분 확인 (2) 틀리거나 오해한 부분 교정 (3) 요청받은 네이밍/주석 보완을 적용하는 순서로 진행(사용자 지정 방식, 이후 리뷰도 이 형식 유지 예정).

**정적 리뷰 & 네이밍/주석 개선**:
- `TickCost.cs`: 구조 변경 불필요 확인(사용자 판단 동의)
- `ITurnActor.cs`: `CompleteTurn(this, tickCost)` 참조하는 주석으로 갱신(TickManager 리네이밍과 일관성 맞춤), 인터페이스 자체는 변경 없음
- `DungeonClock.cs`: `TicksPerMIA` → `MIAThresholdTicks`(의미가 더 명확), `LockDisplayToHour(int hour)` → `LockDisplayToMidnight()`(파라미터 제거 — `ALTAR_AND_TIME_SYSTEM_DESIGN.md`의 실제 설계가 "항상 자정 고정"이라 파라미터화가 과설계였음), `OnEnable` 이벤트 구독에 방어적 `-=` 선추가, `[Summary]` 헤더 및 필드별 설명 주석 추가. 리네이밍 전 `/tmp` 클린 클론으로 다른 참조 없는지 확인 후 진행
- `TickManager.cs`: `currentTurnElapsedSeconds` → `watchdogElapsedSeconds`, `warnedAboutCurrentTurn` → `stuckTurnAlreadyReported`, `CompleteTurn(actor, cost)` → `CompleteTurn(actor, tickCost)`(모든 호출부가 위치 인자라 안전 확인 후 진행). tick-시간(`NextActionTime`, 게임 논리 턴 순서)과 실시간(`watchdogElapsedSeconds`, 워치독 전용)이 서로 다른 개념이라는 걸 명시하는 `[주의]` 블록 추가
- **오해 교정**: `NextActionTime`이 10초 워치독과 관련 있다는 사용자 추측 — 틀림, 둘은 완전히 다른 시간 도메인(하나는 턴 순서용 추상 단위, 하나는 `Time.deltaTime` 누적 실초)이라는 걸 짚어줌
- **오해 교정**: `PlayerTurnActor.OnEnable`이 불필요하다는 추측 — 틀림. 플레이어는 씬에 항상 존재해서 스스로 등록/해제하는 self-registering 패턴이 맞는 설계(테스트 더미처럼 외부 부트스트래퍼가 등록해주는 방식과 의도적으로 다름)
- **오해 교정**: `Update()`에서 수동 입력이 자동 경로보다 우선이라 "경로 변경이 불가능해진다"는 추측 — 틀림. 오히려 수동 입력이 들어오면 기존 예약 경로를 버리고(`ClearQueuedAutoPath`) 새 입력을 따르는 구조라 언제든 자동 경로를 취소/변경 가능
- **오해 교정**: `StepAlongAutoPath()` 안에 반복문이 있다는 추측 — 틀림. 매 `Update()` 프레임마다 큐에서 한 칸씩만 꺼내는 구조(반복문 없음), 그래야 한 칸마다 애니메이션/몹 감지를 확인할 수 있음
- `PlayerTurnActor.cs`: 방향 벡터를 `Vector3Int`(2D인데 3 아닌가?)로 쓴 이유 설명 — `Tilemap`/`Vector3Int` API 자체가 셀 좌표를 3성분으로 다뤄서(z는 항상 0) 굳이 `Vector2Int`로 바꿔서 변환 비용을 만들 필요가 없음
- `HopTo()` 코루틴이 애니메이션 종료 즉시 `CompleteTurn`을 호출하는 구조라는 사용자 이해 확인 — 정확함. 단, 그 직전에 있던 `isMyTurn=false` 순서 버그(위 `DEV_LOG.md` 참고)까지 포함해서 최종 구조 확정
- `TilePathfinder.cs`: `cameFrom`/`visited`/`queue`/`found` → `previousCellOnPath`/`visitedCells`/`frontierQueue`/`foundGoal`로 개명, BFS 동작 원리(물결 확장 비유) + 메인 while 루프 + goal→start 역추적/`Reverse()` 부분에 단계별 주석 추가, `[Summary]` 클래스 헤더에 "BFS를 쓰는 이유(A* 대비 이득 없음)"와 "Pathfinding 폴더에 독립 배치한 이유(Enemy 재사용 목적)" 명시
- `CameraFollow.cs`: 구조 변경 불필요 확인(사용자 판단 동의), 이동 재구성 과정에서 GUID 재할당 버그가 있었으나 코드 자체 문제는 아니었음(`.meta` 복원으로 해결, 위 DEV_LOG 참고)
- `DungeonSceneBootstrapper.cs`: 흐름(TickManager/DungeonClock 준비 → ResetForNewRun → BeginSchedule) 이해 확인, `if (tickManager == null)` 폴백은 실질적으로 데드 코드(Unity의 Awake/OnEnable 순서상 `PlayerTurnActor.OnEnable`이 항상 먼저 TickManager를 만들어둠)라는 점 짚어줌 — 지금은 안전망으로 유지, 수정 불필요

**동적 리뷰(Test Scene 실행 결과)**:
- 방향키 4방향 이동, 마우스 클릭 자동 이동(경로 탐색 → 한 칸씩 순차 이동) 모두 정상 동작 확인
- 이동 1회당 0.2 tick 소비, 5칸 이동 시 DungeonClock 실시계 5분 갱신 확인
- 홉 애니메이션(사인 곡선 점프) 정상 재생 확인, 좌우 이동 시 스프라이트 반전 정상 확인
- 카메라가 플레이어 위치로 지연 없이 즉시 스냅 확인

**발견된 기술부채**:
- `IsMonsterVisibleNow()`가 항상 false를 반환하는 스텁 상태 — Enemy/시야 시스템 붙기 전까지는 정상, 다음 단계에서 실제 구현 필요
- 씬 참조 스크립트를 폴더 이동할 때 Unity가 GUID를 재할당해버리는 버그 패턴 확인됨 — 앞으로 스크립트 이동 시 반드시 이동 후 `.meta` GUID를 재확인하는 습관 필요(현재는 코드/툴로 자동화되어 있지 않음, 수동 체크에 의존)

**후속 조치**:
- [x] `DungeonClock`/`TickManager`/`ITurnActor`/`TilePathfinder` 네이밍·주석 보완
- [x] Y좌표 `.5` collider 문제 수정(`GetCellFootWorldPosition`)
- [x] 좌우 이동 스프라이트 반전 추가
- [x] "한 번 움직이면 멈춘다" 버그 두 원인 모두 수정
- [ ] `IsMonsterVisibleNow()` 실제 구현 — Enemy 시스템 붙을 때 진행
- [ ] Enemy `ITurnActor` 구현체로 Tick 파이프라인 end-to-end 실전 검증 (다음 세션 작업)

**결론**: 플레이어 이동 + Tick 파이프라인 연결 1차 리뷰 통과. 코드 구조/네이밍/주석 모두 보완 완료. 다음 단계는 경량 Enemy 구현으로 파이프라인이 Player 외의 실제 액터에서도 정상 동작하는지 검증.

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
