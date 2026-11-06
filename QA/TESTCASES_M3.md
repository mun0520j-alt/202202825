# M3 테스트 케이스 — 측정과 1차 밸런스

> 상태: 구현 1차. Unity 배치 컴파일 통과, Play 검증 대기.

| ID | 절차 | 기대 결과 | 상태 |
|---|---|---|---|
| TC-M3-01 | 같은 Run Seed로 맵을 두 번 생성 | 방·연결·Room ID·문 위치가 동일 | Play 검증 대기 |
| TC-M3-02 | Player가 닫힌 문 옆에서 E | 문이 열리고 정확히 1Tick 소비 | Play 검증 대기 |
| TC-M3-03 | Player가 열린 문 옆에서 E | Actor가 문에 없으면 닫히고 정확히 1Tick 소비 | Play 검증 대기 |
| TC-M3-04 | Enemy가 닫힌 문을 통해 추적 | 2Tick 행동으로 문을 열고 다음 행동부터 통과 | Play 검증 대기 |
| TC-M3-05 | 닫힌 문으로 Player와 Enemy 시야 차단 | Enemy가 Player를 직접 인식하지 않음 | Play 검증 대기 |
| TC-M3-06 | 같은 방 Enemy 한 마리가 Player 발견 | 같은 Room ID Enemy만 1회 경보, 연쇄 없음 | Play 검증 대기 |
| TC-M3-07 | 추적 중 3회 행동 동안 Player 재인식 실패 | Search 3회로 전환 후 기본 상태 복귀 | Play 검증 대기 |
| TC-M3-08 | Chase 또는 Search 상태에서 Space 길게 입력 | 자동 휴식 시작 불가, 수동 1Tick 대기는 가능 | Play 검증 대기 |
| TC-M3-09 | 같은 칸에서 Space 짧게 5회 | HP +1, 이동·공격·피격 시 누적 초기화 | Play 검증 대기 |
| TC-M3-10 | Player 또는 Enemy가 문 칸에 있는 상태에서 Player가 닫기 시도 | 문 닫기 실패, 0Tick | Play 검증 대기 |
| TC-M3-11 | 첫 Enemy가 문을 연 뒤 다른 Enemy가 같은 문으로 이동 | 추가 2Tick 개방 없이 열린 문 통과 | Play 검증 대기 |
| TC-M3-12 | 다른 Room ID Enemy가 Player 발견 | 인접 방 Enemy는 경보받지 않음 | Play 검증 대기 |
| TC-M3-13 | Search 중 Player 재발견 | 즉시 Chase 복귀, 미인식 횟수 초기화 | Play 검증 대기 |
| TC-M3-14 | 마우스로 닫힌 문 너머를 선택 | 경로 미리보기가 닫힌 문을 통과하지 않음 | Play 검증 대기 |
| TC-M3-15 | 1→2→3층에서 동일 셀 좌표의 문 확인 | 다른 층 문 상태가 서로 덮어쓰이지 않음 | Play 검증 대기 |

## 권장 실행 순서

1. TC-M3-01~03: Seed·문 기본 동작
2. TC-M3-10·14·15: 문 경계값과 다층 분리
3. TC-M3-04·05·11: Enemy 문 개방과 시야
4. TC-M3-06·12: Room ID 집단 경보
5. TC-M3-07·13: Chase → Search → 복귀
6. TC-M3-08·09: 자동 휴식 차단과 수동 대기 회복
