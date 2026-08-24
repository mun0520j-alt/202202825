using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 7) 플레이어를 한 칸(1 유닛 = 1 타일)씩 이동시킨다. 이동 한 번 성공할 때마다
// Tick을 1 소비한다 — 목표 칸이 벽이면 아예 안 움직이고 Tick도 안 든다.
// 이동 중(다음 칸에 도착하기 전)에는 새 입력을 안 받는다 — 그리드에서 벗어난 애매한 위치에서
// 다시 방향을 꺾는 걸 막기 위함.
public class GridMover : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.12f; // 한 칸 이동에 걸리는 시간(연출용, Tick 소비량과는 무관)

    private Tilemap wallTilemap;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private float moveT = 1f; // 1이면 이동 중이 아님

    public void SetWallTilemap(Tilemap tilemap)
    {
        wallTilemap = tilemap;
    }

    // 생성 직후 등, 애니메이션 없이 즉시 특정 위치로 맞춰 세울 때 사용.
    public void SnapTo(Vector3 worldPos)
    {
        transform.position = worldPos;
        moveStart = moveTarget = worldPos;
        moveT = 1f;
    }

    private void Update()
    {
        if (moveT < 1f)
        {
            moveT += Time.deltaTime / moveDuration;
            transform.position = Vector3.Lerp(moveStart, moveTarget, Mathf.Clamp01(moveT));
            return;
        }

        var dir = ReadDirectionInput();
        if (dir == Vector2Int.zero) return;

        TryMove(dir);
    }

    private Vector2Int ReadDirectionInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Vector2Int.right;
        return Vector2Int.zero;
    }

    private void TryMove(Vector2Int dir)
    {
        Vector3 targetWorld = transform.position + new Vector3(dir.x, dir.y, 0f);

        if (wallTilemap != null)
        {
            Vector3Int targetCell = wallTilemap.WorldToCell(targetWorld);
            if (wallTilemap.HasTile(targetCell))
            {
                return; // 벽 — 이동도 Tick 소비도 안 함
            }
        }

        moveStart = transform.position;
        moveTarget = targetWorld;
        moveT = 0f;

        Tick.Advance();
    }
}
