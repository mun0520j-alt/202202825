using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BaseCamp 전용 영지 정비 UI. 데이터 변경은 LootRuntimeState가 소유한다.
public sealed class BaseCampManagementPanel : MonoBehaviour
{
    private enum SortMode { Acquired, RarityHigh, RarityLow, Type }
    private enum MerchantMode { Sell, Buy }
    private SortMode sortMode;
    private MerchantMode merchantMode;
    private LootItemType? typeFilter;
    private LootRarity? rarityFilter;
    private Vector2 merchantScroll;
    private Vector2 stashScroll;
    private string notice;
    private string pendingUniqueSale;

    private void OnGUI()
    {
        float sideWidth = Mathf.Clamp(Screen.width * 0.24f, 360f, 480f);
        DrawMerchantPanel(new Rect(20, 20, sideWidth, Screen.height - 40));
        float gridReserve = Mathf.Clamp(Screen.width * 0.25f, 300f, 420f);
        DrawStashPanel(new Rect(Screen.width - sideWidth - gridReserve - 40, 20, sideWidth, Screen.height - 40));
    }

    private void DrawMerchantPanel(Rect area)
    {
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label($"상인   Gold {LootRuntimeState.Gold}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(merchantMode == MerchantMode.Sell, "판매", GUI.skin.button)) merchantMode = MerchantMode.Sell;
        if (GUILayout.Toggle(merchantMode == MerchantMode.Buy, "구매", GUI.skin.button)) merchantMode = MerchantMode.Buy;
        GUILayout.EndHorizontal();

        merchantScroll = GUILayout.BeginScrollView(merchantScroll);
        if (merchantMode == MerchantMode.Sell)
        {
            GUILayout.Label("Stash");
            foreach (var item in LootRuntimeState.Stash.ToArray()) DrawSaleItem(item, "Stash");
            GUILayout.Space(6);
            GUILayout.Label("출격 가방");
            foreach (var item in LootRuntimeState.RunBag.ToArray()) DrawSaleItem(item, "가방");
        }
        else
        {
            foreach (LootItemData item in LootItemDatabase.Items.Where(LootRuntimeState.IsMerchantPurchaseItem)) DrawPurchaseItem(item);
        }
        GUILayout.EndScrollView();

        DrawUniqueSaleConfirmation();
        if (!string.IsNullOrEmpty(notice)) GUILayout.Label(notice);
        GUILayout.EndArea();
    }

    private void DrawStashPanel(Rect area)
    {
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("장비 · 가방 · Stash");
        GUILayout.Label("장착 장비");
        DrawEquipment(LootItemType.Weapon, "무기");
        DrawEquipment(LootItemType.Shield, "방패");
        DrawEquipment(LootItemType.Armor, "갑옷");
        DrawEquipment(LootItemType.Bag, "가방");
        DrawInsurancePanel();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"정렬: {sortMode}")) sortMode = (SortMode)(((int)sortMode + 1) % 4);
        if (GUILayout.Button($"부위: {(typeFilter?.ToString() ?? "전체")}")) CycleType();
        if (GUILayout.Button($"등급: {(rarityFilter?.ToString() ?? "전체")}")) CycleRarity();
        GUILayout.EndHorizontal();

        GUILayout.Label("Stash");
        stashScroll = GUILayout.BeginScrollView(stashScroll);
        foreach (var item in FilteredStash()) DrawStorageItem(item, true);
        GUILayout.EndScrollView();
        if (!string.IsNullOrEmpty(notice)) GUILayout.Label(notice);
        GUILayout.EndArea();
    }

    private void DrawEquipment(LootItemType type, string label)
    {
        var item = LootRuntimeState.GetEquipped(type);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {(item?.Definition?.Name ?? "비어 있음")}", GUILayout.Width(240));
        GUI.enabled = item != null;
        if (GUILayout.Button("해제 → Stash"))
        {
            if (!LootRuntimeState.TryUnequipToStash(type)) notice = LootRuntimeState.LastFailureReason;
            else notice = null;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawInsurancePanel()
    {
        GUILayout.Space(8);
        GUILayout.Label("보험 (장비에 거는 보험 — 잃을 때까지 유지)");
        foreach (var item in LootRuntimeState.InsurableCandidates.ToArray())
        {
            bool selected = LootRuntimeState.IsInsuranceSelected(item.InstanceId);
            bool next = GUILayout.Toggle(selected, $"{item.Definition.Name} ({Mathf.CeilToInt(item.Definition.SellPrice * M7EconomyConfig.InsurancePremiumRate)}G)");
            if (next != selected) LootRuntimeState.ToggleInsuranceSelection(item.InstanceId);
        }
        if (LootRuntimeState.SelectedInsurancePremium > 0
            && GUILayout.Button($"보험 가입 ({LootRuntimeState.SelectedInsurancePremium}G)"))
        {
            notice = LootRuntimeState.TryConfirmInsurance() ? "보험됨" : LootRuntimeState.LastFailureReason;
        }

        foreach (var entry in LootRuntimeState.InsuranceReturns.ToArray())
        {
            string itemName = LootItemDatabase.FindById(entry.ItemId)?.Name ?? entry.ItemId;
            if (!entry.Revealed)
            {
                GUILayout.Label($"{itemName} — 반환 확인 대기 {Mathf.CeilToInt(entry.WaitTicksRemaining)} Tick");
                continue;
            }
            GUILayout.BeginHorizontal();
            if (!entry.Returned)
            {
                GUILayout.Label($"{itemName} — 보험 회수 실패");
                if (GUILayout.Button("확인", GUILayout.Width(55)))
                    notice = LootRuntimeState.TryDismissInsuranceResult(entry.ReturnId) ? null : LootRuntimeState.LastFailureReason;
            }
            else
            {
                GUILayout.Label($"{itemName} — 수령 {Mathf.CeilToInt(entry.ClaimTicksRemaining)} Tick");
                if (GUILayout.Button("수령", GUILayout.Width(55)))
                    notice = LootRuntimeState.TryClaimInsurance(entry.ReturnId) ? "보험품 수령 완료" : LootRuntimeState.LastFailureReason;
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawStorageItem(LootItemInstance item, bool inStash)
    {
        if (item?.Definition == null) return;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"[{item.Definition.Rarity}] {(LootRuntimeState.IsInsured(item.InstanceId) ? "[보험] " : "")}{item.Definition.Name}", GUILayout.Width(170));
        if (GUILayout.Button(inStash ? "가방" : "Stash", GUILayout.Width(60)))
        {
            bool ok = inStash ? LootRuntimeState.MoveStashItemToBag(item.InstanceId) : LootRuntimeState.MoveBagItemToStash(item.InstanceId);
            notice = ok ? null : "출격 가방이 가득 찼습니다.";
        }
        GUI.enabled = item.Definition.Type != LootItemType.Junk && item.Definition.Type != LootItemType.Consumable;
        if (GUILayout.Button("장착", GUILayout.Width(55)))
        {
            bool equipped = inStash
                ? LootRuntimeState.TryEquipFromStash(item.InstanceId)
                : LootRuntimeState.TryEquipFromBaseCampBag(item.InstanceId);
            notice = equipped ? null : LootRuntimeState.LastFailureReason;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawPurchaseItem(LootItemData item)
    {
        int price = item.SellPrice * M7EconomyConfig.PurchasePriceMultiplier;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"[{item.Rarity}] {item.Name}");
        GUI.enabled = LootRuntimeState.Gold >= price;
        if (GUILayout.Button($"{price}G", GUILayout.Width(70)))
        {
            bool purchased = LootRuntimeState.TryBuy(item.Id);
            notice = purchased ? $"{item.Name} 구매 완료 — Stash로 이동" : LootRuntimeState.LastFailureReason;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawSaleItem(LootItemInstance item, string source)
    {
        if (item?.Definition == null) return;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"[{source}] [{item.Definition.Rarity}] {(LootRuntimeState.IsInsured(item.InstanceId) ? "[보험] " : "")}{item.Definition.Name}");
        if (GUILayout.Button($"{item.Definition.SellPrice}G", GUILayout.Width(70)))
        {
            if (item.Definition.Rarity >= LootRarity.Unique) pendingUniqueSale = item.InstanceId;
            else if (LootRuntimeState.TrySell(item.InstanceId)) notice = $"{item.Definition.Name} 판매 완료";
        }
        GUILayout.EndHorizontal();
    }

    private void DrawUniqueSaleConfirmation()
    {
        if (string.IsNullOrEmpty(pendingUniqueSale)) return;
        GUILayout.Label("Unique 장비를 판매할까요?");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("판매 확정"))
        {
            LootRuntimeState.TrySell(pendingUniqueSale);
            pendingUniqueSale = null;
        }
        if (GUILayout.Button("취소")) pendingUniqueSale = null;
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
        else if (typeFilter == LootItemType.Armor) typeFilter = LootItemType.Bag;
        else if (typeFilter == LootItemType.Bag) typeFilter = LootItemType.Junk;
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
