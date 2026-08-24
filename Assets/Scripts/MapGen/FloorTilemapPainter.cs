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
