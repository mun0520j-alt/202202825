using UnityEngine;
using UnityEngine.UI;

// 영지에서도 던전과 동일한 LootGridView를 사용한다. 데이터 변경은 LootRuntimeState에 위임한다.
public sealed class BaseCampGridPanel : MonoBehaviour
{
    private LootGridView gridView;
    private Text title;
    private Text notice;
    private LootItemInstance selected;

    // 던전 인벤토리와 같은 장착 슬롯 표시 (무기·방패·갑옷·가방). 보험 장비는 청록 표시.
    private static readonly (LootItemType type, string label)[] SlotOrder =
    {
        (LootItemType.Weapon, "무기"), (LootItemType.Shield, "방패"),
        (LootItemType.Armor, "갑옷"), (LootItemType.Bag, "가방"),
    };
    private static readonly Color SlotBackground = new Color(0.27f, 0.15f, 0.08f, 0.95f);
    private static readonly Color EmptyIconColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color EmptyLabelColor = new Color(1f, 1f, 1f, 0.45f);
    private readonly System.Collections.Generic.Dictionary<LootItemType, (Image icon, Outline frame, Text label, Button unequip)> slots =
        new System.Collections.Generic.Dictionary<LootItemType, (Image, Outline, Text, Button)>();

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

        CreateText(root.transform, "EquipTitle", "장착 장비", 18, new Vector2(0f, 0.93f), new Vector2(1f, 0.98f));
        for (int i = 0; i < SlotOrder.Length; i++)
        {
            int col = i % 2, row = i / 2;
            var min = new Vector2(0.04f + col * 0.47f, 0.84f - row * 0.075f);
            CreateSlot(root.transform, SlotOrder[i].type, min, min + new Vector2(0.45f, 0.065f));
        }

        title = CreateText(root.transform, "Title", "출격 가방", 18, new Vector2(0f, 0.66f), new Vector2(1f, 0.71f));
        var gridObject = new GameObject("Grid", typeof(RectTransform));
        gridObject.transform.SetParent(root.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.65f);
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
        RefreshSlots();
        gridView.Bind(LootRuntimeState.RunBagGrid, LootRuntimeState.MoveRunBagItem);
        gridView.SetItemClickHandler(Select);
        gridView.SetItemDoubleClickHandler(MoveToStash);
        title.text = $"출격 가방 {LootRuntimeState.RunBagGrid.Width}×{LootRuntimeState.RunBagGrid.Height}";
        if (selected != null && LootRuntimeState.RunBagGrid.Find(selected.InstanceId) == null) selected = null;
        if (selected == null) gridView.ClearSelection();
        notice.text = selected?.Definition != null ? selected.Definition.Name : "아이템을 선택하세요";
    }

    private void CreateSlot(Transform parent, LootItemType type, Vector2 min, Vector2 max)
    {
        var go = new GameObject($"Slot_{type}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = SlotBackground;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.03f, 0.12f);
        iconRect.anchorMax = new Vector2(0.03f, 0.88f);
        iconRect.sizeDelta = new Vector2(36f, 0f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        Image icon = iconGo.GetComponent<Image>();
        icon.raycastTarget = false;
        Outline frame = iconGo.AddComponent<Outline>();
        frame.effectColor = LootGridView.InsuredColor;
        frame.effectDistance = new Vector2(2f, -2f);
        frame.enabled = false;

        // 이름이 해제 버튼에 가리지 않게 버튼 왼쪽까지만 쓰고, 길면 두 줄·자동 축소.
        Text label = CreateText(go.transform, "Label", "", 12, new Vector2(0.27f, 0.04f), new Vector2(0.73f, 0.96f));
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 9;
        label.resizeTextMaxSize = 12;

        var buttonGo = new GameObject("Unequip", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(go.transform, false);
        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.74f, 0.18f);
        buttonRect.anchorMax = new Vector2(0.97f, 0.82f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
        buttonGo.GetComponent<Image>().color = new Color(0.45f, 0.16f, 0.12f, 0.95f);
        Button unequip = buttonGo.GetComponent<Button>();
        unequip.onClick.AddListener(() =>
        {
            bool ok = LootRuntimeState.TryUnequipToStash(type);
            notice.text = ok ? "Stash로 해제했습니다." : (LootRuntimeState.LastFailureReason ?? "해제 실패");
        });
        CreateText(buttonGo.transform, "Label", "해제", 11, Vector2.zero, Vector2.one);

        slots[type] = (icon, frame, label, unequip);
    }

    private void RefreshSlots()
    {
        foreach (var (type, slotLabel) in SlotOrder)
        {
            if (!slots.TryGetValue(type, out var slot)) continue;
            LootItemInstance item = LootRuntimeState.GetEquipped(type);
            bool insured = item != null && LootRuntimeState.IsInsured(item.InstanceId);
            slot.icon.color = item?.Definition != null ? LootGridView.RarityColor(item.Definition.Rarity) : EmptyIconColor;
            slot.frame.enabled = insured;
            string name = item?.Definition != null ? item.Definition.Name : $"{slotLabel} 없음";
            slot.label.text = (insured ? "[보험] " : "") + name;
            slot.label.color = item == null ? EmptyLabelColor : insured ? LootGridView.InsuredColor : Color.white;
            slot.unequip.gameObject.SetActive(item != null);
        }
    }

    private void Select(LootItemInstance item)
    {
        selected = item;
        notice.text = item?.Definition?.Name ?? "아이템을 선택하세요";
    }

    private void MoveToStash()
    {
        MoveToStash(selected);
    }

    private void MoveToStash(LootItemInstance item)
    {
        if (item == null) return;
        selected = item;
        bool ok = LootRuntimeState.MoveBagItemToStash(item.InstanceId);
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
