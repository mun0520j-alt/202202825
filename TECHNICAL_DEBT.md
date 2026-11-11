# 기술 부채

| ID | 항목 | 이유 | 우선순위 | 상태 | 목표 버전 |
|---|---|---|---|---|---|
| TD-001 | M2 가방·Stash의 즉시 모드 UI를 정식 Canvas UI로 교체 | 핵심 루팅·손실 흐름을 먼저 검증하기 위한 프로토타입 UI이며, 해상도·게임패드·시각 디자인 대응이 부족함 | Medium | Open | M2.1 |
| TD-002 | Stash 영구 저장 도입 | Stash·장비·영지 대기 가방의 파일 저장, 백업 복구, 원정 중 종료 MIA 처리가 필요함 | Medium | In Progress — v1 구현, Play 검증 대기 | M5 |
| TD-003 | Unity 6 폐기 예정 탐색 API 교체 | 컴파일은 성공하지만 `FindObjectOfType`, `FindObjectsOfType`, `GetInstanceID` 경고가 누적되어 향후 Unity 버전 호환성과 탐색 비용에 위험이 있음 | Low | Open | M3.1 |
| TD-004 | Confluence 테스트 결과 수동 동기화 | 리포의 TC가 Pass로 갱신돼도 Confluence 초기 업로드본이 미실행 상태로 남을 수 있음 | Medium | Open | M3.1 |
| TD-005 | 디버그 원정 제외 로그에 조작 원인 표시 | F4·F5·Tick·HP 조작이 모두 같은 제외 문구를 사용해 측정 데이터 감사 시 원인을 구분하기 어려움 | Low | Open | M4 |
| TD-006 | Fixed Seed 모드 화면 표시 | Fixed 모드가 PlayerPrefs에 유지되어 일반 측정 원정이 Fixed로 기록될 위험이 있음 | Medium | Open | M4 |
| TD-007 | 작은 정수 단위로 인한 방어 성장 상한 | 기존 수치에서는 Rare 방어구 1부위만으로 최소 피해에 도달해 상위 등급의 가치가 사라졌음 | High | Resolved — 전투 단위 확대 및 등급별 방어 수치 재조정 | M4 |
| TD-008 | 휴식 회복량 설정값 분리 | 일반 휴식 +10과 보스 처치 후 +20이 `PlayerTurnActor`의 `bossDefeated ? 20 : 10` 조건식에 직접 들어 있어 향후 회복 배율 조정 시 코드 수정이 필요함 | Low | Open | M5 후보 |
| TD-009 | 출격 가방 목록·격자 이중 표현 제거 | 기존 저장·측정 호환을 위해 `runBag` 목록과 `LootGridModel`을 함께 유지해 모든 이동에서 동기화 누락 위험이 있음 | Medium | Open | M6.1 |

## 관리 규칙

- 목표 버전을 넘긴 항목은 완료로 간주하지 않고 `일정 재검토`로 표시한다.
- Confluence는 원본이 아니며, 상태 충돌 시 리포의 TC와 커밋을 우선한다.
- 기능을 차단하지 않는 경고 정리는 유지보수 버전으로 분리한다.
