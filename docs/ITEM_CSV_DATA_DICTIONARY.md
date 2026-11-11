# 아이템 CSV 데이터 사전

> 원본: `Assets/Resources/Data/items.csv`. M6부터 출격 가방 격자 크기의 권위 데이터도 이 파일에 둔다.

## 컬럼

| 컬럼 | 형식 | 필수 | 의미 | 검증 규칙 |
|---|---|---|---|---|
| `item_id` | string | Y | 저장·참조용 고유 ID | 중복 금지, 표시명으로 사용 금지 |
| `item_name` | string | Y | UI 표시명 | 빈 값 금지 |
| `item_type` | enum | Y | Weapon, Shield, Armor, Bag | 지원하지 않는 값은 로드 실패 처리 |
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
| 기본 지급(드롭 제외) | 3 | 0 | 0 | 0 | 3 |
| 합계 | 7 | 4 | 4 | 4 | 19 |

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

## 변경 절차

1. ID 중복과 enum 값을 확인한다.
2. 코드 수정 없이 CSV만 변경한다.
3. `Dungeon Tools > QA > M6 Grid Model Validation`으로 크기·가방 형상·경계·겹침·회전을 확인한다.
4. TC-M2-42로 이름·등급·스탯·가중치 반영을 확인한다.
5. M3 분포 자동화를 실행한다.
6. 스키마 변경이면 세이브 마이그레이션 영향을 기록한다.

## 알려진 제한

- 현재 `items.csv`는 M6 장비·가방 최소 데이터이며 장신구·소모품·수집품 전체 설계를 포함하지 않는다.
- `ITEM_SYSTEM_DESIGN.md`는 장기 설계 근거이고 CSV는 현재 런타임 데이터다.
- 가방 이름과 드롭 가중치는 M5 통합 Play 전 임시값이다.
