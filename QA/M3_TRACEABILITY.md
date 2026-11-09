# M3 요구사항 추적표

> 기준: `MILESTONES.md`, Jira DUN, M3 TC. 자동화 PASS는 밸런스 승인과 동일하지 않다.

| 요구사항 | Jira | 테스트 | 자동화·증거 | 상태 |
|---|---|---|---|---|
| 같은 Seed의 맵·배치 결정성 | DUN-40 | TC-M3-01·21·27·29 | 1,000 Seed × 3층 레이아웃 서명 | 자동화 Pass |
| 모든 방 연결·양방향 링크·Room ID | DUN-40 | TC-M3-22·27 | `seed_validation_failures.csv` | 자동화 Pass — 실패 0 |
| 시작 방 Enemy 금지, Room당 최대 1마리 | DUN-20 | TC-M3-25·27 | Seed 1,000개 × 3층 일괄 검증 | 자동화 Pass |
| 문이 이동·시야·경보를 차단 | DUN-21 | TC-M3-02~07·10~18·20 | 사용자 Play·첨부 로그 | Pass |
| 5Tick 휴식 회복, 추적 중 자동 휴식 차단 | DUN-29 | TC-M3-08·09·19 | 사용자 Play | Pass |
| 원정 전체를 CSV v3 한 행으로 기록 | DUN-40 | TC-M3-23·24·26·29·35 | `expedition_runs_v3.csv` | Pass |
| Random/Fixed Seed 분리 및 Seed별 반복 집계 | DUN-43 | TC-M3-31~36 | `seed_mode`·`seed_attempt`·`seed_summary.csv` | Pass |
| 1~4층 Legendary 0, 등급 분포 허용 오차 충족 | DUN-39 | TC-M2-41·TC-M3-28 | 층별 100,000회 | 자동화 Pass |
| 3층 탈출 300~450Tick 원인 설명 | DUN-40 | TC-M3-26·29 | Random 생환 5판 평균 330.6Tick | Pass — 목표 진입 4/5 |

## 종료 조건

- DUN-39: TC-M3-28에서 층별 분포가 ±0.5%p 이내이고 Legendary가 0건이다.
- DUN-40: TC-M3-21~29 결과와 생성 파일을 검토한다.
- 실패 Seed는 수정 전 보존해 동일 Seed로 재현한다.
- 밸런스 수치 변경은 측정 보고서와 사용자 결정 후 수행한다.
