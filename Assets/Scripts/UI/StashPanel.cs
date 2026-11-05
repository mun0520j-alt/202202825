using UnityEngine;

public sealed class StashPanel : MonoBehaviour
{
    private Rect windowRect;
    private Vector2 scroll;

    private void OnGUI()
    {
        const float width = 430f;
        const float margin = 16f;
        windowRect = new Rect(Mathf.Max(margin, Screen.width - width - margin), 64f,
            width, Mathf.Max(420f, Screen.height - 80f));
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "STASH (무제한)");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label($"장착 전리품: {LootRuntimeState.EquippedLoot.Count}");
        if (LootRuntimeState.EquippedLoot.Count == 0)
        {
            GUILayout.Label("장착한 전리품 장비가 없습니다.");
        }
        else
        {
            foreach (var entry in LootRuntimeState.EquippedLoot)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.Key}: {entry.Value.Definition.Name} [{entry.Value.Definition.Rarity}]");
                if (GUILayout.Button("해제 → Stash", GUILayout.Width(110f)))
                {
                    LootRuntimeState.TryUnequipToStash(entry.Key);
                    GUILayout.EndHorizontal();
                    break;
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(12f);
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
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        foreach (var item in LootRuntimeState.Stash)
        {
            GUILayout.Label($"• {item.Definition.Name} [{item.Definition.Rarity}]");
        }
        GUILayout.EndScrollView();
    }
}
