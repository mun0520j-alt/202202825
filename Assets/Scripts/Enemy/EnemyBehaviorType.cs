// Enemy가 자기 턴(OnTurnStart)에 어떤 기본 행동을 할지 결정하는 타입. 종류가 늘어나면
// (추적/순찰/도주 등) 여기 추가하고 EnemyTurnActor.OnTurnStart()의 switch에 분기를 더한다.
// 지금은 Tick 파이프라인 검증용 약식 구현이라 Idle/Wander 두 가지만 둔다(2026-08-28).
public enum EnemyBehaviorType
{
    Idle,   // 제자리에 가만히 있는다 — 위치는 그대로, tick만 소비한다.
    Wander, // 4방향 중 무작위로 한 칸씩 배회한다.
}
