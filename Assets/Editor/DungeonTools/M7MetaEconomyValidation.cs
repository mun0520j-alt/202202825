using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class M7MetaEconomyValidation
{
    private const int SamplesPerRarity = 100000;
    private const double Tolerance = 0.005;

    private sealed class ExpectedJunk
    {
        public string Id;
        public LootRarity Rarity;
        public int Width;
        public int Height;
        public int SellPrice;
        public int Weight;
    }

    private static readonly ExpectedJunk[] Expected =
    {
        new ExpectedJunk { Id = "junk_coin_pouch", Rarity = LootRarity.Normal, Width = 1, Height = 1, SellPrice = 15, Weight = 100 },
        new ExpectedJunk { Id = "junk_candles", Rarity = LootRarity.Normal, Width = 1, Height = 2, SellPrice = 10, Weight = 100 },
        new ExpectedJunk { Id = "junk_small_gem", Rarity = LootRarity.Rare, Width = 1, Height = 1, SellPrice = 60, Weight = 50 },
        new ExpectedJunk { Id = "junk_old_relic", Rarity = LootRarity.Epic, Width = 2, Height = 1, SellPrice = 120, Weight = 50 },
        new ExpectedJunk { Id = "junk_crown_shard", Rarity = LootRarity.Unique, Width = 1, Height = 1, SellPrice = 300, Weight = 50 }
    };

    [MenuItem("Dungeon Tools/QA/M7 Meta Economy Validation")]
    public static void Run()
    {
        var failures = new List<string>();
        ValidateDefinitions(failures);
        ValidateWeightedRolls(failures);

        if (failures.Count == 0)
        {
            Debug.Log("[M7 QA] PASS — 잡템 5종 데이터와 등급별 가중치 분포 검증 완료.");
            return;
        }

        Debug.LogError("[M7 QA] FAIL\n" + string.Join("\n", failures));
    }

    private static void ValidateDefinitions(List<string> failures)
    {
        foreach (ExpectedJunk expected in Expected)
        {
            LootItemData actual = LootItemDatabase.FindById(expected.Id);
            if (actual == null)
            {
                failures.Add($"누락: {expected.Id}");
                continue;
            }

            if (actual.Type != LootItemType.Junk || actual.Rarity != expected.Rarity
                || actual.GridWidth != expected.Width || actual.GridHeight != expected.Height
                || actual.SellPrice != expected.SellPrice || actual.DropWeight != expected.Weight)
            {
                failures.Add($"값 불일치: {expected.Id}");
            }
        }
    }

    private static void ValidateWeightedRolls(List<string> failures)
    {
        foreach (LootRarity rarity in new[] { LootRarity.Normal, LootRarity.Rare, LootRarity.Epic, LootRarity.Unique })
        {
            int totalWeight = LootItemDatabase.Items
                .Where(item => item.Rarity == rarity && item.DropWeight > 0)
                .Sum(item => item.DropWeight);
            int junkWeight = LootItemDatabase.Items
                .Where(item => item.Rarity == rarity && item.Type == LootItemType.Junk && item.DropWeight > 0)
                .Sum(item => item.DropWeight);
            double expectedRatio = totalWeight == 0 ? 0d : (double)junkWeight / totalWeight;

            var rng = new System.Random(20261112 + (int)rarity);
            int junkCount = 0;
            for (int i = 0; i < SamplesPerRarity; i++)
            {
                LootItemData rolled = LootItemDatabase.Roll(rarity, rng);
                if (rolled != null && rolled.Type == LootItemType.Junk) junkCount++;
            }

            double actualRatio = (double)junkCount / SamplesPerRarity;
            Debug.Log($"[M7 QA] {rarity} 잡템 {actualRatio:P2} / 기대 {expectedRatio:P2}");
            if (Math.Abs(actualRatio - expectedRatio) > Tolerance)
                failures.Add($"{rarity} 잡템 비율 초과: 실제 {actualRatio:P2}, 기대 {expectedRatio:P2}");
        }
    }
}
