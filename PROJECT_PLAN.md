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

### Step 3 — 아이템 데이터베이스 🟡 설계/약식 데이터 완료, Unity 반영은 대기
- 목표: 아이템 데이터를 database 형태로 정리, 각 아이템 data값을 약식으로 정리
- **설계 논의 문서 `ITEM_SYSTEM_DESIGN.md` + 약식 데이터표 `ITEM_DATA_DRAFT.xlsx` 완성** (노트북 대화에서 진행)
  - 슬롯 구조, 무기(검/지팡이/활)+방패, 반지(5스탯 종류: 힘/체력/속도/방어력/치명타), 목걸이(5종 고유능력 + 휴대 1개 제한), 포션 8종, 스크롤 4종 전부 확정
  - 스탯 수치는 v1 초안(실플레이 조정 예정), 카테고리↔슬롯 매핑 확정
- **아직 Unity 코드/ScriptableObject로는 안 옮김** — 사용자 명령 대기 (코드 작성 전 확인 필수, 표준 규칙 준수)
- 남은 미정: 시작 tick 상향폭(아키텍처 문서와 연동), 무기 스킬 시스템(별도 엑셀로 분리 예정)

### Step 4 — 맵 제너레이터 ⏳ 대기 중
- 목표: 규격에 맞는 맵을 절차적으로 생성
- 방침: 직접 제너레이터가 어려우면, 먼저 큰 틀(프레임워크)을 만들고 그 안에서 제너레이터를 돌리는 형태로 우회
- Step 3 완료 후 착수 예정

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

- [ ] `OrganizeKyriseIcons.cs` 실행 결과 확인 (포션/목걸이 반영해서 스크립트 업데이트 필요할 수 있음)
- [x] 포션 선별 기준 확정 → `potion_01a~h` 8종, 효과 확정
- [x] 활/화살 처리 방침 확정 → `bow_01a/b/cc` 3종만, 화살 미사용
- [x] 목걸이 선별 기준 확정 → `necklace_01a~e` 5종, 반지와 다른 고유능력 컨셉
- [x] `ITEM_SYSTEM_DESIGN.md` + `ITEM_DATA_DRAFT.xlsx` 로 Step 3 설계/약식 데이터 완료
- [ ] `ITEM_DATA_DRAFT.xlsx`의 v1 스탯 초안 실플레이 조정
- [x] 시작 tick 값 확정 → **1 Tick = 5분 = 이동 5타일, 288 Tick = 24시간(MIA 상한)**. 이동/공격/스왑/아이템사용/상호작용 전부 기본 1 tick으로 통일
- [x] Step 4 (맵 제너레이터) 설계 착수 → `MAP_GENERATOR_DESIGN.md` — BSP 방+복도 생성, 24x24 그리드 v4 제안, 0x72 타일 매핑 정리
- [x] 시간대 시스템 + 5층 보스/NPC/제단 기획 착수 → `ALTAR_AND_TIME_SYSTEM_DESIGN.md` — 아침/낮/저녁/심야 4단계, "월식의 제단" 첫 제단 해금 퀘스트 흐름 설계
- [ ] 승인 시 Unity ScriptableObject/데이터 파일로 옮기는 코드 작업 착수 명령 대기 (아이템/맵제너레이터 공통)
- [ ] 홈PC 쪽 `DUNGEON_RUNTIME_ARCHITECTURE.md`(층 배치/씬 전환)와 `MAP_GENERATOR_DESIGN.md`/`ALTAR_AND_TIME_SYSTEM_DESIGN.md` 내용 서로 맞춰보기 — 두 대화가 따로 진행 중이라 동기화 필요

## 6. 변경 이력 (Changelog)

- **2026-08-24**: 노트북→홈PC 전환 중 `DebugAnimatorStatePlayer.cs` git 미커밋으로 인한 CS0246 에러 발생 및 해결. 이 진행 문서(PROJECT_PLAN.md) 최초 작성.
- **2026-08-25**: 노트북 대화에서 아이템 시스템 설계 논의 진행 — 슬롯/무기/방패/반지/목걸이/포션/스크롤 구조 전부 확정. `ITEM_SYSTEM_DESIGN.md`(설계 근거) + `ITEM_DATA_DRAFT.xlsx`(약식 데이터, v1 초안) 작성. 홈PC 대화 쪽에서는 별도로 `DUNGEON_RUNTIME_ARCHITECTURE.md`(Tick/이동/카메라 설계 논의) 진행 중.
- **2026-08-25 (계속)**: 같은 노트북 대화에서 맵 제너레이터 설계(`MAP_GENERATOR_DESIGN.md`) + 시간대/제단 시스템 기획(`ALTAR_AND_TIME_SYSTEM_DESIGN.md`) 착수. 핵심 확정: tick 시스템 실제 수치(1tick=5분=5타일, 288tick=24h MIA), 게임 내 모든 기본 행동(이동/공격/스왑/아이템/상호작용)이 1tick으로 통일, 10층 완주는 비정석(중간 탈출이 기본값), 5층 보스 이후 "월식의 제단" 첫 제단 해금 퀘스트 흐름 설계.
