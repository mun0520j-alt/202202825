# 아이템 CSV 데이터 사전

> 원본: `Assets/Resources/Data/items.csv`. 현재 M2~M3 런타임에서 사용하는 최소 스키마다.

## 컬럼

| 컬럼 | 형식 | 필수 | 의미 | 검증 규칙 |
|---|---|---|---|---|
| `item_id` | string | Y | 저장·참조용 고유 ID | 중복 금지, 표시명으로 사용 금지 |
| `item_name` | string | Y | UI 표시명 | 빈 값 금지 |
| `item_type` | enum | Y | Weapon, Shield, Armor, Bag | 지원하지 않는 값은 로드 실패 처리 |
| `rarity` | enum | Y | Normal, Rare, Epic, Unique | 1~4층 Legendary 데이터 금지 |
| `attack_bonus` | int | Y | 장착 공격력 보너스 | 0 이상 |
| `defense_bonus` | int | Y | 장착 방어력 보너스 | 0 이상 |
| `capacity_bonus` | int | Y | 가방 용량 보너스 | Bag 외 사용은 확인 필요 |
| `icon_key` | string | N | 아이콘 리소스 키 | 빈 값이면 대체 표시 사용 |
| `drop_weight` | int | Y | 같은 등급·풀 안의 상대 가중치 | 0은 일반 드롭 제외 |

## 현재 데이터 요약

| 타입 | Normal | Rare | Epic | Unique | 합계 |
|---|---:|---:|---:|---:|---:|
| Weapon | 1 | 1 | 1 | 1 | 4 |
| Shield | 1 | 1 | 1 | 1 | 4 |
| Armor | 1 | 1 | 1 | 1 | 4 |
| Bag | 1 | 0 | 0 | 0 | 1 |
| 합계 | 4 | 3 | 3 | 3 | 13 |

## 변경 절차

1. ID 중복과 enum 값을 확인한다.
2. 코드 수정 없이 CSV만 변경한다.
3. TC-M2-42로 이름·등급·스탯·가중치 반영을 확인한다.
4. M3 분포 자동화를 실행한다.
5. 스키마 변경이면 CSV v2처럼 버전을 분리하고 마이그레이션 영향을 기록한다.

## 알려진 제한

- 현재 `items.csv`는 M2 장비 최소 데이터이며 장신구·소모품·수집품 전체 설계를 포함하지 않는다.
- `ITEM_SYSTEM_DESIGN.md`는 장기 설계 근거이고 CSV는 현재 런타임 데이터다.
- Bag의 `drop_weight=0`은 일반 전리품 풀에서 제외한다는 의미다.
