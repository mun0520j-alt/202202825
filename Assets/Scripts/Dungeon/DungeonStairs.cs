using UnityEngine;
using UnityEngine.Tilemaps;

// 현재 층의 가장 먼 바닥 칸에 생성되는 단방향 계단/탈출구. 해당 칸에서 E로 사용한다.
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
        renderer.color = finalExit ? new Color(0.2f, 1f, 0.75f) : new Color(1f, 0.75f, 0.15f);
        renderer.sortingOrder = 5;
        transform.localScale = new Vector3(0.7f, 0.7f, 1f);
    }

    private void Update()
    {
        var player = PlayerTurnActor.Instance;
        if (requestSent || player == null || player.CurrentCell != cell) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        requestSent = ExpeditionManager.Instance != null && ExpeditionManager.Instance.TryUseFloorExit(player);
    }

    private void OnGUI()
    {
        var player = PlayerTurnActor.Instance;
        if (requestSent || player == null || player.CurrentCell != cell) return;

        string label = isFinalExit
            ? $"E: 탈출 절차 ({ExpeditionManager.Instance?.EscapeProcedureTickCost ?? 30f:0} Tick)"
            : $"E: {ExpeditionSession.CurrentFloor + 1}층으로 하강 ({ExpeditionManager.Instance?.FloorChangeTickCost ?? TickCost.Interact:0} Tick)";
        GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height - 90f, 360f, 40f), label);
    }
}
