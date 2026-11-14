using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 퀘스트의 진행 상태만 소유한다. 원정 사실은 ExpeditionRunRecord에서 전달받는다.
public static class QuestRuntimeState
{
    private static readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);
    public static string ActiveQuestId { get; private set; }
    public static QuestRoute SelectedRoute { get; private set; } = QuestRoute.Common;
    public static int Progress { get; private set; }
    public static bool ObjectiveReady { get; private set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        completed.Clear();
        ActiveQuestId = null;
        SelectedRoute = QuestRoute.Common;
        Progress = 0;
        ObjectiveReady = false;
        Changed = null;
    }

    public static IReadOnlyCollection<string> CompletedQuestIds => completed;

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

    // 보상 지급은 3단계 UI에서 호출한다. 여기서는 체인 상태만 원자적으로 확정한다.
    public static bool TryFinalizeObjective()
    {
        if (!ObjectiveReady || string.IsNullOrEmpty(ActiveQuestId)) return false;
        completed.Add(ActiveQuestId);
        ActiveQuestId = null;
        Progress = 0;
        ObjectiveReady = false;
        Changed?.Invoke();
        return true;
    }
}
