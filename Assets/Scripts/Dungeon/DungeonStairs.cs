using UnityEngine;
using UnityEngine.Tilemaps;

// 일반 계단은 해당 칸에서 E로 사용하고, 최종 탈출구는 주변 1칸의 대기 구역에 들어가면 자동 시작한다.
public sealed class DungeonStairs : MonoBehaviour
{
    private Vector3Int cell;
    private bool isFinalExit;
    private bool requestSent;

    public void Initialize(Tilemap floorTilemap, Vector3Int targetCell, bool finalExit)
    {
        cell = targetCell;
        isFinalExit = finalExit;
        transform.position = floorTilemap.GetCellCenterWorld(cell);
        gameObject.name = finalExit ? "EscapeExit(AutoCreated)" : "DownStairs(AutoCreated)";

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = finalExit ? new Color(0.2f, 1f, 0.75f, 0.25f) : new Color(1f, 0.75f, 0.15f);
        renderer.sortingOrder = finalExit ? 1 : 5;
        transform.localScale = finalExit ? new Vector3(3f, 3f, 1f) : new Vector3(0.7f, 0.7f, 1f);
    }

    private void Update()
    {
        var player = PlayerTurnActor.Instance;
        if (requestSent || player == null) return;

        if (isFinalExit)
        {
            if (!IsInsideExtractionZone(player.CurrentCell)) return;
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

        string label = isFinalExit
            ? $"탈출 구역 — {ExpeditionManager.Instance?.EscapeProcedureTickCost ?? 10f:0} Tick 대기 시작"
            : $"E: {ExpeditionSession.CurrentFloor + 1}층으로 하강 ({ExpeditionManager.Instance?.FloorChangeTickCost ?? TickCost.Interact:0} Tick)";
        GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height - 90f, 360f, 40f), label);
    }

    private bool IsInsideExtractionZone(Vector3Int playerCell)
    {
        return Mathf.Abs(playerCell.x - cell.x) <= 1 && Mathf.Abs(playerCell.y - cell.y) <= 1;
    }
}
