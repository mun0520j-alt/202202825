using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 6) FloorLayoutGenerator 결과를 실제 씬의 Tilemap에 칠해서 확인하는 창.
// RuleTile 없이 단순 바닥/벽 타일 하나씩만 써서 "구조(방 모양/문 위치/바깥 벽)가 맞는지"부터
// 검증한다 — 비주얼 퀄리티(코너/엣지 자동 선택)는 RuleTile을 붙이는 다음 단계에서.
public class FloorTilemapPreviewWindow : EditorWindow
{
    private int gridWidth = 8;
    private int gridHeight = 8;
    private int minRooms = 12;
    private int maxRooms = 18;
    private int seed = 12345;
    private bool randomSeedOnGenerate = true;
    private int chunkSize = 16;

    private const string RootName = "MapGenPreview";

    [MenuItem("Dungeon Tools/6) Floor Tilemap Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<FloorTilemapPreviewWindow>("Floor Tilemap Preview");
        window.minSize = new Vector2(340, 280);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            $"씬에 '{RootName}' Grid/Tilemap을 자동으로 만들고 그 위에 칠합니다. RuleTile 없이 단순 타일만 씁니다.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 4, 16);
        gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 4, 16);

        minRooms = EditorGUILayout.IntField("Min Rooms", minRooms);
        maxRooms = EditorGUILayout.IntField("Max Rooms", maxRooms);
        minRooms = Mathf.Clamp(minRooms, 1, gridWidth * gridHeight);
        maxRooms = Mathf.Clamp(maxRooms, minRooms, gridWidth * gridHeight);

        chunkSize = Mathf.Max(4, EditorGUILayout.IntField("Chunk Size (tiles)", chunkSize));

        EditorGUILayout.Space(4);
        randomSeedOnGenerate = EditorGUILayout.Toggle("Randomize Seed On Generate", randomSeedOnGenerate);
        using (new EditorGUI.DisabledScope(randomSeedOnGenerate))
        {
            seed = EditorGUILayout.IntField("Seed", seed);
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Generate & Paint", GUILayout.Height(30)))
        {
            if (randomSeedOnGenerate) seed = System.Guid.NewGuid().GetHashCode();
            var layout = FloorLayoutGenerator.Generate(gridWidth, gridHeight, minRooms, maxRooms, seed);
            PaintToScene(layout);
        }
    }

    private void PaintToScene(FloorLayout layout)
    {
        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName, typeof(Grid));
        }

        var floorTilemap = GetOrCreateChildTilemap(root.transform, "Floor", sortingOrder: 0);
        var wallTilemap = GetOrCreateChildTilemap(root.transform, "Walls", sortingOrder: 1);

        var floorTile = PlaceholderTileFactory.GetFloorTile();
        var wallTile = PlaceholderTileFactory.GetWallTile();
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("[FloorTilemapPreviewWindow] 플레이스홀더 타일을 못 만들어서 중단합니다 — Console 로그 확인해주세요.");
            return;
        }

        var painter = new FloorTilemapPainter(floorTilemap, wallTilemap, floorTile, wallTile) { ChunkSize = chunkSize };
        painter.Paint(layout);

        CenterMainCameraOn(layout, floorTilemap);
        PlacePlayerAtStart(layout, floorTilemap, root.transform);

        Debug.Log($"[FloorTilemapPreviewWindow] 방 {layout.RoomCount}개 · Shape {layout.Shape} · Seed {seed} 칠하기 완료 — '{RootName}' 확인해보세요.");

        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    // 방은 청크 좌표(예: 시작 방이 (8,8))에 ChunkSize를 곱한 위치에 그려지기 때문에, 실제
    // 월드 좌표로는 원점에서 한참 떨어진 곳에 생긴다. 씬에 원래 있던 Main Camera는 그 사실을
    // 모르니, 생성된 방들의 바운딩 박스 중심으로 카메라를 옮겨서 Game 뷰에 잡히게 한다.
    private void CenterMainCameraOn(FloorLayout layout, Tilemap referenceTilemap)
    {
        if (layout.RoomCells.Count == 0) return;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var cell in layout.RoomCells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }

        int centerChunkX = (minX + maxX) / 2;
        int centerChunkY = (minY + maxY) / 2;
        var centerTilePos = new Vector3Int(
            centerChunkX * chunkSize + chunkSize / 2,
            centerChunkY * chunkSize + chunkSize / 2,
            0);
        Vector3 worldCenter = referenceTilemap.CellToWorld(centerTilePos);

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[FloorTilemapPreviewWindow] 씬에 MainCamera 태그된 카메라가 없어서 카메라 위치는 못 옮겼어요 — 직접 옮기거나 태그를 확인해주세요.");
            return;
        }

        // 카메라 Z가 스프라이트(Z=0)랑 같거나 너무 가까우면 Near Clip(기본 0.3)에 걸려서
        // 아무것도 안 보인다 — 원래 Z를 그대로 쓰지 않고 2D 표준값(-10)으로 강제한다.
        float safeZ = cam.transform.position.z <= -1f ? cam.transform.position.z : -10f;
        cam.transform.position = new Vector3(worldCenter.x, worldCenter.y, safeZ);
    }

    // 아직 실제 플레이어 프리팹/컨트롤러가 없어서, 검증용으로 이미 만들어둔 몬스터/캐릭터
    // 프리팹 중 하나(knight_m — hit 애니까지 있어서 플레이어블 후보로 봤던 것)를 시작 방
    // 중앙에 세워둔다. 나중에 진짜 플레이어 프리팹이 생기면 이 경로만 바꾸면 된다.
    private const string PlayerPrefabPath = "Assets/Generated/Prefabs/Creatures/knight_m.prefab";
    private const string PlayerObjectName = "Player";
    private const int PlayerSortingOrder = 2; // Floor(0)/Walls(1)보다 위에 그려지도록

    private void PlacePlayerAtStart(FloorLayout layout, Tilemap referenceTilemap, Transform parent)
    {
        var startTilePos = new Vector3Int(
            layout.StartCell.x * chunkSize + chunkSize / 2,
            layout.StartCell.y * chunkSize + chunkSize / 2,
            0);
        Vector3 worldPos = referenceTilemap.CellToWorld(startTilePos);

        var existing = parent.Find(PlayerObjectName);
        GameObject player;
        if (existing != null)
        {
            player = existing.gameObject;
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[FloorTilemapPreviewWindow] 플레이어용 프리팹을 못 찾음: {PlayerPrefabPath} — 배치를 건너뜁니다.");
                return;
            }

            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            player.name = PlayerObjectName;

            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = PlayerSortingOrder;
        }

        player.transform.position = worldPos;
    }

    private Tilemap GetOrCreateChildTilemap(Transform parent, string childName, int sortingOrder)
    {
        var child = parent.Find(childName);
        GameObject go = child != null ? child.gameObject : new GameObject(childName, typeof(Tilemap), typeof(TilemapRenderer));
        if (child == null)
        {
            go.transform.SetParent(parent, false);
        }

        var renderer = go.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return go.GetComponent<Tilemap>();
    }
}
