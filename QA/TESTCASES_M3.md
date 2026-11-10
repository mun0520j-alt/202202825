# M3 테스트 케이스 — 측정과 1차 밸런스

> 상태: M3-A 문·추적·휴식 Play 검증 완료 (2026-11-06).

| ID | 절차 | 기대 결과 | 상태 |
|---|---|---|---|
| TC-M3-01 | 같은 Run Seed로 맵을 두 번 생성 | 방·연결·Room ID·문 위치가 동일 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-02 | Player가 닫힌 문 옆에서 E | 문이 열리고 정확히 1Tick 소비 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-03 | Player가 열린 문 옆에서 E | Actor가 문에 없으면 닫히고 정확히 1Tick 소비 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-04 | Enemy가 닫힌 문을 통해 추적 | `/OpenDoor/2`로 다음 Enemy 행동을 2Tick 뒤 예약하고 문 개방 | Pass (첨부 로그 확인, 2026-11-06) |
| TC-M3-05 | 닫힌 문으로 Player와 Enemy 시야 차단 | Enemy가 Player를 직접 인식하지 않음 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-06 | 같은 방 Enemy 한 마리가 Player 발견 | 같은 Room ID Enemy만 1회 경보, 연쇄 없음 | Pass (첨부 로그 확인, 2026-11-06) |
| TC-M3-07 | 추적 중 Player 재인식 실패 | 마지막 목격 위치까지 추적한 뒤 Search 3회로 전환하고 기본 상태 복귀 | Pass (첨부 로그 확인, 2026-11-06) |
| TC-M3-08 | Chase 또는 Search 상태에서 Space 길게 입력 | 자동 휴식 시작 불가, 수동 1Tick 대기는 가능 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-09 | 같은 칸에서 Space 짧게 5회 | HP +10, 이동·공격·피격 시 누적 초기화 | Play 재검증 대기 (누적 초기화 규칙 유지) |
| TC-M3-10 | Player 또는 Enemy가 문 칸에 있는 상태에서 Player가 닫기 시도 | 문 닫기 실패, 0Tick | Pass (사용자 Play, 2026-11-06) |
| TC-M3-11 | 첫 Enemy가 문을 연 뒤 다른 Enemy가 같은 문으로 이동 | 추가 2Tick 개방 없이 열린 문 통과 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-12 | 다른 Room ID Enemy가 Player 발견 | 인접 방 Enemy는 경보받지 않음 | Pass (첨부 로그 확인, 2026-11-06) |
| TC-M3-13 | Search 중 Player 재발견 | 즉시 Chase 복귀, 미인식 횟수 초기화 | Pass (첨부 로그, 2026-11-06) |
| TC-M3-20 | 멀리서 Player를 발견한 Enemy와 거리를 벌린 뒤 문 닫기 | 마지막 목격 위치까지 이동하며 닫힌 문을 2Tick으로 열고, 도착 후 Search 3회 | Pass (첨부 로그 확인, 2026-11-06) |
| TC-M3-21 | F6에서 Seed 입력 후 재시작하고 같은 Seed를 다시 적용 | 방·연결·Room ID·문·Enemy·상자 배치가 동일하고 `[M3Validate] PASS` | Pass (Fixed Seed `123456` 연속 3회, 2026-11-09) |
| TC-M3-22 | F6의 현재 맵 자동 검증 실행 | 전체 방 연결, Actor·상자·출구 겹침 없음, 출구 경로 존재 | Pass (Seed `123456`, `547951530` 현재 맵 검증, 2026-11-09) |
| TC-M3-23 | 이동·공격·대기·문·상자 행동 후 생환/사망/MIA | `expedition_runs_v3.csv`에 결과 1행과 행동별 Tick·방문 방 수 저장 | Pass (Random 5판·Fixed 3판 v3 CSV 대조, 2026-11-09) |
| TC-M3-24 | 1→2→3층 진행 후 원정 종료 | 층마다 별도 행이 아닌 원정 전체가 한 행으로 기록되고 Seed 유지 | Pass (Fixed Seed `123456` 1→2→3층 유지, 2026-11-09) |
| TC-M3-25 | F6에서 여러 Seed를 적용하고 현재 층 Enemy의 Room ID 확인 | 시작 방에는 Enemy가 없고, 각 Room ID에는 초기 Enemy가 0~1마리만 배치되며 같은 Seed에서 배치가 동일 | Pass (동일 Seed 배치 재현·현재 맵 검증, 2026-11-09) |
| TC-M3-26 | 전투·회복·상자·줍기를 포함한 원정 종료 후 `expedition_runs_v3.csv` 확인 | 조우·처치·준 피해·받은 피해·회복·상자·획득·바닥 드롭·출구 경로·출구 방 Enemy가 실제 플레이와 일치. 기존 v1·v2 CSV는 보존 | Pass (Random 5판·Fixed 3판 v3 CSV 대조, 2026-11-09) |
| TC-M3-27 | M3 Automation에서 Run Seed 1,000개 자동 검증 | 3,000개 층의 연결·양방향 연결·Room ID·동일 Seed 결정성 통과, 실패 시 재현 Seed CSV 저장 | Pass (3,000층 실패 0, 2026-11-09) |
| TC-M3-28 | M3 Automation에서 층별 드롭 100,000회 실행 | 설정 분포 ±0.5%p, 1~4층 Legendary 0건, 결과 CSV 저장 | Pass (층별 100,000회, 2026-11-09) |
| TC-M3-29 | M3 Automation 전체 실행 | Tick·가방·Seed 핵심 회귀 통과 및 원정 CSV 자동 요약 보고서 생성 | Pass (자동화 보고서 확인, 2026-11-09) |
| TC-M3-30 | 원정 중 F4, F5, QA/Set Elapsed Tick, QA/Set Player HP를 각각 사용한 뒤 종료 | Console에 디버그 원정 제외 로그가 나오고 `expedition_runs_v3.csv`에 행이 추가되지 않음 | Pass (F5 Seed `-553415871`, 314Tick 생환 제외 및 기존 QA 조작 회귀 확인, 2026-11-09) |
| TC-M3-31 | Random 모드에서 연속 3회 출격 | 매번 다른 Seed·다른 맵. CSV v3 3행 모두 `seed_mode=Random`, Seed가 서로 다름 | Pass (Random 5판 CSV 확인, 2026-11-09) |
| TC-M3-32 | F6에서 Seed 입력 후 적용, 원정 종료 후 다시 출격 | 두 원정의 Seed·맵 동일, `seed_mode=Fixed` | Pass (Seed `123456` 연속 3회, 2026-11-09) |
| TC-M3-33 | Fixed 상태에서 `Random 모드로 전환` 후 다음 출격 | 새 Seed 생성, `seed_mode=Random` | Pass (사용자 Play·CSV 확인, 2026-11-09) |
| TC-M3-34 | Random 원정 CSV의 Seed를 F6 Fixed로 입력 | 해당 원정과 같은 맵·Room ID·문·Enemy·상자 배치 재현 | Pass (Random Seed `547951530` 재현, 2026-11-09) |
| TC-M3-35 | 원정 중 1→2→3층 이동 | 원정 도중 Seed가 바뀌지 않고 CSV v3 한 행에 하나의 Seed 기록 | Pass (Random 5판·Fixed 1판 CSV 확인, 2026-11-09) |
| TC-M3-36 | Fixed로 같은 Seed 3회 + Random 2회 종료 후 `seed_summary.csv` 확인 | Fixed Seed는 `runs=3`, v3의 `seed_attempt=1·2·3`; Random Seed는 각각 한 행, 1회차 Tick 일치 | Pass (`123456` 17:33·17:39·17:48, `runs=3`, 평균 403Tick, 2026-11-09) |
| TC-M3-37 | 문과 미개봉 상자가 동시에 인접한 위치에서 E | 주변 상호작용 UI에 문 열기/닫기와 상자 열기가 함께 표시되고, 선택한 대상만 1회 실행 | Pass (사용자 Play 확인, 2026-11-09) |
| TC-M3-38 | 방 수 9~12 설정으로 Random Seed 5판 진행 | 모든 층 방 수 9~12, 최원거리 출구 유지, 기존 12~16 표본 대비 출구 경로·이동 Tick·총 Tick·상자/획득 수 비교 가능 | Pass — 277·378·345·313·340Tick, 평균 330.6Tick (2026-11-09) |
| TC-M3-39 | 3층 탈출 구역에서 Space를 누른 채 10Tick 이상 유지 | 정확히 체류 10Tick에서 추가 입력을 받지 않고 생환. 완료 시점 1,439는 생환, 1,440 이상은 MIA, 같은 Tick 사망은 Dead 우선 | Pass (1,439 생환·1,440 MIA·사망 확인, 2026-11-09) |
| TC-M3-40 | 인접 Enemy를 방향키·마우스로 빠르게 연속 공격해 5타 처치 | 정상 Player 턴마다 데미지 1회와 `Player/Attack/5` 1행. 5타 처치는 총 25Tick이며 Enemy 행동 중 입력은 추가 피해를 주지 않음 | Play 재검증 대기 (입력 중복 회귀) |
| TC-M3-14 | 마우스로 닫힌 문 너머를 선택 | 경로 미리보기가 닫힌 문을 통과하지 않음. 문 개폐는 키보드 E 전용 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-15 | 1→2→3층에서 동일 셀 좌표의 문 확인 | 다른 층 문 상태가 서로 덮어쓰이지 않음 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-16 | F3에서 연결된 두 방의 경계와 문 점유 셀 확인 | 공용 벽은 1칸 두께, 문은 그 경계의 3칸만 점유하고 양쪽 상호작용 거리가 대칭 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-17 | 상자 생성 위치 전체 확인 | 문 점유 셀에는 상자가 생성되지 않음 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-18 | Enemy가 문을 연 직후 문 반대편 Player가 E로 닫기 시도 | Enemy가 문 칸 또는 인접 칸에 있으면 닫기 실패·0Tick, 문 반복 개폐로 추적 봉쇄 불가 | Pass (사용자 Play, 2026-11-06) |
| TC-M3-19 | HP 2에서 Space를 계속 눌러 자동 휴식 | 5Tick마다 HP와 체력바 +1, 40Tick에 HP 10 도달 후 자동 대기 정지 | Pass (사용자 Play, 2026-11-06) |

## 권장 실행 순서

1. TC-M3-01~03: Seed·문 기본 동작
2. TC-M3-10·14·15: 문 경계값과 다층 분리
3. TC-M3-04·05·11: Enemy 문 개방과 시야
4. TC-M3-06·12: Room ID 집단 경보
5. TC-M3-07·13: Chase → Search → 복귀
6. TC-M3-08·09: 자동 휴식 차단과 수동 대기 회복
