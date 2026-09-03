using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 6) FloorLayout(좌표/연결 데이터)을 실제 Tilemap에 칠한다.
// RuleTile 없이 바닥/벽 각각 한 종류짜리 단순 Tile만 써서 "방 모양 · 문 위치 · 바깥 벽"
// 구조부터 검증하는 단계 — 나중에 RuleTile로 코너/엣지 자동 선택을 붙일 때도 이 클래스의
// Paint() 인터페이스는 그대로 두고 내부 타일 선택 로직만 바꾸면 되게 분리해뒀다.
//
// 방식: 방마다 ChunkSize x ChunkSize 칸을 바닥으로 채우고, 그 테두리 1칸을 벽으로 두른다.
// 서로 연결된(=인접한) 두 방 사이의 겹치는 벽 구간만 가운데를 뚫어 문을 낸다. 연결 안 된
// 바깥 경계는 아무도 뚫지 않으니 자동으로 막힌 벽이 된다(맵 가장자리를 벽으로 감추는 설계와 일치).
public class FloorTilemapPainter
{
    public int ChunkSize = 16;
    public int DoorWidth = 3;

    private readonly Tilemap floorTilemap;
    private readonly Tilemap wallTilemap;
    private readonly TileBase floorTile;
    private readonly TileBase wallTile;

    public FloorTilemapPainter(Tilemap floorTilemap, Tilemap wallTilemap, TileBase floorTile, TileBase wallTile)
    {
        this.floorTilemap = floorTilemap;
        this.wallTilemap = wallTilemap;
        this.floorTile = floorTile;
        this.wallTile = wallTile;
    }

    public void Paint(FloorLayout layout)
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        foreach (var cell in layout.RoomCells)
        {
            FillRoomFloor(cell);
            DrawRoomWallRing(cell);
        }

        var opened = new HashSet<(Vector2Int, Vector2Int)>();
        foreach (var kvp in layout.Connections)
        {
            foreach (var neighbor in kvp.Value)
            {
                var pair = OrderedPair(kvp.Key, neighbor);
                if (!opened.Add(pair)) continue;
                OpenDoorway(pair.Item1, pair.Item2);
            }
        }

        OpenMergedRoomWalls(layout);
    }

    // [2026-09-03 버그 수정] FloorLayoutGenerator.MergeRooms()가 "합쳐져서 큰 방 하나로 취급된다"고
    // 표시해둔 방 쌍(MergeGroupId)을 이 클래스가 지금까지 전혀 안 읽고 있었다 — 그래서 병합된
    // 방도 그냥 일반 연결처럼 폭 3짜리 문 하나만 뚫려있고, 실제로는 "합쳐진 큰 방"처럼 안 보이는
    // 버그가 있었다. 여기서 그룹별로 경계 전체(OpenDoorway처럼 가운데만이 아니라 ChunkSize
    // 전체)를 터서 진짜 하나의 방처럼 만든다.
    private void OpenMergedRoomWalls(FloorLayout layout)
    {
        var groups = new Dictionary<int, List<Vector2Int>>();
        foreach (var kvp in layout.MergeGroupId)
        {
            if (!groups.TryGetValue(kvp.Value, out var list))
            {
                list = new List<Vector2Int>();
                groups[kvp.Value] = list;
            }
            list.Add(kvp.Key);
        }

        foreach (var pairCells in groups.Values)
        {
            // MergeRooms()는 항상 인접한 두 방만 짝짓는 설계라(2칸짜리 그룹만 존재) 그 외
            // 크기는 나오지 않아야 정상이지만, 혹시 모를 데이터 불일치에 방어적으로 대응한다.
            if (pairCells.Count != 2) continue;
            OpenFullBorder(pairCells[0], pairCells[1]);
        }
    }

    // 두 방 사이의 경계를 문 폭(DoorWidth)만이 아니라 전체를 터서 벽을 완전히 없앤다 —
    // 병합된 방 쌍 전용(일반 연결은 OpenDoorway를 그대로 씀).
    //
    // [2026-09-03 버그 수정] y(또는 x) 범위를 0..ChunkSize-1 전체로 돌리면, 양 끝(0과
    // ChunkSize-1)은 이 두 방의 "북/남"(또는 "동/서") 벽 링과 동시에 겹치는 모서리 칸이라서,
    // 여기를 지우면 그 모서리에서 직각으로 이어지는 다른 쪽 벽에도 의도치 않은 구멍이 뚫린다
    // (실제로 Test.unity에 저장된 타일 데이터를 직접 읽어서 확인함 — 병합된 두 방의 대각
    // 모서리 4칸이 전부 뚫려있었음. 그 칸을 통해 문/병합 범위 바깥에서 벽을 그냥 넘어가는
    // 버그로 나타났다). OpenDoorway가 애초에 mid±half로 모서리 근처를 절대 안 건드리는 것과
    // 같은 이유로, 여기서도 양 끝 한 칸씩(0, ChunkSize-1)은 남겨서 직각 벽의 모서리를
    // 건드리지 않는다 — 병합된 방 경계에는 아주 얇은(1칸) 기둥이 양 끝에만 남지만, 실질적으로
    // "하나의 큰 방"으로 보이고 걷는 데는 전혀 지장 없다.
    private void OpenFullBorder(Vector2Int a, Vector2Int b)
    {
        var pair = OrderedPair(a, b);
        a = pair.Item1;
        b = pair.Item2;

        if (b.x == a.x + 1 && b.y == a.y) // b가 a의 동쪽
        {
            int wallColA = a.x * ChunkSize + ChunkSize - 1;
            int wallColB = b.x * ChunkSize;
            int baseY = a.y * ChunkSize;
            for (int y = 1; y < ChunkSize - 1; y++)
            {
                ClearWall(wallColA, baseY + y);
                ClearWall(wallColB, baseY + y);
            }
        }
        else if (b.y == a.y + 1 && b.x == a.x) // b가 a의 북쪽
        {
            int wallRowA = a.y * ChunkSize + ChunkSize - 1;
            int wallRowB = b.y * ChunkSize;
            int baseX = a.x * ChunkSize;
            for (int x = 1; x < ChunkSize - 1; x++)
            {
                ClearWall(baseX + x, wallRowA);
                ClearWall(baseX + x, wallRowB);
            }
        }
    }

    private void FillRoomFloor(Vector2Int cell)
    {
        int baseX = cell.x * ChunkSize;
        int baseY = cell.y * ChunkSize;
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                floorTilemap.SetTile(new Vector3Int(baseX + x, baseY + y, 0), floorTile);
            }
        }
    }

    private void DrawRoomWallRing(Vector2Int cell)
    {
        int baseX = cell.x * ChunkSize;
        int baseY = cell.y * ChunkSize;
        for (int x = 0; x < ChunkSize; x++)
        {
            SetWall(baseX + x, baseY);
            SetWall(baseX + x, baseY + ChunkSize - 1);
        }
        for (int y = 0; y < ChunkSize; y++)
        {
            SetWall(baseX, baseY + y);
            SetWall(baseX + ChunkSize - 1, baseY + y);
        }
    }

    private void SetWall(int x, int y) => wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
    private void ClearWall(int x, int y) => wallTilemap.SetTile(new Vector3Int(x, y, 0), null);

    // a가 항상 b보다 왼쪽(x 작음) 혹은 같은 x에서 아래(y 작음)가 되도록 정렬 — 어느 방향에서
    // 순회하든 같은 (a,b) 쌍으로 정규화해서 문을 두 번 뚫는 걸 방지한다.
    private (Vector2Int, Vector2Int) OrderedPair(Vector2Int a, Vector2Int b)
    {
        bool aFirst = a.x < b.x || (a.x == b.x && a.y < b.y);
        return aFirst ? (a, b) : (b, a);
    }

    private void OpenDoorway(Vector2Int a, Vector2Int b)
    {
        int mid = ChunkSize / 2;
        int half = DoorWidth / 2;

        if (b.x == a.x + 1 && b.y == a.y) // b가 a의 동쪽
        {
            int wallColA = a.x * ChunkSize + ChunkSize - 1;
            int wallColB = b.x * ChunkSize;
            int baseY = a.y * ChunkSize;
            for (int i = -half; i <= half; i++)
            {
                int y = baseY + mid + i;
                ClearWall(wallColA, y);
                ClearWall(wallColB, y);
            }
        }
        else if (b.y == a.y + 1 && b.x == a.x) // b가 a의 북쪽
        {
            int wallRowA = a.y * ChunkSize + ChunkSize - 1;
            int wallRowB = b.y * ChunkSize;
            int baseX = a.x * ChunkSize;
            for (int i = -half; i <= half; i++)
            {
                int x = baseX + mid + i;
                ClearWall(x, wallRowA);
                ClearWall(x, wallRowB);
            }
        }
    }
}
