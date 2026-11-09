using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// 실제 InDungeon 씬이 GUID로 직접 참조하는 런타임 진입점이다.
// Dungeon Tools 7) DungeonScene(InDungeon)이 로드되면 Awake에서 자동으로 10개 층을 전부
// 생성해서 배치하고, 1층 시작 방에 플레이어를 세운 뒤 카메라를 붙인다. BaseCampScene에서
// SceneManager.LoadScene("InDungeon")만 호출하면 되고, 별도 신호/이벤트 연결은 필요 없다.
//
// 층마다 독립된 Grid/Tilemap 루트를 만들고, 그 루트의 월드 위치만 옆으로(X축) 옮겨서 서로
// 안 겹치게 배치한다 — "10개 층을 같은 씬에 동시 생성, 카메라만 이동" 설계 그대로.
// FloorLayoutGenerator/FloorTilemapPainter는 손 안 대고 그대로 재사용한다.
public class DungeonRunManager : MonoBehaviour
{
    [Header("Floor Generation")]
    [SerializeField] private int floorCount = 10;
    [SerializeField] private int gridWidth = 16;
    [SerializeField] private int gridHeight = 16;
    [SerializeField] private int minRooms = 12;
    [SerializeField] private int maxRooms = 16;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private float floorSpacingMargin = 32f;
    [SerializeField] private int runSeed = 20261106;

    [Header("M1 Temporary Balance")]
    [SerializeField, Min(1)] private int escapeProcedureTickCost = 10;

    [Header("Test Scene Visual Settings")]
    [SerializeField] private float playerCellOffsetX = 0.5f;
    [SerializeField] private float playerCellOffsetY = 0f;
    [SerializeField, Min(0.01f)] private float playerMoveDuration = 0.1f;
    [SerializeField, Min(0.1f)] private float cameraOrthographicSize = 11.25f;
    [SerializeField] private float cameraZ = -10f;

    [Header("Placeholder Tiles (RuleTile 붙이기 전까지 임시 — Assets/Generated/PlaceholderTiles/ 참고)")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Enemy (M1 Runtime)")]
    [SerializeField, Min(1)] private int enemiesPerFloor = 3;
    [SerializeField, Min(0.01f)] private float enemyMoveDuration = 0.1f;
    [SerializeField, Min(0f)] private float enemyHopHeight = 0.4f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private readonly List<FloorLayout> floors = new List<FloorLayout>();
    private readonly List<Tilemap> floorTileTilemaps = new List<Tilemap>();
    private readonly List<Tilemap> floorWallTilemaps = new List<Tilemap>();
    private readonly List<int> floorSeeds = new List<int>();
    private GameObject player;

    public int EscapeProcedureTickCost => escapeProcedureTickCost;
    public int RunSeed => runSeed;

    private void Awake()
    {
        runSeed = M3MeasurementAutomation.ResolveRunSeed();
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("[DungeonRunManager] floorTile/wallTile이 인스펙터에 안 물려있어서 생성을 중단합니다.");
            return;
        }

        if (!ExpeditionSession.IsActive) ExpeditionSession.BeginNewRun();
        M3MeasurementAutomation.BeginOrResume(runSeed);
        GenerateAllFloors();
        int activeFloorIndex = ExpeditionSession.CurrentFloor - 1;
        SpawnPlayerAtFloor(activeFloorIndex);
        SpawnEnemiesAtFloor(activeFloorIndex);
        SpawnChestsAtFloor(activeFloorIndex);
        CreateFogForFloor(activeFloorIndex);
        if (GetComponent<DungeonSceneBootstrapper>() == null) gameObject.AddComponent<DungeonSceneBootstrapper>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateGeneratedRun();
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("QA/Validate Generated Run")]
    public bool ValidateGeneratedRun()
    {
        var errors = new List<string>();
        for (int i = 0; i < floors.Count; i++)
        {
            FloorLayout actual = floors[i];
            FloorLayout replay = FloorLayoutGenerator.Generate(gridWidth, gridHeight, minRooms, maxRooms, floorSeeds[i]);
            string actualSignature = BuildLayoutSignature(actual);
            string replaySignature = BuildLayoutSignature(replay);
            if (actualSignature != replaySignature) errors.Add($"F{i + 1}: 동일 Seed 재생성 결과 불일치");

            var reached = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(actual.StartCell);
            reached.Add(actual.StartCell);
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (!actual.Connections.TryGetValue(current, out var neighbors)) continue;
                foreach (var next in neighbors)
                {
                    if (reached.Add(next)) queue.Enqueue(next);
                }
            }
            if (reached.Count != actual.RoomCells.Count)
                errors.Add($"F{i + 1}: 연결된 방 {reached.Count}/{actual.RoomCells.Count}");
        }

        var playerActor = PlayerTurnActor.Instance;
        if (playerActor != null)
        {
            Tilemap activeFloor = playerActor.FloorTilemap;
            Tilemap activeWalls = playerActor.WallsTilemap;
            var occupied = new HashSet<Vector3Int> { playerActor.CurrentCell };
            var enemyRooms = new HashSet<int>();
            int playerRoom = DungeonRoomRegistry.GetRoomId(activeFloor, playerActor.CurrentCell);
            foreach (var enemy in FindObjectsOfType<EnemyTurnActor>())
            {
                if (enemy.FloorTilemap != activeFloor) continue;
                if (!occupied.Add(enemy.CurrentCell)) errors.Add($"현재 층 Actor 겹침: {enemy.CurrentCell}");
                if (!activeFloor.HasTile(enemy.CurrentCell) || activeWalls.HasTile(enemy.CurrentCell))
                    errors.Add($"Enemy 비정상 시작 칸: {enemy.CurrentCell}");
                int enemyRoom = DungeonRoomRegistry.GetRoomId(activeFloor, enemy.CurrentCell);
                if (enemyRoom == playerRoom) errors.Add($"시작 방 Enemy 배치: Room {enemyRoom}");
                if (enemyRoom >= 0 && !enemyRooms.Add(enemyRoom)) errors.Add($"같은 방 Enemy 중복 배치: Room {enemyRoom}");
            }
            foreach (var chest in FindObjectsOfType<DungeonChest>())
            {
                if (chest.FloorTilemap != activeFloor) continue;
                if (DungeonDoor.IsDoorCell(activeFloor, chest.Cell)) errors.Add($"문 칸 상자: {chest.Cell}");
                if (!occupied.Add(chest.Cell)) errors.Add($"상자·Actor 겹침: {chest.Cell}");
            }
            foreach (var stairs in FindObjectsOfType<DungeonStairs>())
            {
                if (stairs.FloorTilemap != activeFloor) continue;
                if (!occupied.Add(stairs.Cell)) errors.Add($"계단·출구 겹침: {stairs.Cell}");
                var path = TilePathfinder.FindPath(playerActor.CurrentCell, stairs.Cell, activeWalls, activeFloor);
                if (playerActor.CurrentCell != stairs.Cell && path.Count == 0)
                    errors.Add($"계단·출구 도달 불가: {stairs.Cell}");
            }
        }

        if (errors.Count == 0)
        {
            Debug.Log($"[M3Validate] PASS — Seed {runSeed}, {floors.Count}개 층 동일 생성·전체 방 연결 확인");
            return true;
        }
        foreach (string error in errors) Debug.LogError("[M3Validate] " + error);
        return false;
    }

    private static string BuildLayoutSignature(FloorLayout layout)
    {
        var cells = new List<Vector2Int>(layout.RoomCells);
        cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        var builder = new System.Text.StringBuilder();
        foreach (var cell in cells)
        {
            builder.Append(cell.x).Append(':').Append(cell.y).Append(':').Append(layout.GetRoomId(cell)).Append('|');
            if (!layout.Connections.TryGetValue(cell, out var neighbors)) continue;
            var sorted = new List<Vector2Int>(neighbors);
            sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            foreach (var neighbor in sorted) builder.Append(neighbor.x).Append(':').Append(neighbor.y).Append(',');
            builder.Append(';');
        }
        return builder.ToString();
    }
#endif

    private void GenerateAllFloors()
    {
        float spacing = gridWidth * chunkSize + floorSpacingMargin;
        var rng = new System.Random(runSeed);

        int m1FloorCount = Mathf.Min(floorCount, ExpeditionSession.FinalFloor);
        for (int i = 0; i < m1FloorCount; i++)
        {
            int seed = rng.Next();
            floorSeeds.Add(seed);
            var layout = FloorLayoutGenerator.Generate(gridWidth, gridHeight, minRooms, maxRooms, seed);

            var floorRoot = new GameObject($"Floor_{i + 1}", typeof(Grid));
            floorRoot.transform.SetParent(transform, false);
            floorRoot.transform.position = new Vector3(i * spacing, 0f, 0f);

            var floorTilemap = CreateTilemapChild(floorRoot.transform, "Floor", 0);
            var wallTilemap = CreateTilemapChild(floorRoot.transform, "Walls", 1);

            var painter = new FloorTilemapPainter(floorTilemap, wallTilemap, floorTile, wallTile) { ChunkSize = chunkSize };
            painter.Paint(layout);
            DungeonRoomRegistry.Register(floorTilemap, layout, chunkSize);
            SpawnDoors(layout, floorTilemap, wallTilemap, floorRoot.transform);

            floors.Add(layout);
            floorTileTilemaps.Add(floorTilemap);
            floorWallTilemaps.Add(wallTilemap);
        }

        Debug.Log($"[DungeonRunManager] Seed {runSeed} ({M3MeasurementAutomation.CurrentSeedMode}), {m1FloorCount}개 층 생성 완료.");
    }

    private void SpawnDoors(FloorLayout layout, Tilemap floorTilemap, Tilemap wallTilemap, Transform parent)
    {
        var opened = new HashSet<(Vector2Int, Vector2Int)>();
        int index = 0;
        foreach (var pair in layout.Connections)
        {
            foreach (var neighbor in pair.Value)
            {
                var a = pair.Key;
                var b = neighbor;
                if (a.x > b.x || (a.x == b.x && a.y > b.y)) (a, b) = (b, a);
                if (!opened.Add((a, b))) continue;
                if (layout.GetRoomId(a) == layout.GetRoomId(b)) continue;

                int mid = chunkSize / 2;
                var doorCells = new List<Vector3Int>();
                bool horizontal;
                if (b.x == a.x + 1)
                {
                    horizontal = false;
                    int bx = b.x * chunkSize;
                    int baseY = a.y * chunkSize;
                    for (int i = -1; i <= 1; i++)
                    {
                        doorCells.Add(new Vector3Int(bx, baseY + mid + i, 0));
                    }
                }
                else
                {
                    horizontal = true;
                    int by = b.y * chunkSize;
                    int baseX = a.x * chunkSize;
                    for (int i = -1; i <= 1; i++)
                    {
                        doorCells.Add(new Vector3Int(baseX + mid + i, by, 0));
                    }
                }
                var door = new GameObject($"Door_{++index}");
                door.transform.SetParent(parent, false);
                door.AddComponent<DungeonDoor>().Initialize(floorTilemap, wallTilemap, doorCells, horizontal);
            }
        }
    }

    private void SpawnPlayerAtFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floors.Count) return;

        var layout = floors[floorIndex];
        var floorTilemap = floorTileTilemaps[floorIndex];
        var wallTilemap = floorWallTilemaps[floorIndex];

        var startTilePos = new Vector3Int(
            layout.StartCell.x * chunkSize + chunkSize / 2,
            layout.StartCell.y * chunkSize + chunkSize / 2,
            0);
        Vector3 worldPos = floorTilemap.CellToWorld(startTilePos) +
                           new Vector3(playerCellOffsetX, playerCellOffsetY, 0f);

        if (playerPrefab == null)
        {
            Debug.LogError("[DungeonRunManager] playerPrefab이 인스펙터에 안 물려있어서 플레이어를 못 만듭니다.");
            return;
        }

        player = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        player.name = "Player";
        player.tag = "Player";

        var playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (playerRenderer != null) playerRenderer.sortingOrder = 2;

        var collider = player.GetComponent<BoxCollider2D>();
        if (collider == null) collider = player.AddComponent<BoxCollider2D>();
        // AddComponent 기본값은 Sprite 전체 bounds(현재 knight_m은 1 x 1.3125)를 따라가서
        // 한 타일보다 큰 Collider가 위/아래 셀을 침범한다. 현재 플레이어 transform 기준 발밑은
        // 아래쪽이므로 Collider 중심을 반 칸 내리고, 한 셀 내부에 여유 있게 들어오도록 고정한다.
        Vector3 cellSize = floorTilemap.cellSize;
        collider.size = new Vector2(cellSize.x * 0.8f, cellSize.y * 0.8f);
        collider.offset = new Vector2(0f, cellSize.y * -0.5f);

        var actor = player.GetComponent<PlayerTurnActor>();
        if (actor == null) actor = player.AddComponent<PlayerTurnActor>();
        actor.ConfigureRuntime(wallTilemap, floorTilemap, playerMoveDuration, 0.4f);

        if (targetCamera == null)
        {
            Debug.LogWarning("[DungeonRunManager] targetCamera가 인스펙터에 안 물려있어서 카메라 추적은 건너뜁니다.");
            return;
        }

        var follow = targetCamera.GetComponent<CameraFollow>();
        if (follow == null) follow = targetCamera.gameObject.AddComponent<CameraFollow>();
        follow.SetCameraZ(cameraZ);
        follow.SetTarget(player.transform);

        var pixelPerfect = targetCamera.GetComponent<PixelPerfectCamera>();
        if (pixelPerfect != null) pixelPerfect.enabled = false;
        targetCamera.orthographicSize = cameraOrthographicSize;
        targetCamera.transform.position = new Vector3(worldPos.x, worldPos.y, cameraZ);
    }

    private void CreateFogForFloor(int floorIndex)
    {
        var floorTilemap = floorTileTilemaps[floorIndex];
        var fogObject = new GameObject("Fog", typeof(Tilemap), typeof(TilemapRenderer));
        fogObject.transform.SetParent(floorTilemap.transform.parent, false);
        var fogTilemap = fogObject.GetComponent<Tilemap>();
        fogObject.GetComponent<TilemapRenderer>().sortingOrder = 10;

        var fog = fogObject.AddComponent<FogOfWarController>();
        fog.ConfigureRuntime(floorTilemap, floorWallTilemaps[floorIndex], fogTilemap);
    }

    private void SpawnEnemiesAtFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floors.Count || player == null) return;

        var floorTilemap = floorTileTilemaps[floorIndex];
        var wallTilemap = floorWallTilemaps[floorIndex];
        var playerActor = player.GetComponent<PlayerTurnActor>();
        Vector3Int playerCell = playerActor != null
            ? playerActor.CurrentCell
            : floorTilemap.WorldToCell(player.transform.position);

        int playerRoomId = DungeonRoomRegistry.GetRoomId(floorTilemap, playerCell);
        var candidatesByRoom = new Dictionary<int, List<Vector3Int>>();
        foreach (var cell in floorTilemap.cellBounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(cell) || wallTilemap.HasTile(cell) || cell == playerCell) continue;
            if (Mathf.Abs(cell.x - playerCell.x) + Mathf.Abs(cell.y - playerCell.y) < 6) continue;
            int roomId = DungeonRoomRegistry.GetRoomId(floorTilemap, cell);
            if (roomId < 0 || roomId == playerRoomId) continue;
            if (!candidatesByRoom.TryGetValue(roomId, out var cells))
            {
                cells = new List<Vector3Int>();
                candidatesByRoom.Add(roomId, cells);
            }
            cells.Add(cell);
        }

        // Enemy 수를 늘려도 특정 먼 방이나 계단 방에 몰리지 않도록 방마다 최대 1마리만 뽑는다.
        // 층 Seed에서 파생한 RNG를 써서 같은 Run Seed의 배치는 항상 동일하다.
        var rng = new System.Random(floorSeeds[floorIndex] ^ 0x4D335F45);
        var roomIds = new List<int>(candidatesByRoom.Keys);
        Shuffle(roomIds, rng);

        var occupied = new List<Vector3Int> { playerCell };
        int spawned = 0;
        foreach (int roomId in roomIds)
        {
            var roomCells = candidatesByRoom[roomId];
            Shuffle(roomCells, rng);
            bool placed = false;
            foreach (var cell in roomCells)
            {
                bool tooClose = occupied.Exists(other => Mathf.Abs(other.x - cell.x) + Mathf.Abs(other.y - cell.y) < 3);
                if (tooClose) continue;

                CreateRuntimeEnemy(floorIndex, spawned, cell, floorTilemap, wallTilemap);
                occupied.Add(cell);
                spawned++;
                placed = true;
                break;
            }
            if (!placed) continue;
            if (spawned >= enemiesPerFloor) break;
        }

        Debug.Log($"[DungeonRunManager] {floorIndex + 1}층 Enemy {spawned}개 배치 및 TickManager 등록 완료.");
    }

    private static void Shuffle<T>(IList<T> values, System.Random rng)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T value = values[i];
            values[i] = values[j];
            values[j] = value;
        }
    }

    private void CreateRuntimeEnemy(int floorIndex, int enemyIndex, Vector3Int cell, Tilemap floorTilemap, Tilemap wallTilemap)
    {
        var enemy = new GameObject($"Enemy_F{floorIndex + 1}_{enemyIndex + 1}");
        enemy.SetActive(false);
        enemy.transform.SetParent(floorTilemap.transform.parent, false);

        var renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = new Color(0.35f, 0.85f, 0.3f, 1f);
        renderer.sortingOrder = 2;
        enemy.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        Vector3 cellOrigin = floorTilemap.CellToWorld(cell);
        enemy.transform.position = cellOrigin + new Vector3(floorTilemap.cellSize.x * 0.5f, 0.375f, 0f);

        var actor = enemy.AddComponent<EnemyTurnActor>();
        actor.ConfigureRuntime(wallTilemap, floorTilemap, EnemyBehaviorType.Wander, enemyMoveDuration, enemyHopHeight);
        enemy.SetActive(true);
    }

    private void SpawnChestsAtFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floors.Count || player == null) return;

        var layout = floors[floorIndex];
        var floorTilemap = floorTileTilemaps[floorIndex];
        var wallTilemap = floorWallTilemaps[floorIndex];
        var playerActor = player.GetComponent<PlayerTurnActor>();
        Vector3Int playerCell = playerActor.CurrentCell;

        Vector3Int reservedExitCell = playerCell;
        int farthest = -1;
        var candidates = new List<Vector3Int>();
        foreach (var cell in floorTilemap.cellBounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(cell) || wallTilemap.HasTile(cell) || cell == playerCell) continue;
            if (DungeonDoor.IsDoorCell(floorTilemap, cell)) continue;
            if (TickManager.Instance != null && TickManager.Instance.IsCellOccupied(cell)) continue;
            int distance = Mathf.Abs(cell.x - playerCell.x) + Mathf.Abs(cell.y - playerCell.y);
            if (distance > farthest)
            {
                farthest = distance;
                reservedExitCell = cell;
            }
            if (distance >= 4) candidates.Add(cell);
        }

        var rng = new System.Random(floorSeeds[floorIndex] ^ 0x4D325F43);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swap = rng.Next(i + 1);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }

        int targetCount = Mathf.Clamp(Mathf.CeilToInt(layout.RoomCount * 0.5f), 6, 8);
        var placed = new List<Vector3Int>();
        foreach (var cell in candidates)
        {
            if (cell == reservedExitCell) continue;
            if (placed.Exists(other => Mathf.Abs(other.x - cell.x) + Mathf.Abs(other.y - cell.y) < 6)) continue;

            int chestIndex = placed.Count;
            var chest = new GameObject($"Chest_F{floorIndex + 1}_{chestIndex + 1}");
            chest.transform.SetParent(floorTilemap.transform.parent, false);
            chest.AddComponent<DungeonChest>().Initialize(floorTilemap, cell, floorIndex + 1,
                floorSeeds[floorIndex] ^ ((chestIndex + 1) * 7919));
            placed.Add(cell);
            if (placed.Count >= targetCount) break;
        }

        Debug.Log($"[DungeonRunManager] {floorIndex + 1}층 상자 {placed.Count}/{targetCount}개 배치 완료.");
    }

    private Tilemap CreateTilemapChild(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
        return go.GetComponent<Tilemap>();
    }
}
