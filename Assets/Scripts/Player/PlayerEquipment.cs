using System;
using System.Collections.Generic;
using UnityEngine;

// ================================================================================================
// [Summary] PlayerEquipment
// Player가 지금 각 슬롯에 뭘 장착하고 있는지 들고 있는 컴포넌트(2026-09-04 신규, Dev Sequence
// 3단계 착수 — 사용자 확정: "가벼운 ScriptableObject 장비 데이터"). PlayerTurnActor의 전투
// 계산(공격력-방어력)에 필요한 "장비 보너스 합계"만 계산해서 내어주는 역할 — 실제 데미지 공식/
// HP 처리는 여전히 PlayerTurnActor 책임이다(단일 책임 유지, HP바/알림표시와 같은 패턴으로
// "붙이면 끝"인 컴포넌트).
//
// [2026-09-04] 아직 루팅/상점 등 "진짜로 아이템을 얻는 경로"가 하나도 없어서(3단계 착수 초기),
// 인스펙터에 아무 것도 안 꽂아놨으면 Awake에서 테스트용 기본 장비(기본 검/낡은 방패/가죽 갑옷)를
// 런타임에 직접 만들어서 자동 장착해준다 — 그래야 인벤토리 UI에 뭐라도 채워진 상태를 바로
// 눈으로 확인할 수 있다. 나중에 진짜 아이템(.asset)이 생기면 인스펙터의 starting* 필드에 그
// 에셋을 꽂기만 하면 되고, 이 자동 생성 로직은 "아무 것도 안 꽂았을 때의 폴백"이라 그때는
// 조용히 안 쓰이게 된다.
// ================================================================================================
public class PlayerEquipment : MonoBehaviour
{
    [Header("시작 장비 (비워두면 테스트용 기본 장비를 자동 생성해서 장착)")]
    [SerializeField] private EquipmentItemDefinition startingMainWeapon1;
    [SerializeField] private EquipmentItemDefinition startingOffHandShield;
    [SerializeField] private EquipmentItemDefinition startingArmor;

    private readonly Dictionary<EquipmentSlotType, EquipmentItemDefinition> equippedItems =
        new Dictionary<EquipmentSlotType, EquipmentItemDefinition>();

    // icon이 없는 테스트용 기본 장비의 대표색을 따로 들고 있는 표 — EquipmentItemDefinition
    // 자체에 "디버그용 색상" 필드를 추가하면 나중에 실제 아이템 에셋에도 안 쓰는 필드가
    // 영구히 붙어있게 되니, 여기(자동 생성 폴백 쪽)에서만 보조적으로 관리한다.
    private readonly Dictionary<EquipmentItemDefinition, Color> fallbackColors =
        new Dictionary<EquipmentItemDefinition, Color>();

    // 슬롯 구성이 바뀔 때마다(장착/해제) 발행 — InventoryPanel이 이걸 구독해서 아이콘만 갱신한다.
    public event Action OnEquipmentChanged;

    public int TotalAttackBonus { get; private set; }
    public int TotalDefenseBonus { get; private set; }

    private void Awake()
    {
        Equip(startingMainWeapon1 != null ? startingMainWeapon1 : CreateDefaultItem(
            "기본 검", EquipmentSlotType.MainWeapon1, attackBonus: 2, defenseBonus: 0, new Color(0.85f, 0.35f, 0.25f)));

        Equip(startingOffHandShield != null ? startingOffHandShield : CreateDefaultItem(
            "낡은 방패", EquipmentSlotType.OffHandShield, attackBonus: 0, defenseBonus: 1, new Color(0.3f, 0.55f, 0.9f)));

        Equip(startingArmor != null ? startingArmor : CreateDefaultItem(
            "가죽 갑옷", EquipmentSlotType.Armor, attackBonus: 0, defenseBonus: 1, new Color(0.4f, 0.75f, 0.35f)));

        // MainWeapon2는 의도적으로 비워둔다 — 무기 스왑(ITEM_SYSTEM_DESIGN.md 2장, 전투중 1tick/
        // 비전투 무료)은 아직 범위 밖이라, 지금은 슬롯이 "존재는 하지만 비어있는" 상태로만 둔다.
    }

    // 슬롯 하나에 아이템을 장착(또는 교체)한다 — 같은 슬롯에 이미 있던 건 그냥 덮어써서 버려진다
    // (인벤토리로 돌아가는 "해제" 개념은 아직 범위 밖, ITEM_SYSTEM_DESIGN.md 9장 미정 항목과 연동).
    public void Equip(EquipmentItemDefinition item)
    {
        if (item == null) return;

        equippedItems[item.slotType] = item;
        RecalculateBonuses();
        OnEquipmentChanged?.Invoke();
    }

    public EquipmentItemDefinition GetEquipped(EquipmentSlotType slot)
    {
        return equippedItems.TryGetValue(slot, out var item) ? item : null;
    }

    // icon이 없는 아이템(지금은 자동 생성된 테스트용 기본 장비뿐)을 그릴 때 쓸 대표색.
    // 실제 아이템(icon이 채워진 것)에 대해 호출하면 의미 없는 회색이 나오니, InventoryPanel
    // 쪽에서 icon == null인 경우에만 이 값을 쓰도록 구분해서 사용한다.
    public Color GetFallbackColor(EquipmentItemDefinition item)
    {
        return fallbackColors.TryGetValue(item, out var color) ? color : Color.gray;
    }

    private void RecalculateBonuses()
    {
        int attack = 0;
        int defense = 0;
        foreach (var item in equippedItems.Values)
        {
            attack += item.attackPowerBonus;
            defense += item.defensePowerBonus;
        }
        TotalAttackBonus = attack;
        TotalDefenseBonus = defense;
    }

    // 테스트용 기본 장비를 에셋 파일 없이 메모리에서만 만든다(ScriptableObject.CreateInstance).
    private EquipmentItemDefinition CreateDefaultItem(string name, EquipmentSlotType slot, int attackBonus, int defenseBonus, Color placeholderColor)
    {
        var item = ScriptableObject.CreateInstance<EquipmentItemDefinition>();
        item.itemName = name;
        item.slotType = slot;
        item.attackPowerBonus = attackBonus;
        item.defensePowerBonus = defenseBonus;
        item.icon = null;
        fallbackColors[item] = placeholderColor;
        return item;
    }
}
