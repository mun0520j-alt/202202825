using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 6) 아직 RuleTile을 안 만든 단계에서, 구조 검증용으로 바닥/벽 각각 한 종류짜리
// 단순 Tile 애셋을 (없으면) 만들어서 돌려준다. 나중에 RuleTile로 교체할 때는 이 팩토리만
// 갈아끼우면 FloorTilemapPainter 쪽은 안 건드려도 되게 분리해뒀다.
//
// 주의: 소스 스프라이트 경로가 Assets/Art_Asset/... 로 돼있음 — 예전엔 Assets/Art/... 였는데
// 폴더가 Art_Asset으로 리네임된 상태라 여기에 맞춤(TilesetFrameSlicer.cs의 FramesFolder 상수는
// 아직 예전 경로라 재실행하면 깨질 수 있음, 확인 필요).
public static class PlaceholderTileFactory
{
    private const string FramesFolder =
        "Assets/Art_Asset/DungeonTiles_0x72/DungeonTiles/0x72_DungeonTilesetII_v1.7/frames";

    private const string FloorSpritePath = FramesFolder + "/floor_1.png";
    private const string WallSpritePath = FramesFolder + "/wall_mid.png";

    private const string OutputFolder = "Assets/Generated/PlaceholderTiles";
    private const string FloorTilePath = OutputFolder + "/PlaceholderFloor.asset";
    private const string WallTilePath = OutputFolder + "/PlaceholderWall.asset";

    public static Tile GetFloorTile() => GetOrCreateTile(FloorTilePath, FloorSpritePath, "PlaceholderFloor");
    public static Tile GetWallTile() => GetOrCreateTile(WallTilePath, WallSpritePath, "PlaceholderWall");

    private static Tile GetOrCreateTile(string tilePath, string spritePath, string assetName)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (existing != null) return existing;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"[PlaceholderTileFactory] 소스 스프라이트를 못 찾음: {spritePath}");
            return null;
        }

        EnsureFolder();
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.name = assetName;
        AssetDatabase.CreateAsset(tile, tilePath);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "PlaceholderTiles");
        }
    }
}
