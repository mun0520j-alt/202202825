using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class M6GridInventoryValidation
{
    [MenuItem("Dungeon Tools/QA/M6 Grid Model Validation")]
    public static void RunFromMenu()
    {
        string summary = RunAll();
        EditorUtility.DisplayDialog("M6 Grid Validation", summary, "확인");
    }

    public static void RunFromCommandLine()
    {
        string summary = RunAll();
        Debug.Log(summary);
    }

    public static string RunAll()
    {
        var failures = new List<string>();
        ValidateCsv(failures);
        ValidateBoundaryOverlapAndMove(failures);
        ValidateDefaultBag(failures);
        ValidateRareBag(failures);
        ValidateNormalBag(failures);

        string summary = failures.Count == 0
            ? "[M6 Grid Validation] PASS — CSV/경계/겹침/이동/회전/가방 형상 실패 0"
            : $"[M6 Grid Validation] FAIL {failures.Count}\n- {string.Join("\n- ", failures)}";
        if (failures.Count > 0) throw new InvalidOperationException(summary);
        Debug.Log(summary);
        return summary;
    }

    private static void ValidateCsv(List<string> failures)
    {
        var expectedBagSizes = new Dictionary<string, Vector2Int>
        {
            { "bag_basic", new Vector2Int(4, 2) },
            { "bag_rare", new Vector2Int(3, 3) },
            { "bag_epic", new Vector2Int(5, 3) },
            { "bag_unique", new Vector2Int(4, 5) }
        };

        foreach (LootItemData item in LootItemDatabase.Items)
        {
            if (item.GridWidth <= 0 || item.GridHeight <= 0)
                failures.Add($"{item.Id}: grid_w/grid_h는 1 이상이어야 함");
            if (item.Type != LootItemType.Bag && (item.BagGridWidth != 0 || item.BagGridHeight != 0))
                failures.Add($"{item.Id}: Bag이 아닌 아이템에 가방 격자 값이 있음");
        }

        foreach (var expected in expectedBagSizes)
        {
            LootItemData bag = LootItemDatabase.FindById(expected.Key);
            if (bag == null)
            {
                failures.Add($"{expected.Key}: CSV 항목 없음");
                continue;
            }
            if (bag.BagGridWidth != expected.Value.x || bag.BagGridHeight != expected.Value.y)
                failures.Add($"{expected.Key}: 가방 격자 {bag.BagGridWidth}x{bag.BagGridHeight}, 기대 {expected.Value.x}x{expected.Value.y}");
            if (bag.CapacityBonus <= 0)
                failures.Add($"{expected.Key}: v2 해석용 capacity_bonus가 사라짐");
        }
    }

    private static void ValidateBoundaryOverlapAndMove(List<string> failures)
    {
        var grid = new LootGridModel(4, 4);
        LootItemInstance armor = Item("armor", 2, 3);
        LootItemInstance shield = Item("shield", 2, 2);
        if (!grid.TryPlace(armor, 0, 0, false)) failures.Add("경계: 유효한 갑옷 배치 실패");
        if (grid.TryPlace(shield, 1, 1, false)) failures.Add("겹침: 기존 갑옷 위 배치를 허용함");
        if (grid.TryPlace(Item("outside", 2, 2), 3, 3, false)) failures.Add("경계: 격자 밖 배치를 허용함");
        if (!grid.Move(armor.InstanceId, 2, 1, false)) failures.Add("이동: 유효한 위치 이동 실패");
        if (grid.Move(armor.InstanceId, 3, 2, false)) failures.Add("이동: 격자 밖 이동을 허용함");
        if (grid.Find(armor.InstanceId)?.X != 2 || grid.Find(armor.InstanceId)?.Y != 1)
            failures.Add("이동: 실패한 이동이 기존 위치를 변경함");
    }

    private static void ValidateDefaultBag(List<string> failures)
    {
        var grid = new LootGridModel(3, 2);
        if (!grid.TryAutoPlace(Item("armor_default", 2, 3), out LootGridPlacement armor))
            failures.Add("기본 3x2: 갑옷 자동 회전 배치 실패");
        else if (!armor.Rotated || armor.Width != 3 || armor.Height != 2)
            failures.Add("기본 3x2: 갑옷이 3x2 회전 상태가 아님");
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
                if (grid.GetOccupant(x, y) == null) failures.Add($"기본 3x2: ({x},{y})가 비어 있음");
    }

    private static void ValidateRareBag(List<string> failures)
    {
        var grid = new LootGridModel(3, 3);
        if (!grid.TryAutoPlace(Item("armor_rare_test", 2, 3), out _))
            failures.Add("Rare 3x3: 갑옷 배치 실패");
        if (!grid.TryAutoPlace(Item("weapon_rare_test", 1, 3), out _))
            failures.Add("Rare 3x3: 갑옷과 무기 동시 수납 실패");
    }

    private static void ValidateNormalBag(List<string> failures)
    {
        var grid = new LootGridModel(4, 2);
        if (!grid.TryAutoPlace(Item("armor_normal_test", 2, 3), out _))
            failures.Add("Normal 4x2: 갑옷 회전 배치 실패");
        if (grid.TryAutoPlace(Item("weapon_normal_test", 1, 3), out _))
            failures.Add("Normal 4x2: 갑옷 뒤 무기 수납을 잘못 허용함");
    }

    private static LootItemInstance Item(string id, int width, int height)
    {
        return new LootItemInstance(new LootItemData
        {
            Id = id,
            Name = id,
            Type = LootItemType.Armor,
            Rarity = LootRarity.Normal,
            GridWidth = width,
            GridHeight = height
        });
    }
}
