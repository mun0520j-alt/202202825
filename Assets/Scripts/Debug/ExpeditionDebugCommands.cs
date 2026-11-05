#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.Tilemaps;

// M1 경계값 QA용. 에디터 또는 Development Build에만 포함된다.
public sealed class ExpeditionDebugCommands : MonoBehaviour
{
    [Min(0f)] public float targetElapsedTicks = 1439f;
    [Min(1)] public int targetPlayerHP = 1;
    [SerializeField] private bool showTileGrid;

    private GUIStyle gridLabelStyle;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) showTileGrid = !showTileGrid;
        if (Input.GetKeyDown(KeyCode.F4)) BuildM1DebugMap();
        if (Input.GetKeyDown(KeyCode.F5)) FillM2Bag();
    }

    private void OnGUI()
    {
        if (!showTileGrid) return;
        DrawTileGridOverlay();
    }

    [ContextMenu("QA/Toggle Tile Grid (F3)")]
    public void ToggleTileGrid()
    {
        showTileGrid = !showTileGrid;
    }

    [ContextMenu("QA/Set Elapsed Tick")]
    public void SetConfiguredTick()
    {
        SetElapsedTicks(targetElapsedTicks);
    }

    [ContextMenu("QA/Build M1 Debug Map")]
    public void BuildM1DebugMap()
    {
        BuildNearPlayer();
    }

    [ContextMenu("QA/Fill M2 Bag (F5)")]
    public void FillM2Bag()
    {
        LootItemData definition = null;
        foreach (var candidate in LootItemDatabase.Items)
        {
            if (candidate.DropWeight <= 0) continue;
            definition = candidate;
            break;
        }
        if (definition == null)
        {
            Debug.LogWarning("[QA] 가방을 채울 아이템 정의가 없습니다.");
            return;
        }

        while (LootRuntimeState.RunBag.Count < LootRuntimeState.BagCapacity)
            LootRuntimeState.TryAddToRunBag(new LootItemInstance(definition));

        Debug.Log($"[QA] M2 가방을 {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}로 채웠습니다.");
    }

    [ContextMenu("QA/Set Player HP")]
    public void SetConfiguredPlayerHP()
    {
        if (PlayerTurnActor.Instance != null) PlayerTurnActor.Instance.DebugSetCurrentHP(targetPlayerHP);
    }

    public static bool SetElapsedTicks(float elapsedTicks)
    {
        var tickManager = TickManager.Instance;
        var dungeonClock = tickManager != null ? tickManager.GetComponent<DungeonClock>() : null;
        if (tickManager == null || dungeonClock == null || !ExpeditionSession.IsActive)
        {
            Debug.LogWarning("[QA] 활성 원정의 TickManager/DungeonClock을 찾지 못해 Tick을 설정하지 못했습니다.");
            return false;
        }

        float safeTick = Mathf.Max(0f, elapsedTicks);
        tickManager.DebugSetTimeline(safeTick);
        dungeonClock.DebugSetElapsedTicks(safeTick);
        ExpeditionSession.DebugSetElapsedTicks(safeTick);
        Debug.Log($"[QA] 누적 Tick을 {safeTick:0.##}(으)로 설정했습니다.");
        return true;
    }

    private void DrawTileGridOverlay()
    {
        var player = PlayerTurnActor.Instance;
        var camera = Camera.main;
        if (player == null || camera == null || player.FloorTilemap == null || player.WallsTilemap == null) return;

        Tilemap floor = player.FloorTilemap;
        Tilemap walls = player.WallsTilemap;
        Vector3Int playerCell = player.CurrentCell;

        Vector3 bottomLeftWorld = camera.ViewportToWorldPoint(new Vector3(0f, 0f, -camera.transform.position.z));
        Vector3 topRightWorld = camera.ViewportToWorldPoint(new Vector3(1f, 1f, -camera.transform.position.z));
        Vector3Int minCell = floor.WorldToCell(bottomLeftWorld) - Vector3Int.one;
        Vector3Int maxCell = floor.WorldToCell(topRightWorld) + Vector3Int.one;

        EnsureGridLabelStyle();
        for (int y = minCell.y; y <= maxCell.y; y++)
        {
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                Rect rect = CellToGuiRect(floor, camera, cell);
                if (rect.width < 2f || rect.height < 2f) continue;

                bool hasFloor = floor.HasTile(cell);
                bool hasWall = walls.HasTile(cell);
                ITurnActor occupyingActor = TickManager.Instance != null
                    ? TickManager.Instance.GetActorAt(cell, player)
                    : null;
                Color fill = hasWall
                    ? new Color(1f, 0.1f, 0.1f, 0.28f)
                    : hasFloor ? new Color(0.1f, 0.9f, 0.35f, 0.12f) : new Color(0.25f, 0.25f, 0.25f, 0.08f);
                FillRect(rect, fill);
                DrawRectOutline(rect, hasWall ? Color.red : hasFloor ? Color.green : Color.gray, 1f);

                string direction = GetDirectionLabel(playerCell, cell);
                if (cell == playerCell)
                {
                    DrawRectOutline(rect, Color.yellow, 3f);
                    direction = "PLAYER";
                }
                else if (!string.IsNullOrEmpty(direction))
                {
                    DrawRectOutline(rect, new Color(1f, 0.55f, 0f), 3f);
                }
                if (occupyingActor != null)
                {
                    DrawRectOutline(rect, new Color(0.85f, 0.2f, 1f), 4f);
                    direction = $"ACTOR:{occupyingActor.GetType().Name}";
                }

                if (rect.width >= 34f)
                {
                    string state = hasWall ? "W" : hasFloor ? "F" : "-";
                    GUI.Label(rect, $"{x},{y}\n{state} {direction}", gridLabelStyle);
                }
            }
        }

        GUI.Box(new Rect(12f, 12f, 360f, 86f),
            "F3: Grid 끄기 / F4: 빠른 검증 배치\n초록=Floor / 빨강=Wall / 노랑=Player\n" +
            "주황=검색 칸 / 보라=다른 Actor 점유\nF4 배치: 왼쪽 상자 / 위 Enemy / 오른쪽 탈출구");
    }

    private void EnsureGridLabelStyle()
    {
        if (gridLabelStyle != null) return;
        gridLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            normal = { textColor = Color.white }
        };
    }

    private static Rect CellToGuiRect(Tilemap tilemap, Camera camera, Vector3Int cell)
    {
        Vector3 worldMin = tilemap.CellToWorld(cell);
        Vector3 worldMax = tilemap.CellToWorld(cell + Vector3Int.one);
        Vector3 screenMin = camera.WorldToScreenPoint(worldMin);
        Vector3 screenMax = camera.WorldToScreenPoint(worldMax);
        float x = Mathf.Min(screenMin.x, screenMax.x);
        float y = Screen.height - Mathf.Max(screenMin.y, screenMax.y);
        return new Rect(x, y, Mathf.Abs(screenMax.x - screenMin.x), Mathf.Abs(screenMax.y - screenMin.y));
    }

    private static string GetDirectionLabel(Vector3Int origin, Vector3Int cell)
    {
        Vector3Int delta = cell - origin;
        if (delta == Vector3Int.up) return "+Y";
        if (delta == Vector3Int.down) return "-Y";
        if (delta == Vector3Int.right) return "+X";
        if (delta == Vector3Int.left) return "-X";
        return string.Empty;
    }

    private static void FillRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        FillRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        FillRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        FillRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        FillRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    private static bool BuildNearPlayer()
    {
        var player = PlayerTurnActor.Instance;
        if (player == null || player.FloorTilemap == null || player.WallsTilemap == null)
        {
            Debug.LogWarning("[QA] 플레이어 또는 Floor/Walls Tilemap을 찾지 못해 디버그 맵을 만들 수 없습니다.");
            return false;
        }

        Tilemap floor = player.FloorTilemap;
        Tilemap walls = player.WallsTilemap;
        Vector3Int center = player.CurrentCell;
        TileBase floorTile = floor.GetTile(center);
        if (floorTile == null)
        {
            Debug.LogWarning("[QA] 플레이어 칸의 바닥 타일을 찾지 못했습니다.");
            return false;
        }

        for (int y = -4; y <= 4; y++)
        {
            for (int x = -4; x <= 4; x++)
            {
                var cell = center + new Vector3Int(x, y, 0);
                floor.SetTile(cell, floorTile);
                walls.SetTile(cell, null);
            }
        }

        ExpeditionSession.DebugSetCurrentFloor(ExpeditionSession.FinalFloor);
        ReplaceExit(floor, center + new Vector3Int(2, 0, 0));
        PlaceEnemy(center + new Vector3Int(0, 2, 0));
        CreateDebugChest(floor, center + new Vector3Int(-2, 0, 0));
        FogOfWarController.Instance?.UpdateVisibility(center, 8);

        Debug.Log("[QA] M2 디버그 맵 구성 완료: 왼쪽 실제 상자 / 위 Enemy / 오른쪽 3층 탈출구. F5=가방 채우기.");
        return true;
    }

    private static void ReplaceExit(Tilemap floor, Vector3Int cell)
    {
        foreach (var stairs in Object.FindObjectsOfType<DungeonStairs>()) Object.Destroy(stairs.gameObject);
        new GameObject("EscapeExit(QA)").AddComponent<DungeonStairs>().Initialize(floor, cell, true);
    }

    private static void PlaceEnemy(Vector3Int cell)
    {
        var enemies = Object.FindObjectsOfType<EnemyTurnActor>();
        if (enemies.Length == 0)
        {
            Debug.LogWarning("[QA] 배치할 Enemy가 없습니다. DungeonRunManager의 Enemy 생성을 확인하세요.");
            return;
        }

        enemies[0].DebugTeleportTo(cell);
        enemies[0].gameObject.SetActive(true);
        for (int i = 1; i < enemies.Length; i++) enemies[i].gameObject.SetActive(false);
    }

    private static void CreateDebugChest(Tilemap floor, Vector3Int cell)
    {
        var previous = GameObject.Find("DebugChest(M2)");
        if (previous != null) return;

        var chest = new GameObject("DebugChest(M2)");
        chest.AddComponent<DungeonChest>().Initialize(
            floor, cell, ExpeditionSession.CurrentFloor, 20261105);
    }
}
#endif
