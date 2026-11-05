using UnityEngine;

public sealed class StashPanel : MonoBehaviour
{
    private Rect windowRect = new Rect(710f, 120f, 420f, 540f);
    private Vector2 scroll;

    private void OnGUI()
    {
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "STASH (무제한)");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label($"원정 가방 {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}");
        if (LootRuntimeState.RunBag.Count == 0)
        {
            GUILayout.Label("가방이 비어 있습니다.");
        }
        else
        {
            for (int i = 0; i < LootRuntimeState.RunBag.Count; i++)
            {
                var item = LootRuntimeState.RunBag[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.Definition.Name} [{item.Definition.Rarity}]");
                if (GUILayout.Button("Stash로", GUILayout.Width(85f)))
                {
                    LootRuntimeState.MoveBagItemToStash(item.InstanceId);
                    break;
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(12f);
        GUILayout.Label($"보관 아이템: {LootRuntimeState.Stash.Count}");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(340f));
        foreach (var item in LootRuntimeState.Stash)
        {
            GUILayout.Label($"• {item.Definition.Name} [{item.Definition.Rarity}]");
        }
        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
    }
}
