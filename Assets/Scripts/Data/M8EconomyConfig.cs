using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class M8EconomyConfig
{
    private const string ResourcePath = "Data/m8_economy";
    private static Dictionary<string, float> values;

    public static int Level2Reputation => Mathf.RoundToInt(Get("level_2_reputation", 100));
    public static int Level2Gold => Mathf.RoundToInt(Get("level_2_gold", 300));
    public static int Level3Reputation => Mathf.RoundToInt(Get("level_3_reputation", 300));
    public static int Level3Gold => Mathf.RoundToInt(Get("level_3_gold", 1000));
    public static float SellBonusPerLevel => Get("sell_bonus_per_level", 0.1f);
    public static float BuyDiscountPerLevel => Get("buy_discount_per_level", 0.1f);
    public static float InsuranceBonusPerLevel => Get("insurance_bonus_per_level", 0.1f);

    private static float Get(string key, float fallback)
    {
        EnsureLoaded();
        return values.TryGetValue(key, out float value) ? value : fallback;
    }

    private static void EnsureLoaded()
    {
        if (values != null) return;
        values = new Dictionary<string, float>(StringComparer.Ordinal);
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null) { Debug.LogError($"[M8EconomyConfig] Resources/{ResourcePath}.csv를 찾지 못했습니다."); return; }
        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            if (fields.Length >= 2 && float.TryParse(fields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                values[fields[0].Trim()] = value;
        }
    }
}
