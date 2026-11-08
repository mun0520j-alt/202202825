# 문서 지도

## 우선순위

1. `AGENTS.md` — 작업 규칙
2. `MILESTONES.md` — 현재 범위·완료 기준
3. `QA/TESTCASES_M1.md`~`M3.md` — 규칙과 결과
4. `QA/BUGS.md`, `TECHNICAL_DEBT.md` — 결함·부채
5. `CHANGELOG.md` — 변경 요약
6. 시스템·장기 기획 문서 — 설계 근거와 미래 방향

## 현재 작업 문서

| 문서 | 역할 |
|---|---|
| `QA/M3_TRACEABILITY.md` | 요구사항·Jira·TC·증거 연결 |
| `QA/M3_MEASUREMENT_REPORT_TEMPLATE.md` | 자동화·플레이 결과 양식 |
| `QA/M3_RELEASE_CHECKLIST.md` | M3 종료 절차 |
| `docs/STATUS_REPORT_2026-11-06.md` | Jira·진행 상태 |
| `docs/PORTFOLIO_ONE_PAGE_BRIEF.md` | 대외 설명용 요약 |

## 역사·장기 문서

`TICK_SYSTEM_DESIGN.md`, `MAP_GENERATOR_DESIGN.md`, `ALTAR_AND_TIME_SYSTEM_DESIGN.md`, `HOMEPC_SYNC_NOTES.md`, `PROJECT_PLAN.md`, `VERTICAL_SLICE_SCOPE.md`, `CODE_REVIEW_LOG.md`, `DEV_LOG.md`는 설계 변화를 보존한다. 여기에 있는 1Tick=5분, 이동 5타일, 공격 1Tick, MIA 288Tick은 현재 명세가 아니다.

## 데이터 원본

| 데이터 | 원본 |
|---|---|
| 현재 아이템 | `Assets/Resources/Data/items.csv` |
| 아이템 스키마 | `docs/ITEM_CSV_DATA_DICTIONARY.md` |
| 원정 측정 | 실행 후 생성되는 `expedition_runs_v2.csv` |

## 외부 동기화

- Jira는 DUN, Confluence는 DUN 스페이스만 사용한다.
- 외부 페이지에는 원본 경로와 커밋 해시를 적는다.
- 충돌 시 Git 리포가 원본이다.

## 열린 위험

- Kyrise 아이콘은 수정·사용하지 않았으며, 공개 빌드에서 미사용 원본 파일을 제외할지 확인한다.
- 없는 파일의 과거 참조는 역사 문맥인지 실제 누락인지 구분한다.
- M3 실행 후 결과 파일과 추적표를 함께 갱신한다.
