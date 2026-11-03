using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

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

    [Header("Placeholder Tiles (RuleTile 붙이기 전까지 임시 — Assets/Generated/PlaceholderTiles/ 참고)")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private readonly List<FloorLayout> floors = new List<FloorLayout>();
    private readonly List<Tilemap> floorTileTilemaps = new List<Tilemap>();
    private readonly List<Tilemap> floorWallTilemaps = new List<Tilemap>();
    private GameObject player;
    private bool resultRequested;

    private void Awake()
    {
        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("[DungeonRunManager] floorTile/wallTile이 인스펙터에 안 물려있어서 생성을 중단합니다.");
            return;
        }

        if (!ExpeditionSession.IsActive) ExpeditionSession.BeginNewRun();
        Tick.ResetForNewRun();
        Tick.OnTick -= HandleTickAdvanced;
        Tick.OnTick += HandleTickAdvanced;
        GenerateAllFloors();
        SpawnPlayerAtFloor(ExpeditionSession.CurrentFloor - 1);
        CreateFloorExits();
    }

    private void OnDestroy()
    {
        Tick.OnTick -= HandleTickAdvanced;
    }

    private void GenerateAllFloors()
    {
        float spacing = gridWidth * chunkSize + floorSpacingMargin;
        var rng = new System.Random(); // 시드 고정 안 함 — 런마다 다르게

        int m1FloorCount = Mathf.Min(floorCount, ExpeditionSession.FinalFloor);
        for (int i = 0; i < m1FloorCount; i++)
        {
            int seed = rng.Next();
            var layout = FloorLayoutGenerator.Generate(gridWidth, gridHeight, minRooms, maxRooms, seed);

            var floorRoot = new GameObject($"Floor_{i + 1}", typeof(Grid));
            floorRoot.transform.SetParent(transform, false);
            floorRoot.transform.position = new Vector3(i * spacing, 0f, 0f);

            var floorTilemap = CreateTilemapChild(floorRoot.transform, "Floor", 0);
            var wallTilemap = CreateTilemapChild(floorRoot.transform, "Walls", 1);

            var painter = new FloorTilemapPainter(floorTilemap, wallTilemap, floorTile, wallTile) { ChunkSize = chunkSize };
            painter.Paint(layout);

            floors.Add(layout);
            floorTileTilemaps.Add(floorTilemap);
            floorWallTilemaps.Add(wallTilemap);
        }

        Debug.Log($"[DungeonRunManager] M1용 {m1FloorCount}개 층 생성 완료.");
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
        Vector3 worldPos = floorTilemap.CellToWorld(startTilePos);

        if (playerPrefab == null)
        {
            Debug.LogError("[DungeonRunManager] playerPrefab이 인스펙터에 안 물려있어서 플레이어를 못 만듭니다.");
            return;
        }

        player = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        player.name = "Player";

        var mover = player.GetComponent<GridMover>();
        if (mover == null) mover = player.AddComponent<GridMover>();
        mover.SetWallTilemap(wallTilemap);
        mover.SnapTo(worldPos);

        if (targetCamera == null)
        {
            Debug.LogWarning("[DungeonRunManager] targetCamera가 인스펙터에 안 물려있어서 카메라 추적은 건너뜁니다.");
            return;
        }

        var follow = targetCamera.GetComponent<CameraFollow>();
        if (follow == null) follow = targetCamera.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;

        float safeZ = targetCamera.transform.position.z <= -1f ? targetCamera.transform.position.z : -10f;
        targetCamera.transform.position = new Vector3(worldPos.x, worldPos.y, safeZ);
    }

    private void CreateFloorExits()
    {
        for (int i = 0; i < floors.Count; i++)
        {
            var layout = floors[i];
            var tilemap = floorTileTilemaps[i];
            var exitCell = new Vector3Int(
                layout.BossCell.x * chunkSize + chunkSize / 2,
                layout.BossCell.y * chunkSize + chunkSize / 2,
                0);
            var exitObject = new GameObject(i == ExpeditionSession.FinalFloor - 1
                ? "EscapeExit(AutoCreated)"
                : $"DownStairs_Floor{i + 1}(AutoCreated)");
            exitObject.transform.SetParent(tilemap.transform.parent, false);
            exitObject.transform.position = tilemap.GetCellCenterWorld(exitCell);
            exitObject.AddComponent<DungeonFloorExit>().Initialize(this, player.transform, i + 1);
        }
    }

    public void UseFloorExit(int floorNumber)
    {
        if (resultRequested || !ExpeditionSession.IsActive || floorNumber != ExpeditionSession.CurrentFloor) return;

        int cost = floorNumber >= ExpeditionSession.FinalFloor ? 30 : Mathf.RoundToInt(TickCost.Interact);
        Tick.Advance(cost);
        if (!ExpeditionSession.IsActive) return; // 비용 적용으로 MIA

        if (floorNumber >= ExpeditionSession.FinalFloor)
        {
            resultRequested = true;
            ExpeditionSession.Complete(ExpeditionResult.Escaped);
            SceneManager.LoadScene("BaseCamp");
            return;
        }

        if (!ExpeditionSession.TryAdvanceFloor()) return;
        MovePlayerToFloor(ExpeditionSession.CurrentFloor - 1);
    }

    private void MovePlayerToFloor(int floorIndex)
    {
        var layout = floors[floorIndex];
        var floorTilemap = floorTileTilemaps[floorIndex];
        var startCell = new Vector3Int(
            layout.StartCell.x * chunkSize + chunkSize / 2,
            layout.StartCell.y * chunkSize + chunkSize / 2,
            0);
        Vector3 worldPosition = floorTilemap.CellToWorld(startCell);

        var mover = player.GetComponent<GridMover>();
        mover.SetWallTilemap(floorWallTilemaps[floorIndex]);
        mover.SnapTo(worldPosition);

        if (targetCamera != null)
        {
            float safeZ = targetCamera.transform.position.z <= -1f ? targetCamera.transform.position.z : -10f;
            targetCamera.transform.position = new Vector3(worldPosition.x, worldPosition.y, safeZ);
        }
        Debug.Log($"[DungeonRunManager] {ExpeditionSession.CurrentFloor}층으로 하강 — 누적 {Tick.Current} Tick.");
    }

    private void HandleTickAdvanced(int currentTick)
    {
        ExpeditionSession.RecordElapsedTicks(currentTick);
        if (resultRequested || currentTick < DungeonClock.MIAThresholdTicks) return;

        resultRequested = true;
        ExpeditionSession.Complete(ExpeditionResult.MIA);
        StartCoroutine(LoadBaseCampNextFrame());
    }

    private static IEnumerator LoadBaseCampNextFrame()
    {
        yield return null;
        SceneManager.LoadScene("BaseCamp");
    }

    private Tilemap CreateTilemapChild(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
        return go.GetComponent<Tilemap>();
    }
}
