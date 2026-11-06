using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class DungeonDoor : MonoBehaviour
{
    private static readonly Dictionary<(Tilemap, Vector3Int), DungeonDoor> byCell = new Dictionary<(Tilemap, Vector3Int), DungeonDoor>();
    private readonly List<Vector3Int> cells = new List<Vector3Int>();
    private Tilemap floor;
    private Tilemap walls;
    private SpriteRenderer view;
    public bool IsOpen { get; private set; }

    public void Initialize(Tilemap floorTilemap, Tilemap wallsTilemap, IEnumerable<Vector3Int> occupiedCells, bool horizontal)
    {
        floor = floorTilemap;
        walls = wallsTilemap;
        cells.AddRange(occupiedCells);
        foreach (var cell in cells)
        {
            byCell[(floor, cell)] = this;
            byCell[(walls, cell)] = this;
        }
        transform.position = floor.GetCellCenterWorld(cells[cells.Count / 2]);
        view = gameObject.AddComponent<SpriteRenderer>();
        view.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        view.color = new Color(0.38f, 0.2f, 0.08f, 1f);
        view.sortingOrder = 4;
        transform.localScale = horizontal ? new Vector3(2.7f, 0.35f, 1f) : new Vector3(0.35f, 2.7f, 1f);
        SetOpen(false);
    }

    private void OnDestroy()
    {
        foreach (var cell in cells)
        {
            if (byCell.TryGetValue((floor, cell), out var floorDoor) && floorDoor == this) byCell.Remove((floor, cell));
            if (byCell.TryGetValue((walls, cell), out var wallDoor) && wallDoor == this) byCell.Remove((walls, cell));
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E) || InventoryPanel.IsOpen || GroundLoot.IsSelectionOpen) return;
        var player = PlayerTurnActor.Instance;
        if (player == null || !player.IsReadyForInput || !IsAdjacent(player.CurrentCell) || !LootInteractionGate.TryClaim()) return;
        if (!IsOpen) SetOpen(true);
        else if (!ContainsActor()) SetOpen(false);
        else return;
        TickManager.Instance.CompleteTurn(player, TickCost.PerTileMove, IsOpen ? "OpenDoor" : "CloseDoor");
    }

    public static bool IsClosedAt(Tilemap map, Vector3Int cell) => byCell.TryGetValue((map, cell), out var door) && door != null && !door.IsOpen;
    public static bool TryOpenForEnemy(Tilemap map, Vector3Int cell, EnemyTurnActor enemy)
    {
        if (!byCell.TryGetValue((map, cell), out var door) || door == null || door.IsOpen) return false;
        door.SetOpen(true);
        TickManager.Instance.CompleteTurn(enemy, 2f, "OpenDoor");
        return true;
    }

    private bool IsAdjacent(Vector3Int playerCell)
    {
        foreach (var cell in cells) if (Mathf.Abs(cell.x-playerCell.x)+Mathf.Abs(cell.y-playerCell.y) <= 1) return true;
        return false;
    }

    private bool ContainsActor()
    {
        if (TickManager.Instance == null) return false;
        foreach (var cell in cells) if (TickManager.Instance.IsCellOccupied(cell)) return true;
        return false;
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        if (view != null) view.enabled = !open;
    }
}
