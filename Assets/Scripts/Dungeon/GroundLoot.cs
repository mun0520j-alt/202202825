using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public sealed class GroundLoot : MonoBehaviour
{
    private static readonly List<GroundLoot> activeLoot = new List<GroundLoot>();
    private static readonly List<GroundLoot> selection = new List<GroundLoot>();
    private static readonly List<DungeonChest> chestSelection = new List<DungeonChest>();
    private static bool selectionOpen;
    private LootItemInstance item;
    private Vector3Int cell;
    public static bool IsSelectionOpen => selectionOpen || DungeonInteractionMenu.IsOpen;
    public static bool IsLootSelectionOpen => selectionOpen;

    public void Initialize(Tilemap floor, Vector3Int targetCell, LootItemInstance loot)
    {
        item = loot;
        cell = targetCell;
        transform.position = floor.GetCellCenterWorld(cell);
        name = $"GroundLoot({loot.Definition.Name})";
        activeLoot.Add(this);

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = RarityColor(loot.Definition.Rarity);
        renderer.sortingOrder = 6;
        transform.localScale = new Vector3(0.35f, 0.35f, 1f);
    }

    private void OnDestroy()
    {
        activeLoot.Remove(this);
        selection.Remove(this);
        if (activeLoot.Count == 0 || (selection.Count == 0 && chestSelection.Count == 0)) CloseSelection();
    }

    public static bool HasAdjacent(Vector3Int playerCell)
    {
        return activeLoot.Exists(loot => loot != null && loot.item != null && Manhattan(playerCell, loot.cell) <= 1);
    }

    private void Update()
    {
        var player = PlayerTurnActor.Instance;
        if (item == null || player == null || activeLoot.Count == 0 || activeLoot[0] != this) return;
        if (InventoryPanel.IsOpen) return;
        if (selectionOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CloseSelection();
            return;
        }
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (!LootInteractionGate.TryClaim()) return;
        OpenOrPickAdjacent(player);
    }

    public static bool TryInteractAt(Vector3Int targetCell, PlayerTurnActor player)
    {
        if (player == null || Manhattan(player.CurrentCell, targetCell) > 1) return false;
        var target = activeLoot.Find(loot => loot != null && loot.item != null && loot.cell == targetCell);
        if (target == null) return false;
        Pick(target, player);
        return true;
    }

    private static void OpenOrPickAdjacent(PlayerTurnActor player)
    {
        selection.Clear();
        Vector3Int origin = player.CurrentCell;
        AddAt(origin);
        AddAt(origin + Vector3Int.up);
        AddAt(origin + Vector3Int.down);
        AddAt(origin + Vector3Int.left);
        AddAt(origin + Vector3Int.right);
        chestSelection.Clear();
        DungeonChest.CollectAdjacent(origin, chestSelection);

        int interactionCount = selection.Count + chestSelection.Count;
        if (interactionCount == 0) return;
        if (interactionCount == 1 && selection.Count == 1)
        {
            var only = selection[0];
            selection.Clear();
            Pick(only, player);
            return;
        }
        if (interactionCount == 1 && chestSelection.Count == 1)
        {
            var chest = chestSelection[0];
            chestSelection.Clear();
            chest.TryOpenFromInteractionMenu(player);
            return;
        }
        selectionOpen = true;
    }

    private static void AddAt(Vector3Int targetCell)
    {
        foreach (var loot in activeLoot)
        {
            if (loot != null && loot.item != null && loot.cell == targetCell) selection.Add(loot);
        }
    }

    private static void Pick(GroundLoot target, PlayerTurnActor player)
    {
        if (target == null || target.item == null) return;
        LootItemInstance picked = target.item;
        bool spent = player.TryPerformTimedAction(TickCost.PickUp, "PickUp", () =>
        {
            if (!LootRuntimeState.TryAddToRunBag(picked))
            {
                Debug.Log("[Loot] 가방이 가득 차 아이템을 바닥에 남겼습니다.");
                return false;
            }
            target.item = null;
            M3MeasurementAutomation.RecordItemAcquired();
            Debug.Log($"[Loot] {picked.Definition.Name} ({picked.Definition.Rarity}) 획득 — {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}");
            Destroy(target.gameObject);
            return true;
        });
        if (!spent) return;

        selection.RemoveAll(loot => loot == null || loot.item == null || loot == target);
        if (selection.Count == 0 && chestSelection.Count == 0) CloseSelection();
    }

    private void OnGUI()
    {
        var player = PlayerTurnActor.Instance;
        if (item == null || player == null || activeLoot.Count == 0 || activeLoot[0] != this) return;
        if (selectionOpen)
        {
            int interactionCount = selection.Count + chestSelection.Count;
            float height = 42f + interactionCount * 34f;
            var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - height * 0.5f, 360f, height);
            GUI.Box(rect, "주변 상호작용");
            int row = 0;
            for (int i = 0; i < selection.Count; i++)
            {
                var loot = selection[i];
                if (loot == null || loot.item == null) continue;
                string label = $"{loot.item.Definition.Name} [{loot.item.Definition.Rarity}]";
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row * 34f, rect.width - 24f, 28f), label)) Pick(loot, player);
                row++;
            }
            for (int i = 0; i < chestSelection.Count; i++)
            {
                var chest = chestSelection[i];
                if (chest == null) continue;
                if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row * 34f, rect.width - 24f, 28f), chest.InteractionLabel))
                {
                    CloseSelection();
                    chest.TryOpenFromInteractionMenu(player);
                    return;
                }
                row++;
            }
            return;
        }
        if (!HasAdjacent(player.CurrentCell)) return;
        string suffix = LootRuntimeState.RunBag.Count >= LootRuntimeState.BagCapacity ? " (가방 가득 참)" : string.Empty;
        GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height - 135f, 380f, 36f),
            $"E: 주변 아이템 줍기{suffix}");
    }

    private static void CloseSelection()
    {
        selectionOpen = false;
        selection.Clear();
        chestSelection.Clear();
    }

    public static void CloseSelectionForModalSwitch()
    {
        CloseSelection();
    }

    private static int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Color RarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Rare: return new Color(0.25f, 0.55f, 1f);
            case LootRarity.Epic: return new Color(0.75f, 0.3f, 1f);
            case LootRarity.Unique: return new Color(1f, 0.55f, 0.1f);
            case LootRarity.Legendary: return new Color(1f, 0.2f, 0.15f);
            default: return new Color(0.85f, 0.85f, 0.85f);
        }
    }
}

public static class LootInteractionGate
{
    private static int claimedFrame = -1;

    public static bool TryClaim()
    {
        if (claimedFrame == Time.frameCount) return false;
        claimedFrame = Time.frameCount;
        return true;
    }
}
