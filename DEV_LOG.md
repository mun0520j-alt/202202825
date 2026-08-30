# 개발 작업 로그 (Dev Log)

> 날짜별 작업 기록. `DEV_WORKFLOW_RULES.md` 규칙 2-1에 따라 작성. 코드 작업 세션마다 새 항목을 맨 위에 추가한다(최신이 위).

---

## 2026-08-28

**진행 상태**: 플레이어 이동(`PlayerTurnActor`) 구현 + Tick 파이프라인 실전 연결 완료, 코드 리뷰 1라운드 완료

- 디바이스 브릿지 파일 읽기(`device_stage_files`) HTTP 401 재발 — 원인은 데스크톱 앱 인증 토큰 만료였음(이전 세션의 "다른 대화가 폴더를 물고 있다" 추정은 틀렸던 것으로 확인). 사용자가 데스크톱 앱 로그아웃/재로그인으로 해결, 이후 읽기/쓰기 모두 디바이스 브릿지로 정상 동작
- `.gitignore` 버그 발견/수정 — 비주얼스튜디오 빌드 산출물용 `[Dd]ebug/` 규칙이 이름이 같은 실제 코드 폴더 `Assets/Scripts/Debug/`까지 같이 무시하고 있었음. `!/Assets/Scripts/Debug/` + `!/Assets/Scripts/Debug/**` 예외 규칙 추가로 해결(사용자가 로컬에서 직접 적용 후 커밋/푸시)
- **스크립트 폴더 재구성** — `Assets/Scripts/` 하위를 더 깊은 서브폴더 구조로 재배치(예: `Core/Tick/`, `Player/`, `Pathfinding/`, `Camera/`, `Debug/TickQueueTest/`). 디바이스 브릿지에 파일 삭제 기능이 없어서 "새 경로에 복사(메타데이터 보존) → 사용자가 기존 파일 수동 삭제" 방식으로 진행
  - **[발견] Unity GUID 재할당 버그**: 씬(`Test.unity`)이 참조하는 스크립트를 옮길 때, 새 경로에 파일을 먼저 복사해두면(기존 파일과 동일한 `.meta`가 일시적으로 공존) Unity가 리임포트 시 GUID 충돌로 판단해서 살아남은 쪽에 새 GUID를 재할당해버림 — 씬의 컴포넌트 참조가 깨짐(Missing Script). `TickQueueTestBootstrapper.cs`, `CameraFollow.cs` 두 파일에서 실제로 발생, 둘 다 `.meta` 파일을 원래 GUID로 수동 복원해서 해결. 앞으로 씬 참조 스크립트를 옮길 때마다 재확인해야 하는 리스크로 기록
- **플레이어 이동 설계 확정 후 구현** — `ITurnActor` 구현체로 `PlayerTurnActor.cs` 신규 작성, `Test.unity`의 임시 맵(`FloorLayoutGenerator`/`FloorTilemapPainter`)에 연결
  - 4방향(WASD/화살표) 이동 + 마우스 클릭 자동 경로 이동(녹픽던 스타일), 자동 이동 중 몹 발견 시 즉시 정지(현재는 스텁)
  - 이동 경로 탐색용 `TilePathfinder.cs`(BFS, 4방향 전용) 신규 작성 — Player 전용 로직을 몰라서 Enemy 이동에도 그대로 재사용 가능하도록 `Pathfinding/` 폴더에 독립 배치
  - `DungeonSceneBootstrapper.cs`로 TickManager/DungeonClock 초기화 + 스케줄 시작을 실제 던전 씬 진입점으로 확정
  - `CameraFollow.cs` — 카메라가 플레이어 위치로 스무딩 없이 즉시 스냅(이 장르 특성상 지연 없이 바로 따라가야 함)
  - 이동 연출: 사인 곡선 기반의 살짝 점프하는 홉 애니메이션(네크로댄서 참고), 기울임/찌그러짐 없음(게임 스타일에 안 맞아서 제외)
  - 좌우 이동 시 스프라이트 좌우 반전(flipX) 추가
- **버그: "한 번 움직이면 멈춘다"** — 두 개의 독립된 원인이 겹쳐 있었음
  1. 씬 상태 문제: 테스트용 `TickQueueTestBootstrapper`(오브젝트명 "TickSet")가 저장된 씬에서도 여전히 활성 상태였고, 반대로 실제 진입점인 `DungeonSceneBootstrapper`는 씬에 아예 없었음 — 사용자가 씬에서 직접 수정 후 재저장
  2. 실제 코드 버그: `PlayerTurnActor.HopTo()` 코루틴에서 `isMyTurn = false;`가 `TickManager.Instance.CompleteTurn(...)` 호출보다 "뒤에" 실행되고 있었음. 액터가 플레이어 혼자 등록된 상태에서는 `CompleteTurn()`이 동기적으로 바로 다음 턴(=플레이어 자신)을 시작시켜서 `isMyTurn`을 다시 true로 세팅하는데, 그 직후 코루틴의 남은 줄이 실행되며 `isMyTurn = false`로 새 턴 플래그를 덮어써버려서 입력이 영원히 무시되는 구조였음. `isMyTurn = false`를 `CompleteTurn()` 호출보다 먼저 실행하도록 순서 변경으로 해결
- **Y좌표 `.5` collider 이슈** — 배치되는 오브젝트의 월드 Y좌표가 `.5`로 끝나면 실제 Collider가 타일 경계와 어긋나는 문제가 있음(이 프로젝트의 일반 타일맵 규칙으로 기록: X만 `.5`, Y는 정수). `GetCellFootWorldPosition()` 메서드로 X만 셀 중앙(+0.5), Y는 셀 원점(정수, 바닥 경계) 값을 쓰도록 수정
- **코드 리뷰 1라운드 진행** — 사용자가 먼저 분석 → 이해/오해 확인 및 보완 → 네이밍/주석 개선 적용 형식으로 진행(자세한 내용은 `CODE_REVIEW_LOG.md` 참고). 대상: `TickCost.cs`, `ITurnActor.cs`, `DungeonClock.cs`, `TickManager.cs`, `DungeonSceneBootstrapper.cs`, `PlayerTurnActor.cs`, `TilePathfinder.cs`, `CameraFollow.cs`
- 다음 액션: 경량("약식") Enemy `ITurnActor` 구현체 설계 및 작성 — 실제 non-player 액터로 Tick 파이프라인이 end-to-end로 정상 동작하는지 검증

---

## 2026-08-27

**진행 상태**: Tick 시스템 코어 1차 구현 완료, Unity 컴파일/동작 검증 대기

- 코드 작업 대화(이 대화)와 로컬 git 저장소(`C:\Users\munju\OneDrive\문서\202202825`) 연결
- 디바이스 브릿지 파일 읽기(`device_stage_files`)가 HTTP 401로 지속 실패 — 원인은 다른 대화("노트북 기획 대화")가 같은 폴더를 동시에 물고 있었기 때문으로 추정 (`DOC_STORAGE_README.md` 참고)
- 우회책으로 GitHub 저장소(`https://github.com/mun0520j-alt/202202825`) 신규 연결 — 이후 읽기는 `git pull`, 쓰기는 디바이스 브릿지 직접 쓰기(정상 동작 확인됨)로 이원화
- 사용자가 `Assets/Scenes/Test.unity` 신규 생성 (빈 카메라만 있는 실험용 씬) — 임시 맵 제네레이터(`FloorLayoutGenerator`/`FloorTilemapPainter`) 실험 및 Tick 시스템 테스트용
- **개발 워크플로 규칙 수립** — `DEV_WORKFLOW_RULES.md` 신규 작성 (설계 우선 원칙, 네이밍/문서화 규칙, 작업보고서·코드리뷰 프로세스)
- **Tick 시스템 설계 문서 작성** — `TICK_SYSTEM_DESIGN.md`, 사용자 피드백 2라운드 거쳐 확정:
  - 몹도 플레이어와 동일한 "5타일 = 1tick" 기본 비율 사용, 단 완전 고정은 아니고 추후 플레이어(장비)/몹(종류별) 이동속도 배율 확장 가능하게 설계 — 단, 공격 tick 비용은 배율 영향 없이 항상 고정(밸런스 붕괴 방지)
  - 스케줄링(TickManager) → 시간 표시/MIA(DungeonClock) 단방향 이벤트 흐름으로 확정
  - `ITurnActor`에 `SuppressStuckTurnWarning` 추가 — 플레이어(입력 대기로 오래 걸림)와 AI(즉시 완료 기대)를 구분해서 "CompleteTurn 호출 누락 버그" 워치독이 플레이어 사고 시간에 오작동하지 않게 함
  - `TickManager`는 DungeonScene 전용 오브젝트로 확정, BaseCampScene에는 존재 안 함 (연성로 연동은 추후 별도 브릿지로 설계 예정)
  - Enemy 실제 구현은 이번 단계 범위에서 제외 — 플레이어 이동+Tick 검증 이후 착수 예정
- **코드 작성**: `Assets/Scripts/Core/` 아래 4개 파일 신규 작성
  - `TickCost.cs` — 행동별 tick 비용 상수
  - `ITurnActor.cs` — Player/Enemy 공통 턴 참여 인터페이스
  - `TickManager.cs` — 큐 기반 스케줄러 + 개발용 워치독(콜백 누락 경고)
  - `DungeonClock.cs` — 실시계 표시 + 288tick MIA 판정 + 월식의 제단용 표시 고정 확장 포인트(`LockDisplayToHour`, 미사용)
- **검증 하네스 작성**: `Assets/Scripts/Debug/` 아래 2개 파일 신규 작성
  - `TickQueueTestActor.cs` — 지정된 tick 비용을 소비하며 턴을 도는 더미 액터, 정지 조건 도달 시 의도적으로 `CompleteTurn` 생략(워치독 검증용)
  - `TickQueueTestBootstrapper.cs` — Test Scene에서 Fast(0.2)/Medium(1)/Slow(2) 더미 액터 3개 + `DungeonClock`을 세팅하고 스케줄을 시작
- **Unity 실행 검증 완료**: Test Scene에 `TickQueueTestBootstrapper` 붙여서 Play — Fast 액터가 압도적으로 자주 차례 받음(큐 정상), DungeonClock 실시계가 tick에 맞춰 정확히 갱신(06:01→06:49), 20턴째 의도적 `CompleteTurn` 생략 후 10초 뒤 워치독 버그 리포트(`LogError`) 정상 발동 — 전부 확인
- **코드 리뷰 1라운드**: `CODE_REVIEW_LOG.md`에 기록. `CompleteTurn → 다음 액터 호출`이 재귀 구조라 액터 연쇄가 길어지면 StackOverflow 위험 있음을 발견 → 즉시 수정(비용이 적을 때 고치는 게 낫다는 판단) → `isAdvancingSchedule` 가드 + `AdvanceSchedule()` 반복문으로 리팩터링 → 재실행해서 동일 결과(스택만 평탄화) 확인
- **Tick 코어 1차 리뷰 통과** — 다음 단계는 플레이어 이동을 Test Scene의 임시 맵 제네레이터(`FloorLayoutGenerator`/`FloorTilemapPainter`)에 연결하는 작업
- 다음 액션: 플레이어 이동(`ITurnActor` 구현체) 설계 → 맵 제네레이터로 그린 씬에서 실제 이동+tick 소비 검증
