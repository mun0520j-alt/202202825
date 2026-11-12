using UnityEngine;
using UnityEngine.Tilemaps;

// M7 귀환 스크롤이 활성화하는 고정 지점. 규칙 상태는 후속 스크롤 처리기가 소유하고,
// 이 컴포넌트는 시작 방 위치와 시각 표현만 담당한다.
public sealed class ReturnRitualAnchor : MonoBehaviour
{
    public static ReturnRitualAnchor Active { get; private set; }

    public Tilemap Floor { get; private set; }
    public Vector3Int CenterCell { get; private set; }
    public int RoomId { get; private set; }
    private SpriteRenderer ritualRenderer;
    private bool activated;
    private bool completionRequested;
    private float activationTick = -1f;
    private float progressStartTick = -1f;

    public void Initialize(Tilemap floor, Vector3Int centerCell, Sprite sprite)
    {
        Floor = floor;
        CenterCell = centerCell;
        RoomId = DungeonRoomRegistry.GetRoomId(floor, centerCell);
        Active = this;

        transform.position = floor.GetCellCenterWorld(centerCell);
        name = $"ReturnRitual_F{ExpeditionSession.CurrentFloor}";

        ritualRenderer = gameObject.AddComponent<SpriteRenderer>();
        ritualRenderer.sprite = sprite != null ? sprite : ProceduralSprite.GetWhiteSpriteCenterPivot();
        ritualRenderer.color = sprite != null
            ? new Color(0.3f, 1f, 0.65f, 0.55f)
            : new Color(0.15f, 0.9f, 0.5f, 0.22f);
        ritualRenderer.sortingOrder = 1;
        transform.localScale = sprite != null ? new Vector3(2.4f, 2.4f, 1f) : new Vector3(3f, 3f, 1f);
    }

    public bool Contains(Vector3Int cell)
    {
        return Mathf.Abs(cell.x - CenterCell.x) <= 1 && Mathf.Abs(cell.y - CenterCell.y) <= 1;
    }

    public static bool TryUseEscapeScroll(string instanceId, out string failure)
    {
        failure = null;
        ReturnRitualAnchor ritual = Active;
        PlayerTurnActor player = PlayerTurnActor.Instance;
        if (ritual == null || player == null || player.FloorTilemap != ritual.Floor)
        {
            failure = "이 층의 귀환진을 찾을 수 없습니다.";
            return false;
        }
        if (ritual.activated)
        {
            failure = "귀환진이 이미 활성화되어 있습니다.";
            return false;
        }
        if (!ritual.Contains(player.CurrentCell))
        {
            failure = "귀환진 안에서만 사용할 수 있습니다.";
            return false;
        }
        if (ExpeditionSession.CurrentFloor == 5 && GameObject.Find("Boss_F5") != null)
        {
            failure = "보스를 처치해야 귀환진을 활성화할 수 있습니다.";
            return false;
        }

        bool spent = player.TryPerformTimedAction(M7EconomyConfig.EscapeScrollUseTicks, "UseEscapeScroll", () =>
        {
            if (!LootRuntimeState.TryConsumeRunBagItem(instanceId, "escape_scroll")) return false;
            ritual.Activate();
            return true;
        });
        if (!spent) failure = LootRuntimeState.LastFailureReason ?? "지금은 스크롤을 사용할 수 없습니다.";
        return spent;
    }

    public static void PrepareForAction(PlayerTurnActor player)
    {
        ReturnRitualAnchor ritual = Active;
        if (ritual == null || !ritual.activated || ritual.completionRequested || player == null) return;
        ritual.RefreshState(player);
        if (ritual.activated && ritual.Contains(player.CurrentCell) && ritual.progressStartTick < 0f)
            ritual.progressStartTick = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
    }

    public static bool TryResolveCompleted(PlayerTurnActor player)
    {
        ReturnRitualAnchor ritual = Active;
        if (ritual == null || !ritual.activated || ritual.completionRequested || player == null || !player.IsReadyForInput) return false;
        ritual.RefreshState(player);
        if (!ritual.activated || ritual.progressStartTick < 0f || !ritual.Contains(player.CurrentCell)) return false;
        float now = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
        if (now - ritual.progressStartTick < M7EconomyConfig.EscapeScrollStayTicks) return false;
        ritual.completionRequested = ExpeditionManager.Instance != null && ExpeditionManager.Instance.TryCompleteScrollExtraction(player);
        return ritual.completionRequested;
    }

    private void Activate()
    {
        float now = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
        activated = true;
        activationTick = now;
        progressStartTick = now;
        ritualRenderer.color = new Color(0.25f, 1f, 0.5f, 0.95f);
        Debug.Log($"[ReturnRitual] 활성화 — {M7EconomyConfig.EscapeScrollStayTicks:0} Tick 체류, {M7EconomyConfig.EscapeScrollLifetimeTicks:0} Tick 후 소멸.");
    }

    private void RefreshState(PlayerTurnActor player)
    {
        float now = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
        if (now - activationTick >= M7EconomyConfig.EscapeScrollLifetimeTicks)
        {
            Deactivate("50 Tick 만료");
            return;
        }
        if (!Contains(player.CurrentCell)) progressStartTick = -1f;
    }

    private void Deactivate(string reason)
    {
        activated = false;
        activationTick = -1f;
        progressStartTick = -1f;
        ritualRenderer.color = new Color(0.3f, 1f, 0.65f, 0.55f);
        Debug.Log($"[ReturnRitual] 비활성화 — {reason}");
    }

    private void Update()
    {
        if (!activated || completionRequested) return;
        PlayerTurnActor player = PlayerTurnActor.Instance;
        if (player != null) RefreshState(player);
    }

    private void OnGUI()
    {
        if (!activated || completionRequested) return;
        PlayerTurnActor player = PlayerTurnActor.Instance;
        if (player == null || !Contains(player.CurrentCell)) return;
        float now = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
        float stayed = progressStartTick < 0f ? 0f : now - progressStartTick;
        float remainStay = Mathf.Max(0f, M7EconomyConfig.EscapeScrollStayTicks - stayed);
        float remainLife = Mathf.Max(0f, M7EconomyConfig.EscapeScrollLifetimeTicks - (now - activationTick));
        GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height - 90f, 380f, 40f),
            $"귀환진 — 체류 {remainStay:0} Tick / 소멸 {remainLife:0} Tick");
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }
}
