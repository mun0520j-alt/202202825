using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ================================================================================================
// [Summary] TilePathfinder
// 플레이어 마우스 클릭 자동 이동을 위한 경로 탐색 — start에서 goal까지 4방향(대각선 없음)
// BFS로 최단 경로를 찾아서 "지나갈 칸 목록"을 순서대로 반환한다. Player 전용 로직을 전혀
// 모르는 순수 정적 유틸리티라서(Tilemap 두 개만 입력으로 받음), 나중에 Enemy 이동에도 그대로
// 재사용 가능하다(Assets/Scripts/Pathfinding/에 독립 배치한 이유, 2026-08-27 폴더 재구성).
//
// 왜 BFS인가 (A*가 아니라): 대각선 이동을 안 쓰기로 확정했으니(2026-08-27 설계 확정) 칸마다
// 이동 비용이 전부 동일(1칸)하다 — 비용이 균일하면 A*의 "목표까지 대충 얼마나 남았는지"
// 휴리스틱이 주는 이득이 없어서, 더 단순한 BFS만으로도 최단 경로가 그대로 보장된다.
// 나중에 몹이 생겨서 "칸마다 비용이 다른"(늪지대는 2배 느림 등) 상황이 오면 그때 A*/다익스트라로
// 바꾸면 된다.
//
// BFS 동작 원리 요약(자세한 설명은 FindPath 안 주석 참고): start부터 시작해서 "한 칸씩 멀어지는
// 순서대로" 사방을 넓혀가며 탐색하다가(마치 물결이 퍼지듯) goal을 만나면 멈춘다. 이 "물결 순서"
// 덕분에 goal을 처음 발견한 시점이 항상 최단 경로가 된다는 게 BFS의 핵심 성질이다.
// ================================================================================================
public static class TilePathfinder
{
    private static readonly Vector3Int[] Directions =
    {
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0),
    };

    // start 바로 다음 칸부터 goal까지의 경로를 순서대로 반환한다(start 자신은 포함 안 함).
    // 갈 수 없으면(막혀있거나 너무 멀어서 안전 상한에 걸리면) 빈 리스트를 반환한다.
    //
    // 막힌 칸 판정: Walls 타일맵에 타일이 있으면 못 지나감 + Floor 타일맵에 타일이 아예
    // 없으면(방/통로 바깥 허공) 못 지나감 — 이렇게 두 개를 같이 봐야 "방 밖 허공"으로
    // 새는 걸 막을 수 있다(Walls만 보면 애초에 타일이 없는 빈 공간은 안 막힌 걸로 오판함).
    //
    // [2026-09-03 버그 수정] isBlockedExtra: 벽/바닥 타일맵만으로는 "지금 그 칸에 다른 액터
    // (Enemy 등)가 서있는지"를 알 수 없어서, 예전엔 이 함수가 몹이 서있는 칸도 그냥 통과
    // 가능한 바닥으로 보고 경로를 그려버렸다. 그러면 플레이어가 클릭한 목적지 자체는 멀쩡히
    // 뚫려있는데, 실제로 그 경로를 따라 걷다가 몹이 서있는 칸에서 TryStep이 막혀서
    // "경로 중간에서 갑자기 멈추는" 버그가 났다(PlayerTurnActor에 남아있던 디버그 로그가 이
    // 증상을 추적하려던 흔적). 호출하는 쪽(Player/Enemy)이 TickManager.IsCellOccupied 같은
    // 점유 판정을 델리게이트로 넘겨주면, 이 함수가 애초에 그 칸을 우회해서 경로를 짠다 —
    // TilePathfinder는 여전히 TickManager를 몰라도 되니 계층 분리는 그대로 유지된다.
    public static List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, Tilemap wallsTilemap, Tilemap floorTilemap,
        Func<Vector3Int, bool> isBlockedExtra = null)
    {
        // [2026-09-03 전투 준비 중 발견/수정] 목적지(goal) 자체는 isBlockedExtra를 안 본다 —
        // 지형(벽/바닥)만 확인한다. 이유: Enemy가 Player를 쫓아갈 때 goal이 정확히 "Player가
        // 서있는 칸" 자체인데, 위 isBlockedExtra 방식으로 goal까지 점유 여부를 검사해버리면
        // Player가 서있는 칸은 항상 "점유중=못감"으로 막혀서 goal 자체가 영원히 도달 불가 판정이
        // 나고, 그 결과 FindPath가 매번 빈 경로만 반환해서 추적(Trace)이 아예 동작을 안 하게
        // 된다(플레이어 공격 기능을 붙이려다가 발견함). 실제로 "점유된 칸을 밟지 않는다"는
        // 규칙은 중간 경유지(아래 BFS 확장 부분)에만 적용하면 충분하다 — 목적지에 거의 다
        // 도착해서 마지막 한 칸을 실제로 밟을지 말지는 호출하는 쪽(TryStep/TryTraceTowards의
        // IsCellOccupied 체크)이 이미 별도로 방어하고 있다.
        var result = new List<Vector3Int>();
        if (start == goal) return result;
        if (!IsWalkableTerrain(goal, wallsTilemap, floorTilemap)) return result; // 목적지 자체가 막혀있으면 바로 포기

        // previousCellOnPath: "이 칸에 처음 도달했을 때 바로 직전에 있던 칸이 어디였는지" 기록하는
        // 발자국 딕셔너리(key=도착한 칸, value=거기 오기 직전 칸). BFS가 사방으로 퍼져나가는 동안
        // 모든 칸마다 "나는 어디서 왔다"를 하나씩 남겨두는 것 — 나중에 goal에 도달하면 이 발자국을
        // goal → ... → start 순서로 거꾸로 따라가서 전체 경로를 복원하는 데 쓴다(아래 역추적 부분).
        var previousCellOnPath = new Dictionary<Vector3Int, Vector3Int>();

        // visitedCells: "이미 큐에 넣어본 적 있는 칸" 집합. 이게 없으면 같은 칸을 여러 경로로
        // 계속 다시 큐에 넣게 되어 중복 계산이 쌓이거나 최악의 경우 무한히 맴돌 수 있다 —
        // "한 번 발견한 칸은 다시 발견하지 않는다"는 표시.
        var visitedCells = new HashSet<Vector3Int> { start };

        // frontierQueue: 다음에 탐색할 칸들의 대기줄(FIFO). BFS는 "start에서 가까운 칸부터
        // 순서대로" 탐색해야 최단 경로가 보장되는데, Queue(선입선출)를 쓰면 자동으로 이 순서가
        // 지켜진다 — 먼저 발견한(=더 가까운) 칸이 먼저 처리됨.
        var frontierQueue = new Queue<Vector3Int>();
        frontierQueue.Enqueue(start);

        // 맵이 비정상적으로 커지거나 순회 로직에 버그가 생겨도 무한루프에 빠지지 않도록 하는
        // 안전 상한 — FloorLayoutGenerator의 safety 카운터와 같은 목적.
        int safety = 20000;
        bool foundGoal = false;

        // [탐색 루프] 큐가 빌 때까지(더 볼 칸이 없을 때까지) 반복한다.
        //   1) 대기줄 맨 앞(=지금까지 발견한 것 중 start에서 가장 가까운 칸)을 하나 꺼낸다(current).
        //   2) 그게 goal이면 — BFS 성질상 "처음 발견한 순간이 곧 최단 경로"이므로 바로 탐색 종료.
        //   3) 아니면 current의 4방향 이웃(next)을 하나씩 확인해서:
        //      - 이미 본 칸(visitedCells)이거나 못 가는 칸(벽/맵 바깥)이면 건너뜀
        //      - 아니면 "새로 발견함" 표시(visitedCells 추가) + "여기 오기 전엔 current에 있었다"고
        //        발자국을 남김(previousCellOnPath) + 나중에 얘 차례에서도 탐색하도록 대기줄에 추가
        while (frontierQueue.Count > 0 && safety-- > 0)
        {
            var current = frontierQueue.Dequeue();
            if (current == goal)
            {
                foundGoal = true;
                break;
            }

            foreach (var dir in Directions)
            {
                var next = current + dir;
                if (visitedCells.Contains(next)) continue;

                // [2026-09-04 버그 수정] 여기서도 next가 goal 자신인 경우엔 지형만 본다(isBlockedExtra
                // 무시) — 위쪽 "목적지 자체는 isBlockedExtra 무시" 사전 체크(IsWalkableTerrain(goal,...))
                // 는 BFS 시작 전 딱 한 번만 실행되는 별개 체크라서, 정작 BFS가 사방으로 퍼지다가 goal을
                // "이웃 칸"으로 처음 발견하는 이 지점에는 적용이 안 되고 있었다. 그 결과 이 줄이 계속
                // IsWalkable(next, ..., isBlockedExtra)를 썼는데, goal은 항상 Player 자신이 서있는
                // 칸이라 isBlockedExtra(goal)이 항상 true가 되어 goal이 큐에 아예 안 들어가고, 그래서
                // "current == goal"이 될 일이 영원히 없어서 추적 경로가 100% 실패하고 있었다(거리/위치
                // 무관하게 항상 path.Count==0 — 로그로 확인, "감지는 성공하는데 매번 바로 추적이
                // 풀리는" 증상의 진짜 원인).
                bool nextIsWalkable = (next == goal)
                    ? IsWalkableTerrain(next, wallsTilemap, floorTilemap)
                    : IsWalkable(next, wallsTilemap, floorTilemap, isBlockedExtra);
                if (!nextIsWalkable) continue;

                visitedCells.Add(next);
                previousCellOnPath[next] = current;
                frontierQueue.Enqueue(next);
            }
        }

        if (!foundGoal) return result; // 도달 불가(막혀있거나 safety 상한에 걸림) — 빈 경로

        // [역추적] previousCellOnPath는 "각 칸이 어디서 왔는지"만 알려주기 때문에, goal에서부터
        // 발자국을 거꾸로(goal → 그 직전 칸 → ... → start) 따라가야 전체 경로가 나온다.
        // 이 과정에서 만든 리스트는 자연히 "goal이 맨 앞, start 바로 다음 칸이 맨 뒤"인 역순이라서,
        // 마지막에 Reverse()로 뒤집어야 실제 이동 순서(start 다음 칸 → ... → goal)가 된다.
        var cell = goal;
        while (cell != start)
        {
            result.Add(cell);
            cell = previousCellOnPath[cell];
        }
        result.Reverse();
        return result;
    }

    // 지형만 확인(벽/맵 바깥) — 점유 여부는 안 봄. goal 판정 전용(위 FindPath 주석 참고).
    private static bool IsWalkableTerrain(Vector3Int cell, Tilemap wallsTilemap, Tilemap floorTilemap)
    {
        if (wallsTilemap.HasTile(cell)) return false;
        if (!floorTilemap.HasTile(cell)) return false;
        return true;
    }

    // 지형 + 점유 여부까지 확인 — BFS가 경유지를 확장할 때 씀(goal 자체에는 안 씀).
    private static bool IsWalkable(Vector3Int cell, Tilemap wallsTilemap, Tilemap floorTilemap, Func<Vector3Int, bool> isBlockedExtra)
    {
        if (!IsWalkableTerrain(cell, wallsTilemap, floorTilemap)) return false;
        if (isBlockedExtra != null && isBlockedExtra(cell)) return false; // 다른 액터가 점유 중인 칸
        return true;
    }
}
