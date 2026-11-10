using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class LootItemDatabase
{
    private const string ResourcePath = "Data/items";
    private static List<LootItemData> items;

    public static IReadOnlyList<LootItemData> Items
    {
        get
        {
            EnsureLoaded();
            return items;
        }
    }

    public static LootItemData Roll(int floor, System.Random rng)
    {
        EnsureLoaded();
        LootRarity rarity = RollRarity(floor, rng);
        var candidates = items.Where(item => item.Rarity == rarity && item.DropWeight > 0 && item.Type != LootItemType.Bag).ToList();
        if (candidates.Count == 0) candidates = items.Where(item => item.Rarity == LootRarity.Normal && item.DropWeight > 0).ToList();
        if (candidates.Count == 0) return null;

        int totalWeight = candidates.Sum(item => Mathf.Max(1, item.DropWeight));
        int roll = rng.Next(totalWeight);
        foreach (var item in candidates)
        {
            roll -= Mathf.Max(1, item.DropWeight);
            if (roll < 0) return item;
        }
        return candidates[candidates.Count - 1];
    }

    public static LootItemData FindById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        EnsureLoaded();
        return items.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
    }

    public static LootItemData Roll(LootRarity rarity, System.Random rng)
    {
        EnsureLoaded();
        var candidates = items.Where(item => item.Rarity == rarity && item.DropWeight > 0 && item.Type != LootItemType.Bag).ToList();
        if (candidates.Count == 0) return null;
        int totalWeight = candidates.Sum(item => Mathf.Max(1, item.DropWeight));
        int roll = rng.Next(totalWeight);
        foreach (var item in candidates)
        {
            roll -= Mathf.Max(1, item.DropWeight);
            if (roll < 0) return item;
        }
        return candidates[candidates.Count - 1];
    }

    public static LootRarity RollRarity(int floor, System.Random rng)
    {
        int roll = rng.Next(100);
        if (roll == 0) return floor <= 4 ? LootRarity.Unique : LootRarity.Legendary;

        if (floor <= 1) return roll < 3 ? LootRarity.Epic : roll < 21 ? LootRarity.Rare : LootRarity.Normal;
        if (floor == 2) return roll < 6 ? LootRarity.Epic : roll < 31 ? LootRarity.Rare : LootRarity.Normal;
        return roll < 11 ? LootRarity.Epic : roll < 41 ? LootRarity.Rare : LootRarity.Normal;
    }

    public static int RollBossDropCount(System.Random rng)
    {
        int roll = rng.Next(100);
        return roll < 10 ? 0 : roll < 80 ? 1 : 2;
    }

    public static LootRarity RollBossDropRarity(System.Random rng)
    {
        int roll = rng.Next(100);
        return roll < 50 ? LootRarity.Rare : roll < 90 ? LootRarity.Epic : LootRarity.Unique;
    }

    private static void EnsureLoaded()
    {
        if (items != null) return;
        items = new List<LootItemData>();
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
        {
            Debug.LogError($"[LootItemDatabase] Resources/{ResourcePath}.csv를 찾지 못했습니다.");
            return;
        }

        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            if (fields.Length < 9) continue;
            if (!Enum.TryParse(fields[2], true, out LootItemType type)) continue;
            if (!Enum.TryParse(fields[3], true, out LootRarity rarity)) continue;

            items.Add(new LootItemData
            {
                Id = fields[0].Trim(),
                Name = fields[1].Trim(),
                Type = type,
                Rarity = rarity,
                AttackBonus = ParseInt(fields[4]),
                DefenseBonus = ParseInt(fields[5]),
                CapacityBonus = ParseInt(fields[6]),
                IconKey = fields[7].Trim(),
                DropWeight = ParseInt(fields[8])
            });
        }
        Debug.Log($"[LootItemDatabase] CSV 아이템 {items.Count}개 로드 완료.");
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
    }
}
