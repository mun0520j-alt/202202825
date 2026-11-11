using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BaseCamp 전용 영지 정비 UI. 데이터 변경은 LootRuntimeState가 소유한다.
public sealed class BaseCampManagementPanel : MonoBehaviour
{
    private enum SortMode { Acquired, RarityHigh, RarityLow, Type }
    private SortMode sortMode;
    private LootItemType? typeFilter;
    private LootRarity? rarityFilter;
    private Vector2 scroll;
    private string notice;
    private string pendingUniqueSale;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 520, Screen.height - 40), GUI.skin.box);
        GUILayout.Label($"영지 정비   Gold {LootRuntimeState.Gold}");
        GUILayout.Label("장착 장비");
        DrawEquipment(LootItemType.Weapon, "무기");
        DrawEquipment(LootItemType.Shield, "방패");
        DrawEquipment(LootItemType.Armor, "갑옷");

        GUILayout.Space(6);
        GUILayout.Label($"출격 가방 {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}");
        foreach (var item in LootRuntimeState.RunBag.ToArray()) DrawItem(item, false);

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"정렬: {sortMode}")) sortMode = (SortMode)(((int)sortMode + 1) % 4);
        if (GUILayout.Button($"부위: {(typeFilter?.ToString() ?? "전체")}")) CycleType();
        if (GUILayout.Button($"등급: {(rarityFilter?.ToString() ?? "전체")}")) CycleRarity();
        GUILayout.EndHorizontal();

        GUILayout.Label("Stash");
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var item in FilteredStash()) DrawItem(item, true);
        GUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(pendingUniqueSale))
        {
            GUILayout.Label("Unique 장비를 판매할까요?");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("판매 확정")) { LootRuntimeState.TrySell(pendingUniqueSale); pendingUniqueSale = null; }
            if (GUILayout.Button("취소")) pendingUniqueSale = null;
            GUILayout.EndHorizontal();
        }
        if (!string.IsNullOrEmpty(notice)) GUILayout.Label(notice);
        GUILayout.EndArea();
    }

    private void DrawEquipment(LootItemType type, string label)
    {
        var item = LootRuntimeState.GetEquipped(type);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {(item?.Definition?.Name ?? "비어 있음")}", GUILayout.Width(300));
        GUI.enabled = item != null;
        if (GUILayout.Button("해제 → Stash")) LootRuntimeState.TryUnequipToStash(type);
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawItem(LootItemInstance item, bool inStash)
    {
        if (item?.Definition == null) return;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"[{item.Definition.Rarity}] {item.Definition.Name}", GUILayout.Width(245));
        if (GUILayout.Button(inStash ? "가방" : "Stash", GUILayout.Width(60)))
        {
            bool ok = inStash ? LootRuntimeState.MoveStashItemToBag(item.InstanceId) : LootRuntimeState.MoveBagItemToStash(item.InstanceId);
            notice = ok ? null : "출격 가방이 가득 찼습니다.";
        }
        if (item.Definition.Type != LootItemType.Bag && GUILayout.Button("장착", GUILayout.Width(55)))
        {
            if (inStash) LootRuntimeState.TryEquipFromStash(item.InstanceId);
            else LootRuntimeState.TryEquipFromBaseCampBag(item.InstanceId);
        }
        if (GUILayout.Button($"판매 {item.Definition.SellPrice}G", GUILayout.Width(80)))
        {
            if (item.Definition.Rarity >= LootRarity.Unique) pendingUniqueSale = item.InstanceId;
            else LootRuntimeState.TrySell(item.InstanceId);
        }
        GUILayout.EndHorizontal();
    }

    private IEnumerable<LootItemInstance> FilteredStash()
    {
        IEnumerable<LootItemInstance> items = LootRuntimeState.Stash;
        if (typeFilter.HasValue) items = items.Where(x => x.Definition.Type == typeFilter.Value);
        if (rarityFilter.HasValue) items = items.Where(x => x.Definition.Rarity == rarityFilter.Value);
        switch (sortMode)
        {
            case SortMode.RarityHigh: return items.OrderByDescending(x => x.Definition.Rarity).ThenBy(x => x.Definition.Name).ToArray();
            case SortMode.RarityLow: return items.OrderBy(x => x.Definition.Rarity).ThenBy(x => x.Definition.Name).ToArray();
            case SortMode.Type: return items.OrderBy(x => x.Definition.Type).ThenByDescending(x => x.Definition.Rarity).ToArray();
            default: return items.ToArray();
        }
    }

    private void CycleType()
    {
        if (!typeFilter.HasValue) typeFilter = LootItemType.Weapon;
        else if (typeFilter == LootItemType.Weapon) typeFilter = LootItemType.Shield;
        else if (typeFilter == LootItemType.Shield) typeFilter = LootItemType.Armor;
        else typeFilter = null;
    }

    private void CycleRarity()
    {
        if (!rarityFilter.HasValue) rarityFilter = LootRarity.Normal;
        else if (rarityFilter == LootRarity.Normal) rarityFilter = LootRarity.Rare;
        else if (rarityFilter == LootRarity.Rare) rarityFilter = LootRarity.Epic;
        else if (rarityFilter == LootRarity.Epic) rarityFilter = LootRarity.Unique;
        else rarityFilter = null;
    }
}
