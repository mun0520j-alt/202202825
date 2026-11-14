using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class M8QuestValidation
{
    [MenuItem("Dungeon Tools/QA/M8 Quest Data Validation")]
    public static void RunFromMenu()
    {
        EditorUtility.DisplayDialog("M8 Quest Validation", RunAll(), "확인");
    }

    public static void RunFromCommandLine()
    {
        Debug.Log(RunAll());
    }

    public static string RunAll()
    {
        var failures = new List<string>();
        QuestDefinition[] quests = QuestDefinitionDatabase.All.ToArray();
        if (quests.Length != 9) failures.Add($"퀘스트 수 {quests.Length}, 기대 9");
        if (quests.Select(q => q.Id).Distinct().Count() != quests.Length) failures.Add("quest_id 중복");
        foreach (QuestDefinition quest in quests)
        {
            if (!string.IsNullOrEmpty(quest.PrerequisiteId) && QuestDefinitionDatabase.Find(quest.PrerequisiteId) == null)
                failures.Add($"{quest.Id}: 선행 퀘스트 {quest.PrerequisiteId} 없음");
            if (quest.RewardReputation < 0 || quest.RewardGold < 0) failures.Add($"{quest.Id}: 음수 보상");
        }

        ValidateRouteTotal(quests, QuestRoute.Combat, failures);
        ValidateRouteTotal(quests, QuestRoute.Collection, failures);
        ValidateDelivery("q01", "junk_coin_pouch:3", failures);
        ValidateDelivery("q04b", "junk_small_gem:2", failures);
        ValidateDelivery("q05b", "junk_old_relic:1|junk_candles:3", failures);
        QuestDefinition q06a = QuestDefinitionDatabase.Find("q06a");
        if (q06a == null || q06a.TargetCount != 2 || !q06a.RequiresBossKill || q06a.TargetFloor != 5)
            failures.Add("q06a 연속 보스 생환 조건 불일치");
        if (LootItemDatabase.Items.Count(item => item.Rarity == LootRarity.Epic
                && item.Type != LootItemType.Junk && item.Type != LootItemType.Consumable && item.Type != LootItemType.Bag) == 0)
            failures.Add("q06a Epic 장비 보상 후보 없음");
        if (M8EconomyConfig.Level2Reputation != 100 || M8EconomyConfig.Level2Gold != 300
            || M8EconomyConfig.Level3Reputation != 300 || M8EconomyConfig.Level3Gold != 1000)
            failures.Add("우호도 레벨 기준 불일치");

        string summary = failures.Count == 0
            ? "[M8 Quest Validation] PASS — 9개 정의/선행/분기/우호도 300/복합 납품/연속 생환 검증 완료"
            : $"[M8 Quest Validation] FAIL {failures.Count}\n- {string.Join("\n- ", failures)}";
        if (failures.Count > 0) throw new InvalidOperationException(summary);
        return summary;
    }

    private static void ValidateRouteTotal(IEnumerable<QuestDefinition> quests, QuestRoute route, List<string> failures)
    {
        int total = quests.Where(q => q.Route == QuestRoute.Common || q.Route == route).Sum(q => q.RewardReputation);
        if (total != 300) failures.Add($"{route}: 우호도 합계 {total}, 기대 300");
    }

    private static void ValidateDelivery(string id, string expected, List<string> failures)
    {
        QuestDefinition quest = QuestDefinitionDatabase.Find(id);
        if (quest == null || quest.Type != QuestObjectiveType.Delivery || quest.TargetItemId != expected || !quest.RequiresFoundInRaid)
            failures.Add($"{id}: 인레이드 납품 조건 불일치");
    }
}
