// ================================================================================================
// [Summary] EquipmentSlotType
// 장비가 들어갈 수 있는 슬롯 종류(2026-09-04 신규, Dev Sequence 3단계 착수). ITEM_SYSTEM_DESIGN.md
// 1장 슬롯 구조를 그대로 옮긴 것 — 무기2/방패/상의는 버티컬 슬라이스 범위 안이라 실제 전투
// 보너스가 있는 아이템이 자동 장착되고, Ring1/Ring2/Necklace는 VERTICAL_SLICE_SCOPE.md에서
// "추가 업데이트로 연기"로 명시적으로 범위 밖 처리된 항목이라(사용자 확정) 슬롯 자체(빈 칸)만
// 인벤토리 UI에 미리 준비해둔 상태다 — 실제 반지/목걸이 아이템(고유 스탯/능력)은 아직 없다.
// 나중에 그 범위가 열리면 EquipmentItemDefinition에 반지용 스탯 필드(힘/체력/속도/방어력/
// 치명타 — ITEM_SYSTEM_DESIGN.md 3장)를 추가하고 실제 아이템을 만들면 된다.
// ================================================================================================
public enum EquipmentSlotType
{
    MainWeapon1,
    MainWeapon2,
    OffHandShield,
    Armor,
    Ring1,
    Ring2,
    Necklace,
}
