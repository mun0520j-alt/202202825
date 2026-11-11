using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 셀당 하나의 5x5 바닥 루팅 보관함. 기존 GroundLoot는 생성 호출 호환 어댑터로 남긴다.
public sealed class GroundLootPile : MonoBehaviour, ILootContainer
{
    private static readonly List<GroundLootPile> active = new List<GroundLootPile>();
    private static readonly List<GroundLootPile> selection = new List<GroundLootPile>();
    private static readonly List<DungeonChest> chestSelection = new List<DungeonChest>();
    private static bool selectionOpen;

    private readonly LootGridModel lootGrid = new LootGridModel(5, 5);
    private Tilemap floor;
    private Vector3Int cell;
    private string displayName = "바닥 더미";

    public static bool IsSelectionOpen => selectionOpen || LootingPanel.IsOpen;
    public string DisplayName => displayName;
    public LootGridModel Grid => lootGrid;

    public static bool AddOrCreate(Tilemap floor, Vector3Int cell, LootItemInstance item, string name = "바닥 더미")
    {
        return TryAddOrCreate(floor, cell, item, out _, name);
    }

    public static bool TryAddOrCreate(Tilemap floor, Vector3Int cell, LootItemInstance item,
        out GroundLootPile pile, string name = "바닥 더미")
    {
        pile = Find(floor, cell);
        if (pile == null)
        {
            pile = new GameObject($"GroundLootPile_{cell.x}_{cell.y}").AddComponent<GroundLootPile>();
            pile.Initialize(floor, cell, name);
        }
        return pile.TryAdd(item);
    }

    public static GroundLootPile Create(Tilemap floor, Vector3Int cell,
        IEnumerable<LootItemInstance> items, string name)
    {
        GroundLootPile pile = Find(floor, cell);
        if (pile == null)
        {
            pile = new GameObject($"GroundLootPile_{cell.x}_{cell.y}").AddComponent<GroundLootPile>();
            pile.Initialize(floor, cell, name);
        }
        foreach (LootItemInstance item in items)
            if (!pile.TryAdd(item)) Debug.LogWarning("[Loot] 5x5 바닥 더미 공간 부족으로 아이템을 추가하지 못했습니다.");
        return pile;
    }

    private void Initialize(Tilemap floorTilemap, Vector3Int targetCell, string name)
    {
        floor = floorTilemap;
        cell = targetCell;
        displayName = name;
        transform.position = floor.GetCellCenterWorld(cell);
        active.Add(this);
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = new Color(0.9f, 0.65f, 0.18f);
        renderer.sortingOrder = 6;
        transform.localScale = new Vector3(0.42f, 0.42f, 1f);
    }

    private void OnDestroy()
    {
        active.Remove(this);
        selection.Remove(this);
        if (selection.Count == 0 && chestSelection.Count == 0) CloseSelection();
    }

    private void Update()
    {
        if (active.Count == 0 || active[0] != this || PauseMenuController.IsOpen || LootingPanel.IsOpen) return;
        if (InventoryPanel.IsOpen) return;
        if (selectionOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CloseSelection();
            return;
        }
        if (!Input.GetKeyDown(KeyCode.E) || !LootInteractionGate.TryClaim()) return;
        OpenAdjacent(PlayerTurnActor.Instance);
    }

    public static bool HasAdjacent(Vector3Int playerCell)
    {
        return active.Exists(value => value != null && value.lootGrid.Placements.Count > 0
            && Manhattan(playerCell, value.cell) <= 1);
    }

    public static bool TryInteractAt(Vector3Int targetCell, PlayerTurnActor player)
    {
        if (player == null || Manhattan(player.CurrentCell, targetCell) > 1) return false;
        GroundLootPile pile = active.Find(value => value != null && value.cell == targetCell && value.lootGrid.Placements.Count > 0);
        if (pile == null) return false;
        LootingPanel.Open(pile, player);
        return true;
    }

    public static void CollectAdjacent(Vector3Int playerCell, List<GroundLootPile> results)
    {
        if (results == null) return;
        foreach (GroundLootPile pile in active)
            if (pile != null && pile.lootGrid.Placements.Count > 0 && Manhattan(playerCell, pile.cell) <= 1)
                results.Add(pile);
    }

    public void OpenFromInteractionMenu(PlayerTurnActor player)
    {
        if (player != null && Manhattan(player.CurrentCell, cell) <= 1) LootingPanel.Open(this, player);
    }

    private static void OpenAdjacent(PlayerTurnActor player)
    {
        if (player == null) return;
        selection.Clear();
        foreach (GroundLootPile pile in active)
            if (pile != null && pile.lootGrid.Placements.Count > 0 && Manhattan(player.CurrentCell, pile.cell) <= 1)
                selection.Add(pile);
        chestSelection.Clear();
        DungeonChest.CollectAdjacent(player.CurrentCell, chestSelection);
        int count = selection.Count + chestSelection.Count;
        if (count == 1 && selection.Count == 1)
        {
            LootingPanel.Open(selection[0], player);
            selection.Clear();
        }
        else if (count == 1 && chestSelection.Count == 1)
        {
            DungeonChest chest = chestSelection[0];
            chestSelection.Clear();
            chest.TryOpenFromInteractionMenu(player);
        }
        else if (count > 0) selectionOpen = true;
    }

    private void OnGUI()
    {
        PlayerTurnActor player = PlayerTurnActor.Instance;
        if (player == null || active.Count == 0 || active[0] != this || LootingPanel.IsOpen) return;
        if (selectionOpen)
        {
            int count = selection.Count + chestSelection.Count;
            float height = 42f + count * 34f;
            var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - height * 0.5f, 360f, height);
            GUI.Box(rect, "주변 상호작용");
            int row = 0;
            foreach (GroundLootPile pile in selection)
            {
                if (pile == null) continue;
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row++ * 34f, rect.width - 24f, 28f),
                    $"{pile.DisplayName} ({pile.Grid.Placements.Count}개)"))
                {
                    CloseSelection();
                    LootingPanel.Open(pile, player);
                    return;
                }
            }
            foreach (DungeonChest chest in chestSelection)
            {
                if (chest == null) continue;
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row++ * 34f, rect.width - 24f, 28f), chest.InteractionLabel))
                {
                    CloseSelection();
                    chest.TryOpenFromInteractionMenu(player);
                    return;
                }
            }
        }
        else if (HasAdjacent(player.CurrentCell))
            GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height - 135f, 380f, 36f), "E: 주변 루팅 보관함 열기");
    }

    public bool TryAdd(LootItemInstance item) => item != null && lootGrid.TryAutoPlace(item, out _);
    public bool TryRemove(string instanceId, out LootItemInstance item) => lootGrid.Remove(instanceId, out item);

    public void HandleContentsChanged()
    {
        if (lootGrid.Placements.Count != 0) return;
        LootingPanel.CloseLootOnly();
        Destroy(gameObject);
    }

    public static bool CloseForModalSwitch()
    {
        if (LootingPanel.CloseLootOnly()) return true;
        if (!selectionOpen) return false;
        CloseSelection();
        return true;
    }

    private static GroundLootPile Find(Tilemap floor, Vector3Int cell)
    {
        return active.Find(value => value != null && value.floor == floor && value.cell == cell);
    }

    private static void CloseSelection()
    {
        selectionOpen = false;
        selection.Clear();
        chestSelection.Clear();
    }

    private static int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
