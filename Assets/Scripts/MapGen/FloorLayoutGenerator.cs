using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Dungeon Tools 5) 청크 그리드 위에 층 구조를 생성한다.
//
// 2026-08-24 설계 이력:
// v1) 순수 프론티어 랜덤 워크 — 밀도(방 개수/그리드 용량)에 따라 결과 편차가 컸음.
// v2) Loop/FigureEight "팔 뻗기" 방식 시도 — 실제로 씬에 그려보니 항상 거의 일직선으로
//     되돌아오기만 해서 "통로가 하나뿐"인 좁고 긴 구조가 나오는 버그 발견 (분기가 없었음).
// v3(현재) — v1의 프론티어 랜덤 워크로 되돌림(자연스러운 T자/십자 분기가 생김) +
//     완성된 구조에 "가까운데 안 이어진 두 방 사이"를 몇 개 골라 다리를 놓아서 순환 경로를
//     최소 1~2개 보장한다 — 녹픽던(맵 시스템만 참고, 게임 시스템은 참고 안 함)의 "항상
//     루프가 있어야 한다"는 원칙을 이렇게 구현.
//
// 이 클래스는 좌표/연결/병합 정보만 만들고, 실제 타일맵에 그리는 건 FloorTilemapPainter가
// 담당한다 — 여기서 Tilemap/RuleTile을 직접 건드리지 않는다.
public static class FloorLayoutGenerator
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
    };

    // 방 하나가 인접한 다른 방과 병합될 확률 (합쳐지면 둘 다 큰 방 하나로 취급됨).
    private const double MergeChance = 0.15;

    public static FloorLayout Generate(int gridWidth, int gridHeight, int minRooms, int maxRooms, int seed)
    {
        var rng = new System.Random(seed);
        int targetRooms = Mathf.Clamp(rng.Next(minRooms, maxRooms + 1), 3, gridWidth * gridHeight);
        var start = new Vector2Int(gridWidth / 2, gridHeight / 2);

        var occupied = GrowBranchingWalk(start, targetRooms, gridWidth, gridHeight, rng);

        var layout = new FloorLayout { GridWidth = gridWidth, GridHeight = gridHeight, RoomCells = occupied, StartCell = start };
        BuildConnections(layout);

        int desiredLoops = rng.Next(1, 3); // 1~2개 추가 순환 경로를 시도(후보 없으면 트리로 남음)
        int loopsAdded = AddExtraLoops(layout, rng, desiredLoops);
        layout.Shape = loopsAdded > 0 ? FloorShape.Loop : FloorShape.Tree;

        layout.BossCell = FindFarthestCell(layout, layout.StartCell);
        layout.KeyCells = PickKeyCells(layout, rng, maxCount: 2);
        MergeRooms(layout, rng);

        return layout;
    }

    // 이미 놓인 방들(프론티어) 중 아무거나 랜덤하게 골라서 그 옆 빈 칸에 새 방을 뻗는다.
    // "가장 최근에 놓은 칸"만 계속 고르는 게 아니라 프론티어 전체에서 고르기 때문에,
    // 인기 있는 칸에는 방이 여러 개 붙어서 자연스럽게 T자/십자 분기가 생긴다.
    private static HashSet<Vector2Int> GrowBranchingWalk(Vector2Int start, int targetRooms, int gw, int gh, System.Random rng)
    {
        var occupied = new HashSet<Vector2Int> { start };
        var frontier = new List<Vector2Int> { start };

        int safety = targetRooms * 50; // 극단적으로 좁은 그리드에서 무한루프 방지용 상한
        while (occupied.Count < targetRooms && frontier.Count > 0 && safety-- > 0)
        {
            var from = frontier[rng.Next(frontier.Count)];
            var dirs = Directions.OrderBy(_ => rng.Next()).ToArray();

            bool placed = false;
            foreach (var dir in dirs)
            {
                var next = from + dir;
                if (next.x < 0 || next.x >= gw || next.y < 0 || next.y >= gh) continue;
                if (occupied.Contains(next)) continue;

                occupied.Add(next);
                frontier.Add(next);
                placed = true;
                break;
            }

            if (!placed)
            {
                frontier.Remove(from); // 더 뻗어나갈 데가 없는 방 — 프론티어에서 제외
            }
        }

        return occupied;
    }

    // 경로 순서와 무관하게, 실제로 직교 인접한 모든 방 쌍을 연결로 잡는다.
    private static void BuildConnections(FloorLayout layout)
    {
        layout.Connections.Clear();
        foreach (var cell in layout.RoomCells)
        {
            var neighbors = new List<Vector2Int>();
            foreach (var dir in Directions)
            {
                var n = cell + dir;
                if (layout.RoomCells.Contains(n)) neighbors.Add(n);
            }
            layout.Connections[cell] = neighbors;
        }
    }

    // 서로 안 이어진 방 두 개가 사이에 빈 칸 하나만 두고 가까우면(맨해튼 거리 2), 그 사이를
    // 채워서 지름길을 놓는다 — 트리 구조에 최소 1~2개의 진짜 순환 경로를 보장하기 위함.
    private static int AddExtraLoops(FloorLayout layout, System.Random rng, int desiredLoops)
    {
        var cells = layout.RoomCells.ToList();
        var candidates = new HashSet<Vector2Int>();

        for (int i = 0; i < cells.Count; i++)
        {
            for (int j = i + 1; j < cells.Count; j++)
            {
                var a = cells[i];
                var b = cells[j];
                int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                if (dist != 2) continue;

                if (a.x == b.x)
                {
                    candidates.Add(new Vector2Int(a.x, (a.y + b.y) / 2));
                }
                else if (a.y == b.y)
                {
                    candidates.Add(new Vector2Int((a.x + b.x) / 2, a.y));
                }
                else
                {
                    candidates.Add(new Vector2Int(a.x, b.y));
                    candidates.Add(new Vector2Int(b.x, a.y));
                }
            }
        }

        var shuffled = candidates.Where(c => !layout.RoomCells.Contains(c)).OrderBy(_ => rng.Next()).ToList();
        int added = 0;
        foreach (var bridge in shuffled)
        {
            if (added >= desiredLoops) break;
            if (layout.RoomCells.Contains(bridge)) continue; // 다른 다리가 먼저 채웠을 수 있음
            layout.RoomCells.Add(bridge);
            added++;
        }

        if (added > 0) BuildConnections(layout);
        return added;
    }

    // BFS로 시작 방에서 가장 멀리 떨어진 방을 찾는다 — 보스/계단 방 후보.
    private static Vector2Int FindFarthestCell(FloorLayout layout, Vector2Int from)
    {
        var visited = new HashSet<Vector2Int> { from };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        Vector2Int farthest = from;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            farthest = current;
            foreach (var neighbor in layout.Connections[current])
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return farthest;
    }

    // 막다른 방(연결이 1개뿐) 중에서 시작/보스 방을 제외하고 랜덤하게 골라 열쇠방 후보로 삼는다.
    private static List<Vector2Int> PickKeyCells(FloorLayout layout, System.Random rng, int maxCount)
    {
        var deadEnds = layout.RoomCells
            .Where(c => c != layout.StartCell && c != layout.BossCell && layout.Connections[c].Count == 1)
            .OrderBy(_ => rng.Next())
            .ToList();

        return deadEnds.Take(maxCount).ToList();
    }

    // 녹픽던의 "표준 방은 인접한 방 1개와 병합 가능" 규칙을 단순화해서 적용.
    // 시작/보스/열쇠방은 역할이 명확해야 하니 병합 대상에서 제외한다.
    private static void MergeRooms(FloorLayout layout, System.Random rng)
    {
        var reserved = new HashSet<Vector2Int> { layout.StartCell, layout.BossCell };
        foreach (var key in layout.KeyCells) reserved.Add(key);

        int nextGroupId = 0;
        var cells = layout.RoomCells.OrderBy(_ => rng.Next()).ToList();

        foreach (var cell in cells)
        {
            if (reserved.Contains(cell)) continue;
            if (layout.MergeGroupId.ContainsKey(cell)) continue;
            if (rng.NextDouble() >= MergeChance) continue;

            var partner = layout.Connections[cell]
                .Where(n => !reserved.Contains(n) && !layout.MergeGroupId.ContainsKey(n))
                .OrderBy(_ => rng.Next())
                .Cast<Vector2Int?>()
                .FirstOrDefault();

            if (partner == null) continue;

            int groupId = nextGroupId++;
            layout.MergeGroupId[cell] = groupId;
            layout.MergeGroupId[partner.Value] = groupId;
        }
    }
}
