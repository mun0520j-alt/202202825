# 개발 작업 로그 (Dev Log)

> 날짜별 작업 기록. `DEV_WORKFLOW_RULES.md` 규칙 2-1에 따라 작성. 코드 작업 세션마다 새 항목을 맨 위에 추가한다(최신이 위).

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
