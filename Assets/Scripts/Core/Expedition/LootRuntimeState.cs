using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LootRuntimeState
{
    public const int BaseBagGridWidth = 3;
    public const int BaseBagGridHeight = 2;
    public const int BaseBagCapacity = BaseBagGridWidth * BaseBagGridHeight;

    private static readonly List<LootItemInstance> runBag = new List<LootItemInstance>();
    private static readonly List<LootItemInstance> stash = new List<LootItemInstance>();
    private static readonly Dictionary<LootItemType, LootItemInstance> equippedLoot = new Dictionary<LootItemType, LootItemInstance>();
    private static readonly HashSet<LootItemType> unequippedBasicSlots = new HashSet<LootItemType>();
    private static LootGridModel runBagGrid = new LootGridModel(BaseBagGridWidth, BaseBagGridHeight);
    private static readonly HashSet<string> insuranceSelection = new HashSet<string>();
    private static readonly HashSet<string> insuredThisRun = new HashSet<string>();
    private static readonly Dictionary<string, string> insuredItemIds = new Dictionary<string, string>();
    private static readonly HashSet<string> droppedInsuredThisRun = new HashSet<string>();
    private static readonly List<InsuranceReturn> insuranceReturns = new List<InsuranceReturn>();

    public static IReadOnlyList<LootItemInstance> RunBag => runBag;
    public static IReadOnlyList<LootItemInstance> Stash => stash;
    public static IReadOnlyDictionary<LootItemType, LootItemInstance> EquippedLoot => equippedLoot;
    public static LootGridModel RunBagGrid => runBagGrid;
    public static int EquippedBagCapacityBonus { get; private set; }
    public static bool HasBasicLoadout { get; private set; }
    public static int Gold { get; private set; }
    public static int BagCapacity => runBagGrid.Width * runBagGrid.Height;
    public static string LastFailureReason { get; private set; }
    public static event Action Changed;
    public static IReadOnlyList<InsuranceReturn> InsuranceReturns => insuranceReturns;
    public static IEnumerable<string> InsuredThisRunIds => insuredThisRun;
    public static string GetInsuredItemId(string instanceId) => insuredItemIds.TryGetValue(instanceId, out string itemId) ? itemId : null;
    public static float EffectiveInsuranceReturnRate => Mathf.Clamp01(M7EconomyConfig.InsuranceReturnRate
        + (QuestRuntimeState.ReputationLevel - 1) * M8EconomyConfig.InsuranceBonusPerLevel);
    public static int GetSellPrice(LootItemData item) => item == null ? 0 : Mathf.Max(0,
        Mathf.RoundToInt(item.SellPrice * (1f + (QuestRuntimeState.ReputationLevel - 1) * M8EconomyConfig.SellBonusPerLevel)));
    public static int GetPurchasePrice(LootItemData item) => item == null ? 0 : Mathf.Max(0,
        Mathf.RoundToInt(item.SellPrice * M7EconomyConfig.PurchasePriceMultiplier
            * (1f - (QuestRuntimeState.ReputationLevel - 1) * M8EconomyConfig.BuyDiscountPerLevel)));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        runBag.Clear();
        runBagGrid = new LootGridModel(BaseBagGridWidth, BaseBagGridHeight);
        stash.Clear();
        equippedLoot.Clear();
        unequippedBasicSlots.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
        Gold = 0;
        LastFailureReason = null;
        Changed = null;
        insuranceSelection.Clear();
        insuredThisRun.Clear();
        insuredItemIds.Clear();
        droppedInsuredThisRun.Clear();
        insuranceReturns.Clear();
    }

    public static void BeginNewRun()
    {
        if (HasBasicLoadout) return;
        HasBasicLoadout = true;
        Debug.Log("[Loadout] 장비가 없어 기본 검·방패·갑옷을 무료 지급했습니다.");
    }

    public static bool TryAddToRunBag(LootItemInstance item)
    {
        if (item == null || !runBagGrid.TryAutoPlace(item, out _)) return Fail("공간 부족");
        runBag.Add(item);
        droppedInsuredThisRun.Remove(item.InstanceId);
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool MoveBagItemToStash(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item == null) return false;
        runBagGrid.Remove(instanceId, out _);
        runBag.Remove(item);
        stash.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static LootItemInstance DropBagItem(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item == null) return null;
        runBagGrid.Remove(instanceId, out _);
        runBag.Remove(item);
        if (insuredThisRun.Contains(instanceId)) droppedInsuredThisRun.Add(instanceId);
        Changed?.Invoke();
        return item;
    }

    public static bool TryEquipFromBag(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        if (item.Definition.Type == LootItemType.Junk || item.Definition.Type == LootItemType.Consumable) return Fail("이 아이템은 장착할 수 없습니다.");
        if (item.Definition.Type == LootItemType.Bag) return TryEquipBagFromRunBag(item);
        LootGridPlacement original = runBagGrid.Find(instanceId);
        if (original == null) return Fail("가방 배치 정보를 찾을 수 없습니다.");
        int originalX = original.X;
        int originalY = original.Y;
        bool originalRotated = original.Rotated;
        equippedLoot.TryGetValue(item.Definition.Type, out var previous);
        bool replacingBasic = previous == null && item.Definition.Type != LootItemType.Bag
            && !unequippedBasicSlots.Contains(item.Definition.Type);
        LootItemInstance previousForBag = previous;
        if (replacingBasic)
        {
            string basicId = $"basic_{item.Definition.Type.ToString().ToLowerInvariant()}";
            LootItemData basicDefinition = LootItemDatabase.FindById(basicId);
            if (basicDefinition != null) previousForBag = new LootItemInstance(basicDefinition);
        }
        runBagGrid.Remove(instanceId, out _);
        runBag.Remove(item);
        if (previousForBag != null && !runBagGrid.TryAutoPlace(previousForBag, out _))
        {
            runBag.Add(item);
            runBagGrid.TryPlace(item, originalX, originalY, originalRotated);
            return Fail("공간 부족");
        }
        if (previousForBag != null) runBag.Add(previousForBag);
        if (replacingBasic && previousForBag != null) unequippedBasicSlots.Add(item.Definition.Type);
        equippedLoot[item.Definition.Type] = item;
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static LootItemInstance GetEquipped(LootItemType type)
    {
        return equippedLoot.TryGetValue(type, out var item) ? item : null;
    }

    public static bool IsBasicSlotUnequipped(LootItemType type)
    {
        return unequippedBasicSlots.Contains(type);
    }

    public static bool TryStoreUnequippedBasic(LootItemType type, LootItemInstance item)
    {
        if (item == null || !runBagGrid.TryAutoPlace(item, out _)) return Fail("공간 부족");
        unequippedBasicSlots.Add(type);
        runBag.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static bool TryUnequipToBag(LootItemType type)
    {
        if (!equippedLoot.TryGetValue(type, out var item) || item == null) return Fail("해제할 장비가 없습니다.");
        if (type == LootItemType.Bag) return TryUnequipBagToRunBag(item);
        if (!runBagGrid.TryAutoPlace(item, out _)) return Fail("공간 부족");
        equippedLoot.Remove(type);
        runBag.Add(item);
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    private static bool TryEquipBagFromRunBag(LootItemInstance newBag)
    {
        int width = newBag.Definition.BagGridWidth;
        int height = newBag.Definition.BagGridHeight;
        if (width <= 0 || height <= 0) return Fail("가방 격자 정보가 없습니다.");

        var repackedItems = new List<LootItemInstance>();
        foreach (LootItemInstance candidate in runBag)
            if (candidate != null && candidate.InstanceId != newBag.InstanceId) repackedItems.Add(candidate);
        equippedLoot.TryGetValue(LootItemType.Bag, out LootItemInstance previousBag);
        if (previousBag != null) repackedItems.Add(previousBag);

        if (!TryBuildRepackedGrid(width, height, repackedItems, out LootGridModel candidateGrid))
            return Fail("새 가방에 기존 아이템과 가방을 모두 넣을 공간이 없습니다.");

        runBag.Clear();
        runBag.AddRange(repackedItems);
        runBagGrid = candidateGrid;
        equippedLoot[LootItemType.Bag] = newBag;
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    private static bool TryUnequipBagToRunBag(LootItemInstance equippedBag)
    {
        var repackedItems = new List<LootItemInstance>(runBag) { equippedBag };
        if (!TryBuildRepackedGrid(BaseBagGridWidth, BaseBagGridHeight, repackedItems, out LootGridModel candidateGrid))
            return Fail("기본 가방에 기존 아이템과 장착 가방을 모두 넣을 공간이 없습니다.");

        runBag.Clear();
        runBag.AddRange(repackedItems);
        runBagGrid = candidateGrid;
        equippedLoot.Remove(LootItemType.Bag);
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    private static bool TryBuildRepackedGrid(int width, int height, List<LootItemInstance> items,
        out LootGridModel candidateGrid)
    {
        candidateGrid = new LootGridModel(width, height);
        var placementOrder = new List<LootItemInstance>(items);
        placementOrder.Sort((left, right) => ItemArea(right).CompareTo(ItemArea(left)));
        foreach (LootItemInstance item in placementOrder)
            if (item == null || !candidateGrid.TryAutoPlace(item, out _)) return false;
        return true;
    }

    private static int ItemArea(LootItemInstance item)
    {
        return item?.Definition == null ? 0 : item.Definition.GridWidth * item.Definition.GridHeight;
    }

    public static bool TryUnequipToStash(LootItemType type)
    {
        if (!equippedLoot.TryGetValue(type, out var item) || item == null) return Fail("해제할 장비가 없습니다.");
        if (type == LootItemType.Bag) MoveAllRunBagItemsToStash();
        equippedLoot.Remove(type);
        stash.Add(item);
        RefreshBagCapacity();
        if (type == LootItemType.Bag) CreateRunBagGridForEquippedBag();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool MoveStashItemToBag(string instanceId)
    {
        var item = stash.Find(candidate => candidate.InstanceId == instanceId);
        if (item == null || !runBagGrid.TryAutoPlace(item, out _)) return Fail("공간 부족");
        stash.Remove(item);
        runBag.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static bool TryEquipFromStash(string instanceId)
    {
        var item = stash.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        if (item.Definition.Type == LootItemType.Junk || item.Definition.Type == LootItemType.Consumable) return Fail("이 아이템은 장착할 수 없습니다.");
        if (item.Definition.Type == LootItemType.Bag) MoveAllRunBagItemsToStash();
        stash.Remove(item);
        if (equippedLoot.TryGetValue(item.Definition.Type, out var previous) && previous != null) stash.Add(previous);
        equippedLoot[item.Definition.Type] = item;
        unequippedBasicSlots.Remove(item.Definition.Type);
        RefreshBagCapacity();
        if (item.Definition.Type == LootItemType.Bag) CreateRunBagGridForEquippedBag();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TryEquipFromBaseCampBag(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        if (item.Definition.Type == LootItemType.Junk || item.Definition.Type == LootItemType.Consumable) return Fail("이 아이템은 장착할 수 없습니다.");
        runBagGrid.Remove(instanceId, out _);
        runBag.Remove(item);
        if (item.Definition.Type == LootItemType.Bag) MoveAllRunBagItemsToStash();
        if (equippedLoot.TryGetValue(item.Definition.Type, out var previous) && previous != null) stash.Add(previous);
        equippedLoot[item.Definition.Type] = item;
        unequippedBasicSlots.Remove(item.Definition.Type);
        RefreshBagCapacity();
        if (item.Definition.Type == LootItemType.Bag) CreateRunBagGridForEquippedBag();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TrySell(string instanceId)
    {
        var item = stash.Find(candidate => candidate.InstanceId == instanceId)
            ?? runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null || item.Definition.Id.StartsWith("basic_", StringComparison.Ordinal)) return false;
        if (!stash.Remove(item))
        {
            runBagGrid.Remove(item.InstanceId, out _);
            runBag.Remove(item);
        }
        Gold += GetSellPrice(item.Definition);
        insuredThisRun.Remove(item.InstanceId);
        insuredItemIds.Remove(item.InstanceId);
        Changed?.Invoke();
        return true;
    }

    public static bool TryBuy(string itemId)
    {
        LootItemData definition = LootItemDatabase.FindById(itemId);
        if (definition == null) return Fail("구매할 아이템을 찾을 수 없습니다.");
        if (!IsMerchantPurchaseItem(definition)) return Fail("상인이 판매하지 않는 아이템입니다.");

        int price = GetPurchasePrice(definition);
        if (Gold < price) return Fail("Gold가 부족합니다.");

        Gold -= price;
        stash.Add(new LootItemInstance(definition));
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TrySpendGold(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Gold < amount) return Fail("Gold가 부족합니다.");
        Gold -= amount;
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static void AddGold(int amount)
    {
        Gold += Mathf.Max(0, amount);
        Changed?.Invoke();
    }

    public static void AddRewardToStash(LootItemInstance item)
    {
        if (item == null) return;
        stash.Add(item);
        Changed?.Invoke();
    }

    public static bool TryConsumeQuestItems(IEnumerable<string> instanceIds)
    {
        var ids = new HashSet<string>(instanceIds ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        var items = stash.Concat(runBag).Where(item => item != null && ids.Contains(item.InstanceId)).ToList();
        if (items.Count != ids.Count) return Fail("납품 아이템 상태가 변경되었습니다.");
        foreach (LootItemInstance item in items)
        {
            stash.Remove(item);
            if (runBag.Remove(item)) runBagGrid.Remove(item.InstanceId, out _);
            insuredThisRun.Remove(item.InstanceId);
            insuredItemIds.Remove(item.InstanceId);
            droppedInsuredThisRun.Remove(item.InstanceId);
        }
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool IsMerchantPurchaseItem(LootItemData definition)
    {
        if (definition == null || definition.Id.StartsWith("basic_", StringComparison.Ordinal)) return false;
        if (definition.Id == "escape_scroll") return true;
        if (definition.Type == LootItemType.Bag)
            return definition.Rarity == LootRarity.Normal || definition.Rarity == LootRarity.Rare;
        return definition.Rarity == LootRarity.Normal
            && (definition.Type == LootItemType.Weapon || definition.Type == LootItemType.Shield || definition.Type == LootItemType.Armor);
    }

    public static bool TryConsumeRunBagItem(string instanceId, string expectedItemId)
    {
        LootItemInstance item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null || !string.Equals(item.Definition.Id, expectedItemId, StringComparison.Ordinal))
            return Fail("사용할 아이템을 찾을 수 없습니다.");
        runBagGrid.Remove(instanceId, out _);
        runBag.Remove(item);
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static void ResolveRun(ExpeditionResult result)
    {
        AdvanceInsuranceTicks(ExpeditionSession.ElapsedTicks);
        ResolveCurrentInsurance(result, ExpeditionSession.CurrentFloor);
        if (result == ExpeditionResult.Escaped) return;
        runBag.Clear();
        runBagGrid = new LootGridModel(BaseBagGridWidth, BaseBagGridHeight);
        equippedLoot.Clear();
        unequippedBasicSlots.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
        Changed?.Invoke();
    }

    // 보험은 원정이 아니라 장비 인스턴스에 건다 (사용자 결정 2026-11-12).
    // insuredThisRun은 이름과 달리 "보험에 든 장비" 영구 목록이다. 세이브 호환을 위해 필드 이름은 유지한다.
    // 가입하면 그 장비를 잃을 때(사망·버림 생환·MIA)까지 유지되고, 보험료는 가입할 때 한 번만 낸다.
    public static bool IsInsuranceSelected(string instanceId) => insuranceSelection.Contains(instanceId);
    public static bool IsInsured(string instanceId) => !string.IsNullOrEmpty(instanceId) && insuredThisRun.Contains(instanceId);
    public static bool IsInsuredThisRun(string instanceId) => IsInsured(instanceId);
    public static bool HasConfirmedInsurance => insuredThisRun.Count > 0;

    public static bool IsInsurable(LootItemInstance item)
    {
        if (item?.Definition == null || item.Definition.Id.StartsWith("basic_", StringComparison.Ordinal)) return false;
        LootItemType type = item.Definition.Type;
        return type == LootItemType.Weapon || type == LootItemType.Shield || type == LootItemType.Armor || type == LootItemType.Bag;
    }

    public static IEnumerable<LootItemInstance> InsurableCandidates =>
        equippedLoot.Values.Concat(runBag).Where(item => IsInsurable(item) && !IsInsured(item.InstanceId));

    public static int SelectedInsurancePremium => InsurableCandidates
        .Where(item => insuranceSelection.Contains(item.InstanceId))
        .Sum(item => Mathf.CeilToInt(item.Definition.SellPrice * M7EconomyConfig.InsurancePremiumRate));

    public static void ToggleInsuranceSelection(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId) || IsInsured(instanceId)) return;
        if (!insuranceSelection.Add(instanceId)) insuranceSelection.Remove(instanceId);
        Changed?.Invoke();
    }

    public static bool TryConfirmInsurance()
    {
        var eligible = InsurableCandidates.Where(item => insuranceSelection.Contains(item.InstanceId)).ToArray();
        if (eligible.Length == 0) return Fail("보험에 가입할 장비를 선택하세요.");
        int premium = eligible.Sum(item => Mathf.CeilToInt(item.Definition.SellPrice * M7EconomyConfig.InsurancePremiumRate));
        if (Gold < premium) return Fail("보험료를 낼 Gold가 부족합니다.");
        Gold -= premium;
        foreach (var item in eligible)
        {
            insuredThisRun.Add(item.InstanceId);
            insuredItemIds[item.InstanceId] = item.Definition.Id;
        }
        insuranceSelection.Clear();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TryDismissInsuranceResult(string returnId)
    {
        InsuranceReturn entry = insuranceReturns.Find(value => value.ReturnId == returnId);
        if (entry == null || !entry.Revealed || entry.Returned) return Fail("확인할 보험 결과가 없습니다.");
        insuranceReturns.Remove(entry);
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TryClaimInsurance(string returnId)
    {
        InsuranceReturn entry = insuranceReturns.Find(value => value.ReturnId == returnId);
        if (entry == null || !entry.Revealed || !entry.Returned || entry.ClaimTicksRemaining <= 0f)
            return Fail("수령할 수 없는 보험품입니다.");
        LootItemData definition = LootItemDatabase.FindById(entry.ItemId);
        if (definition == null) return Fail("보험품 데이터를 찾을 수 없습니다.");
        stash.Add(new LootItemInstance(definition, entry.InstanceId));
        insuranceReturns.Remove(entry);
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static void AdvanceInsuranceTicks(float ticks)
    {
        if (ticks <= 0f) return;
        for (int i = insuranceReturns.Count - 1; i >= 0; i--)
        {
            InsuranceReturn entry = insuranceReturns[i];
            if (!entry.Revealed)
            {
                entry.WaitTicksRemaining -= ticks;
                if (entry.WaitTicksRemaining <= 0f) entry.Revealed = true;
            }
            else if (entry.Returned)
            {
                entry.ClaimTicksRemaining -= ticks;
                if (entry.ClaimTicksRemaining <= 0f) insuranceReturns.RemoveAt(i);
            }
        }
        Changed?.Invoke();
    }

    // 원정 종료 시 보험 판정. 생환: 버린 보험 장비만 판정. 사망: 가지고 들어간 보험 장비(장착·가방)와 버린 것 판정.
    // MIA·8층 이상: 잃은 보험 장비는 반환 없이 보험만 종료. 판정을 받은 장비의 보험은 끝난다(반환품은 보험 없음).
    private static void ResolveCurrentInsurance(ExpeditionResult result, int reachedFloor)
    {
        var carried = new HashSet<string>(equippedLoot.Values.Where(item => item != null).Select(item => item.InstanceId)
            .Concat(runBag.Where(item => item != null).Select(item => item.InstanceId)));
        bool eligibleResult = result != ExpeditionResult.MIA && reachedFloor < 8;
        foreach (string instanceId in insuredThisRun.ToArray())
        {
            bool dropped = droppedInsuredThisRun.Contains(instanceId);
            bool lostWithPlayer = result != ExpeditionResult.Escaped && carried.Contains(instanceId);
            if (!dropped && !lostWithPlayer) continue;
            if (eligibleResult && insuredItemIds.TryGetValue(instanceId, out string itemId))
            {
                bool returned = new System.Random(Guid.NewGuid().GetHashCode()).NextDouble() < EffectiveInsuranceReturnRate;
                insuranceReturns.Add(new InsuranceReturn
                {
                    ReturnId = Guid.NewGuid().ToString("N"), ItemId = itemId, InstanceId = instanceId,
                    Returned = returned, Revealed = false,
                    WaitTicksRemaining = M7EconomyConfig.InsuranceWaitTicks,
                    ClaimTicksRemaining = M7EconomyConfig.InsuranceClaimTicks
                });
            }
            insuredThisRun.Remove(instanceId);
            insuredItemIds.Remove(instanceId);
        }
        droppedInsuredThisRun.Clear();
        insuranceSelection.Clear();
    }

    public static void RestoreInsurance(IEnumerable<InsuranceReturn> restored)
    {
        insuranceReturns.Clear();
        if (restored != null) insuranceReturns.AddRange(restored.Where(value => value != null));
    }

    public static void RestoreConfirmedInsurance(IEnumerable<InsurancePolicy> restored)
    {
        insuredThisRun.Clear();
        insuredItemIds.Clear();
        if (restored == null) return;
        foreach (InsurancePolicy policy in restored)
        {
            if (policy == null || string.IsNullOrEmpty(policy.InstanceId) || string.IsNullOrEmpty(policy.ItemId)) continue;
            insuredThisRun.Add(policy.InstanceId);
            insuredItemIds[policy.InstanceId] = policy.ItemId;
        }
    }

    // 다음 실행 MIA 처리 등으로 사라진 장비의 보험을 반환 없이 정리한다. 남아 있는 보험 장비는 유지한다.
    public static void ClearConfirmedInsuranceWithoutReturn()
    {
        var existing = new HashSet<string>(equippedLoot.Values.Concat(runBag).Concat(stash)
            .Where(item => item != null).Select(item => item.InstanceId));
        foreach (string instanceId in insuredThisRun.ToArray())
        {
            if (existing.Contains(instanceId)) continue;
            insuredThisRun.Remove(instanceId);
            insuredItemIds.Remove(instanceId);
        }
        droppedInsuredThisRun.Clear();
        insuranceSelection.Clear();
        Changed?.Invoke();
    }

    [Serializable]
    public sealed class InsuranceReturn
    {
        public string ReturnId;
        public string ItemId;
        public string InstanceId;
        public bool Returned;
        public bool Revealed;
        public float WaitTicksRemaining;
        public float ClaimTicksRemaining;
    }

    [Serializable]
    public sealed class InsurancePolicy
    {
        public string InstanceId;
        public string ItemId;
    }

    public static void RestorePersistentState(
        IEnumerable<LootGridRestoreItem> restoredRunBag,
        IEnumerable<LootItemInstance> restoredStash,
        IEnumerable<LootItemInstance> restoredEquipment,
        IEnumerable<LootItemType> restoredUnequippedBasicSlots,
        bool hasBasicLoadout,
        int gold = 0)
    {
        runBag.Clear();
        stash.Clear();
        equippedLoot.Clear();
        unequippedBasicSlots.Clear();

        if (restoredStash != null) stash.AddRange(restoredStash);
        if (restoredEquipment != null)
        {
            foreach (var item in restoredEquipment)
            {
                if (item?.Definition == null) continue;
                equippedLoot[item.Definition.Type] = item;
            }
        }
        if (restoredUnequippedBasicSlots != null)
        {
            foreach (var type in restoredUnequippedBasicSlots) unequippedBasicSlots.Add(type);
        }

        HasBasicLoadout = hasBasicLoadout;
        Gold = Mathf.Max(0, gold);
        RefreshBagCapacity();
        CreateRunBagGridForEquippedBag();
        if (restoredRunBag != null)
        {
            int moved = 0;
            foreach (var restored in restoredRunBag)
            {
                LootItemInstance item = restored?.Item;
                bool placed = item != null && (restored.HasPlacement
                    ? runBagGrid.TryPlace(item, restored.X, restored.Y, restored.Rotated)
                    : runBagGrid.TryAutoPlace(item, out _));
                if (placed) runBag.Add(item);
                else if (item != null)
                {
                    stash.Add(item);
                    moved++;
                }
            }
            if (moved > 0) Debug.LogWarning($"[Loadout] 격자에 들어가지 않는 기존 세이브 아이템 {moved}개를 Stash로 이동했습니다.");
        }
        Changed?.Invoke();
    }

    public static bool MoveRunBagItem(string instanceId, int x, int y, bool rotated)
    {
        if (!runBagGrid.Move(instanceId, x, y, rotated)) return Fail("해당 위치에 놓을 수 없습니다");
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    private static void RefreshBagCapacity()
    {
        EquippedBagCapacityBonus = equippedLoot.TryGetValue(LootItemType.Bag, out var bag) && bag?.Definition != null
            ? Mathf.Max(0, bag.Definition.CapacityBonus) : 0;
    }

    private static void CreateRunBagGridForEquippedBag()
    {
        int width = BaseBagGridWidth;
        int height = BaseBagGridHeight;
        if (equippedLoot.TryGetValue(LootItemType.Bag, out var bag) && bag?.Definition != null
            && bag.Definition.BagGridWidth > 0 && bag.Definition.BagGridHeight > 0)
        {
            width = bag.Definition.BagGridWidth;
            height = bag.Definition.BagGridHeight;
        }
        runBagGrid = new LootGridModel(width, height);
    }

    private static void MoveAllRunBagItemsToStash()
    {
        if (runBag.Count > 0) stash.AddRange(runBag);
        runBag.Clear();
        runBagGrid = new LootGridModel(BaseBagGridWidth, BaseBagGridHeight);
    }

    private static bool Fail(string reason)
    {
        LastFailureReason = reason;
        return false;
    }
}

public sealed class LootGridRestoreItem
{
    public LootItemInstance Item { get; }
    public int X { get; }
    public int Y { get; }
    public bool Rotated { get; }
    public bool HasPlacement { get; }

    public LootGridRestoreItem(LootItemInstance item, int x, int y, bool rotated, bool hasPlacement)
    {
        Item = item;
        X = x;
        Y = y;
        Rotated = rotated;
        HasPlacement = hasPlacement;
    }
}
