using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class M3QaAutomationWindow : EditorWindow
{
    private int seedStart = 20261106;
    private int seedCount = 1000;
    private int lootSamplesPerFloor = 100000;
    private Vector2 scroll;
    private string lastSummary = "아직 실행하지 않았습니다.";

    private static string OutputDirectory => Path.Combine(Application.persistentDataPath, "M3Measurements");
    private static string ReportPath => Path.Combine(OutputDirectory, "M3_QA_AUTOMATION_REPORT.md");

    [MenuItem("Dungeon Tools/QA/M3 Automation")]
    public static void ShowWindow()
    {
        var window = GetWindow<M3QaAutomationWindow>("M3 QA Automation");
        window.minSize = new Vector2(540f, 430f);
    }

    public static void RunAllFromCommandLine()
    {
        string summary = RunAll(20261106, 1000, 100000);
        Debug.Log(summary);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("M3 QA 자동화", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "게임 규칙을 변경하지 않고 Seed 안정성, 드롭 분포, 핵심 상수 회귀, 플레이 CSV를 검사합니다. " +
            "실패 Seed와 보고서는 Persistent Data/M3Measurements에 저장됩니다.", MessageType.Info);

        seedStart = EditorGUILayout.IntField("시작 Seed", seedStart);
        seedCount = Mathf.Clamp(EditorGUILayout.IntField("검증 Seed 수", seedCount), 1, 100000);
        lootSamplesPerFloor = Mathf.Clamp(EditorGUILayout.IntField("층별 드롭 표본", lootSamplesPerFloor), 100, 1000000);

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("전체 자동화 실행", GUILayout.Height(34f)))
            lastSummary = RunAll(seedStart, seedCount, lootSamplesPerFloor);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Seed 검증"))
            lastSummary = RunSeedValidation(seedStart, seedCount).Summary;
        if (GUILayout.Button("드롭 시뮬레이션"))
            lastSummary = RunLootSimulation(lootSamplesPerFloor).Summary;
        if (GUILayout.Button("CSV 분석"))
            lastSummary = AnalyzeRunCsv().Summary;
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("결과 폴더 열기"))
        {
            Directory.CreateDirectory(OutputDirectory);
            EditorUtility.RevealInFinder(OutputDirectory);
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("최근 결과", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(lastSummary, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private static string RunAll(int startSeed, int count, int lootSamples)
    {
        Directory.CreateDirectory(OutputDirectory);
        var seed = RunSeedValidation(startSeed, count);
        var loot = RunLootSimulation(lootSamples);
        var regression = RunCoreRegression();
        var runs = AnalyzeRunCsv();

        var report = new StringBuilder();
        report.AppendLine("# M3 QA 자동화 보고서");
        report.AppendLine();
        report.AppendLine($"- 생성 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"- 종합: {(seed.Passed && loot.Passed && regression.Passed ? "PASS" : "FAIL")}");
        report.AppendLine();
        AppendSection(report, "다중 Seed 생성 검증", seed);
        AppendSection(report, "아이템 드롭 분포", loot);
        AppendSection(report, "핵심 회귀", regression);
        AppendSection(report, "원정 CSV 분석", runs);
        File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(true));

        string summary = $"M3 자동화 {(seed.Passed && loot.Passed && regression.Passed ? "PASS" : "FAIL")}\n" +
                         $"Seed: {seed.Summary}\n드롭: {loot.Summary}\n회귀: {regression.Summary}\nCSV: {runs.Summary}\n" +
                         $"보고서: {ReportPath}";
        Debug.Log(summary);
        AssetDatabase.Refresh();
        return summary;
    }

    private static QaResult RunSeedValidation(int startSeed, int count)
    {
        var failures = new List<string>();
        int loopFloors = 0;
        int treeFloors = 0;
        for (int runIndex = 0; runIndex < count; runIndex++)
        {
            int runSeed = unchecked(startSeed + runIndex);
            var runRng = new System.Random(runSeed);
            for (int floor = 1; floor <= ExpeditionSession.FinalFloor; floor++)
            {
                int floorSeed = runRng.Next();
                FloorLayout layout = FloorLayoutGenerator.Generate(16, 16, 12, 16, floorSeed);
                if (layout.Shape == FloorShape.Loop) loopFloors++; else treeFloors++;
                foreach (string error in ValidateLayout(layout))
                    failures.Add($"{runSeed},{floor},{floorSeed},{Csv(error)}");

                FloorLayout replay = FloorLayoutGenerator.Generate(16, 16, 12, 16, floorSeed);
                if (LayoutSignature(layout) != LayoutSignature(replay))
                    failures.Add($"{runSeed},{floor},{floorSeed},{Csv("동일 Seed 재생성 불일치")}");
            }
        }

        Directory.CreateDirectory(OutputDirectory);
        string failurePath = Path.Combine(OutputDirectory, "seed_validation_failures.csv");
        var lines = new List<string> { "run_seed,floor,floor_seed,error" };
        lines.AddRange(failures);
        File.WriteAllLines(failurePath, lines, new UTF8Encoding(true));
        int floors = count * ExpeditionSession.FinalFloor;
        return new QaResult(
            failures.Count == 0,
            $"{count}개 Run Seed/{floors}개 층, 실패 {failures.Count}, Loop {loopFloors}, Tree {treeFloors}",
            $"검증 범위: Run Seed {startSeed}~{unchecked(startSeed + count - 1)}\n" +
            $"총 층: {floors}\n실패: {failures.Count}\nLoop/Tree: {loopFloors}/{treeFloors}\n실패 목록: {failurePath}");
    }

    private static IEnumerable<string> ValidateLayout(FloorLayout layout)
    {
        if (layout == null) { yield return "Layout null"; yield break; }
        if (layout.RoomCount < 12 || layout.RoomCount > 18) yield return $"방 수 범위 이탈: {layout.RoomCount}";
        if (!layout.RoomCells.Contains(layout.StartCell)) yield return "시작 방 누락";
        if (!layout.RoomCells.Contains(layout.BossCell)) yield return "최원거리 방 누락";

        var reached = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        reached.Add(layout.StartCell);
        queue.Enqueue(layout.StartCell);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (!layout.Connections.TryGetValue(current, out var neighbors))
            {
                yield return $"연결 데이터 누락: {current}";
                continue;
            }
            foreach (Vector2Int neighbor in neighbors)
            {
                if (!layout.RoomCells.Contains(neighbor)) yield return $"존재하지 않는 방 연결: {current}->{neighbor}";
                if (!layout.Connections.TryGetValue(neighbor, out var reverse) || !reverse.Contains(current))
                    yield return $"비대칭 연결: {current}->{neighbor}";
                if (reached.Add(neighbor)) queue.Enqueue(neighbor);
            }
        }
        if (reached.Count != layout.RoomCount) yield return $"방 연결 실패: {reached.Count}/{layout.RoomCount}";

        foreach (Vector2Int cell in layout.RoomCells)
            if (layout.GetRoomId(cell) < 0) yield return $"Room ID 누락: {cell}";
    }

    private static QaResult RunLootSimulation(int samplesPerFloor)
    {
        Directory.CreateDirectory(OutputDirectory);
        string output = Path.Combine(OutputDirectory, "loot_distribution.csv");
        var lines = new List<string> { "floor,samples,normal,rare,epic,unique,legendary,normal_pct,rare_pct,epic_pct,unique_pct,legendary_pct" };
        var detail = new StringBuilder();
        bool passed = true;
        double[][] expected =
        {
            new[] { 79d, 18d, 2d, 1d },
            new[] { 69d, 25d, 5d, 1d },
            new[] { 59d, 30d, 10d, 1d },
            new[] { 59d, 30d, 10d, 1d },
        };
        for (int floor = 1; floor <= 4; floor++)
        {
            var counts = new int[5];
            var rng = new System.Random(0x4D330000 + floor);
            for (int i = 0; i < samplesPerFloor; i++) counts[(int)LootItemDatabase.RollRarity(floor, rng)]++;
            var pct = counts.Select(value => value * 100d / samplesPerFloor).ToArray();
            bool floorPass = counts[(int)LootRarity.Legendary] == 0;
            for (int rarity = 0; rarity < 4; rarity++)
                if (Math.Abs(pct[rarity] - expected[floor - 1][rarity]) > 0.5d) floorPass = false;
            passed &= floorPass;
            lines.Add(string.Join(",", new[]
            {
                floor.ToString(), samplesPerFloor.ToString(), counts[0].ToString(), counts[1].ToString(),
                counts[2].ToString(), counts[3].ToString(), counts[4].ToString(),
                F(pct[0]), F(pct[1]), F(pct[2]), F(pct[3]), F(pct[4])
            }));
            detail.AppendLine($"F{floor}: N {F(pct[0])}% / R {F(pct[1])}% / E {F(pct[2])}% / U {F(pct[3])}% / L {F(pct[4])}% — {(floorPass ? "PASS" : "FAIL")}");
        }
        File.WriteAllLines(output, lines, new UTF8Encoding(true));

        var ids = new HashSet<string>();
        foreach (LootItemData item in LootItemDatabase.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Name)
                || item.DropWeight < 0 || !ids.Add(item.Id)) passed = false;
        }
        return new QaResult(passed,
            $"층별 {samplesPerFloor:N0}회, Legendary 0 검증 {(passed ? "통과" : "실패")}",
            detail + $"아이템 정의: {LootItemDatabase.Items.Count}개, ID 중복 없음 {ids.Count == LootItemDatabase.Items.Count}\n분포 CSV: {output}");
    }

    private static QaResult RunCoreRegression()
    {
        var checks = new List<string>();
        Check(checks, TickCost.PerTileMove == 1f, "이동 1Tick");
        Check(checks, TickCost.Attack == 5f, "공격 5Tick");
        Check(checks, TickCost.PickUp == 1f, "줍기 1Tick");
        Check(checks, TickCost.Drop == 1f, "버리기 1Tick");
        Check(checks, TickCost.SwapWeaponInCombat == 2f, "장비 교체 2Tick");
        Check(checks, LootRuntimeState.BaseBagCapacity == 6, "기본 가방 6칸");
        Check(checks, ExpeditionSession.FirstFloor == 1 && ExpeditionSession.FinalFloor == 3, "원정 1~3층");

        bool layoutDeterministic = true;
        bool lootDeterministic = true;
        for (int i = 0; i < 100; i++)
        {
            int seed = 20261106 + i;
            layoutDeterministic &= LayoutSignature(FloorLayoutGenerator.Generate(16, 16, 12, 16, seed))
                                   == LayoutSignature(FloorLayoutGenerator.Generate(16, 16, 12, 16, seed));
            LootItemData a = LootItemDatabase.Roll(1 + i % 4, new System.Random(seed));
            LootItemData b = LootItemDatabase.Roll(1 + i % 4, new System.Random(seed));
            lootDeterministic &= a != null && b != null && a.Id == b.Id && a.Type != LootItemType.Bag;
        }
        Check(checks, layoutDeterministic, "맵 Seed 결정성 100회");
        Check(checks, lootDeterministic, "드롭 Seed 결정성·Bag 드롭 제외 100회");

        bool passed = checks.All(value => value.StartsWith("PASS"));
        return new QaResult(passed, $"{checks.Count(value => value.StartsWith("PASS"))}/{checks.Count} 통과", string.Join("\n", checks));
    }

    private static QaResult AnalyzeRunCsv()
    {
        M3MeasurementAutomation.RebuildSeedSummary();
        string v3 = Path.Combine(OutputDirectory, "expedition_runs_v3.csv");
        string v2 = Path.Combine(OutputDirectory, "expedition_runs_v2.csv");
        string v1 = Path.Combine(OutputDirectory, "expedition_runs.csv");
        string path = File.Exists(v3) ? v3 : File.Exists(v2) ? v2 : v1;
        if (!File.Exists(path)) return new QaResult(true, "분석할 원정 CSV 없음", "Play 원정 종료 후 다시 실행하세요.");

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2) return new QaResult(true, "CSV 헤더만 존재", path);
        string[] headers = ParseCsvLine(lines[0]);
        var rows = new List<Dictionary<string, string>>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCsvLine(lines[i]);
            if (values.Length != headers.Length) continue;
            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++) row[headers[j]] = values[j];
            rows.Add(row);
        }
        if (rows.Count == 0) return new QaResult(false, "유효한 CSV 행 0개", path);

        var measurementRows = headers.Contains("seed_mode")
            ? rows.Where(row => Get(row, "seed_mode") == "Random").ToList()
            : rows;
        var escaped = measurementRows.Where(row => Get(row, "result") == "Escaped").ToList();
        double avg = Average(escaped, "total_ticks");
        int target = escaped.Count(row => Number(row, "total_ticks") >= 300d && Number(row, "total_ticks") <= 450d);
        var detail = new StringBuilder();
        detail.AppendLine($"파일: {path}");
        detail.AppendLine($"전체 {rows.Count}, 목표 판단 표본 {measurementRows.Count}, 생환 {escaped.Count}, 사망 {measurementRows.Count(row => Get(row, "result") == "Dead")}, MIA {measurementRows.Count(row => Get(row, "result") == "MIA")}");
        if (headers.Contains("seed_mode")) detail.AppendLine("목표 Tick 판단은 Random 모드만 포함하며 Fixed 반복 표본은 제외했다.");
        detail.AppendLine($"생환 평균 Tick: {F(avg)}, 목표 300~450 진입: {target}/{escaped.Count}");
        detail.AppendLine($"평균 이동/대기/공격/루팅: {F(Average(measurementRows, "move_ticks"))}/{F(Average(measurementRows, "wait_ticks"))}/{F(Average(measurementRows, "attack_ticks"))}/{F(Average(measurementRows, "loot_ticks"))}");
        if (headers.Contains("enemy_encounters"))
        {
            detail.AppendLine($"평균 조우/처치/받은 피해/회복: {F(Average(measurementRows, "enemy_encounters"))}/{F(Average(measurementRows, "enemies_killed"))}/{F(Average(measurementRows, "damage_taken"))}/{F(Average(measurementRows, "healing_received"))}");
            detail.AppendLine($"평균 상자/획득/출구 경로/출구 방 Enemy: {F(Average(measurementRows, "chests_opened"))}/{F(Average(measurementRows, "items_acquired"))}/{F(Average(measurementRows, "exit_path_tiles"))}/{F(Average(measurementRows, "exit_room_enemies"))}");
        }
        var longest = measurementRows.OrderByDescending(row => Number(row, "total_ticks")).Take(5);
        detail.AppendLine("긴 원정 TOP 5: " + string.Join(", ", longest.Select(row => $"Seed {Get(row, "seed")} {Get(row, "total_ticks")}Tick/{Get(row, "result")}")));
        return new QaResult(true, $"{rows.Count}행 분석, 생환 평균 {F(avg)}Tick", detail.ToString());
    }

    private static string LayoutSignature(FloorLayout layout)
    {
        var builder = new StringBuilder();
        foreach (Vector2Int cell in layout.RoomCells.OrderBy(c => c.y).ThenBy(c => c.x))
        {
            builder.Append(cell.x).Append(':').Append(cell.y).Append(':').Append(layout.GetRoomId(cell)).Append('|');
            if (!layout.Connections.TryGetValue(cell, out var neighbors)) continue;
            foreach (Vector2Int n in neighbors.OrderBy(c => c.y).ThenBy(c => c.x))
                builder.Append(n.x).Append(':').Append(n.y).Append(',');
            builder.Append(';');
        }
        return builder.ToString();
    }

    private static void AppendSection(StringBuilder report, string title, QaResult result)
    {
        report.AppendLine($"## {title} — {(result.Passed ? "PASS" : "FAIL")}");
        report.AppendLine();
        report.AppendLine(result.Detail);
        report.AppendLine();
    }

    private static void Check(List<string> checks, bool passed, string name) => checks.Add($"{(passed ? "PASS" : "FAIL")} — {name}");
    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static string Get(Dictionary<string, string> row, string key) => row.TryGetValue(key, out string value) ? value : string.Empty;
    private static double Number(Dictionary<string, string> row, string key) => double.TryParse(Get(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0d;
    private static double Average(List<Dictionary<string, string>> rows, string key) => rows.Count == 0 ? 0d : rows.Average(row => Number(row, key));

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
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { values.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        values.Add(current.ToString());
        return values.ToArray();
    }

    private sealed class QaResult
    {
        public readonly bool Passed;
        public readonly string Summary;
        public readonly string Detail;
        public QaResult(bool passed, string summary, string detail) { Passed = passed; Summary = summary; Detail = detail; }
    }
}
