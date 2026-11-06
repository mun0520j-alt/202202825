# M3 QA 자동화 사용법

## 목적

M3 측정 자동화는 게임 규칙을 대신 판단하지 않는다. 반복 가능한 생성·분포·회귀 검사를 코드로 수행하고, 플레이 CSV를 요약해 기획·QA 판단 근거를 만든다.

## 실행

Unity 상단 메뉴에서 `Dungeon Tools > QA > M3 Automation`을 연다.

- **전체 자동화 실행**: 다중 Seed, 드롭 분포, 핵심 회귀, 원정 CSV 분석을 순서대로 실행한다.
- **Seed 검증**: 지정한 수의 Run Seed에서 3층씩 생성해 연결·Room ID·결정성을 검사한다.
- **드롭 시뮬레이션**: 1~4층 등급 분포와 Legendary 금지 규칙을 검사한다.
- **CSV 분석**: 최신 `expedition_runs_v2.csv`가 있으면 상세 스키마를, 없으면 기존 v1 CSV를 분석한다.

기본값은 Run Seed 1,000개(3,000층), 층별 드롭 100,000회다.

## 산출물

`Application.persistentDataPath/M3Measurements`에 생성한다.

| 파일 | 내용 |
|---|---|
| `M3_QA_AUTOMATION_REPORT.md` | 전체 실행 결과와 원정 CSV 요약 |
| `seed_validation_failures.csv` | 실패한 Run Seed·층·Floor Seed·원인. 성공 시 헤더만 존재 |
| `loot_distribution.csv` | 층별 Normal/Rare/Epic/Unique/Legendary 횟수와 비율 |
| `expedition_runs_v2.csv` | 조우·처치·피해·회복·루팅·출구 지표를 포함한 플레이 기록 |

기존 `expedition_runs.csv`는 v1 표본 보존용이며 수정하거나 자동 변환하지 않는다.

## 자동 판정

- 생성된 모든 방이 시작 방에서 도달 가능해야 한다.
- 연결은 양방향이어야 하며 모든 방에 Room ID가 있어야 한다.
- 같은 Floor Seed의 레이아웃 서명이 동일해야 한다.
- 1~4층에서 Legendary는 0건이어야 한다.
- 층별 등급 비율은 설정값에서 ±0.5%p 이내여야 한다.
- 이동 1Tick, 공격 5Tick, 줍기·버리기 1Tick, 장비 교체 2Tick, 기본 가방 6칸을 유지해야 한다.
- 맵과 아이템 드롭은 같은 Seed에서 동일해야 한다.

## 사람이 판단할 항목

- 목표 300~450Tick이 재미있는 탐색 시간인지
- 전투 피해와 처치 보상이 교전 선택을 만드는지
- 계단 방 Enemy 0~1마리가 적절한지
- 긴 원정이 탐색, 회복, 전투, 루팅 중 무엇 때문에 발생했는지

자동화 결과가 PASS여도 위 항목이 재미와 의도에 맞지 않으면 밸런스 조정 대상이다.

## 관련 문서

- `QA/M3_TRACEABILITY.md`: 요구사항·Jira·TC·자동화 연결
- `QA/M3_MEASUREMENT_REPORT_TEMPLATE.md`: 자동화 결과와 플레이 5판 기록 양식
- `QA/M3_RELEASE_CHECKLIST.md`: M3 종료 전 확인 목록
- `docs/M3_DOCUMENTATION_AUDIT_2026-11-06.md`: 현재 명세와 역사 문서 충돌 감사
