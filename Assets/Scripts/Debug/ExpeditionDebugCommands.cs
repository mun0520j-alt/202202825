#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// M1 경계값 QA용. 에디터 또는 Development Build에만 포함된다.
public sealed class ExpeditionDebugCommands : MonoBehaviour
{
    [Min(0f)] public float targetElapsedTicks = 1439f;
    [Min(1)] public int targetPlayerHP = 1;
    [SerializeField] private bool showTileGrid;
    [SerializeField] private bool showM3Panel;
    private string seedInput;

    private GUIStyle gridLabelStyle;

    private void Update()
    {
        if (seedInput == null) seedInput = M3MeasurementAutomation.ConfiguredSeed.ToString();
        if (Input.GetKeyDown(KeyCode.F3)) showTileGrid = !showTileGrid;
        if (Input.GetKeyDown(KeyCode.F4)) BuildM1DebugMap();
        if (Input.GetKeyDown(KeyCode.F5)) FillM2Bag();
        if (Input.GetKeyDown(KeyCode.F6)) showM3Panel = !showM3Panel;
        if (Input.GetKeyDown(KeyCode.F7)) JumpToFloorFive();
    }

    private void OnGUI()
    {
        if (showTileGrid) DrawTileGridOverlay();
        if (showM3Panel) DrawM3Panel();
    }

    private void DrawM3Panel()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 370f, 16f, 354f, 270f), "M3 측정 자동화 (F6)", GUI.skin.window);
        GUILayout.Label($"현재 모드: {M3MeasurementAutomation.CurrentSeedMode}");
        GUILayout.Label($"이번 원정 Seed: {M3MeasurementAutomation.CurrentRunSeed}");
        GUILayout.Label("Run Seed");
        seedInput = GUILayout.TextField(seedInput ?? string.Empty);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Seed 적용 후 재시작"))
        {
            if (int.TryParse(seedInput, out int seed))
            {
                M3MeasurementAutomation.SetConfiguredSeed(seed);
                ExpeditionSession.BeginNewRun();
                SceneManager.LoadScene("InDungeon");
            }
            else Debug.LogWarning("[M3Measure] Seed는 정수로 입력하세요.");
        }
        if (GUILayout.Button("Seed 복사"))
        {
            GUIUtility.systemCopyBuffer = M3MeasurementAutomation.CurrentRunSeed.ToString();
            Debug.Log($"[M3Measure] Seed {GUIUtility.systemCopyBuffer} 복사 완료");
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Random 모드로 전환 (다음 출격부터)"))
        {
            M3MeasurementAutomation.SetRandomMode();
            Debug.Log("[M3Measure] 다음 출격부터 Random Seed 모드를 사용합니다.");
        }
        if (GUILayout.Button("현재 맵 자동 검증"))
        {
            var manager = FindObjectOfType<DungeonRunManager>();
            if (manager != null) manager.ValidateGeneratedRun();
        }
        GUILayout.Label("원정 종료 시 CSV 자동 저장:");
        GUILayout.TextArea(M3MeasurementAutomation.OutputPath, GUILayout.Height(42f));
        GUILayout.EndArea();
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
        M3MeasurementAutomation.MarkDebugRun();
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

        M3MeasurementAutomation.MarkDebugRun();
        while (LootRuntimeState.RunBag.Count < LootRuntimeState.BagCapacity)
            LootRuntimeState.TryAddToRunBag(new LootItemInstance(definition));

        Debug.Log($"[QA] M2 가방을 {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}로 채웠습니다.");
    }

    [ContextMenu("QA/Set Player HP")]
    public void SetConfiguredPlayerHP()
    {
        if (PlayerTurnActor.Instance == null) return;
        M3MeasurementAutomation.MarkDebugRun();
        PlayerTurnActor.Instance.DebugSetCurrentHP(targetPlayerHP);
    }

    [ContextMenu("QA/Jump To Floor 5 (F7)")]
    public void JumpToFloorFive()
    {
        if (!ExpeditionSession.IsActive)
        {
            Debug.LogWarning("[QA] 활성 원정이 없어 5층으로 이동할 수 없습니다.");
            return;
        }

        M3MeasurementAutomation.MarkDebugRun();
        ExpeditionSession.DebugSetCurrentFloor(5);
        Debug.Log($"[QA] F7 — 누적 {ExpeditionSession.ElapsedTicks:0.##} Tick과 현재 장비·가방·HP를 유지하고 5층 보스 검증으로 이동합니다.");
        SceneManager.LoadScene("InDungeon");
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

        M3MeasurementAutomation.MarkDebugRun();
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
                int roomId = DungeonRoomRegistry.GetRoomId(floor, cell);
                bool hasDoor = DungeonDoor.TryGetDebugState(floor, cell, out bool doorOpen);
                bool hasChest = DungeonChest.IsCellBlocked(cell);
                bool hasStairs = DungeonStairs.TryGetDebugLabel(cell, out string stairsLabel);
                ITurnActor occupyingActor = TickManager.Instance != null
                    ? TickManager.Instance.GetActorAt(cell, player)
                    : null;
                Color fill = hasWall
                    ? new Color(1f, 0.1f, 0.1f, 0.28f)
                    : hasFloor ? new Color(0.1f, 0.9f, 0.35f, 0.12f) : new Color(0.25f, 0.25f, 0.25f, 0.08f);
                FillRect(rect, fill);
                DrawRectOutline(rect, hasWall ? Color.red : hasFloor ? Color.green : Color.gray, 1f);

                if (hasDoor)
                {
                    FillRect(rect, doorOpen ? new Color(0.1f, 0.75f, 1f, 0.16f) : new Color(1f, 0.45f, 0.05f, 0.35f));
                    DrawRectOutline(rect, doorOpen ? Color.cyan : new Color(1f, 0.45f, 0.05f), 3f);
                }
                else if (hasChest)
                {
                    DrawRectOutline(rect, new Color(0.65f, 0.35f, 0.1f), 3f);
                }
                else if (hasStairs)
                {
                    DrawRectOutline(rect, new Color(0.2f, 0.8f, 1f), 3f);
                }

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
                    string room = roomId >= 0 ? $"R{roomId}" : "R-";
                    string obj = hasDoor ? (doorOpen ? "D:OPEN" : "D:CLOSED")
                        : hasChest ? "CHEST" : hasStairs ? stairsLabel : string.Empty;
                    GUI.Label(rect, $"{x},{y} {room}\n{state} {obj} {direction}", gridLabelStyle);
                }
            }
        }

        GUI.Box(new Rect(12f, 12f, 460f, 108f),
            "F3: Grid 끄기 / F4: 빠른 검증 배치 / F7: 5층 보스 직행\n초록=Floor / 빨강=Wall / 노랑=Player / 보라=Actor\n" +
            "주황=D:CLOSED / 하늘=D:OPEN / 갈색=CHEST / 파랑=STAIRS·EXIT\n" +
            "R번호=Room ID / 주황 테두리=Player 인접 검색 칸\nF4: 왼쪽 상자 / 위 Enemy / 오른쪽 탈출구");
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
