using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class DungeonChest : MonoBehaviour
{
    private static readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private Tilemap floor;
    private Vector3Int cell;
    private int floorNumber;
    private int lootSeed;
    private bool opened;
    private bool opening;
    private SpriteRenderer chestRenderer;

    public void Initialize(Tilemap floorTilemap, Vector3Int targetCell, int currentFloor, int seed)
    {
        floor = floorTilemap;
        cell = targetCell;
        occupiedCells.Add(cell);
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
    }

    public static bool IsCellBlocked(Vector3Int targetCell)
    {
        return occupiedCells.Contains(targetCell);
    }

    private void Update()
    {
        var player = PlayerTurnActor.Instance;
        if (opened || opening || player == null || !Input.GetKeyDown(KeyCode.E)) return;
        if (Manhattan(player.CurrentCell, cell) != 1) return;
        if (!player.TrySpendInteraction(TickCost.Interact, "Loot")) return;

        opening = true;
        StartCoroutine(ResolveOpenAfterTick(player));
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
        if (chestRenderer != null) chestRenderer.color = new Color(0.24f, 0.15f, 0.08f);

        LootItemData definition = LootItemDatabase.Roll(floorNumber, new System.Random(lootSeed));
        if (definition == null)
        {
            Debug.LogError("[Chest] 드롭 가능한 아이템이 없습니다.");
            yield break;
        }

        var item = new LootItemInstance(definition);
        if (LootRuntimeState.TryAddToRunBag(item))
        {
            Debug.Log($"[Chest] {definition.Name} ({definition.Rarity}) 획득 — {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}");
        }
        else
        {
            new GameObject("GroundLoot").AddComponent<GroundLoot>().Initialize(floor, cell, item);
            Debug.Log($"[Chest] 가방이 가득 차 {definition.Name}을(를) 바닥에 남겼습니다.");
        }
    }

    private void OnGUI()
    {
        var player = PlayerTurnActor.Instance;
        if (opened || player == null || Manhattan(player.CurrentCell, cell) != 1) return;
        string label = opening ? "상자 여는 중..." : $"E: 상자 열기 ({TickCost.Interact:0} Tick)";
        GUI.Box(new Rect(Screen.width * 0.5f - 150f, Screen.height - 90f, 300f, 40f), label);
    }

    private static int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
