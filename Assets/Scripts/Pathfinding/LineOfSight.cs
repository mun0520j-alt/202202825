using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ================================================================================================
// [Summary] LineOfSight
// 두 칸 사이에 벽이 없이 직선으로 서로 보이는지 판정하는 순수 정적 유틸리티(2026-08-28 신규,
// Enemy Trace 감지 + Fog of War 시야 계산에 공용으로 씀). TilePathfinder처럼 Player/Enemy 어느
// 쪽 로직도 몰라서 양쪽 다 재사용 가능하게 Pathfinding/ 폴더에 배치했다.
//
// 왜 Bresenham 직선 알고리즘인가: "시야"는 최단 경로(BFS)가 아니라 "실제로 눈에 보이는 한 줄의
// 직선"이 필요하다 — 칸을 우회해서 보이는 척하면 안 되니까, start에서 end까지 이어지는 격자
// 상의 정확히 한 줄(대각선 포함)만 훑어서 그 위에 벽이 있는지 확인한다.
// ================================================================================================
public static class LineOfSight
{
    // start와 end 사이(양 끝 칸 제외)에 벽(wallsTilemap 타일)이 하나라도 있으면 false.
    // 양 끝 칸을 검사에서 빼는 이유: start/end는 보통 액터가 서 있는 칸이라 애초에 벽일 수
    // 없고, 포함시키면 "내가 서 있는 칸 자체" 때문에 오판할 여지만 생긴다.
    public static bool HasLineOfSight(Vector3Int start, Vector3Int end, Tilemap wallsTilemap)
    {
        foreach (var cell in GetLineCells(start, end))
        {
            if (cell == start || cell == end) continue;
            if (wallsTilemap.HasTile(cell)) return false;
        }
        return true;
    }

    // Bresenham 직선 알고리즘 — start에서 end까지 격자 위에서 "가장 직선에 가까운" 칸들을
    // 순서대로 나열한다. 대각선 스텝도 허용한다(이동 규칙의 4방향 제한과는 별개 — 시야는
    // 대각선으로도 통한다는 게 일반적인 로그라이크 관례).
    private static IEnumerable<Vector3Int> GetLineCells(Vector3Int start, Vector3Int end)
    {
        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        int x = x0, y = y0;
        while (true)
        {
            yield return new Vector3Int(x, y, 0);
            if (x == x1 && y == y1) yield break;

            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}
