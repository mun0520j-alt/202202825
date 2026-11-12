using UnityEngine;
using UnityEngine.Tilemaps;

// M7 귀환 스크롤이 활성화하는 고정 지점. 규칙 상태는 후속 스크롤 처리기가 소유하고,
// 이 컴포넌트는 시작 방 위치와 시각 표현만 담당한다.
public sealed class ReturnRitualAnchor : MonoBehaviour
{
    public static ReturnRitualAnchor Active { get; private set; }

    public Tilemap Floor { get; private set; }
    public Vector3Int CenterCell { get; private set; }
    public int RoomId { get; private set; }

    public void Initialize(Tilemap floor, Vector3Int centerCell, Sprite sprite)
    {
        Floor = floor;
        CenterCell = centerCell;
        RoomId = DungeonRoomRegistry.GetRoomId(floor, centerCell);
        Active = this;

        transform.position = floor.GetCellCenterWorld(centerCell);
        name = $"ReturnRitual_F{ExpeditionSession.CurrentFloor}";

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = sprite != null
            ? new Color(0.3f, 1f, 0.65f, 0.55f)
            : new Color(0.15f, 0.9f, 0.5f, 0.22f);
        renderer.sortingOrder = 1;
        transform.localScale = sprite != null ? new Vector3(2.4f, 2.4f, 1f) : new Vector3(3f, 3f, 1f);
    }

    public bool Contains(Vector3Int cell)
    {
        return Mathf.Abs(cell.x - CenterCell.x) <= 1 && Mathf.Abs(cell.y - CenterCell.y) <= 1;
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }
}
