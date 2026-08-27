# 던전 크롤러 프로젝트 — 진행 문서 (Living Doc)

> 이 문서는 여러 PC/세션에서 이어서 작업하기 위한 살아있는 문서입니다.
> **git에 커밋해서 관리**하고, 매 작업 세션마다 "진행 상황" / "다음 할 일" 섹션을 갱신해주세요.
> Claude에게 새 세션에서 이 문서를 먼저 읽게 하면 바로 문맥을 따라잡을 수 있습니다.

---

## 0. 프로젝트 개요

- **장르**: 로그라이트 + 익스트랙션 + 던전 크롤러 하이브리드
- **시점/구조**: 2D 탑뷰, 턴제(Tick 기반)
- **개인작** — 아트팀 없음, 실제 게임 화면 퀄리티는 낮을 수밖에 없음을 감수
- **아이템 설계 원칙**: 장착해도 스탯만 변화, 스프라이트 변형 없음 (통일성 확보 목적)
- **애니메이션 범위**: 기본적으로 idle / run 2종 상태만 (공격 프레임 없는 애셋 특성 반영, "멈추고 번쩍" 연출로 대체 예정)
- **저장소**: `C:\Users\user\Documents\ForGit\202202825` (홈PC, git 연결, `main` 브랜치)
  - 참고: 과거 노트북에서는 `C:\Users\munju\OneDrive\문서\202202825` 경로로 작업한 이력 있음 (OneDrive 동기화 이슈로 혼선 있었음, 현재는 git 중심으로 통일)

## 1. 작업 규칙 (반드시 준수)

1. **사용자 명령 없이 코드 작성이나 씬 하이라키를 건드리지 않는다.**
2. **모든 코드는 반드시 검증(사용자 직접 테스트)을 거치며, 단일 책임 원칙에 따라 파일을 분리한다.** (이전 "Raptors" 프로젝트의 기술 부채를 반면교사로 삼음 — 절대 모놀리식 금지)
3. 아이템/데이터베이스 등 신규 단계는 사용자가 먼저 검토 후 명시적으로 명령을 줄 때 진행한다.

## 2. 애셋 구성

- **0x72 DungeonTileset II v1.7** — 메인 타일/캐릭터 스프라이트 (`Assets/Art/DungeonTiles_0x72/`)
  - `frames/` 폴더에 370개 개별 프레임 PNG가 이미 사전 크롭되어 있음 (직접 슬라이싱 불필요, 이름 기반 그룹핑만 하면 됨)
- **Dungeon Tileset II Extended v1.0 / v1.1** — 확장 타일셋
- **Kyrise's 16x16 RPG Icon Pack V1.3** — 아이템 아이콘 (`Assets/Art/ItemIcons_Kyrise/`)
  - 선별 완료 후 `Assets/Art/ItemIcons_Kyrise/Selected/{Category}/` 로 이동
- **텍스처 임포트 규격**: 16x16 픽셀 기준, Point(no filter), 압축 없음

### Kyrise 아이콘 선별 결정사항 (완료)
| 카테고리 | 선별 기준 |
|---|---|
| 갑옷(Armor) | 전부 사용 |
| 검(Sword) | 3종 시리즈 전부 (날 모양이 달라 구분 가치 있음) |
| 지팡이(Staff) | 02 시리즈만 |
| 스펠북(Spellbook) | 보류 |
| 방패(Shield) | 02 시리즈만 |
| 반지(Ring) | 전부 (종류/스탯만 다르게 활용) |
| 포션(Potion) | 일부 사용 예정 — **색상/개수 미정, 사용자 결정 대기** |
| 활/화살(Bow/Arrow) | 보류 또는 01 시리즈에서 3개만 추릴지 **미정, 사용자 결정 대기** |
| 잡템류 (fish, candy, coin, cup, gift 등) | 보류 (cup_02b는 향후 퀘스트 아이템 후보로 메모됨) |

## 3. Unity 포팅 로드맵

### Step 1 — 타일 프레임을 오브젝트별로 정리 ✅ 완료
- 도구: `Assets/Editor/DungeonTools/TilesetFrameSlicer.cs`
- 메뉴: `Dungeon Tools/1b) Organize 0x72 Frames Into Sprite Sets`
- 방식: `frames/` 폴더의 사전 크롭된 스프라이트를 이름 패턴(`{base}_{state}_f{n}`)으로 파싱해 `SpriteFrameSet` ScriptableObject로 그룹핑
- 결과: 370개 파일 → 142개 `SpriteFrameSet` 에셋 (`Assets/Generated/SpriteSets/`)
- 선행 도구: `PixelArtImportFixer.cs` (`Dungeon Tools/1a`) — 임포트 세팅 일괄 정규화, 1432/1433 텍스처 적용 완료

### Step 2 — Anim State + 프리팹 정리 ✅ 완료 (사용자 검증 대기 확인됨)
- 도구 4종 (역할별 분리):
  - `AnimationClipBuilder.cs` — SpriteFrameSet → AnimationClip 생성
  - `AnimatorControllerBuilder.cs` — idle/run 상태를 가진 AnimatorController 생성
  - `PrefabBuilder.cs` — 프리팹 생성/저장
  - `BuildAnimStatesAndPrefabs.cs` — 위 3개를 오케스트레이션하는 진입점
- 메뉴: `Dungeon Tools/2) Build Anim States + Prefabs`

### Step 2.5 — 디버그 프리팹 프리뷰 씬 (사이드 퀘스트) ✅ 완료
- 도구: `Assets/Editor/DungeonTools/BuildDebugPrefabPreviewScene.cs`
- 메뉴: `Dungeon Tools/3) Build Debug Prefab Preview Scene`
- 기능: 모든 프리팹을 idle 그리드로 배치 + run 애니메이션 있는 것만 별도로 idle 마지막 행 기준 -5 아래에 run 그리드로 배치 (겹침 없이)
- 런타임 헬퍼: `Assets/Scripts/Debug/DebugAnimatorStatePlayer.cs` (Animator에 지정한 state를 Start 시 재생)
  - **최근 이슈**: 이 파일이 노트북↔홈PC 간 git 커밋 누락으로 CS0246 에러 발생 → 홈PC에 직접 재생성 후 push로 해결됨 (2026-08-24 기준 정상)

### Step 3 — 아이템 데이터베이스 🟢 설계 완료 (열쇠/5번째 스크롤까지 확정), Unity 반영은 대기
- 목표: 아이템 데이터를 database 형태로 정리, 각 아이템 data값을 약식으로 정리
- **설계 논의 문서 `ITEM_SYSTEM_DESIGN.md` + 약식 데이터표 `ITEM_DATA_DRAFT.xlsx` 완성** (노트북 대화에서 진행)
  - 슬롯 구조, 무기(검/지팡이/활)+방패, 반지(5스탯 종류: 힘/체력/속도/방어력/치명타), 목걸이(5종 고유능력 + 휴대 1개 제한), 포션 8종, **스크롤 5종**(4종+"다음 층 직행" 신규 추가, `scroll_01e`), **열쇠(Key) 신규 카테고리**(문열쇠 `key_01a~e`/상자열쇠 `key_02a~e`, 타르코프식 다회용 충전형) 전부 확정
  - 스탯 수치는 v1 초안(실플레이 조정 예정), 카테고리↔슬롯 매핑 확정
- **아직 Unity 코드/ScriptableObject로는 안 옮김** — 사용자 명령 대기 (코드 작성 전 확인 필수, 표준 규칙 준수)
- 남은 미정: 무기 스킬 시스템(별도 엑셀로 분리 예정), 열쇠 충전횟수/자물쇠 tick비용 등 정확한 수치(실플레이 튜닝)

### Step 4 — 맵 제너레이터 🟢 설계 완료
- 목표: 규격에 맞는 맵을 절차적으로 생성
- **`MAP_GENERATOR_DESIGN.md` 완성** — 생성 알고리즘 BSP 확정(CA는 특수층 실험용 메모), 1~5층/6층+ 이원화 규격(6층+부터 v4 24x24 풀사이즈), 상자방(=열쇠방) 단칸 dead-end + 잠금 6:4, 보스방 축소, Rule Tile 채택(스펙은 실제 제작 시 논의)
- 정확한 수치(방 개수 세부, tick 비용 등)는 실플레이 튜닝 대상

### Step 5 — 시간대 & 제단(Altar) 시스템 🟢 설계 완료 (메인 시스템)
- **`ALTAR_AND_TIME_SYSTEM_DESIGN.md` 완성** — 로그라이크 승천(Ascension) 시스템 차용, 게임의 메인 시스템으로 확정
- 자정(00:00) 문턱값 기준 시간대 시스템(몹강화×1.3, 심야전용몹 chort/imp/big_demon, 플레이어 시야축소↔보상증가 원칙), 5층 보스(오크 방향, 단일+광역 2패턴)→제단지기 NPC(던전 내에서만 등장, 영지에서 못 만남)→단일 Raid 완결형 퀘스트→월식의 제단(영지에 물리적 생성) 흐름 확정
- 남은 미정: 정확한 수치, 심야 이벤트 세부 내용, 2/3번째 제단 컨셉(3번째는 완전 보류)

### Step 6 — 영지(거점) & 상인 시스템 🟢 설계 완료 (신규)
- **`SETTLEMENT_AND_MERCHANT_DESIGN.md` 신규 작성** — 거점형(다키스트 던전 Hamlet류), 상인은 카테고리별 전문 다수 + 태그 기반 가격시스템(판매가=등급×재료 기본값×상점태그매칭배율), 평판은 상인별 개별 관리, 화폐="골드", 영지 업그레이드로 상인퀘스트 확대(+보관함 확장 퀘스트) 및 은행/스탯 건물, 상인은 영지 전용(던전 조우 없음)
- **연성로(鍊成爐) 신규 시설 추가** — 타르코프 Cultist Circle 오마주, 잉여 아이템을 제물로 등록해 던전에서 실제 경과한 tick만큼 진행도 누적 → 보상 변환. 여러 원정에 걸쳐 누적 가능. `ALTAR_AND_TIME_SYSTEM_DESIGN.md`의 "제단"(출격 전 토글형 규칙조작)과는 이름/기능 모두 별개 시스템 — 헷갈리지 않도록 명확히 분리
- 남은 미정: 건물 목록/수치, 은행 손실방지 정확한 규칙, 상인 태그/배율 수치, 연성로 제물↔보상 공식(의도적으로 미확정 유지) 및 해금 퀘스트 내용

## 3.5. 홈PC 문서와의 충돌 — 해결됨 + 향후 워크플로

`HOMEPC_SYNC_NOTES.md` 참고. 처음엔 Tick/이동 비용 모델이 두 대화에서 다르게 보였으나(홈PC=배율기반 vs 노트북=고정1tick통일) **해결됨**: 홈PC 문서의 "배율"은 실제 tick 비용이 아니라 **카메라 배율(줌/스케일) + 5타일=1tick 단위 표현** 얘기였던 것으로 확인. 게임 로직상 tick 비용은 노트북에서 확정한 고정 모델(모든 기본행동=1tick)로 통일. 다만 홈PC 문서 1.3절의 실제 문구(Freerunner/Ring of Haste 예시 등)가 오해 소지가 있어 그쪽에서 표현 정리를 권장.

**앞으로 프로젝트는 총 3개 대화로 분리 진행**:
1. **이 노트북 대화** — 순수 기획/설계 전용, 코드 없음 (지금까지처럼 유지)
2. **홈PC 기획/아키텍처 대화** — `DUNGEON_RUNTIME_ARCHITECTURE.md` 등 런타임 구조 설계
3. **개발(코드 작성) 대화** — 실제 구현 착수

## 4. 현재 만들어진 스크립트 인벤토리

| 경로 | 역할 | 상태 |
|---|---|---|
| `Assets/Scripts/Data/SpriteFrameSet.cs` | 런타임 ScriptableObject 데이터 컨트랙트 | ✅ |
| `Assets/Editor/DungeonTools/PixelArtImportFixer.cs` | 텍스처 임포트 세팅 정규화 (1a) | ✅ |
| `Assets/Editor/DungeonTools/TilesetFrameSlicer.cs` | 프레임→오브젝트 그룹핑 (1b) | ✅ |
| `Assets/Editor/DungeonTools/AnimationClipBuilder.cs` | AnimationClip 생성 | ✅ |
| `Assets/Editor/DungeonTools/AnimatorControllerBuilder.cs` | AnimatorController 생성 | ✅ |
| `Assets/Editor/DungeonTools/PrefabBuilder.cs` | 프리팹 생성 | ✅ |
| `Assets/Editor/DungeonTools/BuildAnimStatesAndPrefabs.cs` | Step2 오케스트레이터 (2) | ✅ |
| `Assets/Scripts/Debug/DebugAnimatorStatePlayer.cs` | 디버그용 애니메이터 state 강제 재생 | ✅ |
| `Assets/Editor/DungeonTools/BuildDebugPrefabPreviewScene.cs` | 디버그 프리뷰 씬 빌더 (3) | ✅ |
| `Assets/Editor/DungeonTools/OrganizeKyriseIcons.cs` | Kyrise 아이콘 카테고리별 정리 (3a) | ✅ 배포됨, 실행 확인 대기 |

## 5. 다음 할 일 (Next Actions) — 매 세션마다 갱신

**설계 단계(Step 3~6)는 전부 구조적으로 완료됨. 프로젝트는 이제 노트북(기획)/홈PC 기획/개발, 총 3개 대화로 나뉘어 진행. 이 대화는 계속 기획 전용으로 유지.**

- [x] 홈PC 대화와의 Tick/이동 비용 모델 충돌 해소 (3.5절 참고) — "배율"은 카메라 표현 얘기였던 것으로 정리됨
- [ ] 개발 착수는 별도 대화에서 진행 (이 노트북 대화에서는 코드 작성 안 함)
- [ ] `OrganizeKyriseIcons.cs` 실행 결과 확인 (포션/목걸이/열쇠/5번째 스크롤 반영해서 스크립트 업데이트 필요할 수 있음)
- [x] 아이템 시스템 설계 완료 (슬롯/무기/방패/반지/목걸이/포션/스크롤 5종/열쇠) — `ITEM_SYSTEM_DESIGN.md` + `ITEM_DATA_DRAFT.xlsx`
- [ ] `ITEM_DATA_DRAFT.xlsx`의 v1 스탯 초안 실플레이 조정 (실제 플레이하면서 잡기로 함)
- [x] Tick 시스템 확정 → 1 Tick = 5분 = 이동 5타일, 288 Tick = 24시간(MIA 상한), 모든 기본행동 1tick 통일
- [x] 맵 제너레이터 설계 완료 (Step 4) — BSP, 층별 규격 이원화, 상자방/보스방 태깅, 열쇠 잠금 메커니즘
- [x] 시간대/제단 시스템 설계 완료 (Step 5) — `ALTAR_AND_TIME_SYSTEM_DESIGN.md`
- [x] 영지/상인 시스템 설계 완료 (Step 6, 신규) — `SETTLEMENT_AND_MERCHANT_DESIGN.md`
- [ ] 승인 시 Unity ScriptableObject/데이터 파일로 옮기는 코드 작업 착수 — **사용자 명령 대기, 코드 작성 전 반드시 확인**
- [x] 홈PC 문서 대조 완료 → `HOMEPC_SYNC_NOTES.md` (충돌 발견, 3.5절 참고— 홈PC 대화에 전달 필요)

## 6. 개발 착수 순서 (Dev Sequence — 선 넘기 방지용)

> **`VERTICAL_SLICE_SCOPE.md` 참고**: 아래 0~5단계를 "Battle MVP" 버티컬 슬라이스(약 3개월)로 구체화한 범위 문서. 무기+방패+상의만 포함(반지/포션/목걸이/스크롤/열쇠는 이번 슬라이스 제외), 몹 2~3종(스탯만 차등, 랜덤 index 소환), 잠금 없는 일반 상자 루팅, 층 규격은 단일 통일(1~5층/6층+ 이원화는 나중).

> 목적: 설계는 다 끝났지만, 실제 코드는 "어느 시스템이 어느 시스템에 의존하는지" 순서를 지키지 않으면 바로 꼬임(과거 Raptors 기술부채의 원인 중 하나). 아래 순서는 **의존관계 기준**으로 정렬됨 — 각 단계는 이전 단계가 사용자 직접 검증을 통과해야 다음 단계로 넘어감(작업 규칙 2번 그대로 적용).

### 0단계 — 남은 엔진 설계 질문 ✅ 전부 결론남 (코드 작성 전 필수 항목, 완료)
`HOMEPC_SYNC_NOTES.md` 2절 참고. 전부 확정:
1. Tick 이벤트 — **큐 기반 스케줄러**(다음 차례 엔티티에게만 직접 콜백)
2. 이동 tick 비용 — **누적형**(타일당 0.2tick 누적, 정수 도달 시 시간 진행)
3. 이동 입력 — **클릭+경로탐색 자동이동**
4. 공격 트리거 — **적 칸 클릭 시 자동 공격 전환**

이 4개는 홈PC 대화의 미정 질문 B/H/I를 해결하는 결론이라, **홈PC 대화에도 전달해서 그쪽 문서를 갱신해야 함**.

### 1단계 — Tick/이동 엔진 코어
- TickManager(이벤트 구독 모델) + 그리드 이동. 아이템/맵 데이터 없이도 테스트 가능한 순수 엔진
- 이 시점 몹/플레이어는 placeholder 스탯으로 충분

### 2단계 — 전투 계산 기초
- 공격=1tick 소모, 데미지 계산 placeholder 공식(정밀 스탯은 3단계 이후 연동)

### 3단계 — 아이템 데이터 (ScriptableObject)
- `ITEM_SYSTEM_DESIGN.md` 구조를 코드로 — 슬롯/장비/포션/스크롤/열쇠
- 이 단계부터 2단계의 placeholder 공식에 실제 스탯이 꽂힘

### 4단계 — 맵 제너레이터 (BSP)
- `MAP_GENERATOR_DESIGN.md` 구조 구현 — 방+복도 생성, 층별 규격 이원화
- 방 태깅(상자방/보스방/열쇠 잠금)은 3단계 아이템(열쇠)이 있어야 완성되므로 이 단계 후반부

### 5단계 — 최소 플레이 가능 루프 (첫 마일스톤) ⭐
- 1~4단계를 합쳐서 "이동 → 전투 → 루팅 → tick 소모 → 탈출/MIA"가 실제로 도는 버전
- **이 마일스톤 통과 전까지는 6/7단계(시간대·제단, 영지·상인) 착수 안 함** — 선 넘기 방지 핵심 지점

### 6단계 — 시간대 & 제단 시스템
- `ALTAR_AND_TIME_SYSTEM_DESIGN.md` — 자정 트리거, 5층 보스, 제단지기 NPC, 단일-raid 퀘스트, 제단 해금
- 5단계의 안정적인 던전 루프 + 실시계가 전제

### 7단계 — 영지 & 상인 시스템
- `SETTLEMENT_AND_MERCHANT_DESIGN.md` — 거점, 상인 태그/평판, 은행, 영지 업그레이드
- 탈출 성공/실패에 따른 인벤토리·골드 반영이 전제라 가장 마지막

### 진행 원칙
- 각 단계는 **사용자가 직접 테스트해서 확인 후** 다음 단계로 — 코드 작성 전 매번 명시적 명령 대기
- 모든 코드는 단일 책임 원칙으로 파일 분리 (작업 규칙 2번)
- 이 순서는 노트북 대화가 만든 "참고 로드맵" — 실제 코드는 홈PC/개발 대화에서 진행되므로, 그쪽에도 이 섹션을 전달해서 같은 순서를 따르는 게 좋음

## 7. 변경 이력 (Changelog)

- **2026-08-24**: 노트북→홈PC 전환 중 `DebugAnimatorStatePlayer.cs` git 미커밋으로 인한 CS0246 에러 발생 및 해결. 이 진행 문서(PROJECT_PLAN.md) 최초 작성.
- **2026-08-25**: 노트북 대화에서 아이템 시스템 설계 논의 진행 — 슬롯/무기/방패/반지/목걸이/포션/스크롤 구조 전부 확정. `ITEM_SYSTEM_DESIGN.md`(설계 근거) + `ITEM_DATA_DRAFT.xlsx`(약식 데이터, v1 초안) 작성. 홈PC 대화 쪽에서는 별도로 `DUNGEON_RUNTIME_ARCHITECTURE.md`(Tick/이동/카메라 설계 논의) 진행 중.
- **2026-08-25 (계속)**: 같은 노트북 대화에서 맵 제너레이터 설계(`MAP_GENERATOR_DESIGN.md`) + 시간대/제단 시스템 기획(`ALTAR_AND_TIME_SYSTEM_DESIGN.md`) 착수. 핵심 확정: tick 시스템 실제 수치(1tick=5분=5타일, 288tick=24h MIA), 게임 내 모든 기본 행동(이동/공격/스왑/아이템/상호작용)이 1tick으로 통일, 10층 완주는 비정석(중간 탈출이 기본값), 5층 보스 이후 "월식의 제단" 첫 제단 해금 퀘스트 흐름 설계.
- **2026-08-26/27**: 맵 제너레이터 설계 완전 마무리(BSP 확정, 층별 규격 이원화, 상자방/보스방 태깅, 열쇠 잠금 시스템 — 녹픽던식 소모품에서 타르코프식 다회용 충전형으로 전환). 시간대/제단 시스템에 구체 수치 채움(자정 문턱값, 몹×1.3, 심야전용몹 3종, 5층 보스 오크 방향+2패턴, 제단지기 NPC 단일-raid 규칙). 아이템에 열쇠 카테고리 + 5번째 스크롤("다음 층 직행") 신규 확정. 영지+상인 시스템(`SETTLEMENT_AND_MERCHANT_DESIGN.md`) 완전 신규 설계 — 거점형+태그기반 시장/평판. 홈PC 문서 대조(`HOMEPC_SYNC_NOTES.md`) 진행, Tick/이동 비용 모델 충돌 발견 후 해소(홈PC "배율"은 카메라 표현 얘기였던 것으로 정리). 프로젝트를 노트북(기획)/홈PC(기획)/개발, 총 3개 대화로 분리하기로 확정. 설계 단계(Step 3~6) 구조적으로 전부 완료. **개발 착수 순서(Dev Sequence, 0~7단계)를 의존관계 기준으로 정리**해서 "선 넘기 방지" 가이드 마련 — 0단계(남은 엔진 질문 4개) → 1단계(Tick/이동 코어) → 2단계(전투 기초) → 3단계(아이템 데이터) → 4단계(맵 제너레이터) → 5단계(최소 플레이 루프, 첫 마일스톤) → 6단계(시간대/제단) → 7단계(영지/상인).
