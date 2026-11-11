using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public enum M3SeedMode
{
    Random,
    Fixed
}

// M3 측정용 런타임 기록기. 게임 규칙에는 관여하지 않고, Editor/Development Build에서
// 재현 Seed와 플레이 행동을 CSV 한 행으로 남긴다.
public static class M3MeasurementAutomation
{
    private const string SeedPreferenceKey = "M3.RunSeed";
    private const string SeedModePreferenceKey = "M3.SeedMode";
    private const int DefaultSeed = 20261106;
    private const string SchemaVersion = "4";
    private static readonly Dictionary<string, float> playerActionTicks = new Dictionary<string, float>();
    private static readonly HashSet<string> visitedRooms = new HashSet<string>();
    private static readonly HashSet<string> encounteredEnemies = new HashSet<string>();
    private static bool sessionPrepared;
    private static bool recording;
    private static int runSeed = DefaultSeed;
    private static int playerActions;
    private static int enemyActions;
    private static float enemyActionTicks;
    private static int roomRevisits;
    private static string lastRoomKey;
    private static int enemiesKilled;
    private static int damageDealt;
    private static int damageTaken;
    private static int healingReceived;
    private static int chestsOpened;
    private static int itemsAcquired;
    private static int itemsGrounded;
    private static int itemsDroppedToContainer;
    private static int chestItemsGenerated;
    private static int bossItemsGenerated;
    private static int exitPathTiles;
    private static int exitRoomEnemies;
    private static bool debugRun;
    private static bool hasActiveRunSeed;
    private static M3SeedMode activeSeedMode;

    public static int ConfiguredSeed => PlayerPrefs.GetInt(SeedPreferenceKey, DefaultSeed);
    public static M3SeedMode ConfiguredSeedMode
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return PlayerPrefs.GetInt(SeedModePreferenceKey, (int)M3SeedMode.Random) == (int)M3SeedMode.Fixed
                ? M3SeedMode.Fixed : M3SeedMode.Random;
#else
            return M3SeedMode.Random;
#endif
        }
    }
    public static int CurrentRunSeed => runSeed;
    public static M3SeedMode CurrentSeedMode => activeSeedMode;
    public static string OutputDirectory => Path.Combine(Application.persistentDataPath, "M3Measurements");
    // v1·v2는 기존 측정 표본으로 보존한다. Seed 모드가 포함된 스키마는 v3에서 새로 시작한다.
    public static string OutputPath => Path.Combine(OutputDirectory, "expedition_runs_v4.csv");
    public static string SeedSummaryPath => Path.Combine(OutputDirectory, "seed_summary.csv");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        sessionPrepared = false;
        recording = false;
        hasActiveRunSeed = false;
        activeSeedMode = M3SeedMode.Random;
        ResetCounters();
    }

    public static void PrepareNewSession()
    {
        sessionPrepared = true;
        recording = false;
        hasActiveRunSeed = false;
        ResetCounters();
    }

    public static int ResolveRunSeed()
    {
        if (hasActiveRunSeed) return runSeed;

        activeSeedMode = ConfiguredSeedMode;
        runSeed = activeSeedMode == M3SeedMode.Fixed ? ConfiguredSeed : CreateRandomSeed();
        hasActiveRunSeed = true;
        return runSeed;
    }

    public static void BeginOrResume(int seed)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) return;
        if (!sessionPrepared) ResetCounters();
        sessionPrepared = false;
        recording = true;
        runSeed = seed;
        Debug.Log($"[M3Measure] 기록 시작 — Seed {runSeed} ({activeSeedMode}), CSV {OutputPath}");
#endif
    }

    public static void SetConfiguredSeed(int seed)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        PlayerPrefs.SetInt(SeedPreferenceKey, seed);
        PlayerPrefs.SetInt(SeedModePreferenceKey, (int)M3SeedMode.Fixed);
        PlayerPrefs.Save();
#endif
    }

    public static void SetRandomMode()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        PlayerPrefs.SetInt(SeedModePreferenceKey, (int)M3SeedMode.Random);
        PlayerPrefs.Save();
#endif
    }

    public static void MarkDebugRun()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!recording) return;
        debugRun = true;
        Debug.Log("[M3Measure] QA 디버그 맵 사용 — 현재 원정은 CSV 저장에서 제외합니다.");
#endif
    }

    public static void RecordAction(ITurnActor actor, string actionName, float tickCost)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!recording || actor == null) return;
        if (actor is PlayerTurnActor player)
        {
            playerActions++;
            string category = NormalizeAction(actionName);
            playerActionTicks[category] = GetTicks(category) + tickCost;
            int roomId = DungeonRoomRegistry.GetRoomId(player.FloorTilemap, player.CurrentCell);
            if (roomId >= 0)
            {
                string roomKey = ExpeditionSession.CurrentFloor + ":" + roomId;
                if (roomKey != lastRoomKey && !visitedRooms.Add(roomKey)) roomRevisits++;
                lastRoomKey = roomKey;
            }
        }
        else if (actor is EnemyTurnActor)
        {
            enemyActions++;
            enemyActionTicks += tickCost;
        }
#endif
    }

    public static void RecordEnemyEncounter(EnemyTurnActor enemy)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording && enemy != null)
            encounteredEnemies.Add(ExpeditionSession.CurrentFloor + ":" + enemy.name);
#endif
    }

    public static void RecordEnemyDamage(int amount)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) damageDealt += Mathf.Max(0, amount);
#endif
    }

    public static void RecordEnemyKill()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) enemiesKilled++;
#endif
    }

    public static void RecordPlayerDamage(int amount)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) damageTaken += Mathf.Max(0, amount);
#endif
    }

    public static void RecordPlayerHealing(int amount)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) healingReceived += Mathf.Max(0, amount);
#endif
    }

    public static void RecordChestOpened()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) chestsOpened++;
#endif
    }

    public static void RecordItemAcquired()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) itemsAcquired++;
#endif
    }

    public static void RecordItemGrounded()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) itemsGrounded++;
#endif
    }

    public static void RecordItemDroppedToContainer()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) itemsDroppedToContainer++;
#endif
    }

    public static void RecordChestItemGenerated()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) chestItemsGenerated++;
#endif
    }

    public static void RecordBossItemsGenerated(int count)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) bossItemsGenerated += Mathf.Max(0, count);
#endif
    }

    public static void RecordFloorExitMetrics(int shortestPathTiles, int enemiesInExitRoom)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!recording) return;
        exitPathTiles += Mathf.Max(0, shortestPathTiles);
        exitRoomEnemies += Mathf.Max(0, enemiesInExitRoom);
#endif
    }

    public static void Finish(ExpeditionResult result, float elapsedTicks, int reachedFloor)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!recording) return;
        recording = false;
        if (debugRun)
        {
            Debug.Log("[M3Measure] QA 디버그 원정 기록 제외 — CSV 행을 저장하지 않았습니다.");
            return;
        }
        Directory.CreateDirectory(OutputDirectory);
        int seedAttempt = CountSeedAttempts(runSeed) + 1;
        bool writeHeader = !File.Exists(OutputPath) || new FileInfo(OutputPath).Length == 0;
        using (var writer = new StreamWriter(OutputPath, true, new UTF8Encoding(true)))
        {
            if (writeHeader)
            {
                writer.WriteLine("schema_version,timestamp,seed,seed_mode,seed_attempt,result,total_ticks,reached_floor,player_actions,enemy_actions,enemy_action_ticks,move_ticks,wait_ticks,attack_ticks,door_ticks,loot_ticks,interact_ticks,other_ticks,visited_rooms,room_revisits,bag_items,hp,enemy_encounters,enemies_killed,damage_dealt,damage_taken,healing_received,chests_opened,items_acquired,items_grounded_legacy,items_dropped_to_container,chest_items_generated,boss_items_generated,exit_path_tiles,exit_room_enemies");
            }
            writer.WriteLine(string.Join(",", new[]
            {
                SchemaVersion,
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                runSeed.ToString(CultureInfo.InvariantCulture),
                activeSeedMode.ToString(),
                seedAttempt.ToString(CultureInfo.InvariantCulture),
                result.ToString(),
                F(elapsedTicks),
                reachedFloor.ToString(CultureInfo.InvariantCulture),
                playerActions.ToString(CultureInfo.InvariantCulture),
                enemyActions.ToString(CultureInfo.InvariantCulture),
                F(enemyActionTicks),
                F(GetTicks("Move")),
                F(GetTicks("Wait")),
                F(GetTicks("Attack")),
                F(GetTicks("Door")),
                F(GetTicks("Loot")),
                F(GetTicks("Interact")),
                F(GetTicks("Other")),
                visitedRooms.Count.ToString(CultureInfo.InvariantCulture),
                roomRevisits.ToString(CultureInfo.InvariantCulture),
                LootRuntimeState.RunBag.Count.ToString(CultureInfo.InvariantCulture),
                ExpeditionSession.PlayerCurrentHP.ToString(CultureInfo.InvariantCulture),
                encounteredEnemies.Count.ToString(CultureInfo.InvariantCulture),
                enemiesKilled.ToString(CultureInfo.InvariantCulture),
                damageDealt.ToString(CultureInfo.InvariantCulture),
                damageTaken.ToString(CultureInfo.InvariantCulture),
                healingReceived.ToString(CultureInfo.InvariantCulture),
                chestsOpened.ToString(CultureInfo.InvariantCulture),
                itemsAcquired.ToString(CultureInfo.InvariantCulture),
                itemsGrounded.ToString(CultureInfo.InvariantCulture),
                itemsDroppedToContainer.ToString(CultureInfo.InvariantCulture),
                chestItemsGenerated.ToString(CultureInfo.InvariantCulture),
                bossItemsGenerated.ToString(CultureInfo.InvariantCulture),
                exitPathTiles.ToString(CultureInfo.InvariantCulture),
                exitRoomEnemies.ToString(CultureInfo.InvariantCulture)
            }));
        }
        RebuildSeedSummary();
        Debug.Log($"[M3Measure] 원정 기록 저장 완료 — {OutputPath}");
        Debug.Log($"[M3Measure] Seed 요약 갱신 완료 — {SeedSummaryPath}");
#endif
    }

    public static void RebuildSeedSummary()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Directory.CreateDirectory(OutputDirectory);
        var rows = ReadV3Rows();
        var groups = new Dictionary<int, List<Dictionary<string, string>>>();
        foreach (var row in rows)
        {
            if (!TryInt(row, "seed", out int seed)) continue;
            if (!groups.TryGetValue(seed, out var list))
            {
                list = new List<Dictionary<string, string>>();
                groups[seed] = list;
            }
            list.Add(row);
        }

        using (var writer = new StreamWriter(SeedSummaryPath, false, new UTF8Encoding(true)))
        {
            writer.WriteLine("seed,seed_mode,runs,escaped,dead,mia,first_run_ticks,first_run_result,min_ticks,avg_ticks,max_ticks,exit_path_tiles,exit_room_enemies");
            var seeds = new List<int>(groups.Keys);
            seeds.Sort();
            foreach (int seed in seeds)
            {
                List<Dictionary<string, string>> runs = groups[seed];
                runs.Sort((a, b) => Number(a, "seed_attempt").CompareTo(Number(b, "seed_attempt")));
                Dictionary<string, string> first = runs[0];
                var escapedTicks = new List<double>();
                var modes = new HashSet<string>();
                int escaped = 0;
                int dead = 0;
                int mia = 0;
                foreach (var row in runs)
                {
                    modes.Add(Get(row, "seed_mode"));
                    string result = Get(row, "result");
                    if (result == ExpeditionResult.Escaped.ToString())
                    {
                        escaped++;
                        escapedTicks.Add(Number(row, "total_ticks"));
                    }
                    else if (result == ExpeditionResult.Dead.ToString()) dead++;
                    else if (result == ExpeditionResult.MIA.ToString()) mia++;
                }
                string mode = modes.Count == 1 ? Get(first, "seed_mode") : "Mixed";
                writer.WriteLine(string.Join(",", new[]
                {
                    seed.ToString(CultureInfo.InvariantCulture),
                    mode,
                    runs.Count.ToString(CultureInfo.InvariantCulture),
                    escaped.ToString(CultureInfo.InvariantCulture),
                    dead.ToString(CultureInfo.InvariantCulture),
                    mia.ToString(CultureInfo.InvariantCulture),
                    Get(first, "total_ticks"),
                    Get(first, "result"),
                    escapedTicks.Count > 0 ? F((float)Min(escapedTicks)) : string.Empty,
                    escapedTicks.Count > 0 ? F((float)Average(escapedTicks)) : string.Empty,
                    escapedTicks.Count > 0 ? F((float)Max(escapedTicks)) : string.Empty,
                    Get(first, "exit_path_tiles"),
                    Get(first, "exit_room_enemies")
                }));
            }
        }
#endif
    }

    private static int CreateRandomSeed()
    {
        byte[] bytes = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt32(bytes, 0);
    }

    private static int CountSeedAttempts(int seed)
    {
        int count = 0;
        foreach (var row in ReadV3Rows())
        {
            if (TryInt(row, "seed", out int existingSeed) && existingSeed == seed) count++;
        }
        return count;
    }

    private static List<Dictionary<string, string>> ReadV3Rows()
    {
        var rows = new List<Dictionary<string, string>>();
        if (!File.Exists(OutputPath)) return rows;
        string[] lines = File.ReadAllLines(OutputPath);
        if (lines.Length < 2) return rows;
        string[] headers = ParseCsvLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCsvLine(lines[i]);
            if (values.Length != headers.Length) continue;
            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++) row[headers[j]] = values[j];
            rows.Add(row);
        }
        return rows;
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted)
            {
                values.Add(current.ToString());
                current.Length = 0;
            }
            else current.Append(c);
        }
        values.Add(current.ToString());
        return values.ToArray();
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static bool TryInt(Dictionary<string, string> row, string key, out int value)
    {
        return int.TryParse(Get(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static double Number(Dictionary<string, string> row, string key)
    {
        return double.TryParse(Get(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value : 0d;
    }

    private static double Min(List<double> values)
    {
        double result = double.MaxValue;
        foreach (double value in values) if (value < result) result = value;
        return result;
    }

    private static double Max(List<double> values)
    {
        double result = double.MinValue;
        foreach (double value in values) if (value > result) result = value;
        return result;
    }

    private static double Average(List<double> values)
    {
        double total = 0d;
        foreach (double value in values) total += value;
        return values.Count == 0 ? 0d : total / values.Count;
    }

    private static string NormalizeAction(string action)
    {
        if (string.IsNullOrEmpty(action) || action == "Move/Wait") return "Move";
        if (action.IndexOf("Wait", StringComparison.OrdinalIgnoreCase) >= 0) return "Wait";
        if (action.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0) return "Attack";
        if (action.IndexOf("Door", StringComparison.OrdinalIgnoreCase) >= 0) return "Door";
        if (action.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0
            || action.IndexOf("Loot", StringComparison.OrdinalIgnoreCase) >= 0
            || action.IndexOf("PickUp", StringComparison.OrdinalIgnoreCase) >= 0) return "Loot";
        if (action.IndexOf("Interact", StringComparison.OrdinalIgnoreCase) >= 0) return "Interact";
        return "Other";
    }

    private static float GetTicks(string category)
    {
        return playerActionTicks.TryGetValue(category, out var value) ? value : 0f;
    }

    private static void ResetCounters()
    {
        playerActionTicks.Clear();
        visitedRooms.Clear();
        encounteredEnemies.Clear();
        playerActions = 0;
        enemyActions = 0;
        enemyActionTicks = 0f;
        roomRevisits = 0;
        lastRoomKey = null;
        enemiesKilled = 0;
        damageDealt = 0;
        damageTaken = 0;
        healingReceived = 0;
        chestsOpened = 0;
        itemsAcquired = 0;
        itemsGrounded = 0;
        itemsDroppedToContainer = 0;
        chestItemsGenerated = 0;
        bossItemsGenerated = 0;
        exitPathTiles = 0;
        exitRoomEnemies = 0;
        debugRun = false;
    }

    private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
