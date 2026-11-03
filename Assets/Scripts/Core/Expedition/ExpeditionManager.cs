using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 원정 결과 판정의 단일 진입점. 생환/사망/MIA가 같은 프레임에 중복 처리되는 것을 막는다.
public sealed class ExpeditionManager : MonoBehaviour
{
    public static ExpeditionManager Instance { get; private set; }

    [Header("미확정 임시값 (Inspector에서 조정)")]
    [SerializeField, Min(1f)] private float floorChangeTickCost = TickCost.Interact;
    [SerializeField, Min(1f)] private float escapeProcedureTickCost = 10f;

    private DungeonClock dungeonClock;
    private bool transitionRequested;

    public float FloorChangeTickCost => floorChangeTickCost;
    public float EscapeProcedureTickCost => escapeProcedureTickCost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (dungeonClock != null)
        {
            dungeonClock.OnClockChanged -= HandleClockChanged;
            dungeonClock.OnMIA -= HandleMIA;
        }
        if (Instance == this) Instance = null;
    }

    public void Initialize(DungeonClock clock)
    {
        dungeonClock = clock;
        dungeonClock.OnClockChanged -= HandleClockChanged;
        dungeonClock.OnClockChanged += HandleClockChanged;
        dungeonClock.OnMIA -= HandleMIA;
        dungeonClock.OnMIA += HandleMIA;
    }

    public bool TryUseFloorExit(PlayerTurnActor player)
    {
        if (transitionRequested || player == null || !ExpeditionSession.IsActive) return false;

        float cost = ExpeditionSession.CurrentFloor < ExpeditionSession.FinalFloor
            ? floorChangeTickCost
            : escapeProcedureTickCost;
        if (!player.TrySpendInteraction(cost)) return false;

        transitionRequested = true;
        StartCoroutine(ResolveExitAfterTick());
        return true;
    }

    public void CompleteDeath()
    {
        if (transitionRequested || !ExpeditionSession.IsActive) return;
        transitionRequested = true;
        ExpeditionSession.Complete(ExpeditionResult.Dead);
        SceneManager.LoadScene("BaseCamp");
    }

    private IEnumerator ResolveExitAfterTick()
    {
        // CompleteTurn/OnMIA 호출 스택이 모두 끝난 다음 씬을 바꾼다.
        yield return null;
        if (!ExpeditionSession.IsActive) yield break; // 절차 비용으로 MIA가 된 경우

        if (ExpeditionSession.CurrentFloor >= ExpeditionSession.FinalFloor)
        {
            ExpeditionSession.Complete(ExpeditionResult.Escaped);
            SceneManager.LoadScene("BaseCamp");
            yield break;
        }

        ExpeditionSession.TryAdvanceFloor();
        SceneManager.LoadScene("InDungeon");
    }

    private void HandleClockChanged(float elapsedTicks)
    {
        ExpeditionSession.RecordElapsedTicks(elapsedTicks);
    }

    private void HandleMIA()
    {
        if (transitionRequested || !ExpeditionSession.IsActive) return;
        transitionRequested = true;
        ExpeditionSession.RecordElapsedTicks(dungeonClock.ElapsedTicks);
        ExpeditionSession.Complete(ExpeditionResult.MIA);
        StartCoroutine(LoadBaseCampNextFrame());
    }

    private static IEnumerator LoadBaseCampNextFrame()
    {
        yield return null;
        SceneManager.LoadScene("BaseCamp");
    }
}
