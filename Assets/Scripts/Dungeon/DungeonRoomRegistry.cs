using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class DungeonRoomRegistry
{
    private static readonly Dictionary<Tilemap, FloorLayout> layouts = new Dictionary<Tilemap, FloorLayout>();
    private static readonly Dictionary<Tilemap, int> chunkSizes = new Dictionary<Tilemap, int>();

    public static void Register(Tilemap floor, FloorLayout layout, int chunkSize)
    {
        layouts[floor] = layout;
        chunkSizes[floor] = chunkSize;
    }

    public static int GetRoomId(Tilemap floor, Vector3Int tileCell)
    {
        if (floor == null || !layouts.TryGetValue(floor, out var layout)) return -1;
        int size = chunkSizes[floor];
        var chunk = new Vector2Int(Mathf.FloorToInt((float)tileCell.x / size), Mathf.FloorToInt((float)tileCell.y / size));
        return layout.GetRoomId(chunk);
    }
}
