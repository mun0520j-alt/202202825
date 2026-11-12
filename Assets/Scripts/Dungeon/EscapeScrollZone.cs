using UnityEngine;
using UnityEngine.Tilemaps;

// M7 탈출 스크롤이 활성화하는 고정 지점. 규칙 상태는 후속 스크롤 처리기가 소유하고,
// 이 컴포넌트는 시작 방 위치와 시각 표현만 담당한다.
public sealed class EscapeScrollZone : MonoBehaviour
{
    public static EscapeScrollZone Active { get; private set; }

    public Tilemap Floor { get; private set; }
    public Vector3Int CenterCell { get; private set; }
    public int RoomId { get; private set; }
    private SpriteRenderer zoneRenderer;
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
        name = $"EscapeScrollZone_F{ExpeditionSession.CurrentFloor}";

        zoneRenderer = gameObject.AddComponent<SpriteRenderer>();
        zoneRenderer.sprite = sprite != null ? sprite : ProceduralSprite.GetWhiteSpriteCenterPivot();
        zoneRenderer.color = sprite != null
            ? new Color(0.3f, 1f, 0.65f, 0.55f)
            : new Color(0.15f, 0.9f, 0.5f, 0.22f);
        zoneRenderer.sortingOrder = 1;
        transform.localScale = sprite != null ? new Vector3(2.4f, 2.4f, 1f) : new Vector3(3f, 3f, 1f);
    }

    public bool Contains(Vector3Int cell)
    {
        return Mathf.Abs(cell.x - CenterCell.x) <= 1 && Mathf.Abs(cell.y - CenterCell.y) <= 1;
    }

    public static bool TryUseEscapeScroll(string instanceId, out string failure)
    {
        failure = null;
        EscapeScrollZone zone = Active;
        PlayerTurnActor player = PlayerTurnActor.Instance;
        if (zone == null || player == null || player.FloorTilemap != zone.Floor)
        {
            failure = "이 층의 탈출 구역을 찾을 수 없습니다.";
            return false;
        }
        if (zone.activated)
        {
            failure = "탈출 구역이 이미 활성화되어 있습니다.";
            return false;
        }
        if (!zone.Contains(player.CurrentCell))
        {
            failure = "탈출 구역 안에서만 사용할 수 있습니다.";
            return false;
        }
        if (ExpeditionSession.CurrentFloor == 5 && GameObject.Find("Boss_F5") != null)
        {
            failure = "보스를 처치해야 탈출 구역을 활성화할 수 있습니다.";
            return false;
        }

        bool spent = player.TryPerformTimedAction(M7EconomyConfig.EscapeScrollUseTicks, "UseEscapeScroll", () =>
        {
            if (!LootRuntimeState.TryConsumeRunBagItem(instanceId, "escape_scroll")) return false;
            zone.Activate();
            return true;
        });
        if (!spent) failure = LootRuntimeState.LastFailureReason ?? "지금은 스크롤을 사용할 수 없습니다.";
        return spent;
    }

    public static void PrepareForAction(PlayerTurnActor player)
    {
        EscapeScrollZone zone = Active;
        if (zone == null || !zone.activated || zone.completionRequested || player == null) return;
        zone.RefreshState(player);
        if (zone.activated && zone.Contains(player.CurrentCell) && zone.progressStartTick < 0f)
            zone.progressStartTick = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
    }

    public static bool TryResolveCompleted(PlayerTurnActor player)
    {
        EscapeScrollZone zone = Active;
        if (zone == null || !zone.activated || zone.completionRequested || player == null || !player.IsReadyForInput) return false;
        zone.RefreshState(player);
        if (!zone.activated || zone.progressStartTick < 0f || !zone.Contains(player.CurrentCell)) return false;
        float now = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
        if (now - zone.progressStartTick < M7EconomyConfig.EscapeScrollStayTicks) return false;
        zone.completionRequested = ExpeditionManager.Instance != null && ExpeditionManager.Instance.TryCompleteScrollExtraction(player);
        return zone.completionRequested;
    }

    private void Activate()
    {
        float now = ExpeditionManager.Instance?.ElapsedTicks ?? 0f;
        activated = true;
        activationTick = now;
        progressStartTick = now;
        zoneRenderer.color = new Color(0.25f, 1f, 0.5f, 0.95f);
        Debug.Log($"[EscapeScroll] 활성화 — {M7EconomyConfig.EscapeScrollStayTicks:0} Tick 체류, {M7EconomyConfig.EscapeScrollLifetimeTicks:0} Tick 후 소멸.");
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
        zoneRenderer.color = new Color(0.3f, 1f, 0.65f, 0.55f);
        Debug.Log($"[EscapeScroll] 비활성화 — {reason}");
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
            $"탈출 구역 — 체류 {remainStay:0} Tick / 소멸 {remainLife:0} Tick");
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }
}
