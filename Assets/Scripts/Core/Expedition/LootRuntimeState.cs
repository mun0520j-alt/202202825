using System;
using System.Collections.Generic;
using UnityEngine;

public static class LootRuntimeState
{
    public const int BaseBagCapacity = 6;

    private static readonly List<LootItemInstance> runBag = new List<LootItemInstance>();
    private static readonly List<LootItemInstance> stash = new List<LootItemInstance>();
    private static readonly Dictionary<LootItemType, LootItemInstance> equippedLoot = new Dictionary<LootItemType, LootItemInstance>();
    private static readonly HashSet<LootItemType> unequippedBasicSlots = new HashSet<LootItemType>();

    public static IReadOnlyList<LootItemInstance> RunBag => runBag;
    public static IReadOnlyList<LootItemInstance> Stash => stash;
    public static IReadOnlyDictionary<LootItemType, LootItemInstance> EquippedLoot => equippedLoot;
    public static int EquippedBagCapacityBonus { get; private set; }
    public static bool HasBasicLoadout { get; private set; }
    public static int Gold { get; private set; }
    public static int BagCapacity => BaseBagCapacity + Mathf.Max(0, EquippedBagCapacityBonus);
    public static string LastFailureReason { get; private set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        runBag.Clear();
        stash.Clear();
        equippedLoot.Clear();
        unequippedBasicSlots.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
        Gold = 0;
        LastFailureReason = null;
        Changed = null;
    }

    public static void BeginNewRun()
    {
        if (HasBasicLoadout) return;
        HasBasicLoadout = true;
        Debug.Log("[Loadout] 장비가 없어 기본 검·방패·갑옷을 무료 지급했습니다.");
    }

    public static bool TryAddToRunBag(LootItemInstance item)
    {
        if (item == null || runBag.Count >= BagCapacity) return false;
        runBag.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static bool MoveBagItemToStash(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item == null) return false;
        runBag.Remove(item);
        stash.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static LootItemInstance DropBagItem(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item == null) return null;
        runBag.Remove(item);
        Changed?.Invoke();
        return item;
    }

    public static bool TryEquipFromBag(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        int resultingCount = runBag.Count - 1;
        if (equippedLoot.TryGetValue(item.Definition.Type, out var previous) && previous != null) resultingCount++;
        int resultingCapacity = item.Definition.Type == LootItemType.Bag
            ? BaseBagCapacity + Mathf.Max(0, item.Definition.CapacityBonus)
            : BagCapacity;
        if (resultingCount > resultingCapacity)
            return Fail($"가방에 아이템이 {resultingCount}개라 교체할 수 없음");
        runBag.Remove(item);
        if (previous != null) runBag.Add(previous);
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
        if (item == null || runBag.Count >= BagCapacity) return false;
        unequippedBasicSlots.Add(type);
        runBag.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static bool TryUnequipToBag(LootItemType type)
    {
        if (!equippedLoot.TryGetValue(type, out var item) || item == null) return Fail("해제할 장비가 없습니다.");
        int resultingCapacity = type == LootItemType.Bag ? BaseBagCapacity : BagCapacity;
        if (runBag.Count + 1 > resultingCapacity)
            return Fail($"가방에 아이템이 {runBag.Count}개라 해제할 수 없음");
        equippedLoot.Remove(type);
        runBag.Add(item);
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TryUnequipToStash(LootItemType type)
    {
        if (!equippedLoot.TryGetValue(type, out var item) || item == null) return Fail("해제할 장비가 없습니다.");
        if (type == LootItemType.Bag && runBag.Count > BaseBagCapacity)
            return Fail($"가방에 아이템이 {runBag.Count}개라 해제할 수 없음");
        equippedLoot.Remove(type);
        stash.Add(item);
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool MoveStashItemToBag(string instanceId)
    {
        var item = stash.Find(candidate => candidate.InstanceId == instanceId);
        if (item == null || runBag.Count >= BagCapacity) return false;
        stash.Remove(item);
        runBag.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static bool TryEquipFromStash(string instanceId)
    {
        var item = stash.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        int resultingCapacity = item.Definition.Type == LootItemType.Bag
            ? BaseBagCapacity + Mathf.Max(0, item.Definition.CapacityBonus)
            : BagCapacity;
        if (runBag.Count > resultingCapacity)
            return Fail($"가방에 아이템이 {runBag.Count}개라 교체할 수 없음");
        stash.Remove(item);
        if (equippedLoot.TryGetValue(item.Definition.Type, out var previous) && previous != null) stash.Add(previous);
        equippedLoot[item.Definition.Type] = item;
        unequippedBasicSlots.Remove(item.Definition.Type);
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TryEquipFromBaseCampBag(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        int resultingCount = runBag.Count - 1;
        int resultingCapacity = item.Definition.Type == LootItemType.Bag
            ? BaseBagCapacity + Mathf.Max(0, item.Definition.CapacityBonus)
            : BagCapacity;
        if (resultingCount > resultingCapacity)
            return Fail($"가방에 아이템이 {resultingCount}개라 교체할 수 없음");
        runBag.Remove(item);
        if (equippedLoot.TryGetValue(item.Definition.Type, out var previous) && previous != null) stash.Add(previous);
        equippedLoot[item.Definition.Type] = item;
        unequippedBasicSlots.Remove(item.Definition.Type);
        RefreshBagCapacity();
        LastFailureReason = null;
        Changed?.Invoke();
        return true;
    }

    public static bool TrySell(string instanceId)
    {
        var item = stash.Find(candidate => candidate.InstanceId == instanceId)
            ?? runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null || item.Definition.Id.StartsWith("basic_", StringComparison.Ordinal)) return false;
        if (!stash.Remove(item)) runBag.Remove(item);
        Gold += Mathf.Max(0, item.Definition.SellPrice);
        Changed?.Invoke();
        return true;
    }

    public static void ResolveRun(ExpeditionResult result)
    {
        if (result == ExpeditionResult.Escaped) return;
        runBag.Clear();
        equippedLoot.Clear();
        unequippedBasicSlots.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
        Changed?.Invoke();
    }

    public static void RestorePersistentState(
        IEnumerable<LootItemInstance> restoredRunBag,
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

        if (restoredRunBag != null) runBag.AddRange(restoredRunBag);
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
        if (runBag.Count > BagCapacity)
        {
            int moved = 0;
            while (runBag.Count > BagCapacity)
            {
                int last = runBag.Count - 1;
                stash.Add(runBag[last]);
                runBag.RemoveAt(last);
                moved++;
            }
            Debug.LogWarning($"[Loadout] 변경된 가방 용량을 초과한 기존 세이브의 아이템 {moved}개를 Stash로 이동했습니다.");
        }
        Changed?.Invoke();
    }

    private static void RefreshBagCapacity()
    {
        EquippedBagCapacityBonus = equippedLoot.TryGetValue(LootItemType.Bag, out var bag) && bag?.Definition != null
            ? Mathf.Max(0, bag.Definition.CapacityBonus) : 0;
    }

    private static bool Fail(string reason)
    {
        LastFailureReason = reason;
        return false;
    }
}
