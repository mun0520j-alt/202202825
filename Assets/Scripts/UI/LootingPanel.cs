using UnityEngine;
using UnityEngine.UI;

// 왼쪽 월드 보관함과 오른쪽 InventoryPanel 사이의 1Tick 전송만 조정한다.
public sealed class LootingPanel : MonoBehaviour
{
    private static LootingPanel instance;
    private GameObject panelRoot;
    private Text title;
    private Text notice;
    private LootGridView gridView;
    private ILootContainer container;
    private PlayerTurnActor player;

    public static bool IsOpen => instance != null && instance.panelRoot != null && instance.panelRoot.activeSelf;

    private void Awake()
    {
        instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Open(ILootContainer source, PlayerTurnActor actor)
    {
        if (instance == null || source == null || actor == null) return;
        instance.container = source;
        instance.player = actor;
        instance.title.text = source.DisplayName;
        instance.notice.text = "첫 클릭: 선택 / 더블클릭: 이동 (1 Tick)";
        instance.gridView.Bind(source.Grid, source.Grid.Move);
        instance.gridView.SetItemClickHandler(null);
        instance.gridView.SetItemDoubleClickHandler(instance.TakeItem);
        instance.panelRoot.SetActive(true);
        InventoryPanel.OpenForLoot(instance.PutItem);
    }

    public static bool CloseLootOnly()
    {
        if (!IsOpen) return false;
        instance.panelRoot.SetActive(false);
        instance.container = null;
        instance.player = null;
        InventoryPanel.ClearLootTransferHandler();
        InventoryPanel.CloseForEscape();
        return true;
    }

    private void TakeItem(LootItemInstance item)
    {
        if (item == null || container == null || player == null) return;
        bool spent = player.TryPerformTimedAction(TickCost.PickUp, "LootTake", () =>
        {
            if (!LootRuntimeState.TryAddToRunBag(item))
            {
                SetNotice("공간 부족");
                return false;
            }
            if (!container.TryRemove(item.InstanceId, out _))
            {
                LootRuntimeState.DropBagItem(item.InstanceId);
                return false;
            }
            M3MeasurementAutomation.RecordItemAcquired();
            container.HandleContentsChanged();
            return true;
        });
        if (spent) RefreshBoth();
    }

    private void PutItem(LootItemInstance item)
    {
        if (item == null || container == null || player == null) return;
        bool spent = player.TryPerformTimedAction(TickCost.Drop, "LootPut", () =>
        {
            if (!container.TryAdd(item))
            {
                SetNotice("보관함 공간 부족");
                return false;
            }
            LootItemInstance removed = LootRuntimeState.DropBagItem(item.InstanceId);
            if (removed != null)
            {
                M3MeasurementAutomation.RecordItemDroppedToContainer();
                container.HandleContentsChanged();
                return true;
            }
            container.TryRemove(item.InstanceId, out _);
            return false;
        });
        if (spent) RefreshBoth();
    }

    private void RefreshBoth()
    {
        SetNotice("첫 클릭: 선택 / 더블클릭: 이동 (1 Tick)");
        if (container == null)
        {
            InventoryPanel.ClearLootTransferHandler();
            return;
        }
        gridView.Bind(container.Grid, container.Grid.Move);
        gridView.SetItemClickHandler(null);
        gridView.SetItemDoubleClickHandler(TakeItem);
        InventoryPanel.OpenForLoot(PutItem);
    }

    private void SetNotice(string message)
    {
        if (notice != null) notice.text = message;
        if (!string.IsNullOrEmpty(message) && message != "첫 클릭: 선택 / 더블클릭: 이동 (1 Tick)")
            Debug.LogWarning($"[Looting] {message}");
    }

    private void Build()
    {
        Transform hud = ScreenSpaceCanvasProvider.GetOrCreateHudRoot();
        panelRoot = new GameObject("LootingPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(hud, false);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(24f, 0f);
        panelRect.sizeDelta = new Vector2(5 * LootGridView.MinimumCellSize + 28f, 5 * LootGridView.MinimumCellSize + 92f);
        panelRoot.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.06f, 0.97f);

        title = CreateText("Title", panelRoot.transform, 18, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -8f);
        titleRect.sizeDelta = new Vector2(0f, 28f);

        var gridObject = new GameObject("LootGrid", typeof(RectTransform));
        gridObject.transform.SetParent(panelRoot.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(14f, -42f);
        gridView = gridObject.AddComponent<LootGridView>();

        notice = CreateText("Notice", panelRoot.transform, 13, TextAnchor.MiddleCenter);
        RectTransform noticeRect = notice.rectTransform;
        noticeRect.anchorMin = new Vector2(0f, 0f);
        noticeRect.anchorMax = new Vector2(1f, 0f);
        noticeRect.pivot = new Vector2(0.5f, 0f);
        noticeRect.anchoredPosition = new Vector2(0f, 8f);
        noticeRect.sizeDelta = new Vector2(0f, 28f);
        panelRoot.SetActive(false);
    }

    private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = HudFont.GetDefault();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }
}
