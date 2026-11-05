using System;
using System.Collections.Generic;
using UnityEngine;

public static class LootRuntimeState
{
    public const int BaseBagCapacity = 6;

    private static readonly List<LootItemInstance> runBag = new List<LootItemInstance>();
    private static readonly List<LootItemInstance> stash = new List<LootItemInstance>();

    public static IReadOnlyList<LootItemInstance> RunBag => runBag;
    public static IReadOnlyList<LootItemInstance> Stash => stash;
    public static int EquippedBagCapacityBonus { get; private set; }
    public static bool HasBasicLoadout { get; private set; }
    public static int BagCapacity => BaseBagCapacity + Mathf.Max(0, EquippedBagCapacityBonus);
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        runBag.Clear();
        stash.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
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

    public static void ResolveRun(ExpeditionResult result)
    {
        if (result == ExpeditionResult.Escaped) return;
        runBag.Clear();
        EquippedBagCapacityBonus = 0;
        HasBasicLoadout = false;
        Changed?.Invoke();
    }
}
