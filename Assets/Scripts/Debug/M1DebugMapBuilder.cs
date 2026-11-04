#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.Tilemaps;

// 현재 생성된 던전 위에 M1 검증용 작은 방을 즉석 구성한다. 빌드용 씬/맵 데이터는 변경하지 않는다.
public static class M1DebugMapBuilder
{
    public static bool BuildNearPlayer()
    {
        var player = PlayerTurnActor.Instance;
        if (player == null || player.FloorTilemap == null || player.WallsTilemap == null)
        {
            Debug.LogWarning("[QA] 플레이어 또는 Floor/Walls Tilemap을 찾지 못해 디버그 맵을 만들 수 없습니다.");
            return false;
        }

        Tilemap floor = player.FloorTilemap;
        Tilemap walls = player.WallsTilemap;
        Vector3Int center = player.CurrentCell;
        TileBase floorTile = floor.GetTile(center);
        if (floorTile == null)
        {
            Debug.LogWarning("[QA] 플레이어 칸의 바닥 타일을 찾지 못했습니다.");
            return false;
        }

        for (int y = -4; y <= 4; y++)
        {
            for (int x = -4; x <= 4; x++)
            {
                var cell = center + new Vector3Int(x, y, 0);
                floor.SetTile(cell, floorTile);
                walls.SetTile(cell, null);
            }
        }

        ExpeditionSession.DebugSetCurrentFloor(ExpeditionSession.FinalFloor);
        ReplaceExit(floor, center + new Vector3Int(2, 0, 0));
        PlaceEnemy(center + new Vector3Int(0, 2, 0));
        CreateChestPlaceholder(floor, center + new Vector3Int(-2, 0, 0));
        FogOfWarController.Instance?.UpdateVisibility(center, 8);

        Debug.Log("[QA] M1 디버그 맵 구성 완료: 왼쪽 상자 자리표시자 / 위 Enemy / 오른쪽 3층 탈출구.");
        return true;
    }

    private static void ReplaceExit(Tilemap floor, Vector3Int cell)
    {
        foreach (var stairs in Object.FindObjectsOfType<DungeonStairs>()) Object.Destroy(stairs.gameObject);
        new GameObject("EscapeExit(QA)").AddComponent<DungeonStairs>().Initialize(floor, cell, true);
    }

    private static void PlaceEnemy(Vector3Int cell)
    {
        var enemies = Object.FindObjectsOfType<EnemyTurnActor>();
        if (enemies.Length == 0)
        {
            Debug.LogWarning("[QA] 배치할 Enemy가 없습니다. DungeonRunManager의 Enemy 생성을 확인하세요.");
            return;
        }

        enemies[0].DebugTeleportTo(cell);
        enemies[0].gameObject.SetActive(true);
        for (int i = 1; i < enemies.Length; i++) enemies[i].gameObject.SetActive(false);
    }

    private static void CreateChestPlaceholder(Tilemap floor, Vector3Int cell)
    {
        var previous = GameObject.Find("DebugChestPlaceholder(M2)");
        if (previous != null) Object.Destroy(previous);

        var chest = new GameObject("DebugChestPlaceholder(M2)");
        chest.transform.position = floor.GetCellCenterWorld(cell);
        chest.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        var renderer = chest.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = new Color(0.75f, 0.42f, 0.12f, 1f);
        renderer.sortingOrder = 5;

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(chest.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        var label = labelObject.AddComponent<TextMesh>();
        label.text = "CHEST\n(M2)";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.12f;
        label.fontSize = 48;
        label.color = Color.yellow;
        labelObject.GetComponent<MeshRenderer>().sortingOrder = 100;
    }
}
#endif
