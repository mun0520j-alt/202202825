using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 퀘스트의 진행 상태만 소유한다. 원정 사실은 ExpeditionRunRecord에서 전달받는다.
public static class QuestRuntimeState
{
    private static readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<string> deliverySelection = new HashSet<string>(StringComparer.Ordinal);
    public static string ActiveQuestId { get; private set; }
    public static QuestRoute SelectedRoute { get; private set; } = QuestRoute.Common;
    public static int Progress { get; private set; }
    public static bool ObjectiveReady { get; private set; }
    public static int Reputation { get; private set; }
    public static int ReputationLevel { get; private set; } = 1;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        completed.Clear();
        ActiveQuestId = null;
        SelectedRoute = QuestRoute.Common;
        Progress = 0;
        ObjectiveReady = false;
        Reputation = 0;
        ReputationLevel = 1;
        deliverySelection.Clear();
        Changed = null;
    }

    public static IReadOnlyCollection<string> CompletedQuestIds => completed;
    public static IReadOnlyCollection<string> DeliverySelection => deliverySelection;
    public static QuestDefinition ActiveQuest => QuestDefinitionDatabase.Find(ActiveQuestId);

    public static bool TrySelectRoute(QuestRoute route)
    {
        if (route == QuestRoute.Common || SelectedRoute != QuestRoute.Common || !completed.Contains("q03")) return false;
        SelectedRoute = route;
        Changed?.Invoke();
        return true;
    }

    public static bool TryAccept(string questId)
    {
        QuestDefinition definition = QuestDefinitionDatabase.Find(questId);
        if (definition == null || !string.IsNullOrEmpty(ActiveQuestId) || completed.Contains(questId)) return false;
        if (!string.IsNullOrEmpty(definition.PrerequisiteId) && !completed.Contains(definition.PrerequisiteId)) return false;
        if (definition.Route != QuestRoute.Common && definition.Route != SelectedRoute) return false;
        ActiveQuestId = questId;
        Progress = 0;
        ObjectiveReady = false;
        deliverySelection.Clear();
        Changed?.Invoke();
        return true;
    }

    public static bool CanAccept(QuestDefinition definition)
    {
        if (definition == null || !string.IsNullOrEmpty(ActiveQuestId) || completed.Contains(definition.Id)) return false;
        if (!string.IsNullOrEmpty(definition.PrerequisiteId) && !completed.Contains(definition.PrerequisiteId)) return false;
        return definition.Route == QuestRoute.Common || definition.Route == SelectedRoute;
    }

    public static IEnumerable<LootItemInstance> EligibleDeliveryItems()
    {
        QuestDefinition quest = ActiveQuest;
        if (quest == null || quest.Type != QuestObjectiveType.Delivery) return Enumerable.Empty<LootItemInstance>();
        return LootRuntimeState.Stash.Concat(LootRuntimeState.RunBag)
            .Where(item => item?.Definition != null && (!quest.RequiresFoundInRaid || item.IsFoundInRaid))
            .Where(item => MatchesDeliveryItemSpec(quest.TargetItemId, item));
    }

    public static bool ToggleDeliverySelection(string instanceId)
    {
        if (!EligibleDeliveryItems().Any(item => item.InstanceId == instanceId)) return false;
        if (!deliverySelection.Add(instanceId)) deliverySelection.Remove(instanceId);
        Changed?.Invoke();
        return true;
    }

    public static bool CanSubmitDelivery(out string reason)
    {
        QuestDefinition quest = ActiveQuest;
        if (quest == null || quest.Type != QuestObjectiveType.Delivery) { reason = "납품 퀘스트가 아닙니다."; return false; }
        var selected = EligibleDeliveryItems().Where(item => deliverySelection.Contains(item.InstanceId)).ToList();
        if (!MatchesDeliverySelection(quest.TargetItemId, selected)) { reason = "필요한 종류와 개수를 정확히 선택하세요."; return false; }
        reason = null;
        return true;
    }

    public static bool TrySubmitDelivery()
    {
        if (!CanSubmitDelivery(out _)) return false;
        if (!LootRuntimeState.TryConsumeQuestItems(deliverySelection)) return false;
        deliverySelection.Clear();
        Progress = Mathf.Max(1, ActiveQuest.TargetCount);
        ObjectiveReady = true;
        Changed?.Invoke();
        return true;
    }

    public static void RecordExpeditionCompleted(ExpeditionRunSnapshot run, IEnumerable<LootItemInstance> carriedItems)
    {
        if (run == null || string.IsNullOrEmpty(ActiveQuestId) || ObjectiveReady) return;
        QuestDefinition quest = QuestDefinitionDatabase.Find(ActiveQuestId);
        if (quest == null) return;

        // q06a만 원정 간 진행도를 가진다. 보스 미처치 생환은 유지, 사망·MIA만 초기화한다.
        if (quest.Id == "q06a")
        {
            if (run.Result == ExpeditionResult.Dead || run.Result == ExpeditionResult.MIA) Progress = 0;
            else if (run.Result == ExpeditionResult.Escaped && run.BossDefeated && run.ReachedFloor >= quest.TargetFloor) Progress++;
            ObjectiveReady = Progress >= quest.TargetCount;
            Changed?.Invoke();
            return;
        }

        if (run.Result != ExpeditionResult.Escaped) return;
        var items = carriedItems?.Where(item => item?.Definition != null).ToList() ?? new List<LootItemInstance>();
        bool matched = quest.Type == QuestObjectiveType.Escape
            ? run.ReachedFloor >= quest.TargetFloor && (!quest.RequiresBossKill || run.BossDefeated)
            : quest.Type == QuestObjectiveType.Carry && MatchesCarryObjective(quest, items, run);
        if (!matched) return;
        Progress = Mathf.Max(1, quest.TargetCount);
        ObjectiveReady = true;
        Changed?.Invoke();
    }

    private static bool MatchesCarryObjective(QuestDefinition quest, List<LootItemInstance> items, ExpeditionRunSnapshot run)
    {
        if (run.ReachedFloor < quest.TargetFloor || quest.RequiresBossKill && !run.BossDefeated) return false;
        IEnumerable<LootItemInstance> candidates = items;
        if (quest.TargetCategory == "Equipment") candidates = candidates.Where(item => item.Definition.Type != LootItemType.Junk && item.Definition.Type != LootItemType.Consumable);
        if (!string.IsNullOrEmpty(quest.TargetRarity) && Enum.TryParse(quest.TargetRarity, true, out LootRarity rarity))
            candidates = candidates.Where(item => item.Definition.Rarity >= rarity);
        if (!string.IsNullOrEmpty(quest.TargetItemId)) candidates = candidates.Where(item => item.Definition.Id == quest.TargetItemId);
        if (quest.TargetValue > 0) return candidates.Where(item => item.Definition.Type == LootItemType.Junk).Sum(item => item.Definition.SellPrice) >= quest.TargetValue;
        return candidates.Count() >= quest.TargetCount;
    }

    public static bool TryClaimReward()
    {
        if (!ObjectiveReady || string.IsNullOrEmpty(ActiveQuestId)) return false;
        QuestDefinition quest = ActiveQuest;
        if (quest == null) return false;
        LootRuntimeState.AddGold(quest.RewardGold);
        Reputation += Mathf.Max(0, quest.RewardReputation);
        for (int i = 0; i < quest.RewardItemCount; i++)
        {
            LootItemData reward = ResolveRewardItem(quest.RewardItemId);
            if (reward != null) LootRuntimeState.AddRewardToStash(new LootItemInstance(reward, foundInRaid: false));
        }
        completed.Add(ActiveQuestId);
        ActiveQuestId = null;
        Progress = 0;
        ObjectiveReady = false;
        Changed?.Invoke();
        return true;
    }

    public static bool CanLevelUp => ReputationLevel < 3
        && Reputation >= (ReputationLevel == 1 ? M8EconomyConfig.Level2Reputation : M8EconomyConfig.Level3Reputation);

    public static int NextLevelGoldCost => ReputationLevel == 1 ? M8EconomyConfig.Level2Gold
        : ReputationLevel == 2 ? M8EconomyConfig.Level3Gold : 0;

    public static bool TryLevelUp()
    {
        if (!CanLevelUp || !LootRuntimeState.TrySpendGold(NextLevelGoldCost)) return false;
        ReputationLevel++;
        Changed?.Invoke();
        return true;
    }

    public static void RecordInterruptedMIA()
    {
        if (ActiveQuestId != "q06a" || Progress == 0) return;
        Progress = 0;
        ObjectiveReady = false;
        Changed?.Invoke();
    }

    private static bool MatchesDeliveryItemSpec(string spec, LootItemInstance item)
    {
        if (string.IsNullOrEmpty(spec) || item?.Definition == null) return false;
        return spec.Split('|').Any(part => part.Split(':')[0] == item.Definition.Id);
    }

    private static bool MatchesDeliverySelection(string spec, List<LootItemInstance> selected)
    {
        if (string.IsNullOrEmpty(spec)) return false;
        var required = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string part in spec.Split('|'))
        {
            string[] pair = part.Split(':');
            required[pair[0]] = pair.Length > 1 && int.TryParse(pair[1], out int count) ? count : 1;
        }
        if (selected.Count != required.Values.Sum()) return false;
        return required.All(pair => selected.Count(item => item.Definition.Id == pair.Key) == pair.Value);
    }

    private static LootItemData ResolveRewardItem(string rewardKey)
    {
        if (string.IsNullOrEmpty(rewardKey)) return null;
        if (rewardKey == "equipment_epic_random")
        {
            LootItemData[] candidates = LootItemDatabase.Items.Where(item => item.Rarity == LootRarity.Epic
                && item.Type != LootItemType.Junk && item.Type != LootItemType.Consumable && item.Type != LootItemType.Bag).ToArray();
            return candidates.Length == 0 ? null : candidates[new System.Random(Guid.NewGuid().GetHashCode()).Next(candidates.Length)];
        }
        return LootItemDatabase.FindById(rewardKey);
    }

    [Serializable]
    public sealed class PersistentState
    {
        public string ActiveQuestId;
        public string SelectedRoute;
        public int Progress;
        public bool ObjectiveReady;
        public int Reputation;
        public int ReputationLevel = 1;
        public List<string> CompletedQuestIds = new List<string>();
    }

    public static PersistentState CapturePersistentState() => new PersistentState
    {
        ActiveQuestId = ActiveQuestId, SelectedRoute = SelectedRoute.ToString(), Progress = Progress,
        ObjectiveReady = ObjectiveReady, Reputation = Reputation, ReputationLevel = ReputationLevel,
        CompletedQuestIds = completed.ToList()
    };

    public static void RestorePersistentState(PersistentState state)
    {
        completed.Clear();
        if (state?.CompletedQuestIds != null) foreach (string id in state.CompletedQuestIds) completed.Add(id);
        ActiveQuestId = state?.ActiveQuestId;
        SelectedRoute = state != null && Enum.TryParse(state.SelectedRoute, out QuestRoute route) ? route : QuestRoute.Common;
        Progress = Mathf.Max(0, state?.Progress ?? 0);
        ObjectiveReady = state?.ObjectiveReady ?? false;
        Reputation = Mathf.Max(0, state?.Reputation ?? 0);
        ReputationLevel = Mathf.Clamp(state?.ReputationLevel ?? 1, 1, 3);
        deliverySelection.Clear();
        Changed?.Invoke();
    }
}
