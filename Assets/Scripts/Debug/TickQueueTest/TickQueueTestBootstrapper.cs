using UnityEngine;

// Dungeon Tools 8) Test Scene에서 TickManager 큐가 실제로 도는지 확인하기 위한 부트스트래퍼.
// 씬에 빈 GameObject 하나 만들어서 이 컴포넌트만 붙이면, 서로 다른 tick 비용을 가진 더미 액터
// 3개를 만들어서 TickManager에 등록하고 스케줄을 시작한다.
//
// 검증 항목:
//   1) 비용이 작은(빠른) 액터가 실제로 더 자주 차례를 받는지 — Console 로그 순서로 확인
//   2) DungeonClock의 실시계 표시가 tick 소비에 맞춰 갱신되는지
//   3) 정해진 턴 수에 도달하면 마지막 액터가 의도적으로 CompleteTurn을 생략 — 이때
//      TickManager 워치독이 10초 뒤 실제로 버그 리포트(LogError)를 찍는지
//
// 이 스크립트는 검증 전용이라 실제 게임 로직(Player/Enemy)에는 전혀 관여하지 않는다.
public class TickQueueTestBootstrapper : MonoBehaviour
{
    [Tooltip("전체 액터를 통틀어 이 턴 수에 도달하면, 그 차례를 받은 액터가 일부러 CompleteTurn을 생략한다.")]
    [SerializeField] private int stopAfterTotalTurns = 20;

    private int totalTurnsSoFar;

    private void Start()
    {
        var tickManager = TickManager.Instance != null ? TickManager.Instance : gameObject.AddComponent<TickManager>();

        var dungeonClock = GetComponent<DungeonClock>();
        if (dungeonClock == null) dungeonClock = gameObject.AddComponent<DungeonClock>();
        dungeonClock.ResetForNewRun();
        dungeonClock.OnClockChanged += elapsedTicks =>
            Debug.Log($"[TickQueueTest] DungeonClock 갱신 — 누적 {elapsedTicks:0.0} tick, 실시계 {dungeonClock.GetClockString()}");
        dungeonClock.OnMIA += () => Debug.LogWarning("[TickQueueTest] MIA 조건(288 tick) 도달!");

        // [2026-09-03] TickCost 단위 재정의(1tick=1분)에 맞춰 라벨/값 갱신 — 실제 비용은 항상
        // TickCost 상수를 그대로 참조하니 나중에 또 스케일이 바뀌어도 여기서 값이 안 썩는다.
        SpawnTestActor($"Fast({TickCost.PerTileMove}tick/턴)", TickCost.PerTileMove);
        SpawnTestActor($"Medium({TickCost.Attack}tick/턴)", TickCost.Attack);
        SpawnTestActor("Slow(10tick/턴)", 10f);

        tickManager.BeginSchedule();
    }

    private void SpawnTestActor(string label, float costPerTurn)
    {
        var actorObject = new GameObject($"TickQueueTestActor_{label}");
        actorObject.transform.SetParent(transform);
        var actor = actorObject.AddComponent<TickQueueTestActor>();
        actor.Initialize(this, label, costPerTurn);
        TickManager.Instance.RegisterActor(actor);
    }

    // 액터가 자기 턴을 소비하기 직전에 호출 — 전체 턴 수를 세고, 정지 조건 도달 여부를 알려준다.
    public bool RegisterTurnAndCheckStop(string actorLabel)
    {
        totalTurnsSoFar++;
        return totalTurnsSoFar >= stopAfterTotalTurns;
    }
}
