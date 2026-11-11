using System;
using System.Collections.Generic;

// 출격 가방과 월드 루팅 보관함이 공유하는 순수 C# 격자 모델이다.
// 상태의 수명과 저장 책임은 이 모델을 소유하는 LootRuntimeState/월드 오브젝트에 있다.
public sealed class LootGridModel
{
    private readonly List<LootGridPlacement> placements = new List<LootGridPlacement>();

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<LootGridPlacement> Placements => placements;

    public LootGridModel(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
    }

    public bool CanPlace(LootItemInstance item, int x, int y, bool rotated)
    {
        return CanPlace(item, x, y, rotated, null);
    }

    public bool TryPlace(LootItemInstance item, int x, int y, bool rotated)
    {
        if (!CanPlace(item, x, y, rotated)) return false;
        placements.Add(new LootGridPlacement(item, x, y, rotated));
        return true;
    }

    public bool TryAutoPlace(LootItemInstance item, out LootGridPlacement placement)
    {
        if (TryAutoPlace(item, false, out placement)) return true;
        if (item?.Definition == null || item.Definition.GridWidth == item.Definition.GridHeight) return false;
        return TryAutoPlace(item, true, out placement);
    }

    public bool Move(string instanceId, int x, int y, bool rotated)
    {
        LootGridPlacement existing = Find(instanceId);
        if (existing == null || !CanPlace(existing.Item, x, y, rotated, instanceId)) return false;
        existing.SetPosition(x, y, rotated);
        return true;
    }

    public bool Remove(string instanceId, out LootItemInstance item)
    {
        LootGridPlacement existing = Find(instanceId);
        if (existing == null)
        {
            item = null;
            return false;
        }
        placements.Remove(existing);
        item = existing.Item;
        return true;
    }

    public LootGridPlacement GetOccupant(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
        foreach (LootGridPlacement placement in placements)
        {
            if (placement.Occupies(x, y)) return placement;
        }
        return null;
    }

    public LootGridPlacement Find(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;
        return placements.Find(value => value.Item != null && value.Item.InstanceId == instanceId);
    }

    private bool TryAutoPlace(LootItemInstance item, bool rotated, out LootGridPlacement placement)
    {
        placement = null;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!TryPlace(item, x, y, rotated)) continue;
                placement = placements[placements.Count - 1];
                return true;
            }
        }
        return false;
    }

    private bool CanPlace(LootItemInstance item, int x, int y, bool rotated, string ignoredInstanceId)
    {
        if (!TryGetSize(item, rotated, out int itemWidth, out int itemHeight)) return false;
        if (x < 0 || y < 0 || x + itemWidth > Width || y + itemHeight > Height) return false;
        if (ignoredInstanceId == null && Find(item.InstanceId) != null) return false;

        foreach (LootGridPlacement existing in placements)
        {
            if (existing.Item?.InstanceId == ignoredInstanceId) continue;
            if (RectanglesOverlap(x, y, itemWidth, itemHeight,
                    existing.X, existing.Y, existing.Width, existing.Height)) return false;
        }
        return true;
    }

    private static bool TryGetSize(LootItemInstance item, bool rotated, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (item?.Definition == null || item.Definition.GridWidth <= 0 || item.Definition.GridHeight <= 0) return false;
        width = rotated ? item.Definition.GridHeight : item.Definition.GridWidth;
        height = rotated ? item.Definition.GridWidth : item.Definition.GridHeight;
        return true;
    }

    private static bool RectanglesOverlap(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
    {
        return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
    }
}

public sealed class LootGridPlacement
{
    public LootItemInstance Item { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool Rotated { get; private set; }
    public int Width => Rotated ? Item.Definition.GridHeight : Item.Definition.GridWidth;
    public int Height => Rotated ? Item.Definition.GridWidth : Item.Definition.GridHeight;

    public LootGridPlacement(LootItemInstance item, int x, int y, bool rotated)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        SetPosition(x, y, rotated);
    }

    public bool Occupies(int x, int y)
    {
        return x >= X && x < X + Width && y >= Y && y < Y + Height;
    }

    internal void SetPosition(int x, int y, bool rotated)
    {
        X = x;
        Y = y;
        Rotated = rotated;
    }
}
