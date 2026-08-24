using System.Collections.Generic;
using UnityEngine;

// Dungeon Tools 5) FloorLayoutGenerator의 결과물. 청크 좌표(그리드 셀) 단위로 방 배치와
// 연결 구조만 담는 순수 데이터 — 실제 타일 렌더링(RuleTile/Tilemap)이나 몬스터 배치는
// 이 데이터를 소비하는 별도 클래스에서 담당한다(단일 책임 원칙).
// Loop = 생성 후 추가 순환 경로(다리)를 최소 1개 이상 놓는 데 성공함.
// Tree = 다리 놓을 자리가 없어서 순수 트리(막다른 길만 있는) 구조로 남음.
public enum FloorShape
{
    Loop,
    Tree,
}

public class FloorLayout
{
    public int GridWidth;
    public int GridHeight;
    public FloorShape Shape;

    // 방으로 쓰이는 청크 좌표 전체
    public HashSet<Vector2Int> RoomCells = new HashSet<Vector2Int>();

    // 각 방 좌표 -> 직교 인접한(=통로로 연결된) 다른 방 좌표 목록
    public Dictionary<Vector2Int, List<Vector2Int>> Connections = new Dictionary<Vector2Int, List<Vector2Int>>();

    public Vector2Int StartCell;
    public Vector2Int BossCell;
    public List<Vector2Int> KeyCells = new List<Vector2Int>();

    // 병합된 방끼리 같은 id를 공유 (녹픽던의 "표준 방은 인접한 방 1개와 병합 가능" 규칙 참고).
    // 병합되지 않은 방은 이 딕셔너리에 아예 안 들어있다.
    public Dictionary<Vector2Int, int> MergeGroupId = new Dictionary<Vector2Int, int>();

    public int RoomCount => RoomCells.Count;

    public bool IsMerged(Vector2Int cell) => MergeGroupId.ContainsKey(cell);
}
