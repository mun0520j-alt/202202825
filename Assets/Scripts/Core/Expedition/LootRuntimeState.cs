using System;
using System.Collections.Generic;
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
        Changed?.Invoke();
        return item;
    }

    public static bool TryEquipFromBag(string instanceId)
    {
        var item = runBag.Find(candidate => candidate.InstanceId == instanceId);
        if (item?.Definition == null) return Fail("장착할 아이템을 찾을 수 없습니다.");
        if (item.Definition.Type == LootItemType.Bag) return Fail("원정 중에는 가방을 바꿀 수 없습니다");
        LootGridPlacement original = runBagGrid.Find(instanceId);
        if (original == null) return Fail("가방 배치 정보를 찾을 수 없습니다.");
        int originalX = original.X;
        int originalY = original.Y;
        bool originalRotated = original.Rotated;
        equippedLoot.TryGetValue(item.Definition.Type, out var previous);
        runBagGrid.Remove(instanceId, out _);
        runBag.Remove(item);
        if (previous != null && !runBagGrid.TryAutoPlace(previous, out _))
        {
            runBag.Add(item);
            runBagGrid.TryPlace(item, originalX, originalY, originalRotated);
            return Fail("공간 부족");
        }
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
        if (item == null || !runBagGrid.TryAutoPlace(item, out _)) return Fail("공간 부족");
        unequippedBasicSlots.Add(type);
        runBag.Add(item);
        Changed?.Invoke();
        return true;
    }

    public static bool TryUnequipToBag(LootItemType type)
    {
        if (!equippedLoot.TryGetValue(type, out var item) || item == null) return Fail("해제할 장비가 없습니다.");
        if (type == LootItemType.Bag) return Fail("원정 중에는 가방을 바꿀 수 없습니다");
        if (!runBagGrid.TryAutoPlace(item, out _)) return Fail("공간 부족");
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
        Gold += Mathf.Max(0, item.Definition.SellPrice);
        Changed?.Invoke();
        return true;
    }

    public static void ResolveRun(ExpeditionResult result)
    {
        if (result == ExpeditionResult.Escaped) return;
        runBag.Clear();
        runBagGrid = new LootGridModel(BaseBagGridWidth, BaseBagGridHeight);
        equippedLoot.Clear();
        unequippedBasicSlots.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
        Changed?.Invoke();
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
