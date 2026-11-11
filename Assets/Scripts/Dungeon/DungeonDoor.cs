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
    public string InteractionLabel => IsOpen ? $"문 닫기 ({TickCost.PerTileMove:0} Tick)" : $"문 열기 ({TickCost.PerTileMove:0} Tick)";

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
        if (PauseMenuController.BlocksWorldInput) return;
        if (!Input.GetKeyDown(KeyCode.E) || InventoryPanel.IsOpen || GroundLoot.IsSelectionOpen) return;
        var player = PlayerTurnActor.Instance;
        if (player == null || player.FloorTilemap != floor || !player.IsReadyForInput
            || !IsAdjacent(player.CurrentCell) || !LootInteractionGate.TryClaim()) return;
        TryInteractFromMenu(player);
    }

    public static bool IsClosedAt(Tilemap map, Vector3Int cell) => byCell.TryGetValue((map, cell), out var door) && door != null && !door.IsOpen;
    public static bool IsDoorCell(Tilemap map, Vector3Int cell) => byCell.TryGetValue((map, cell), out var door) && door != null;
    public static bool TryGetDebugState(Tilemap map, Vector3Int cell, out bool isOpen)
    {
        if (byCell.TryGetValue((map, cell), out var door) && door != null)
        {
            isOpen = door.IsOpen;
            return true;
        }
        isOpen = false;
        return false;
    }
    public static bool TryOpenForEnemy(Tilemap map, Vector3Int cell, EnemyTurnActor enemy)
    {
        if (!byCell.TryGetValue((map, cell), out var door) || door == null || door.IsOpen) return false;
        door.SetOpen(true);
        TickManager.Instance.CompleteTurn(enemy, 2f, "OpenDoor");
        return true;
    }

    public static void CollectAdjacent(Tilemap floorTilemap, Vector3Int playerCell, List<DungeonDoor> results)
    {
        if (floorTilemap == null || results == null) return;
        var unique = new HashSet<DungeonDoor>();
        foreach (var pair in byCell)
        {
            DungeonDoor door = pair.Value;
            if (pair.Key.Item1 != floorTilemap || door == null || !unique.Add(door)) continue;
            if (door.IsAdjacent(playerCell)) results.Add(door);
        }
    }

    public bool TryInteractFromMenu(PlayerTurnActor player)
    {
        if (player == null || player.FloorTilemap != floor || !player.IsReadyForInput || !IsAdjacent(player.CurrentCell))
            return false;
        if (!IsOpen) SetOpen(true);
        else if (!ContainsActor() && !HasAdjacentEnemy()) SetOpen(false);
        else return false;
        TickManager.Instance.CompleteTurn(player, TickCost.PerTileMove, IsOpen ? "OpenDoor" : "CloseDoor");
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

    private bool HasAdjacentEnemy()
    {
        if (TickManager.Instance == null) return false;
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var cell in cells)
        {
            foreach (var direction in directions)
            {
                if (TickManager.Instance.GetActorAt(cell + direction) is EnemyTurnActor) return true;
            }
        }
        return false;
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        if (view != null) view.enabled = !open;
    }
}
