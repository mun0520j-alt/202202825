# Tick 시스템 설계 문서

> 상태: **Draft — 사용자 피드백 대기 중, 코드 작성 전 단계** (`DEV_WORKFLOW_RULES.md` 규칙 1 적용)
> 목표: `DUNGEON_RUNTIME_ARCHITECTURE.md` 1장에서 논의됐던 Tick/스케줄러 개념과 `MAP_GENERATOR_DESIGN.md` 2장에서 확정된 수치를 실제 클래스 구조로 옮긴다. 코드는 아직 없음 — 이 문서에 대한 피드백을 받은 뒤 착수.

---

## 0. 배경

이전에 한 번 `Tick.cs`/`GridMover.cs` 등을 구조 합의 없이 완성 코드로 만들었다가 "이해 못 하는데 무슨 의미냐"는 피드백으로 `_to_delete/`로 이동된 이력이 있음(`DUNGEON_RUNTIME_ARCHITECTURE.md` 0장 참고). 이번엔 순서를 지켜서 설계 → 피드백 → 코드로 진행.

## 1. 스코프

- **DungeonScene 전용** — BaseCampScene은 Tick 없음(실시간). `DUNGEON_RUNTIME_ARCHITECTURE.md` 1.1절과 동일한 전제.
- 이번 단계에서는 Player 이동만 실제로 테스트하고, Enemy는 인터페이스 설계까지만(구현체는 나중 단계) — `VERTICAL_SLICE_SCOPE.md`의 단계적 검증 원칙을 따름.

## 2. 핵심 규칙 재정리 (이미 확정된 것, 코드 설계 관점에서 요약)

| 규칙 | 값 | 근거 문서 |
|---|---|---|
| 1 Tick | 게임 내 5분 | `MAP_GENERATOR_DESIGN.md` 2장 |
| 이동 비용 | 타일당 0.2 tick (5타일=1tick), 누적형 | `MAP_GENERATOR_DESIGN.md` 2장, `HOMEPC_SYNC_NOTES.md` 2절 |
| 공격/스왑/아이템사용/상호작용 | 전부 고정 1 tick | `MAP_GENERATOR_DESIGN.md` 2장 |
| 몹 이동 속도 | 플레이어와 동일(5타일=1tick), 배율 없음 | 이번 대화에서 신규 확정 |
| 던전 체류 상한 | 288 tick = 24시간, 초과 시 MIA | `MAP_GENERATOR_DESIGN.md` 2장 |
| 시간 진행 조건 | 행동해야만 진행, 메뉴/고민은 무료 | `MAP_GENERATOR_DESIGN.md` 2장 |
| 이벤트 모델 | 큐 기반 스케줄러, 다음 차례에게만 콜백(전체 브로드캐스트 아님) | `HOMEPC_SYNC_NOTES.md` 2절 |

## 3. 클래스 구조 (파일 4개, 단일 책임 원칙)

### 3-1. `TickCost.cs` — 행동별 tick 비용 상수
행동 하나가 얼마나 걸리는지 "숫자"만 들고 있는 정적 클래스. 다른 클래스가 이 상수를 참조만 하고, 여기엔 로직이 없음.

```
PerTileMove = 0.2f   // 5타일 = 1tick
Attack = 1f
SwapWeaponInCombat = 1f
UseItem = 1f          // 포션/스크롤
Interact = 1f         // 상자/문 개방
```

### 3-2. `ITickActor.cs` — Player/Enemy 공통 인터페이스
"내 차례가 되면 뭘 한다"만 정의. 실제로 뭘 할지(입력 대기 vs AI 판단)는 구현체 책임.

```
OnTurnStart()  — 이 액터의 차례가 됐을 때 TickManager가 호출
```
액터는 행동을 정하고 나면 반드시 `TickManager.CompleteTurn(this, cost)`를 호출해서 차례를 넘겨야 함 — 안 부르면 스케줄러가 그 자리에서 멈춤(이 제약은 주석으로 명시할 예정).

### 3-3. `TickManager.cs` — 큐 기반 스케줄러
"누가 다음 차례인가"만 관리. 액터별 "다음 행동 시각"(float)을 들고 있다가, 가장 이른 액터 한 명에게만 `OnTurnStart()`를 콜백. 전체 브로드캐스트 안 함(확정 사항 반영).

주요 메서드(가칭, 이름은 규칙 1-1 적용해서 직관적으로):
```
Register(ITickActor actor)         — 액터를 스케줄에 등록
Unregister(ITickActor actor)       — 등록 해제(액터 사망 등)
BeginSchedule()                    — 등록 끝난 뒤 스케줄 시작
CompleteTurn(ITickActor actor, float cost)  — 현재 액터가 행동 완료 보고, 다음 차례로 넘어감
```
`OnTimeAdvanced` 이벤트 발행(소비된 cost 값과 함께) — DungeonClock이 구독.

### 3-4. `DungeonClock.cs` — 실시계 표시 + MIA 판정
TickManager의 스케줄링 로직과는 분리(단일 책임) — "누적 tick을 실제 시:분으로 바꾸고, 288 넘으면 MIA 알림"만 담당.

```
ElapsedTicks          — 누적 소비 tick
IsMIA                 — 288 tick 초과 여부
GetClockString()      — "06:35" 같은 실시계 문자열
OnMIA 이벤트           — MIA 발생 시 발행
```

## 4. 동작 흐름 (한 턴이 어떻게 굴러가는지)

```
1. DungeonScene 진입 → Player/Enemy들이 TickManager.Register() 호출
2. 모든 등록 끝나면 TickManager.BeginSchedule() 호출
   → 가장 이른 액터(처음엔 전부 0이라 등록 순서상 첫 액터) 골라서 OnTurnStart() 콜백
3-A. Player 차례면: 입력(클릭 등) 기다림 → 이동/공격 실행 → TickManager.CompleteTurn(this, cost) 호출
3-B. Enemy 차례면: OnTurnStart() 안에서 즉시 AI 판단 → 행동 실행 → CompleteTurn(this, cost) 호출
4. CompleteTurn 안에서: 그 액터의 "다음 행동 시각" += cost, OnTimeAdvanced 이벤트 발행
   → DungeonClock이 구독해서 ElapsedTicks 갱신, 288 넘으면 OnMIA 발행
5. TickManager가 다시 스케줄 전체를 훑어 가장 이른 액터를 찾아 OnTurnStart() 호출 → 2번부터 반복
```

## 5. 확정 사항 vs 확인 필요한 질문

**확정(이번 대화에서)**:
- 몹도 플레이어와 동일한 이동 비율, 속도 배율 없음
- 큐 기반 스케줄러, 파일 4개 분리(TickCost/ITickActor/TickManager/DungeonClock)

**확인 필요**:
1. **DungeonClock의 시작 시각** — 기본값 06:00으로 가정했는데(문서상 "출격 시각 06:00 예시"), 이게 실제 고정값인지 나중에 가변으로 둘지
2. **TickManager를 씬 전환마다 새로 만들지, 상시 존재시킬지** — `DUNGEON_RUNTIME_ARCHITECTURE.md` 미정 질문 A. 지금 설계는 "DungeonScene 진입 시 Register→BeginSchedule" 흐름이라 둘 다 가능하긴 한데, Player가 GameObject로 씬에 상주하는 방식이라면 TickManager도 DungeonScene 전용 오브젝트로 두는 쪽이 자연스러워 보임 — 이렇게 가도 될지 확인
3. **Enemy 실제 구현은 이번 단계에 포함할지** — 인터페이스만 정의하고 실제 Enemy AI(OnTurnStart 안에서 뭘 할지)는 다음 단계로 미룰지, 이번에 최소 더미(예: "제자리에서 대기"만 하는 스텁)까지 포함할지
4. **CompleteTurn을 안 부르면 스케줄러가 멈추는 구조**가 맞는지 — 안전장치(예: 일정 시간 이상 응답 없으면 경고 로그) 필요한지, 아니면 지금 프로토타입 단계에서는 그냥 냅두는 게 나은지

## 6. 다음 액션

1. 위 4개 항목 피드백/답변
2. 합의되면 `TickCost.cs` → `ITickActor.cs` → `TickManager.cs` → `DungeonClock.cs` 순서로 파일 하나씩 작성 + 검증
3. Test Scene에 최소 동작 확인용 스텁(임시 GameObject 하나가 스스로 OnTurnStart 받아서 로그만 찍는 정도) 붙여서 큐가 실제로 도는지 1차 검증
