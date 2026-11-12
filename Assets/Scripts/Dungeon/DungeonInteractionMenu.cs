using System.Collections.Generic;
using UnityEngine;

// 문과 상자가 동시에 인접한 경우 개별 Update 실행 순서로 E 입력을 선점하지 않도록
// 선택 UI를 먼저 연다. 한 종류만 있으면 기존 단일 상호작용 흐름을 그대로 사용한다.
[DefaultExecutionOrder(-1000)]
public sealed class DungeonInteractionMenu : MonoBehaviour
{
    private readonly List<DungeonDoor> doors = new List<DungeonDoor>();
    private readonly List<DungeonChest> chests = new List<DungeonChest>();
    private readonly List<GroundLootPile> lootPiles = new List<GroundLootPile>();
    public static bool IsOpen { get; private set; }

    private void OnDisable()
    {
        Close();
    }

    private void Update()
    {
        if (PauseMenuController.IsOpen) return;
        if (InventoryPanel.IsOpen) return;
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) Close();
            return;
        }
        if (!Input.GetKeyDown(KeyCode.E) || GroundLoot.IsLootSelectionOpen) return;

        var player = PlayerTurnActor.Instance;
        if (player == null || !player.IsReadyForInput) return;
        doors.Clear();
        chests.Clear();
        DungeonDoor.CollectAdjacent(player.FloorTilemap, player.CurrentCell, doors);
        DungeonChest.CollectAdjacent(player.CurrentCell, chests);
        lootPiles.Clear();
        GroundLootPile.CollectAdjacent(player.CurrentCell, lootPiles);
        if (doors.Count + chests.Count + lootPiles.Count <= 1)
        {
            doors.Clear();
            chests.Clear();
            lootPiles.Clear();
            return;
        }
        if (!LootInteractionGate.TryClaim()) return;
        IsOpen = true;
    }

    private void OnGUI()
    {
        if (!IsOpen) return;
        var player = PlayerTurnActor.Instance;
        if (player == null)
        {
            Close();
            return;
        }

        int count = doors.Count + chests.Count + lootPiles.Count;
        float height = 42f + count * 34f;
        var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - height * 0.5f, 360f, height);
        GUI.Box(rect, "주변 상호작용");
        int row = 0;
        foreach (var door in doors)
        {
            if (door == null) continue;
            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row * 34f, rect.width - 24f, 28f),
                door.InteractionLabel))
            {
                Close();
                door.TryInteractFromMenu(player);
                return;
            }
            row++;
        }
        foreach (var chest in chests)
        {
            if (chest == null) continue;
            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row * 34f, rect.width - 24f, 28f),
                chest.InteractionLabel))
            {
                Close();
                chest.TryOpenFromInteractionMenu(player);
                return;
            }
            row++;
        }
        foreach (var pile in lootPiles)
        {
            if (pile == null) continue;
            if (GUI.Button(new Rect(rect.x + 12f, rect.y + 30f + row * 34f, rect.width - 24f, 28f),
                $"{pile.DisplayName} ({pile.Grid.Placements.Count}개)"))
            {
                Close();
                pile.OpenFromInteractionMenu(player);
                return;
            }
            row++;
        }
    }

    private void Close()
    {
        IsOpen = false;
        doors.Clear();
        chests.Clear();
        lootPiles.Clear();
    }
}
