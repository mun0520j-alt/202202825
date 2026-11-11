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
    private bool miaResolutionPending;
    private bool deathResolutionPending;
    private PlayerTurnActor exitProcedurePlayer;

    public float FloorChangeTickCost => floorChangeTickCost;
    public float EscapeProcedureTickCost => escapeProcedureTickCost;
    public float ElapsedTicks => dungeonClock != null ? dungeonClock.ElapsedTicks : ExpeditionSession.ElapsedTicks;

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
        if (ExpeditionSession.CurrentFloor >= ExpeditionSession.FinalFloor) return false;

        if (!player.TrySpendInteraction(floorChangeTickCost)) return false;

        transitionRequested = true;
        exitProcedurePlayer = player;
        StartCoroutine(ResolveExitAfterTick());
        return true;
    }

    public bool TryCompleteExtraction(PlayerTurnActor player)
    {
        if (transitionRequested || player == null || player.IsDead || !ExpeditionSession.IsActive) return false;
        if (ExpeditionSession.CurrentFloor != 3 && ExpeditionSession.CurrentFloor != 5) return false;
        if (miaResolutionPending || (dungeonClock != null && dungeonClock.IsMIA)) return false;

        transitionRequested = true;
        ExpeditionSession.RecordElapsedTicks(ElapsedTicks);
        ExpeditionSession.Complete(ExpeditionResult.Escaped);
        SceneManager.LoadScene("BaseCamp");
        return true;
    }

    public void CompleteDeath()
    {
        if (!ExpeditionSession.IsActive) return;
        deathResolutionPending = false;
        transitionRequested = true;
        Debug.Log($"[DeathFlow] Dead 결과 확정 — {ExpeditionSession.CurrentFloor}층, {ElapsedTicks:0} Tick");
        ExpeditionSession.Complete(ExpeditionResult.Dead);
        SceneManager.LoadScene("BaseCamp");
    }

    public void RequestDeathResolution()
    {
        if (!ExpeditionSession.IsActive || deathResolutionPending) return;
        deathResolutionPending = true;
        StartCoroutine(ResolveDeathNextFrame());
    }

    private IEnumerator ResolveDeathNextFrame()
    {
        yield return null;
        if (!ExpeditionSession.IsActive) yield break;
        var player = PlayerTurnActor.Instance;
        if (player != null && player.IsDead) CompleteDeath();
        else deathResolutionPending = false;
    }

    private IEnumerator ResolveExitAfterTick()
    {
        // 비용만 지불하고 다음 프레임에 생환시키지 않는다. 해당 시간만큼 적 턴이 처리되고
        // 플레이어 차례가 다시 돌아오거나 사망/MIA가 확정될 때까지 완료 판정을 보류한다.
        do
        {
            yield return null;
        }
        while (ExpeditionSession.IsActive && exitProcedurePlayer != null &&
               !exitProcedurePlayer.IsDead && !exitProcedurePlayer.IsReadyForInput);

        if (!ExpeditionSession.IsActive) yield break;
        if (exitProcedurePlayer != null && exitProcedurePlayer.IsDead)
        {
            CompleteDeath();
            yield break;
        }

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
        if (!ExpeditionSession.IsActive || miaResolutionPending) return;
        miaResolutionPending = true;
        PlayerTurnActor.Instance?.LockInputForResultResolution();
        StartCoroutine(ResolveMIAAfterScheduledActions());
    }

    private IEnumerator ResolveMIAAfterScheduledActions()
    {
        var player = PlayerTurnActor.Instance;
        while (ExpeditionSession.IsActive && player != null && !player.IsDead && !player.IsReadyForInput)
        {
            yield return null;
        }

        if (!ExpeditionSession.IsActive) yield break;
        if (player != null && player.IsDead)
        {
            CompleteDeath();
            yield break;
        }

        transitionRequested = true;
        ExpeditionSession.RecordElapsedTicks(dungeonClock.ElapsedTicks);
        ExpeditionSession.Complete(ExpeditionResult.MIA);
        SceneManager.LoadScene("BaseCamp");
    }
}
