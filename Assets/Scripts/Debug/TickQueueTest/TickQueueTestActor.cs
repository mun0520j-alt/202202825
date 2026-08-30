using UnityEngine;

// Dungeon Tools 8) Tick 스케줄러(TickManager) 검증용 더미 액터. 실제 게임 로직 없이 "내 차례가
// 되면 정해진 tick 비용만큼 소비하고 바로 다음 차례로 넘긴다"만 반복해서, 큐가 액터별 tick
// 비용(속도) 차이를 실제로 어떻게 반영하는지 콘솔 로그로 눈으로 확인하기 위한 용도.
// 게임 릴리즈 로직이 아니라 Test Scene 검증 전용 — Assets/Scripts/Debug/ 아래에 둔다.
public class TickQueueTestActor : MonoBehaviour, ITurnActor
{
    private string actorLabel = "TestActor";
    private float tickCostPerTurn = TickCost.PerTileMove;
    private TickQueueTestBootstrapper bootstrapper;

    // 이 더미 액터는 AI처럼 즉시 판단해서 바로 CompleteTurn을 부르는 성격이라 워치독 감시 대상으로 둔다
    // (false = 감시함) — 마지막 턴에서 의도적으로 CompleteTurn을 생략해서, 워치독이 실제로 버그
    // 리포트를 찍는지도 이 액터로 같이 검증한다(TickQueueTestBootstrapper 참고).
    public bool SuppressStuckTurnWarning => false;

    // ITurnActor.CurrentCell 구현 — 이 더미 액터는 실제 맵 위를 이동하지 않는 순수 스케줄러
    // 검증용이라 진짜 좌표가 없다. TickManager.IsCellOccupied()가 셀 점유 조회 때 모든 등록된
    // 액터를 순회하며 이 값을 읽긴 하지만(2026-08-28 추가, ITurnActor.cs 주석 참고), 이 더미가
    // Test Scene에서 Player/Enemy와 같이 등록되는 일은 없어서(TickQueueTestBootstrapper 전용
    // 시나리오) 항상 원점을 반환해도 실제로 영향은 없다.
    public Vector3Int CurrentCell => Vector3Int.zero;

    public void Initialize(TickQueueTestBootstrapper owner, string label, float costPerTurn)
    {
        bootstrapper = owner;
        actorLabel = label;
        tickCostPerTurn = costPerTurn;
    }

    public void OnTurnStart()
    {
        bool reachedStopCondition = bootstrapper != null && bootstrapper.RegisterTurnAndCheckStop(actorLabel);

        if (reachedStopCondition)
        {
            Debug.Log($"[TickQueueTest] {actorLabel} 차례 — 테스트 종료 조건 도달, 일부러 CompleteTurn을 " +
                      "호출하지 않습니다 (TickManager 워치독이 실제로 버그 리포트를 찍는지 확인하는 목적).");
            return; // 의도적으로 CompleteTurn 생략
        }

        Debug.Log($"[TickQueueTest] {actorLabel} 턴 — 비용 {tickCostPerTurn} tick 소비");
        TickManager.Instance.CompleteTurn(this, tickCostPerTurn);
    }
}
