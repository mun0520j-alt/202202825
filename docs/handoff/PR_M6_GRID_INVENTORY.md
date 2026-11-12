# M6 격자 인벤토리

feature/m6-grid-inventory → main · 병합 후 태그 m6 · Jira 에픽 DUN-57

## 목차

1. 요약
2. 기능별 변경
3. 사용자 결정 (변경 포함)
4. 결함 수정
5. 검증
6. 후속 항목
7. 병합 후 할 일

---

## 1. 요약

무엇을 들고 나갈지 공간으로 고민하는 타르코프식 격자 인벤토리를 도입했다.

- 출격 가방 격자, 아이템 크기·회전, 공간 부족 판정
- 상자·바닥 더미·보스 전리품 5×5 루팅 UI
- 원정 중 장비·가방 교체 (원자적 재배치)
- 세이브 v3 (위치·회전), v2 마이그레이션
- TC-M6 29/29 Pass, BUG-037~043 Closed

## 2. 기능별 변경

| 영역 | 내용 | 주요 파일 |
|---|---|---|
| 격자 모델 | 배치·회전·겹침·자동 배치, CSV grid_w/grid_h, 가방 격자 bag_grid_w/h | LootGridModel.cs, items.csv, LootItemDatabase.cs |
| 가방 격자 | 없음 3×2 / Normal 4×2 / Rare 3×3 / Epic 5×3 / Unique 4×5 | items.csv, LootRuntimeState.cs |
| 아이템 크기 | 무기 1×3 / 방패 2×2 / 갑옷 2×3 / 가방 2×2 (임시값) | items.csv |
| 던전 인벤토리 | 오른쪽 패널, 위 장비 / 아래 격자, 48px 셀, 툴팁, 선택 하이라이트, 큰 가방 장착 시 UI 확장 | InventoryPanel.cs, LootGridView.cs |
| 루팅 | 상자·바닥 더미·보스 전리품 5×5, 왼쪽 패널, 루팅 중 장비 장착 가능 | LootingPanel.cs, GroundLootPile.cs, DungeonChest.cs, EnemyTurnActor.cs |
| 영지 | 출격 가방 격자 재사용, 가방 교체 시 내용물 Stash 이동 | BaseCampGridPanel.cs |
| 세이브 v3 | x·y·rotated 저장, v2 자동 배치 마이그레이션, save_v2.bak 영구 보존 | M5SaveService.cs |
| QA | F8 바닥 더미, F9 아이템 지급 (에디터 전용) | ExpeditionDebugCommands.cs, M6GridInventoryValidation.cs |

## 3. 사용자 결정

회의록: docs/meetings/M6_GRID_INVENTORY_DECISIONS_2026-11-11.md

- 격자는 출격 가방만. Stash 격자는 M8
- R키 회전 0Tick, 자동 배치, 공간 부족 시 0Tick·상태 불변·자동 버림 없음
- 아이템 이동: 첫 클릭 선택, 더블클릭 이동, 1Tick (2026-11-12 추가)
- 장비·가방 장착·교체·해제 2Tick
- 원정 중 가방 교체 허용 (2026-11-12 변경): 내용물과 기존 가방을 새 격자에 전부 재배치할 수 있을 때만 성공, 아니면 전체 취소·0Tick
- 상자 열기 5Tick, 재개봉 0Tick
- 보스 드롭은 사망 칸 5×5 루팅 더미, 유예 없음
- 바닥 더미가 가득 차면 버리기 실패·0Tick
- UI는 왼쪽 바깥(루팅) / 오른쪽 내 것(인벤토리)
- Tick 1,440 유지
- BETA 0.1은 M6 완료 후 m6 기반

## 4. 결함 수정

| BUG | 심각도 | 내용 |
|---|---|---|
| BUG-037 | Major | 영지 격자 UI 중첩, 작업 버튼 라벨 누락 |
| BUG-038 | Major | 루팅 종료 후 인벤토리 잔류, 미니맵 중첩 |
| BUG-039 | Minor | 루팅 중 I 입력이 상자와 인벤토리를 닫음 |
| BUG-040 | Major | 선택 잔류, 기본 장비 교체 버튼·공간 판정 누락 |
| BUG-041 | Minor | 격자 툴팁 깜빡임 |
| BUG-042 | Minor | 루팅 UI에서 가방 장비 장착 불가 |
| BUG-043 | Minor | 큰 가방 장착 시 격자가 프레임 침범 |

## 5. 검증

- C# 컴파일 오류 0, 격자 자동 검증 실패 0
- TC-M6 29/29 Pass (사용자 Play, 2026-11-12)
- M1~M5 핵심 회귀 Pass
- Random 5층 생환 3판: 1,028 / 937 / 678Tick, 평균 881Tick

## 6. 후속 항목

- Tick 평균 881은 재검토 조건(850 초과)에 해당 → M7 착수 전 CSV 분석과 BETA 0.1 표본으로 판단
- TD-009 목록·격자 이중 표현, TD-010 보스 더미 공간 부족 시 소실, TD-011 저장 시 무로그 누락, TD-012 기본 격자 상수

씬·프리팹 변경 없음. ProjectSettings/PackageManagerSettings.asset 제외.

## 7. 병합 후 할 일

- [ ] main 병합 커밋에 m6 주석 태그
- [ ] Jira·Confluence M6 동기화
- [ ] BETA 0.1 또는 M7 기획

🤖 Generated with Claude Code
