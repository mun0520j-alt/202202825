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

## 3. 태그

> 2026-11-11 사용자 결정으로 개발 마일스톤과 배포 빌드를 분리했다. 상세: `docs/VERSIONING.md`

| 구분 | 태그 | 시점 |
|---|---|---|
| 개발 마일스톤 (M0~M4, 구 표기) | `v0.0-base`, `v0.0.1-m0`, `v0.1-m1`, `v0.2-m2`, `v0.3-m3`, `v0.4-m4` | 이미 push됨 — 이름 변경 금지 |
| 개발 마일스톤 (M5~) | `mX` (`m5`, `m6`, …) | 마일스톤 완료 후 main 병합 커밋 |
| 배포 빌드 | `beta-0.X` (`beta-0.1`, …) | itch 업로드할 때마다 |

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
4. 통과하면 main 병합 → (마일스톤 끝이면) `mX` 태그 + CHANGELOG. itch 업로드는 `beta-0.X` 태그
5. `DEV_LOG.md` 맨 위에 그날 항목 추가 (기존 규칙 유지)
