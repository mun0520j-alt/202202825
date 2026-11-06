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
