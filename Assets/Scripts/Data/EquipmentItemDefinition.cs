using UnityEngine;

// ================================================================================================
// [Summary] EquipmentItemDefinition
// 장비 아이템 하나를 나타내는 데이터 컨테이너(2026-09-04 신규, Dev Sequence 3단계 착수 —
// 사용자 확정: "가벼운 ScriptableObject 장비 데이터"). ITEM_SYSTEM_DESIGN.md 1장 슬롯 구조 중
// 버티컬 슬라이스 범위(VERTICAL_SLICE_SCOPE.md — 무기+방패+상의만, 반지/포션/목걸이/스크롤/열쇠는
// 이번 범위 밖)에 해당하는 부분만 우선 코드로 옮긴다.
//
// "장착해도 스탯만 변화, 스프라이트 변형 없음"이라는 프로젝트 원칙(PROJECT_PLAN.md 0장)에 따라
// 데미지 공식(공격력-방어력)에 바로 꽂을 수 있는 단순 보너스 값만 들고 있다 — 무기 스킬, 등급/
// 재질 시스템 등은 전부 이후 범위(ITEM_SYSTEM_DESIGN.md 9장 미정 항목 참고).
//
// [CreateAssetMenu]를 붙여서 나중에 실제 아이콘/수치가 정해지면 에디터에서 진짜 .asset 파일로
// 만들 수 있게 해뒀다 — 지금 당장은 PlayerEquipment가 테스트용 기본 장비를 런타임에 코드로
// 직접 만들어서 쓰고 있어서(에셋 파일 없이도 바로 확인 가능), 에셋 작업 자체는 필수가 아니다.
// ================================================================================================
[CreateAssetMenu(menuName = "Dungeon/Equipment Item", fileName = "NewEquipmentItem")]
public class EquipmentItemDefinition : ScriptableObject
{
    [Tooltip("인벤토리/장비창에 표시할 이름.")]
    public string itemName = "이름 없음";

    [Tooltip("인벤토리 슬롯에 표시할 아이콘 — 비워두면 절차적 색상 사각형으로 대체된다(InventoryPanel/PlayerEquipment 참고).")]
    public Sprite icon;

    [Tooltip("이 아이템이 들어갈 수 있는 슬롯 종류.")]
    public EquipmentSlotType slotType;

    [Header("전투 보너스 (공격력-방어력 공식에 그대로 더해짐, PlayerTurnActor.EffectiveAttackPower/EffectiveDefensePower 참고)")]
    public int attackPowerBonus;
    public int defensePowerBonus;
}
