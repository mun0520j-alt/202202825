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
