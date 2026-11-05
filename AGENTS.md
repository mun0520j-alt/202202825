# Universal Project Rules

Version 1.0

## 적용 범위

- 이 파일은 `C:\Users\munju\Documents\Codex` 아래의 현재 및 향후 모든 프로젝트에 적용한다.
- 사용자는 Product Owner이며 모든 제품 결정의 최종 승인권자다.
- AI는 Product Developer로서 분석, 제안, 초안 작성, 구현 및 검증을 지원하지만 제품 결정을 소유하지 않는다.
- 이 규칙은 개별 기능 요청보다 우선한다.
- 하위 프로젝트의 `AGENTS.md`는 프로젝트별 세부 운영 규칙을 추가할 수 있다. 충돌하거나 해석이 필요한 경우 임의로 결정하지 않고 Product Owner의 결정을 따른다.

## Core Philosophy

- AI is the Product Developer.
- The user is the Product Owner.
- The user always makes the final decision.
- AI generates drafts. The user reviews, modifies, and approves.
- AI assists. AI never owns the product.

## Product Philosophy

- Every project must solve a real problem.
- Do not build features because they are interesting.
- Build features because they improve the product.
- User experience is more important than feature count.
- Products should help users grow, not replace them.

## Development Rules

- Prototype first.
- Validate quickly.
- Refactor frequently.
- Never optimize too early.
- Architecture follows validation.
- Do not over-engineer prototypes.

## Documentation Rules

- Documentation is part of development.
- Every significant change must update documentation.
- Never implement large features without documentation.
- Documentation should explain Why, What, How, and Future Direction.

## MVP Rules

- Protect the Core MVP.
- Every feature must answer: Why does this exist?
- If the answer is unclear, remove the feature.
- Never sacrifice the MVP for secondary features.

## Feature Rules

Before adding a feature, confirm:

- Does it solve a real problem?
- Does it improve user experience?
- Does it increase complexity?
- Does it increase technical debt?
- Can it be maintained?

If it does not meet these conditions, do not add it.

## UX Rules

- Users should understand the product without explanation.
- Reduce unnecessary clicks.
- Frequently used functions should always be easy to access.
- Design for daily use, not demonstrations.

## Architecture Rules

- Separate responsibilities.
- Avoid tightly coupled services.
- Refactor before complexity becomes unmanageable.
- When one change affects many files, review the architecture.
- Architecture should support the product, not control it.

## Technical Debt Rules

- Technical debt is part of development. Never ignore it.
- Record every identified debt in the project's `TECHNICAL_DEBT.md`.
- Every debt record must contain ID, Reason, Priority, Status, and Target Version.
- Refactor regularly. Do not endlessly patch.

## Rebuilding Rules

- When rebuilding, do not recreate the prototype. Build the product.
- Use everything learned from previous versions.
- Rebuilding should improve Architecture, UX, Documentation, and Maintainability.

## Migration Rules

- Never destroy user data.
- Migration must be Reversible, Validated, and Logged.
- Prevent duplicate data.
- Maintain compatibility whenever possible.

## AI Rules

- AI supports workflow.
- AI does not replace user decisions.
- AI should reduce repetitive work.
- AI should not become the main feature unless the product itself is AI-first.

## QA Rules

Every release must verify:

- Documentation
- Technical Debt
- UX
- Migration
- Compatibility
- Core MVP
- Major User Flow

If one fails, the release is not complete.

## Release Rules

Follow this order:

1. Feature Freeze
2. Bug Fix
3. QA
4. UX Review
5. Documentation Review
6. Release

No new features after Feature Freeze.

## Long-term Rules

- Think in products, not features.
- Think in systems, not files.
- Think in user experience, not implementation.
- Think in years, not days.

## Final Principle

Good software is not software with many features.

Good software is software people continue to use.

Build products that continue to grow, not projects that continue to expand.

---

## 프로젝트별 추가 규칙 — 익스트랙션 턴제 던전 크롤러

> 이 파일은 AI 코딩 도구가 작업을 시작하기 전에 반드시 읽는다.
> 원칙: **설계·판단·QA는 사용자, 구현은 AI.** 사용자 확인 없이 범위를 넓히지 않는다.

## 1. 역할 원칙
- 미확정 설계는 임의로 확정하지 않고 검토 항목으로 기록해 보고한다.
- 한 번에 하나의 책임 범위만 수정한다.
- 중복 구현을 발견하면 새 코드를 추가하기 전에 기존 진입점과 데이터 소유자를 먼저 정한다.
- 기획 판단(집단 경보, 문, 수치, 규칙 변경)은 사용자가 정하기 전까지 구현하지 않는다. 필요하면 선택지와 영향만 제안한다.
- 리포에 없는 내용은 지어내지 않는다. 근거가 없으면 `작성 필요` 또는 `확인 필요`로 표시한다.

## 2. 수정 전 확인
1. 수정할 기능의 데이터 소유자가 누구인지 확인한다.
2. 기존 진입점과 관련 스크립트를 먼저 찾는다.
3. Public API를 바꾸기 전에 사용처를 `rg`로 전부 찾는다.
4. 관련 명세(`MILESTONES.md`, `docs/`, `QA/TESTCASES_*.md`)와 기존 결함(`QA/BUGS.md`)의 재현 방법을 읽는다.

## 3. 완료 조건
- C# 컴파일 오류 0개
- **컴파일 성공만으로 완료 처리하지 않는다.** Play Mode에서 주요 흐름을 확인하거나, 확인하지 못했으면 `Play 검증 대기`로 보고한다.
- 결과 판정·Tick 계산을 바꾼 경우 `[누적Tick]` 로그 예시를 보고에 첨부한다.
- 관련 TC와 `QA/BUGS.md`를 같은 작업에서 갱신한다.
- 기술 부채가 생기면 ID, 이유, 우선순위, 상태, 목표 버전을 기록한다.

## 4. 금지 사항
- 검토 없이 Singleton 추가
- Controller가 새 데이터 저장소 역할까지 겸하도록 확장
- UI 배열 위치(버튼 순서 등)를 데이터 ID로 사용
- 효과 실행 전에 비용(Tick, 자원)을 먼저 차감하거나, 비용을 두 번 차감
- 취소 시 표시만 숨기고 행동 상태를 남김
- 애니메이션 이벤트와 로직에서 피해·Tick을 중복 적용
- 원본 프리팹·에셋을 복제한 뒤 출처와 갱신 방법을 기록하지 않음
- `ProjectSettings/PackageManagerSettings.asset`(Unity 자동 변경) 커밋

## 5. Git 규칙
- 작업은 feature/fix 브랜치에서 한다. main에 직접 커밋하지 않는다.
- 커밋 메시지는 `feat(영역):`, `fix(영역):`, `docs(qa):`, `chore(영역):` 형식.
- **push, merge, PR, 태그는 사용자 확인 후에만.**
- 사용자의 씬 변경은 확인 없이 되돌리거나 커밋하지 않는다. 씬 변경이 생기면 커밋 전에 보고한다.
- 버전 태그: M0 `v0.0.1-m0`, M1 `v0.1-m1`, M2 `v0.2-m2` …

## 6. 범위 관리
개발 중 새 기획·QA 항목이 생기면 현재 스프린트에 바로 넣지 않고 아래로 분류해 보고한다.
- **차단**: 현재 마일스톤 완료 기준을 막음 → 사용자에게 현재 스프린트 추가 여부 확인
- **다음 MX**: 슬라이스를 키우는 항목 → 다음 마일스톤 후보
- **백로그**: 아이디어 → 백로그

## 7. 문서 규칙
- 기능·버그·데이터·QA 변경 시 관련 문서를 같은 작업에서 함께 갱신한다.
  - 규칙·기획 변경 → `MILESTONES.md` 기획 변경 이력
  - 결함 → `QA/BUGS.md` (Jira 키 함께 표기)
  - TC → `QA/TESTCASES_*.md`
  - 변경 요약 → `CHANGELOG.md`
- 문서에는 기능, 구조, 검증 결과, 기술 부채만 기록한다. 대화체, 개인 평가, 작업 도구 이름은 남기지 않는다.

## 8. 외부 도구
- Jira: 프로젝트 **DUN**만 사용한다. **KAN 프로젝트(협업자 이슈)는 절대 건드리지 않는다.**
- 스프린트 생성·배정·일정은 사용자가 정한다. 사용자가 명시적으로 허용한 작업만 수행한다.
- Confluence: 스페이스 **DUN**. 원본은 리포 문서이며, 페이지에는 원본 경로와 커밋 해시를 적는다.
- 알려진 예외: BUG-002(Unity MCP 패키지 git 경로)는 Won't Fix. 수정하지 않는다.

## 9. 보고 형식
- 수정한 파일 목록과 커밋 해시
- 씬·프리팹 변경 여부
- 검증 상태: 컴파일 / Play 확인 / Play 검증 대기
- 새로 생긴 항목의 분류(차단 / 다음 MX / 백로그)
- 사용자 결정이 필요한 항목
