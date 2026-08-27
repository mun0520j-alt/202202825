// Dungeon Tools 7) Tick 스케줄러(TickManager) 큐에 참여하는 모든 행동 주체(Player/Enemy 공통)가
// 구현하는 인터페이스. "차례가 오면 콜백을 받는다"는 계약만 정의하고, 실제로 뭘 할지(입력 대기 /
// AI 판단)는 구현체 책임이다 — 이 인터페이스는 그 판단 로직을 전혀 모른다.
public interface ITurnActor
{
    // true면 TickManager의 "너무 오래 CompleteTurn을 안 부름" 경고(워치독)를 이 액터에 한해 끈다.
    // 플레이어처럼 사용자 입력을 기다리느라 실제로 몇 초~몇 분씩 걸릴 수 있는 액터는 true로 둔다.
    // AI처럼 차례가 오면 즉시 판단해서 바로 CompleteTurn을 불러야 하는 액터는 false로 둬서,
    // 호출을 빠뜨리는 버그가 생기면 바로 경고 로그가 뜨게 한다.
    bool SuppressStuckTurnWarning { get; }

    // 이 액터의 차례가 됐을 때 TickManager가 호출한다.
    // 구현체는 행동을 정하고 나면 반드시 TickManager.Instance.CompleteTurn(this, cost)를 호출해서
    // 차례를 다음 액터에게 넘겨줘야 한다 — 호출을 빠뜨리면 스케줄러가 이 액터에서 멈춘다.
    void OnTurnStart();
}
