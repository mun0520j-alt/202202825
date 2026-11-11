using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 데이터는 소유하지 않고 LootGridModel을 화면에 그리는 재사용 uGUI 컴포넌트다.
public sealed class LootGridView : MonoBehaviour
{
    public const float MinimumCellSize = 48f;
    private const float CellGap = 2f;

    private LootGridModel model;
    private Func<string, int, int, bool, bool> moveHandler;
    private RectTransform gridRoot;
    private RectTransform preview;
    private Text tooltip;
    private LootGridItemView dragged;
    private bool dragRotated;

    public void Bind(LootGridModel source, Func<string, int, int, bool, bool> onMove)
    {
        model = source;
        moveHandler = onMove;
        Build();
    }

    public void Refresh()
    {
        if (model == null) return;
        Build();
    }

    private void Update()
    {
        if (dragged == null || !Input.GetKeyDown(KeyCode.R)) return;
        dragRotated = !dragRotated;
        UpdateDragPreview(Input.mousePosition);
    }

    private void Build()
    {
        if (model == null) return;
        RectTransform ownRect = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        ownRect.sizeDelta = new Vector2(model.Width * MinimumCellSize, model.Height * MinimumCellSize);

        for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediateSafe(transform.GetChild(i).gameObject);

        var background = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.03f, 0.04f, 0.98f);

        var rootObject = new GameObject("GridRoot", typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        gridRoot = rootObject.GetComponent<RectTransform>();
        gridRoot.anchorMin = new Vector2(0f, 1f);
        gridRoot.anchorMax = new Vector2(0f, 1f);
        gridRoot.pivot = new Vector2(0f, 1f);
        gridRoot.sizeDelta = ownRect.sizeDelta;

        for (int y = 0; y < model.Height; y++)
        for (int x = 0; x < model.Width; x++) CreateCell(x, y);

        foreach (LootGridPlacement placement in model.Placements) CreateItem(placement);
        CreatePreview();
        CreateTooltip();
    }

    private void CreateCell(int x, int y)
    {
        var cell = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image));
        cell.transform.SetParent(gridRoot, false);
        RectTransform rect = cell.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x * MinimumCellSize, -y * MinimumCellSize);
        rect.sizeDelta = new Vector2(MinimumCellSize - CellGap, MinimumCellSize - CellGap);
        cell.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
    }

    private void CreateItem(LootGridPlacement placement)
    {
        var itemObject = new GameObject($"Item_{placement.Item.InstanceId}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        itemObject.transform.SetParent(gridRoot, false);
        RectTransform rect = itemObject.GetComponent<RectTransform>();
        SetItemRect(rect, placement.X, placement.Y, placement.Width, placement.Height);
        Image image = itemObject.GetComponent<Image>();
        image.color = RarityColor(placement.Item.Definition.Rarity);

        var labelObject = new GameObject("ShortLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(itemObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        Text label = labelObject.GetComponent<Text>();
        label.font = HudFont.GetDefault();
        label.fontSize = 12;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = ShortName(placement.Item.Definition.Name);
        label.raycastTarget = false;

        LootGridItemView view = itemObject.AddComponent<LootGridItemView>();
        view.Initialize(this, placement.Item, placement.Rotated);
    }

    private void CreatePreview()
    {
        var go = new GameObject("DropPreview", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(gridRoot, false);
        preview = go.GetComponent<RectTransform>();
        go.GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.35f, 0.45f);
        go.SetActive(false);
    }

    private void CreateTooltip()
    {
        var go = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(230f, 86f);
        go.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.025f, 0.97f);
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(go.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 6f);
        textRect.offsetMax = new Vector2(-8f, -6f);
        tooltip = textObject.GetComponent<Text>();
        tooltip.font = HudFont.GetDefault();
        tooltip.fontSize = 13;
        tooltip.alignment = TextAnchor.UpperLeft;
        tooltip.color = Color.white;
        tooltip.raycastTarget = false;
        go.SetActive(false);
    }

    internal void BeginDrag(LootGridItemView item, Vector2 screenPosition)
    {
        dragged = item;
        dragRotated = item.Rotated;
        item.GetComponent<CanvasGroup>().alpha = 0.35f;
        item.GetComponent<CanvasGroup>().blocksRaycasts = false;
        HideTooltip();
        UpdateDragPreview(screenPosition);
    }

    internal void Drag(Vector2 screenPosition)
    {
        UpdateDragPreview(screenPosition);
    }

    internal void EndDrag(Vector2 screenPosition)
    {
        if (dragged == null) return;
        if (TryScreenToCell(screenPosition, out int x, out int y)
            && model.CanMove(dragged.Item.InstanceId, x, y, dragRotated))
            moveHandler?.Invoke(dragged.Item.InstanceId, x, y, dragRotated);
        dragged.GetComponent<CanvasGroup>().alpha = 1f;
        dragged.GetComponent<CanvasGroup>().blocksRaycasts = true;
        dragged = null;
        if (preview != null) preview.gameObject.SetActive(false);
        Refresh();
    }

    internal void ShowTooltip(LootGridItemView item, Vector2 screenPosition)
    {
        if (dragged != null || tooltip == null || item?.Item?.Definition == null) return;
        LootItemData data = item.Item.Definition;
        tooltip.text = $"{data.Name} [{data.Rarity}]\n크기 {data.GridWidth}×{data.GridHeight}"
            + $"\n공격 +{data.AttackBonus} / 방어 +{data.DefenseBonus}";
        tooltip.transform.parent.gameObject.SetActive(true);
        PositionTooltip(screenPosition);
    }

    internal void MoveTooltip(Vector2 screenPosition)
    {
        if (tooltip != null && tooltip.transform.parent.gameObject.activeSelf) PositionTooltip(screenPosition);
    }

    internal void HideTooltip()
    {
        if (tooltip != null) tooltip.transform.parent.gameObject.SetActive(false);
    }

    private void UpdateDragPreview(Vector2 screenPosition)
    {
        if (dragged == null || preview == null || !TryScreenToCell(screenPosition, out int x, out int y))
        {
            if (preview != null) preview.gameObject.SetActive(false);
            return;
        }
        int width = dragRotated ? dragged.Item.Definition.GridHeight : dragged.Item.Definition.GridWidth;
        int height = dragRotated ? dragged.Item.Definition.GridWidth : dragged.Item.Definition.GridHeight;
        SetItemRect(preview, x, y, width, height);
        preview.GetComponent<Image>().color = model.CanMove(dragged.Item.InstanceId, x, y, dragRotated)
            ? new Color(0.2f, 0.9f, 0.35f, 0.45f) : new Color(0.95f, 0.15f, 0.12f, 0.55f);
        preview.gameObject.SetActive(true);
        preview.SetAsLastSibling();
    }

    private bool TryScreenToCell(Vector2 screenPosition, out int x, out int y)
    {
        x = y = -1;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRoot, screenPosition, null, out Vector2 local)) return false;
        x = Mathf.FloorToInt(local.x / MinimumCellSize);
        y = Mathf.FloorToInt(-local.y / MinimumCellSize);
        return x >= 0 && y >= 0 && x < model.Width && y < model.Height;
    }

    private void PositionTooltip(Vector2 screenPosition)
    {
        RectTransform own = transform as RectTransform;
        RectTransform tip = tooltip.transform.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(own, screenPosition, null, out Vector2 local))
            tip.anchoredPosition = local + new Vector2(18f, -18f);
        tip.SetAsLastSibling();
    }

    private static void SetItemRect(RectTransform rect, int x, int y, int width, int height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x * MinimumCellSize, -y * MinimumCellSize);
        rect.sizeDelta = new Vector2(width * MinimumCellSize - CellGap, height * MinimumCellSize - CellGap);
    }

    private static string ShortName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "?";
        return value.Length <= 4 ? value : value.Substring(0, 4);
    }

    private static Color RarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Rare: return new Color(0.18f, 0.45f, 0.85f, 0.96f);
            case LootRarity.Epic: return new Color(0.58f, 0.2f, 0.82f, 0.96f);
            case LootRarity.Unique: return new Color(0.92f, 0.48f, 0.08f, 0.96f);
            case LootRarity.Legendary: return new Color(0.9f, 0.12f, 0.1f, 0.96f);
            default: return new Color(0.42f, 0.45f, 0.48f, 0.96f);
        }
    }

    private static void DestroyImmediateSafe(GameObject target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

public sealed class LootGridItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private LootGridView owner;
    public LootItemInstance Item { get; private set; }
    public bool Rotated { get; private set; }

    public void Initialize(LootGridView grid, LootItemInstance item, bool rotated)
    {
        owner = grid;
        Item = item;
        Rotated = rotated;
    }

    public void OnBeginDrag(PointerEventData eventData) => owner.BeginDrag(this, eventData.position);
    public void OnDrag(PointerEventData eventData) => owner.Drag(eventData.position);
    public void OnEndDrag(PointerEventData eventData) => owner.EndDrag(eventData.position);
    public void OnPointerEnter(PointerEventData eventData) => owner.ShowTooltip(this, eventData.position);
    public void OnPointerExit(PointerEventData eventData) => owner.HideTooltip();
    public void OnPointerMove(PointerEventData eventData) => owner.MoveTooltip(eventData.position);
}
