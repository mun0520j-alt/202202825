using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class DungeonChest : MonoBehaviour, ILootContainer
{
    public static float ChestReopenTickCost { get; set; } = 0f;
    private static readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private static readonly Dictionary<Vector3Int, DungeonChest> chestsByCell = new Dictionary<Vector3Int, DungeonChest>();
    private Tilemap floor;
    private Vector3Int cell;
    private int floorNumber;
    private int lootSeed;
    private bool opened;
    private bool opening;
    private SpriteRenderer chestRenderer;
    private readonly LootGridModel lootGrid = new LootGridModel(5, 5);
    public Vector3Int Cell => cell;
    public Tilemap FloorTilemap => floor;
    public string DisplayName => $"상자 — {floorNumber}층";
    public LootGridModel Grid => lootGrid;

    public void Initialize(Tilemap floorTilemap, Vector3Int targetCell, int currentFloor, int seed)
    {
        floor = floorTilemap;
        cell = targetCell;
        occupiedCells.Add(cell);
        chestsByCell[cell] = this;
        floorNumber = currentFloor;
        lootSeed = seed;
        transform.position = floor.GetCellCenterWorld(cell);
        name = $"Chest_F{floorNumber}_{cell.x}_{cell.y}";

        chestRenderer = gameObject.AddComponent<SpriteRenderer>();
        chestRenderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        chestRenderer.color = new Color(0.58f, 0.32f, 0.12f);
        chestRenderer.sortingOrder = 5;
        transform.localScale = new Vector3(0.75f, 0.55f, 1f);
    }

    private void OnDestroy()
    {
        occupiedCells.Remove(cell);
        if (chestsByCell.TryGetValue(cell, out var chest) && chest == this) chestsByCell.Remove(cell);
    }

    public static bool IsCellBlocked(Vector3Int targetCell)
    {
        return occupiedCells.Contains(targetCell);
    }

    public static bool TryOpenAt(Vector3Int targetCell, PlayerTurnActor player)
    {
        return chestsByCell.TryGetValue(targetCell, out var chest) && chest != null && chest.TryBeginOpen(player);
    }

    public static void CollectAdjacent(Vector3Int playerCell, List<DungeonChest> results)
    {
        if (results == null) return;
        foreach (var chest in chestsByCell.Values)
        {
            if (chest != null && !chest.opening && Manhattan(playerCell, chest.cell) == 1)
                results.Add(chest);
        }
    }

    public string InteractionLabel => opening ? "상자 여는 중..."
        : opened ? $"상자 다시 열기 ({ChestReopenTickCost:0} Tick)"
        : $"상자 열기 ({TickCost.Interact:0} Tick)";

    public bool TryOpenFromInteractionMenu(PlayerTurnActor player)
    {
        return TryBeginOpen(player);
    }

    private void Update()
    {
        if (PauseMenuController.BlocksWorldInput) return;
        var player = PlayerTurnActor.Instance;
        if (InventoryPanel.IsOpen || GroundLoot.IsSelectionOpen) return;
        if (opening || player == null || !Input.GetKeyDown(KeyCode.E)) return;
        if (Manhattan(player.CurrentCell, cell) != 1) return;
        if (GroundLoot.HasAdjacent(player.CurrentCell) || !LootInteractionGate.TryClaim()) return;
        TryBeginOpen(player);
    }

    private bool TryBeginOpen(PlayerTurnActor player)
    {
        if (opening || player == null || Manhattan(player.CurrentCell, cell) != 1) return false;
        if (opened)
        {
            if (ChestReopenTickCost <= 0f)
            {
                LootingPanel.Open(this, player);
                return true;
            }
            return player.TryPerformTimedAction(ChestReopenTickCost, "ChestReopen", () =>
            {
                LootingPanel.Open(this, player);
                return true;
            });
        }
        if (!player.TrySpendInteraction(TickCost.Interact, "Loot")) return false;
        opening = true;
        StartCoroutine(ResolveOpenAfterTick(player));
        return true;
    }

    private IEnumerator ResolveOpenAfterTick(PlayerTurnActor player)
    {
        while (ExpeditionSession.IsActive && player != null && !player.IsDead && !player.IsReadyForInput)
        {
            yield return null;
        }

        opening = false;
        if (!ExpeditionSession.IsActive || player == null || player.IsDead) yield break;

        opened = true;
        M3MeasurementAutomation.RecordChestOpened();
        if (chestRenderer != null) chestRenderer.color = new Color(0.24f, 0.15f, 0.08f);

        LootItemData definition = LootItemDatabase.Roll(floorNumber, new System.Random(lootSeed));
        if (definition == null)
        {
            Debug.LogError("[Chest] 드롭 가능한 아이템이 없습니다.");
            yield break;
        }

        var item = new LootItemInstance(definition);
        if (!TryAdd(item)) Debug.LogError($"[Chest] 5x5 상자에 {definition.Name}을 배치하지 못했습니다.");
        else M3MeasurementAutomation.RecordChestItemGenerated();
        LootingPanel.Open(this, player);
    }

    private void OnGUI()
    {
        var player = PlayerTurnActor.Instance;
        if (player == null || Manhattan(player.CurrentCell, cell) != 1) return;
        string label = opening ? "상자 여는 중..." : $"E: {InteractionLabel}";
        GUI.Box(new Rect(Screen.width * 0.5f - 150f, Screen.height - 90f, 300f, 40f), label);
    }

    public bool TryAdd(LootItemInstance item)
    {
        return item != null && lootGrid.TryAutoPlace(item, out _);
    }

    public bool TryRemove(string instanceId, out LootItemInstance item)
    {
        return lootGrid.Remove(instanceId, out item);
    }

    public void HandleContentsChanged() { }

    private static int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
