using UnityEngine;

public sealed class M2BagPanel : MonoBehaviour
{
    private bool expanded;
    private Rect windowRect = new Rect(12f, 190f, 310f, 260f);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) expanded = !expanded;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(12f, 150f, 210f, 34f), $"가방 {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity} (I)");
        if (expanded) windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "원정 가방");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label("버리면 현재 층 바닥에 남고, 층 이동 시 소멸합니다.");
        if (LootRuntimeState.RunBag.Count == 0) GUILayout.Label("비어 있음");

        for (int i = 0; i < LootRuntimeState.RunBag.Count; i++)
        {
            var item = LootRuntimeState.RunBag[i];
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{item.Definition.Name} [{item.Definition.Rarity}]");
            if (GUILayout.Button("버리기", GUILayout.Width(65f)))
            {
                Drop(item.InstanceId);
                break;
            }
            GUILayout.EndHorizontal();
        }
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
    }

    private static void Drop(string instanceId)
    {
        var player = PlayerTurnActor.Instance;
        if (player == null || player.FloorTilemap == null) return;
        LootItemInstance item = LootRuntimeState.DropBagItem(instanceId);
        if (item == null) return;
        new GameObject("GroundLoot").AddComponent<GroundLoot>().Initialize(player.FloorTilemap, player.CurrentCell, item);
        Debug.Log($"[Loot] {item.Definition.Name}을(를) 바닥에 버렸습니다.");
    }
}
