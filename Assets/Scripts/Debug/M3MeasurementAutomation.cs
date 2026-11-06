using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// M3 측정용 런타임 기록기. 게임 규칙에는 관여하지 않고, Editor/Development Build에서
// 재현 Seed와 플레이 행동을 CSV 한 행으로 남긴다.
public static class M3MeasurementAutomation
{
    private const string SeedPreferenceKey = "M3.RunSeed";
    private const int DefaultSeed = 20261106;
    private static readonly Dictionary<string, float> playerActionTicks = new Dictionary<string, float>();
    private static readonly HashSet<string> visitedRooms = new HashSet<string>();
    private static bool sessionPrepared;
    private static bool recording;
    private static int runSeed = DefaultSeed;
    private static int playerActions;
    private static int enemyActions;
    private static float enemyActionTicks;
    private static int roomRevisits;
    private static string lastRoomKey;

    public static int ConfiguredSeed => PlayerPrefs.GetInt(SeedPreferenceKey, DefaultSeed);
    public static string OutputDirectory => Path.Combine(Application.persistentDataPath, "M3Measurements");
    public static string OutputPath => Path.Combine(OutputDirectory, "expedition_runs.csv");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        sessionPrepared = false;
        recording = false;
        ResetCounters();
    }

    public static void PrepareNewSession()
    {
        sessionPrepared = true;
        recording = false;
        ResetCounters();
    }

    public static void BeginOrResume(int seed)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (recording) return;
        if (!sessionPrepared) ResetCounters();
        sessionPrepared = false;
        recording = true;
        runSeed = seed;
        Debug.Log($"[M3Measure] 기록 시작 — Seed {runSeed}, CSV {OutputPath}");
#endif
    }

    public static void SetConfiguredSeed(int seed)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        PlayerPrefs.SetInt(SeedPreferenceKey, seed);
        PlayerPrefs.Save();
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

    public static void Finish(ExpeditionResult result, float elapsedTicks, int reachedFloor)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!recording) return;
        recording = false;
        Directory.CreateDirectory(OutputDirectory);
        bool writeHeader = !File.Exists(OutputPath) || new FileInfo(OutputPath).Length == 0;
        using (var writer = new StreamWriter(OutputPath, true, new UTF8Encoding(true)))
        {
            if (writeHeader)
            {
                writer.WriteLine("timestamp,seed,result,total_ticks,reached_floor,player_actions,enemy_actions,enemy_action_ticks,move_ticks,wait_ticks,attack_ticks,door_ticks,loot_ticks,interact_ticks,other_ticks,visited_rooms,room_revisits,bag_items,hp");
            }
            writer.WriteLine(string.Join(",", new[]
            {
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                runSeed.ToString(CultureInfo.InvariantCulture),
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
                ExpeditionSession.PlayerCurrentHP.ToString(CultureInfo.InvariantCulture)
            }));
        }
        Debug.Log($"[M3Measure] 원정 기록 저장 완료 — {OutputPath}");
#endif
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
        playerActions = 0;
        enemyActions = 0;
        enemyActionTicks = 0f;
        roomRevisits = 0;
        lastRoomKey = null;
    }

    private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
