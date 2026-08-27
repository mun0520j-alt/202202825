using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Dungeon Tools 5) FloorLayoutGenerator를 파라미터 바꿔가며 즉시 시각적으로 확인하기 위한
// 에디터 창. 아직 Tilemap/RuleTile 파이프라인이 없는 단계라, "구조가 매번 다르게 나오는가"만
// 빠르게 검증하는 용도. 씬/프리팹은 전혀 건드리지 않는다 — 순수 미리보기.
public class FloorLayoutPreviewWindow : EditorWindow
{
    private int gridWidth = 8;
    private int gridHeight = 8;
    private int minRooms = 12;
    private int maxRooms = 18;
    private int seed = 12345;
    private bool randomSeedOnGenerate = true;

    private FloorLayout currentLayout;

    private const int CellSize = 32;
    private const int CellGap = 2;

    private static readonly Color RoomColor = new Color(0.55f, 0.58f, 0.65f);
    private static readonly Color StartColor = new Color(0.95f, 0.8f, 0.25f);
    private static readonly Color BossColor = new Color(0.85f, 0.25f, 0.25f);
    private static readonly Color KeyColor = new Color(0.7f, 0.4f, 0.85f);
    private static readonly Color ConnectionColor = new Color(0.3f, 0.32f, 0.38f);
    private static readonly Color EmptyColor = new Color(0.12f, 0.12f, 0.14f);
    private static readonly Color MergeBorderColor = new Color(0.3f, 0.85f, 0.9f);

    [MenuItem("Dungeon Tools/5) Floor Layout Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<FloorLayoutPreviewWindow>("Floor Layout Preview");
        window.minSize = new Vector2(360, 420);
    }

    private void OnGUI()
    {
        DrawControls();
        EditorGUILayout.Space(8);

        if (currentLayout == null)
        {
            EditorGUILayout.HelpBox("Generate를 눌러 층 구조를 만들어보세요.", MessageType.Info);
            return;
        }

        DrawSummary();
        EditorGUILayout.Space(8);
        DrawGrid();
    }

    private void DrawControls()
    {
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 4, 16);
        gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 4, 16);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Room Count Range", EditorStyles.boldLabel);
        minRooms = EditorGUILayout.IntField("Min Rooms", minRooms);
        maxRooms = EditorGUILayout.IntField("Max Rooms", maxRooms);
        minRooms = Mathf.Clamp(minRooms, 1, gridWidth * gridHeight);
        maxRooms = Mathf.Clamp(maxRooms, minRooms, gridWidth * gridHeight);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Seed", EditorStyles.boldLabel);
        randomSeedOnGenerate = EditorGUILayout.Toggle("Randomize Seed On Generate", randomSeedOnGenerate);
        using (new EditorGUI.DisabledScope(randomSeedOnGenerate))
        {
            seed = EditorGUILayout.IntField("Seed", seed);
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Generate", GUILayout.Height(28)))
        {
            if (randomSeedOnGenerate)
            {
                seed = System.Guid.NewGuid().GetHashCode();
            }
            currentLayout = FloorLayoutGenerator.Generate(gridWidth, gridHeight, minRooms, maxRooms, seed);
        }
    }

    private void DrawSummary()
    {
        int mergedPairs = currentLayout.MergeGroupId.Values.Distinct().Count();
        EditorGUILayout.LabelField(
            $"방 {currentLayout.RoomCount}개 (목표 범위 {minRooms}~{maxRooms}) · Shape {currentLayout.Shape} · Seed {seed}",
            EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(
            $"Start {currentLayout.StartCell} · Boss {currentLayout.BossCell} · Key {string.Join(", ", currentLayout.KeyCells)} · 병합된 방 {mergedPairs}쌍",
            EditorStyles.miniLabel);
    }

    // 실제로 방이 놓인 범위(바운딩 박스)만 그린다 — 선언된 그리드 최대 크기(예: 16x16)를
    // 전부 그리면 대부분 빈 칸이라 뭘 보는 건지 알아보기 힘들어서, 사용된 칸 주변에
    // 여백 1칸만 남기고 나머지는 아예 렌더링하지 않는다.
    private void DrawGrid()
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var cell in currentLayout.RoomCells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }

        const int padding = 1;
        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(currentLayout.GridWidth - 1, maxX + padding);
        maxY = Mathf.Min(currentLayout.GridHeight - 1, maxY + padding);

        int viewWidth = maxX - minX + 1;
        int viewHeight = maxY - minY + 1;

        int step = CellSize + CellGap;
        Rect area = GUILayoutUtility.GetRect(viewWidth * step, viewHeight * step, GUILayout.ExpandWidth(false));

        // 여백(사용된 방 바로 옆 1칸)만 배경으로 표시
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Rect cellRect = GetCellRect(area, x, y, step, minX, minY, maxY);
                EditorGUI.DrawRect(cellRect, EmptyColor);
            }
        }

        // 연결선 (방 중심끼리)
        Handles.BeginGUI();
        Handles.color = ConnectionColor;
        foreach (var kvp in currentLayout.Connections)
        {
            Vector2 a = GetCellCenter(area, kvp.Key, step, minX, minY, maxY);
            foreach (var neighbor in kvp.Value)
            {
                Vector2 b = GetCellCenter(area, neighbor, step, minX, minY, maxY);
                Handles.DrawAAPolyLine(4f, a, b);
            }
        }
        Handles.EndGUI();

        // 방 칸 + 실제 그리드 좌표 라벨
        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.black },
        };

        foreach (var cell in currentLayout.RoomCells)
        {
            Rect cellRect = GetCellRect(area, cell.x, cell.y, step, minX, minY, maxY);
            Color color = RoomColor;
            if (cell == currentLayout.StartCell) color = StartColor;
            else if (cell == currentLayout.BossCell) color = BossColor;
            else if (currentLayout.KeyCells.Contains(cell)) color = KeyColor;

            // 병합된 방은 시안색 테두리를 한 겹 더 그려서 표시 (안쪽에 본래 색을 인셋으로 그림).
            if (currentLayout.IsMerged(cell))
            {
                EditorGUI.DrawRect(cellRect, MergeBorderColor);
                cellRect = new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width - 4, cellRect.height - 4);
            }

            EditorGUI.DrawRect(cellRect, color);
            GUI.Label(cellRect, $"{cell.x},{cell.y}", labelStyle);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("노랑=시작 · 빨강=보스 · 보라=열쇠방 후보 · 회색=일반 방 · 하늘색 테두리=병합된 방 (칸 안 숫자 = 그리드 좌표)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField(
            $"표시 범위: X {minX}~{maxX}, Y {minY}~{maxY} (실사용 {currentLayout.RoomCount}칸 + 여백 {padding}칸)",
            EditorStyles.miniLabel);
    }

    private Rect GetCellRect(Rect area, int gridX, int gridY, int step, int viewMinX, int viewMinY, int viewMaxY)
    {
        // 그리드 y가 위로 갈수록 화면에서도 위로 가도록 뒤집어서 그림(원점이 왼쪽 아래인 좌표계에 맞춤).
        float x = area.x + (gridX - viewMinX) * step;
        float y = area.y + (viewMaxY - gridY) * step;
        return new Rect(x, y, CellSize, CellSize);
    }

    private Vector2 GetCellCenter(Rect area, Vector2Int cell, int step, int viewMinX, int viewMinY, int viewMaxY)
    {
        Rect r = GetCellRect(area, cell.x, cell.y, step, viewMinX, viewMinY, viewMaxY);
        return new Vector2(r.x + r.width / 2f, r.y + r.height / 2f);
    }
}
