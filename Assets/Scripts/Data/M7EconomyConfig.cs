using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class M7EconomyConfig
{
    private const string ResourcePath = "Data/m7_economy";
    private static Dictionary<string, float> values;

    public static int PurchasePriceMultiplier => Mathf.Max(1, Mathf.RoundToInt(Get("purchase_price_multiplier", 4f)));
    public static float EscapeScrollUseTicks => Mathf.Max(1f, Get("escape_scroll_use_ticks", 1f));
    public static float EscapeScrollStayTicks => Mathf.Max(1f, Get("escape_scroll_stay_ticks", 20f));
    public static float EscapeScrollLifetimeTicks => Mathf.Max(EscapeScrollStayTicks, Get("escape_scroll_lifetime_ticks", 50f));
    public static float InsurancePremiumRate => Mathf.Clamp01(Get("insurance_premium_rate", 0.5f));
    public static float InsuranceReturnRate => Mathf.Clamp01(Get("insurance_return_rate", 0.3f));
    public static float InsuranceWaitTicks => Mathf.Max(0f, Get("insurance_wait_ticks", 1500f));
    public static float InsuranceClaimTicks => Mathf.Max(0f, Get("insurance_claim_ticks", 3000f));

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
        if (csv == null)
        {
            Debug.LogError($"[M7EconomyConfig] Resources/{ResourcePath}.csv를 찾지 못했습니다.");
            return;
        }

        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            if (fields.Length < 2 || !float.TryParse(fields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) continue;
            values[fields[0].Trim()] = value;
        }
    }
}
