using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 일반 계단은 해당 칸에서 E로 사용하고, 최종 탈출구는 주변 1칸의 대기 구역에 들어가면 자동 시작한다.
public sealed class DungeonStairs : MonoBehaviour
{
    private static readonly Dictionary<Vector3Int, DungeonStairs> stairsByCell = new Dictionary<Vector3Int, DungeonStairs>();
    private Vector3Int cell;
    private bool isFinalExit;
    private bool requestSent;
    private float extractionStartTick = -1f;
    private Tilemap floor;
    public Vector3Int Cell => cell;
    public Tilemap FloorTilemap => floor;
    public bool IsFinalExit => isFinalExit;

    public void Initialize(Tilemap floorTilemap, Vector3Int targetCell, bool finalExit)
    {
        floor = floorTilemap;
        cell = targetCell;
        isFinalExit = finalExit;
        stairsByCell[cell] = this;
        transform.position = floorTilemap.GetCellCenterWorld(cell);
        gameObject.name = finalExit ? "EscapeExit(AutoCreated)" : "DownStairs(AutoCreated)";

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = finalExit ? new Color(0.2f, 1f, 0.75f, 0.25f) : new Color(1f, 0.75f, 0.15f);
        renderer.sortingOrder = finalExit ? 1 : 5;
        transform.localScale = finalExit ? new Vector3(3f, 3f, 1f) : new Vector3(0.7f, 0.7f, 1f);
    }

    private void OnDestroy()
    {
        if (stairsByCell.TryGetValue(cell, out var stairs) && stairs == this) stairsByCell.Remove(cell);
    }

    public static bool TryUseAt(Vector3Int targetCell, PlayerTurnActor player)
    {
        if (!stairsByCell.TryGetValue(targetCell, out var stairs) || stairs == null || stairs.isFinalExit
            || player == null || player.CurrentCell != targetCell || stairs.requestSent) return false;
        stairs.requestSent = ExpeditionManager.Instance != null && ExpeditionManager.Instance.TryUseFloorExit(player);
        return stairs.requestSent;
    }

    public static bool TryGetDebugLabel(Vector3Int targetCell, out string label)
    {
        if (stairsByCell.TryGetValue(targetCell, out var stairs) && stairs != null)
        {
            label = stairs.isFinalExit ? "EXIT" : "STAIRS";
            return true;
        }
        label = null;
        return false;
    }

    public static void PrepareExtractionForAction(PlayerTurnActor player, string actionName)
    {
        // Move/Wait는 이동 완료 후 전달되므로 진입 이동 1Tick까지 체류로 계산하지 않는다.
        // 실제 대기는 명시적인 "Wait" 이름으로 들어온다.
        if (player == null || actionName == "Move/Wait") return;

        foreach (var stairs in stairsByCell.Values)
        {
            if (stairs == null || !stairs.isFinalExit || stairs.requestSent
                || stairs.IsLockedByBoss()
                || stairs.extractionStartTick >= 0f
                || !stairs.IsInsideExtractionZone(player.CurrentCell)) continue;

            var manager = ExpeditionManager.Instance;
            if (manager == null) return;
            stairs.extractionStartTick = manager.ElapsedTicks;
            Debug.Log($"[Extraction] 탈출 구역 행동 시작 — {stairs.RequiredExtractionTicks(manager):0.##} Tick 체류 필요.");
            return;
        }
    }

    // Player Update와 탈출구 Update의 실행 순서에 의존하지 않고, 한 행동에 예약된 Enemy 턴까지
    // 모두 끝난 직후 탈출을 확정한다. Space를 누른 채 대기를 연속 입력해도 완료 Tick에서 멈춘다.
    public static bool TryResolveCompletedExtraction(PlayerTurnActor player)
    {
        if (player == null || !player.IsReadyForInput) return false;

        foreach (var stairs in stairsByCell.Values)
        {
            if (stairs == null || !stairs.isFinalExit || stairs.requestSent
                || stairs.IsLockedByBoss()
                || stairs.extractionStartTick < 0f
                || !stairs.IsInsideExtractionZone(player.CurrentCell)) continue;

            var manager = ExpeditionManager.Instance;
            if (manager == null
                || manager.ElapsedTicks - stairs.extractionStartTick < stairs.RequiredExtractionTicks(manager)) continue;

            stairs.requestSent = manager.TryCompleteExtraction(player);
            return stairs.requestSent;
        }

        return false;
    }

    private void Update()
    {
        var player = PlayerTurnActor.Instance;
        if (InventoryPanel.IsOpen || GroundLoot.IsSelectionOpen) return;
        if (requestSent || player == null) return;

        if (isFinalExit)
        {
            if (IsLockedByBoss())
            {
                extractionStartTick = -1f;
                return;
            }
            var manager = ExpeditionManager.Instance;
            if (!IsInsideExtractionZone(player.CurrentCell))
            {
                extractionStartTick = -1f;
                return;
            }
            if (manager == null) return;

            if (extractionStartTick < 0f)
            {
                extractionStartTick = manager.ElapsedTicks;
                Debug.Log($"[Extraction] 탈출 구역 진입 — {RequiredExtractionTicks(manager):0.##} Tick 체류 필요.");
            }

            TryResolveCompletedExtraction(player);
            return;
        }
        else
        {
            if (player.CurrentCell != cell || !Input.GetKeyDown(KeyCode.E)) return;
        }

        requestSent = ExpeditionManager.Instance != null && ExpeditionManager.Instance.TryUseFloorExit(player);
    }

    private void OnGUI()
    {
        var player = PlayerTurnActor.Instance;
        if (requestSent || player == null) return;

        bool canInteract = isFinalExit ? IsInsideExtractionZone(player.CurrentCell) : player.CurrentCell == cell;
        if (!canInteract) return;

        string label;
        if (isFinalExit)
        {
            if (IsLockedByBoss())
            {
                label = "보스를 처치해야 탈출할 수 있습니다.";
                GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height - 90f, 360f, 40f), label);
                return;
            }
            float required = RequiredExtractionTicks(ExpeditionManager.Instance);
            float elapsed = extractionStartTick < 0f ? 0f : (ExpeditionManager.Instance?.ElapsedTicks ?? 0f) - extractionStartTick;
            float remaining = Mathf.Max(0f, required - elapsed);
            label = $"탈출 구역 체류 중 — {remaining:0.##} Tick 남음 (Space: 대기)";
        }
        else
        {
            label = $"E: {ExpeditionSession.CurrentFloor + 1}층으로 하강 ({ExpeditionManager.Instance?.FloorChangeTickCost ?? TickCost.Interact:0} Tick)";
        }
        GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height - 90f, 360f, 40f), label);
    }

    private bool IsInsideExtractionZone(Vector3Int playerCell)
    {
        return Mathf.Abs(playerCell.x - cell.x) <= 1 && Mathf.Abs(playerCell.y - cell.y) <= 1;
    }

    private bool IsLockedByBoss()
    {
        return ExpeditionSession.CurrentFloor == 5 && GameObject.Find("Boss_F5") != null;
    }

    private float RequiredExtractionTicks(ExpeditionManager manager)
    {
        return ExpeditionSession.CurrentFloor == 5 ? 5f : manager?.EscapeProcedureTickCost ?? 10f;
    }
}
