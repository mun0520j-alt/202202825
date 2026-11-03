# 버전 관리 규칙 (2026-11-03~)

## 1. 브랜치
| 브랜치 | 용도 | 규칙 |
|---|---|---|
| `main` | 항상 Play 가능한 상태 | 직접 커밋 금지. 마일스톤 완료 시에만 병합 |
| `feature/<마일스톤>-<작업>` | 작업 단위 | 예: `feature/m1-stairs`, `feature/m1-result-screen`. 작업 끝나면 Play 확인 후 main에 병합 |
| `fix/<버그ID>` | 버그 수정 | 예: `fix/BUG-003-minimap-destroy`. QA 버그 리포트 ID와 1:1 |

## 2. 커밋 메시지
```
<종류>(<범위>): <요약>

- 무엇을 / 왜
- 관련: BUG-003, TC-M1-012
```
- 종류: `feat` 기능 · `fix` 버그 · `test` 테스트 장치 · `docs` 문서 · `refactor` 동작 변화 없는 정리 · `chore` 설정
- 예: `fix(ui): 에디터 모드에서 MinimapController Destroy 경고 수정` / `feat(dungeon): 3층 탈출 절차 Tick 소모 추가`
- 금지: `asdasd`, `2`, `8.27` 같은 메시지 (나중에 아무것도 못 찾음)

## 3. 태그 (마일스톤 = 버전)
| 태그 | 시점 |
|---|---|
| `v0.0-base` | 현재 상태(9.5 병합 직후, 264ff28) |
| `v0.1-m1` | M1 원정 한 바퀴 완료 |
| `v0.2-m2` | M2 루팅·손실 완료 |
| `v0.3-m3` | M3 측정·밸런스 1차 완료 |
- 태그마다 `CHANGELOG.md`에 한 단락: 추가 / 수정 / 알려진 문제 / 테스트 결과 요약
- 포트폴리오 캡처·영상은 반드시 **태그 기준 빌드**에서 찍는다 (재현 가능하게)

## 4. 줄바꿈 정리 (`.gitattributes`)
현재 `.meta` 파일이 줄바꿈 차이만으로 "수정됨"으로 뜬다. 저장소 루트에 `.gitattributes`를 추가하고 한 번 정규화한다.
- 적용 후 1회: `git add --renormalize .` → `chore: 줄바꿈 정규화` 커밋
- Unity 설정 확인: Asset Serialization = **Force Text** (확인됨, mode 2)

## 5. 작업 한 사이클
1. `main`에서 `feature/...` 브랜치 생성
2. Codex 구현 → 사용자 Play 확인 → 테스트 케이스 실행
3. 버그는 `QA/BUGS.md`에 ID 부여 → 고치면 `fix/...` 브랜치
4. 통과하면 main 병합 → (마일스톤 끝이면) 태그 + CHANGELOG
5. `DEV_LOG.md` 맨 위에 그날 항목 추가 (기존 규칙 유지)
