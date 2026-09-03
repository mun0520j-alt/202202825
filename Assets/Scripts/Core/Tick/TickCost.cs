// Dungeon Tools 7) 게임 내 모든 기본 행동의 tick 비용 상수.
//
// [2026-09-03 단위 재정의] 원래 "1tick = 5분 = 이동 5타일"(PerTileMove=0.2)이던 걸 "1tick = 1분
// = 이동 1타일"로 기준 단위를 바꿨다(사용자 제안) — 실제 게임 내 시간(분 단위 밸런스)은 전혀
// 안 바뀌고, 그동안 0.2 같은 소수점으로 다루던 걸 전부 정수로 승격시킨 것뿐이다(예전 1tick=5분
// 기준으로 계산하면 정확히 5배 스케일). NextActionTime을 float 소수점 누적 오차 걱정 없이
// 정수처럼 다룰 수 있게 하려는 목적. DungeonClock.MinutesPerTick도 5 → 1로 같이 바뀌었다.
//
// 이동 비용에 개별 액터의 속도 배율(플레이어 장비, 몹 종류별 이동속도 등)을 곱하는 계산은
// 이 클래스가 아니라 실제로 비용을 계산해서 TickManager.CompleteTurn에 보고하는 쪽(Mover 등)의
// 책임이다 — 여기는 "배율 적용 전 기본값"만 들고 있다.
//
// Attack은 예외적으로 속도 배율의 영향을 받지 않고 항상 이 값 그대로 쓴다 — 확정 사항
// ("실제 공격 tick은 통일해서 밸런스 붕괴 방지").
public static class TickCost
{
    public const float PerTileMove = 1f;     // 1타일 = 1tick(=1분) (기본값, 액터별 배율은 여기 곱해서 사용)
    public const float Attack = 5f;          // =5분(구 1tick과 동일 시간). 속도 배율 영향 없음 — 항상 고정
    public const float SwapWeaponInCombat = 5f; // =5분
    public const float UseItem = 5f;         // 포션/스크롤, =5분
    public const float Interact = 5f;        // 상자/문 개방 등, =5분
}
