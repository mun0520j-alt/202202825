# Assets/Generated 인벤토리 (SpriteSets / Animations / Prefabs)

> `Assets/Generated/` 아래 3개 폴더(SpriteSets, Animations, Prefabs)를 이름 기준으로 교차 정리한 목록입니다.
> 파일명이 전부 `{base}.asset` / `{base}.controller` / `{base}_idle.anim` 형태로 통일되어 있어, base 이름 하나로 세 폴더를 관통해서 찾을 수 있습니다.
> 총 SpriteSet 142개 / Prefab 42개 / Animation(.anim + .controller) 119개.

---

## 1. 몬스터/캐릭터 — 풀 애니메이션 (idle + run [+ hit]) — 25종
SpriteSet + Prefab + AnimatorController 모두 존재, idle/run(플레이어블 6종은 hit도 있음) 애니메이션 완비.

| 이름 | idle | run | hit |
|---|---|---|---|
| angel | ✅ | ✅ | |
| big_demon | ✅ | ✅ | |
| big_zombie | ✅ | ✅ | |
| chort | ✅ | ✅ | |
| doc | ✅ | ✅ | |
| dwarf_f | ✅ | ✅ | ✅ |
| dwarf_m | ✅ | ✅ | ✅ |
| elf_f | ✅ | ✅ | ✅ |
| elf_m | ✅ | ✅ | ✅ |
| goblin | ✅ | ✅ | |
| imp | ✅ | ✅ | |
| knight_f | ✅ | ✅ | ✅ |
| knight_m | ✅ | ✅ | ✅ |
| lizard_f | ✅ | ✅ | ✅ |
| lizard_m | ✅ | ✅ | ✅ |
| masked_orc | ✅ | ✅ | |
| ogre | ✅ | ✅ | |
| orc_shaman | ✅ | ✅ | |
| orc_warrior | ✅ | ✅ | |
| pumpkin_dude | ✅ | ✅ | |
| skelet | ✅ | ✅ | |
| tiny_zombie | ✅ | ✅ | |
| wizzard_f | ✅ | ✅ | ✅ |
| wizzard_m | ✅ | ✅ | ✅ |
| wogol | ✅ | ✅ | |

> hit 애니가 있는 8종(dwarf_f/m, elf_f/m, knight_f/m, lizard_f/m, wizzard_f/m)은 0x72 세트에서 플레이어블 종족으로 분류된 애들 — 플레이어 캐릭터 후보로 보임.

## 2. 몬스터 — 단일 애니메이션만 (idle 전용, run 없음) — 7종
SpriteSet + Prefab + Controller 있음, 애니는 1개(`_anim`)뿐.

- ice_zombie
- muddy
- necromancer
- slug
- swampy
- tiny_slug
- zombie

## 3. 오브젝트/기믹 — 인터랙션 애니 (open 등) — 6종
Prefab + Controller 있음, 애니는 상호작용용 1개.

| 이름 | 애니 | 비고 |
|---|---|---|
| bomb | bomb_anim | |
| chest_empty | chest_empty_open | |
| chest_full | chest_full_open | |
| chest_mimic | chest_mimic_open | 몬스터 성격의 기믹 (상자 위장 몬스터) |
| coin | coin_anim | |
| floor_spikes | floor_spikes_anim | |

## 4. 환경 기믹 (분수) — 4종
Prefab + Controller + 흐르는 물 애니.

- wall_fountain_basin_blue
- wall_fountain_basin_red
- wall_fountain_mid_blue
- wall_fountain_mid_red

> `wall_fountain_top_1/2/3`은 애니/프리팹 없이 SpriteSet만 존재 (아래 5번 환경 타일에 포함) — 분수 상단 장식은 정적.

## 5. 환경 타일 — SpriteSet만 있음 (애니/프리팹 없음) — 58종
맵 배경/구조물용 정적 타일. 코드에서 직접 스프라이트만 참조해서 배치하는 용도로 보임.

**바닥/구조**
- floor_1 ~ floor_8, floor_ladder, floor_stairs, hole, muddy는 위 2번 참고(제외)

**문/기둥/장치**
- doors_frame_left / right / top, doors_leaf_closed / open
- column, column_wall, crate
- lever_left, lever_right
- edge_down, skull

**벽 (외곽/모서리/장식)**
- wall_left, wall_right, wall_mid, wall_top_left, wall_top_mid, wall_top_right
- wall_edge_left, wall_edge_right, wall_edge_bottom_left, wall_edge_bottom_right, wall_edge_mid_left, wall_edge_mid_right, wall_edge_top_left, wall_edge_top_right
- wall_edge_tshape_left, wall_edge_tshape_right, wall_edge_tshape_bottom_left, wall_edge_tshape_bottom_right
- wall_outer_front_left, wall_outer_front_right, wall_outer_mid_left, wall_outer_mid_right, wall_outer_top_left, wall_outer_top_right
- wall_hole_1, wall_hole_2, wall_goo, wall_goo_base
- wall_banner_blue, wall_banner_green, wall_banner_red, wall_banner_yellow
- wall_fountain_top_1, wall_fountain_top_2, wall_fountain_top_3

## 6. 무기 아이콘 — SpriteSet만 있음 — 27종
장착용 아이템 아이콘 (스탯만 변화, 스프라이트 변형 없음 원칙과 별개로 무기 자체는 종류별 아이콘 존재).

weapon_anime_sword, weapon_arrow, weapon_axe, weapon_baton_with_spikes, weapon_big_hammer, weapon_bow, weapon_bow_2, weapon_cleaver, weapon_double_axe, weapon_duel_sword, weapon_golden_sword, weapon_green_magic_staff, weapon_hammer, weapon_katana, weapon_knife, weapon_knight_sword, weapon_lavish_sword, weapon_mace, weapon_machete, weapon_red_gem_sword, weapon_red_magic_staff, weapon_regular_sword, weapon_rusty_sword, weapon_saw_sword, weapon_spear, weapon_throwing_axe, weapon_waraxe

## 7. 포션/플라스크 — SpriteSet만 있음 — 8종
색상 4종 × 크기 2종 (일반/big).

- flask_blue, flask_green, flask_red, flask_yellow
- flask_big_blue, flask_big_green, flask_big_red, flask_big_yellow

## 8. UI — SpriteSet만 있음 — 7종

- button_blue_up, button_blue_down, button_red_up, button_red_down
- ui_heart_empty, ui_heart_half, ui_heart_full

---

## 요약 (개수 검증)
| 분류 | 개수 |
|---|---|
| 1. 몬스터/캐릭터(풀 애니) | 25 |
| 2. 몬스터(단일 애니) | 7 |
| 3. 오브젝트/기믹 | 6 |
| 4. 환경 기믹(분수) | 4 |
| 5. 환경 타일(스프라이트만) | 58 |
| 6. 무기 아이콘 | 27 |
| 7. 포션/플라스크 | 8 |
| 8. UI | 7 |
| **SpriteSet 합계** | **142** ✅ |

Prefab 42개 = (1)25 + (2)7 + (3)6 + (4)4 = 42 ✅
Animation 파일 119개 = Controller 42개 + Clip 77개(idle/run/hit/anim/open 조합)

---
*생성일: 2026-08-24, `C:\Users\user\Documents\ForGit\202202825\Assets\Generated` 기준*
