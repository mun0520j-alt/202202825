// Dungeon Tools 7) 게임 내 모든 기본 행동의 tick 비용 상수 — MAP_GENERATOR_DESIGN.md 2장에서
// 확정된 값. 스킬만 예외적으로 스킬별 고유 비용을 가지므로 여기 포함하지 않는다(향후 스킬
// 데이터 쪽에서 개별 관리).
//
// 이동 비용에 개별 액터의 속도 배율(플레이어 장비, 몹 종류별 이동속도 등)을 곱하는 계산은
// 이 클래스가 아니라 실제로 비용을 계산해서 TickManager.CompleteTurn에 보고하는 쪽(Mover 등)의
// 책임이다 — 여기는 "배율 적용 전 기본값"만 들고 있다.
//
// Attack은 예외적으로 속도 배율의 영향을 받지 않고 항상 이 값 그대로 쓴다 — 확정 사항
// ("실제 공격 tick은 통일해서 밸런스 붕괴 방지").
public static class TickCost
{
    public const float PerTileMove = 0.2f;   // 5타일 = 1tick (기본값, 액터별 배율은 여기 곱해서 사용)
    public const float Attack = 1f;          // 속도 배율 영향 없음 — 항상 고정
    public const float SwapWeaponInCombat = 1f;
    public const float UseItem = 1f;         // 포션/스크롤
    public const float Interact = 1f;        // 상자/문 개방 등
}
