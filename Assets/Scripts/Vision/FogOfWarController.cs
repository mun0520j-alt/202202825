using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 11) 임시(placeholder) Fog of War 시각 연출(2026-08-28 신규). Enemy Trace의
// 감지 로직(LineOfSight + 시야 반경)이 실제로 뭘 가리고 뭘 보여주는지 눈으로 직접 확인하기
// 위한 1차 버전이다 — "감지는 실제 시각 연출이 있어야 확인되지" 요청 반영.
//
// 지금은 가장 단순한 버전이다: "기억"이 없다. 즉 한 번 봤던 칸이라도 지금 당장 시야 밖으로
// 나가면 다시 안개로 덮인다(진짜 로그라이크들이 흔히 하는 "이미 가본 곳은 어둡게 남기고 기억"
// 방식이 아님) — 나중에 진짜 시야 시스템을 만들 때 "탐험 기록(explored)" 레이어를 추가해서
// 확장하면 된다. 지금은 Player 이동마다 맵 전체를 다시 칠하는 것도(O(맵 크기)) 성능상
// 단순화한 부분 — Test Scene 규모에서는 문제없지만, 실제 던전 규모에서는 최적화가 필요할 수 있다.
public class FogOfWarController : MonoBehaviour
{
    public static FogOfWarController Instance { get; private set; }

    [Header("타일맵 참조")]
    [Tooltip("바닥 타일맵 — 안개를 칠할 범위(맵 전체 칸의 기준)로 쓴다.")]
    [SerializeField] private Tilemap floorTilemap;
    [Tooltip("벽 타일맵 — 시야가 벽에 막히는지 판정(LineOfSight)에 쓴다.")]
    [SerializeField] private Tilemap wallsTilemap;
    [Tooltip("안개를 실제로 그리는 타일맵. Floor/Walls보다 렌더 순서가 위여야 실제로 가려진다.")]
    [SerializeField] private Tilemap fogTilemap;

    // 미술 에셋 없이 바로 확인 가능하도록, 안개 타일을 코드에서 즉석으로 만든다(2026-08-28,
    // "일단 임시로" 요청 반영). 나중에 실제 안개 그래픽(예: 어두운 텍스처, 그라데이션)으로
    // 교체할 때는 이 메서드만 바꾸면 된다 — 나머지 로직(어디를 가릴지)은 안 건드려도 됨.
    private TileBase fogTile;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[FogOfWarController] 씬에 이미 있어서 중복 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (floorTilemap == null || wallsTilemap == null || fogTilemap == null)
        {
            Debug.LogError("[FogOfWarController] floorTilemap/wallsTilemap/fogTilemap이 Inspector에 " +
                            "연결 안 돼있습니다.");
            enabled = false;
            return;
        }

        fogTile = CreateSolidFogTile();
        PaintFullFog(); // 시작 시점엔 아무것도 안 보이는 상태로 시작 — 첫 UpdateVisibility 호출을 기다림.
    }

    // 1x1 검은 반투명 픽셀로 만든 안개 타일. Point 필터라 확대돼도 흐려지지 않고 딱 떨어지는
    // 사각형으로 보인다(이 프로젝트의 픽셀아트 스타일과 어울리게).
    private TileBase CreateSolidFogTile()
    {
        var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.92f));
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        return tile;
    }

    // 바닥이 있는 칸 전부를 안개로 덮는다 — "아무것도 안 보이는" 기본 상태로 리셋할 때 쓴다.
    private void PaintFullFog()
    {
        var bounds = floorTilemap.cellBounds;
        foreach (var cell in bounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(cell)) continue;
            fogTilemap.SetTile(cell, fogTile);
        }
    }

    // Player가 이동을 마칠 때마다(PlayerTurnActor.HopTo() 안에서) 호출된다. 매번 전체를 다시
    // 안개로 덮은 뒤(PaintFullFog), 지금 시야 반경 + 벽에 안 막힌 칸만 안개를 걷어서 "지금
    // 당장 보이는 곳만 보인다"는 느낌을 낸다.
    public void UpdateVisibility(Vector3Int viewerCell, int sightRangeInTiles)
    {
        PaintFullFog();

        var bounds = floorTilemap.cellBounds;
        int rangeSqr = sightRangeInTiles * sightRangeInTiles;

        foreach (var cell in bounds.allPositionsWithin)
        {
            if (!floorTilemap.HasTile(cell)) continue;

            var delta = cell - viewerCell;
            if (delta.x * delta.x + delta.y * delta.y > rangeSqr) continue; // 원형 반경 밖
            if (!LineOfSight.HasLineOfSight(viewerCell, cell, wallsTilemap)) continue; // 벽에 막힘

            fogTilemap.SetTile(cell, null); // 보이는 칸은 안개 제거
        }
    }
}
