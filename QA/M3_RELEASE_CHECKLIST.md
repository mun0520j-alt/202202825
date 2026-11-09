# M3 릴리스 체크리스트

## Feature Freeze

- [x] 사용자 승인 없이 새 기능을 추가하지 않음
- [x] 미확정 수치·규칙을 구현하지 않음

## 자동화·QA

- [x] TC-M3-21~29 실행
- [x] Seed 1,000개 × 3층 검증
- [x] 층별 드롭 100,000회 검증
- [x] 실패 Seed 0건 또는 결함 등록
- [x] 플레이 5판 이상 CSV 기록
- [x] M1·M2 핵심 회귀 통과
- [x] C# 컴파일 오류 0개
- [x] Play Mode 주요 흐름 확인

## 위험·부채

- [x] 열린 BUG의 차단 여부 확인
- [x] `TECHNICAL_DEBT.md` 갱신
- [x] Stash 영구 저장 일정 결정 또는 저장 없음 명시
- [x] CSV v1·v2 보존 및 v3 스키마 확인

## 문서·외부 도구

- [x] M3 측정 보고서 작성
- [x] `MILESTONES.md`, `CHANGELOG.md`, TC 갱신
- [ ] BUG-027~030 Jira DUN 등록·스프린트 배정 (사용자 결정 대기)
- [ ] Confluence 원본 커밋 해시 갱신
- [x] 씬·프리팹 변경 없음 확인

## Git·릴리스

- [x] feature 브랜치 커밋 완료
- [ ] 사용자 확인 후 push·PR
- [ ] PR 검토 및 main 병합
- [ ] 사용자 확인 후 `v0.3-m3` 태그
