# CHANGELOG

형식: 마일스톤 태그 단위. 추가 / 수정 / 알려진 문제 / 테스트 요약.

## Unreleased — M1 원정 한 바퀴
### 수정 확인
- BUG-005: 런타임 Enemy 체력바 초기화 NRE 수정 후 생성·피격·처치 회귀 검증 완료
- BUG-003: 탈출 체류 중 Enemy 행동 및 구역 이탈 시 진행도 초기화 확인
- BUG-006~008: Enemy 행동의 전역 Tick 이중 반영, 탈출 진행도 이중 계산, Space 입력 누락 수정 Play 검증 완료
- M1 임시 전투 기준에 맞춰 Enemy 공격력 2 → 5 적용(Player 실효 방어 3 기준 2피해, 5회 피격 시 사망)
- BUG-009: 원정 세션에 Player 현재 HP를 보존해 층 이동 시 최대치 회복 방지 (Play 검증 완료)
- BUG-010: MIA 확정 대기 중 입력과 마우스 자동 경로를 잠금 (Play 검증 완료)
### 알려진 문제
- QA 전용 F4를 연속 실행하면 9×9 Tilemap 재기록·전체 Fog 재계산·출구/상자 재생성이 반복되어 에디터 프레임 지연이 발생한다.
- QA 전용 F4는 기존 Enemy를 재배치할 뿐 새로 생성하지 않아, Enemy 처치 후 다시 누르면 Enemy가 복구되지 않는다.
### 테스트 요약
- 이동·Space 1Tick, 공격 5Tick, Enemy 공격 주기 5Tick 및 Enemy 행동의 전역/탈출 Tick 미가산 확인
- 탈출 완료 1,439Tick 생환, 1,440·1,441Tick MIA 경계값 확인
- 결과 화면 입력 차단, 다시 출격 초기화, 계단 E 연타, Space 연타 회귀까지 통과해 M1 기능 검증 완료

## v0.0.1-m0 — 2026-11-03 (M0 기반 정리)
### 추가
- `VERSIONING.md` 버전 관리 규칙, `MILESTONES.md` 기획×QA 마일스톤, `.gitattributes` 줄바꿈 규칙
- `QA/BUGS.md` 결함 기록
### 수정
- BUG-001: Edit Mode에서 `MinimapController.OnDestroy`가 `Destroy`를 호출하던 경고 수정 (eea623e)
### 알려진 문제
- BUG-002: Git이 PATH에 없는 환경에서 `com.coplaydev.unity-mcp` 패키지 해석 실패 — 개발 환경 제약으로 수용
### 테스트 요약
- 컴파일·Play(이동·공격) 확인, BUG-001 수정 확인. Edit Mode 재로드·오브젝트 제거 및 Play 진입·종료 3회 반복 회귀 검증 통과.

## v0.0-base — 2026-11-03
- 9.3·9.5 원격 작업 병합(264ff28) + 줄바꿈 정규화(b28aace). 기준선.
- 포함: Tick 코어(1Tick=1분, 이동 1·공격 5·MIA 1,440), 플레이어 이동·전투·사망, 장비·인벤토리, HUD 시계·미니맵·HP바, 맵 생성·시야
