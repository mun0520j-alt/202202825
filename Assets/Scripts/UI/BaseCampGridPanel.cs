using UnityEngine;
using UnityEngine.UI;

// 영지에서도 던전과 동일한 LootGridView를 사용한다. 데이터 변경은 LootRuntimeState에 위임한다.
public sealed class BaseCampGridPanel : MonoBehaviour
{
    private LootGridView gridView;
    private Text title;
    private Text notice;
    private LootItemInstance selected;

    private void Start()
    {
        Build();
        LootRuntimeState.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        LootRuntimeState.Changed -= Refresh;
    }

    private void Build()
    {
        Transform hud = ScreenSpaceCanvasProvider.GetOrCreateHudRoot();
        var root = new GameObject("BaseCampRunBagGrid", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(hud, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(400f, -40f);
        rect.anchoredPosition = new Vector2(-20f, 0f);
        root.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.97f);

        title = CreateText(root.transform, "Title", "출격 가방", 18, new Vector2(0f, 0.90f), new Vector2(1f, 0.98f));
        var gridObject = new GameObject("Grid", typeof(RectTransform));
        gridObject.transform.SetParent(root.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.78f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridView = gridObject.AddComponent<LootGridView>();

        notice = CreateText(root.transform, "Notice", "아이템을 선택하세요", 13,
            new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.30f));
        CreateButton(root.transform, "ToStash", "Stash로 이동", new Vector2(0.08f, 0.10f), MoveToStash);
        CreateButton(root.transform, "Equip", "장착", new Vector2(0.53f, 0.10f), Equip);
    }

    private void Refresh()
    {
        if (gridView == null) return;
        gridView.Bind(LootRuntimeState.RunBagGrid, LootRuntimeState.MoveRunBagItem);
        gridView.SetItemClickHandler(Select);
        title.text = $"출격 가방 {LootRuntimeState.RunBagGrid.Width}×{LootRuntimeState.RunBagGrid.Height}";
        if (selected != null && LootRuntimeState.RunBagGrid.Find(selected.InstanceId) == null) selected = null;
        if (selected == null) gridView.ClearSelection();
        notice.text = selected?.Definition != null ? selected.Definition.Name : "아이템을 선택하세요";
    }

    private void Select(LootItemInstance item)
    {
        selected = item;
        notice.text = item?.Definition?.Name ?? "아이템을 선택하세요";
    }

    private void MoveToStash()
    {
        if (selected == null) return;
        bool ok = LootRuntimeState.MoveBagItemToStash(selected.InstanceId);
        notice.text = ok ? "Stash로 이동했습니다." : (LootRuntimeState.LastFailureReason ?? "이동 실패");
        if (ok)
        {
            selected = null;
            gridView.ClearSelection();
        }
    }

    private void Equip()
    {
        if (selected == null) return;
        bool ok = LootRuntimeState.TryEquipFromBaseCampBag(selected.InstanceId);
        notice.text = ok ? "장착했습니다." : LootRuntimeState.LastFailureReason;
        if (ok)
        {
            selected = null;
            gridView.ClearSelection();
        }
    }

    private static Text CreateText(Transform parent, string name, string value, int size, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.font = HudFont.GetDefault();
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateButton(Transform parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor + new Vector2(0.39f, 0.07f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.16f, 0.36f, 0.48f, 0.95f);
        go.GetComponent<Button>().onClick.AddListener(action);
        CreateText(go.transform, "Label", label, 13, Vector2.zero, Vector2.one);
    }
}
