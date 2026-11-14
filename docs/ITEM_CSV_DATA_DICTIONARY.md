# 아이템 CSV 데이터 사전

> 원본: `Assets/Resources/Data/items.csv`. M6부터 출격 가방 격자 크기의 권위 데이터도 이 파일에 둔다. M7 경제 수치는 `Assets/Resources/Data/m7_economy.csv`, M8 퀘스트·우호도 수치는 `m8_quests.csv`와 `m8_economy.csv`에서 관리한다.

## 컬럼

| 컬럼 | 형식 | 필수 | 의미 | 검증 규칙 |
|---|---|---|---|---|
| `item_id` | string | Y | 저장·참조용 고유 ID | 중복 금지, 표시명으로 사용 금지 |
| `item_name` | string | Y | UI 표시명 | 빈 값 금지 |
| `item_type` | enum | Y | Weapon, Shield, Armor, Bag, Junk, Consumable | 지원하지 않는 값은 로드 실패 처리. Junk·Consumable은 장착 불가 |
| `rarity` | enum | Y | Normal, Rare, Epic, Unique | 1~4층 Legendary 데이터 금지 |
| `attack_bonus` | int | Y | 장착 공격력 보너스 | 0 이상 |
| `defense_bonus` | int | Y | 장착 방어력 보너스 | 0 이상 |
| `capacity_bonus` | int | Y | M5 목록 가방과 v2 세이브 해석용 용량 보너스 | M6 배치 판정에는 사용하지 않음. 마이그레이션 호환용 유지 |
| `icon_key` | string | N | 아이콘 리소스 키 | 빈 값이면 대체 표시 사용 |
| `drop_weight` | int | Y | 같은 등급·풀 안의 상대 가중치 | 0은 일반 드롭 제외 |
| `sell_price` | int | Y | BaseCamp 공용 상인 판매가 | Normal 10, Rare 30, Epic 80, Unique 200 초안 |
| `grid_w` | int | Y | 아이템의 기본 가로 칸 수 | 1 이상. 회전 시 `grid_h`와 교환 |
| `grid_h` | int | Y | 아이템의 기본 세로 칸 수 | 1 이상. 회전 시 `grid_w`와 교환 |
| `bag_grid_w` | int | Y | 해당 가방 장착 시 출격 격자 가로 | Bag만 1 이상, 그 외 0 |
| `bag_grid_h` | int | Y | 해당 가방 장착 시 출격 격자 세로 | Bag만 1 이상, 그 외 0 |

## 현재 데이터 요약

| 타입 | Normal | Rare | Epic | Unique | 합계 |
|---|---:|---:|---:|---:|---:|
| Weapon | 1 | 1 | 1 | 1 | 4 |
| Shield | 1 | 1 | 1 | 1 | 4 |
| Armor | 1 | 1 | 1 | 1 | 4 |
| Bag | 1 | 1 | 1 | 1 | 4 |
| Junk | 2 | 1 | 1 | 1 | 5 |
| Consumable | 1 | 0 | 0 | 0 | 1 |
| 기본 지급(드롭 제외) | 3 | 0 | 0 | 0 | 3 |
| 합계 | 10 | 5 | 5 | 5 | 25 |

## M4 전투 수치

| 타입 | Normal | Rare | Epic | Unique |
|---|---:|---:|---:|---:|
| Weapon 공격 보너스 | 30 | 40 | 50 | 80 |
| Shield 방어 보너스 | 7 | 8 | 9 | 10 |
| Armor 방어 보너스 | 7 | 8 | 9 | 10 |

- 기본 지급 장비는 CSV 드롭 장비와 별도로 기본 검 +20, 기본 방패 +6, 기본 갑옷 +6이다.
- M6 아이템 크기는 무기 1×3, 방패 2×2, 갑옷 2×3, 가방 아이템 2×2다.
- 출격 가방 격자는 기본 3×2, Normal 4×2, Rare 3×3, Epic 5×3, Unique 4×5다.
- `capacity_bonus`의 +2/+3/+4/+6은 v2 데이터 해석을 위해 남기며 M6 격자 배치에는 영향을 주지 않는다.
- `basic_weapon`, `basic_shield`, `basic_armor`는 크기를 코드에 하드코딩하지 않기 위한 드롭 가중치 0의 기본 지급 데이터다.
- 가방 `drop_weight=50`은 같은 등급 내 약 14% 드롭을 노린 초안으로, 통합 Play 후 확정한다.
- M7 잡템 5종의 표시명은 임시다. 잡템은 장착할 수 없으며 판매·향후 퀘스트 납품에 사용한다.
- M7 `escape_scroll`은 1×1 Consumable이며 일반 드롭에서는 제외(`drop_weight=0`)하고 상인 구매로 공급한다.
- 같은 등급 내 가중치 기준 잡템 비율은 Normal 200/550(36.36%), Rare·Epic·Unique 각각 50/400(12.5%)다.

## 변경 절차

1. ID 중복과 enum 값을 확인한다.
2. 코드 수정 없이 CSV만 변경한다.
3. `Dungeon Tools > QA > M6 Grid Model Validation`으로 크기·가방 형상·경계·겹침·회전을 확인한다.
4. TC-M2-42로 이름·등급·스탯·가중치 반영을 확인한다.
5. M3 분포 자동화를 실행한다.
6. 스키마 변경이면 세이브 마이그레이션 영향을 기록한다.

## M8 퀘스트 CSV

원본: `Assets/Resources/Data/m8_quests.csv`

| 컬럼 묶음 | 의미 |
|---|---|
| `quest_id`, `order`, `route`, `type`, `prerequisite_id` | 고유 ID·순서·Common/Combat/Collection 분기·Delivery/Escape/Carry 종류·선행 조건 |
| `target_item_id` | 단일 또는 복합 납품 조건. 복합은 `item_id:개수|item_id:개수` 형식 |
| `target_rarity`, `target_category`, `target_count`, `target_floor`, `target_value` | 운반 등급·범주·수량·도달 층·판매가 합계 조건 |
| `requires_found_in_raid`, `requires_boss_kill` | 인레이드 납품·보스 처치 필요 여부(0/1) |
| `reward_reputation`, `reward_gold`, `reward_item_id`, `reward_item_count` | 우호도·Gold·아이템 보상 |
| `title`, `description` | 상인 퀘스트 UI 표시 문자열 |

- `equipment_epic_random`은 Epic 무기·방패·갑옷 중 하나를 보상하는 예약 키다.
- 두 루트 모두 공통 퀘스트를 포함한 우호도 합계가 300이어야 한다.
- `Dungeon Tools > QA > M8 Quest Data Validation`에서 정의 수·선행·분기 합계·복합 납품을 검사한다.

## M8 경제 CSV

원본: `Assets/Resources/Data/m8_economy.csv`

- Lv2/Lv3 필요 우호도와 Gold, 레벨당 판매 보너스·구매 할인·보험 반환 보너스를 관리한다.
- 런타임 가격과 보험 판정은 UI가 아니라 `LootRuntimeState`에서 이 설정을 적용한다.

## 알려진 제한

- 현재 `items.csv`는 M6 장비·가방 최소 데이터이며 장신구·소모품·수집품 전체 설계를 포함하지 않는다.
- `ITEM_SYSTEM_DESIGN.md`는 장기 설계 근거이고 CSV는 현재 런타임 데이터다.
- 가방 이름과 드롭 가중치는 M5 통합 Play 전 임시값이다.
