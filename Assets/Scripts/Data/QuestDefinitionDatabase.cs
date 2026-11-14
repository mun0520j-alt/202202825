using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum QuestObjectiveType
{
    Delivery,
    Escape,
    Carry
}

public enum QuestRoute
{
    Common,
    Combat,
    Collection
}

[Serializable]
public sealed class QuestDefinition
{
    public string Id;
    public int Order;
    public QuestRoute Route;
    public QuestObjectiveType Type;
    public string PrerequisiteId;
    public string TargetItemId;
    public string TargetRarity;
    public string TargetCategory;
    public int TargetCount;
    public int TargetFloor;
    public int TargetValue;
    public bool RequiresFoundInRaid;
    public bool RequiresBossKill;
    public int RewardReputation;
    public int RewardGold;
    public string RewardItemId;
    public int RewardItemCount;
    public string Title;
    public string Description;
}

public static class QuestDefinitionDatabase
{
    private const string ResourcePath = "Data/m8_quests";
    private static readonly Dictionary<string, QuestDefinition> byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
    private static readonly List<QuestDefinition> all = new List<QuestDefinition>();
    private static bool loaded;

    public static IReadOnlyList<QuestDefinition> All
    {
        get { EnsureLoaded(); return all; }
    }

    public static QuestDefinition Find(string id)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out QuestDefinition value) ? value : null;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
        {
            Debug.LogError($"[QuestDefinitionDatabase] Resources/{ResourcePath}.csv를 찾지 못했습니다.");
            return;
        }

        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] f = lines[i].Split(',');
            if (f.Length < 20 || string.IsNullOrWhiteSpace(f[0])) continue;
            if (!Enum.TryParse(f[2].Trim(), true, out QuestRoute route)
                || !Enum.TryParse(f[3].Trim(), true, out QuestObjectiveType type)) continue;
            var definition = new QuestDefinition
            {
                Id = f[0].Trim(), Order = Int(f[1]), Route = route, Type = type,
                PrerequisiteId = f[4].Trim(), TargetItemId = f[5].Trim(), TargetRarity = f[6].Trim(),
                TargetCategory = f[7].Trim(), TargetCount = Int(f[8]), TargetFloor = Int(f[9]),
                TargetValue = Int(f[10]), RequiresFoundInRaid = Bool(f[11]), RequiresBossKill = Bool(f[12]),
                RewardReputation = Int(f[13]), RewardGold = Int(f[14]), RewardItemId = f[15].Trim(),
                RewardItemCount = Int(f[16]), Title = f[18].Trim(), Description = f[19].Trim()
            };
            byId[definition.Id] = definition;
            all.Add(definition);
        }
        Debug.Log($"[QuestDefinitionDatabase] 퀘스트 {all.Count}개 로드 완료.");
    }

    private static int Int(string value) => int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
    private static bool Bool(string value) => value.Trim() == "1" || bool.TryParse(value.Trim(), out bool result) && result;
}
