using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Dungeon Tools 3a) Kyrise 16x16 RPG Icon Pack에서 실제로 쓰기로 확정한 아이콘만
// 카테고리별 폴더로 옮긴다(AssetDatabase.MoveAsset 사용 — 원본 파일을 직접 옮기지 않고
// 유니티 메타/GUID가 안 깨지게 API로 이동). 아직 결정 안 된 항목(potion 몇 개 쓸지,
// bow/arrow 보류, spellbook 보류)은 건드리지 않고 원래 자리에 그대로 둔다.
//
// 확정된 선별 기준:
// - armor: 5종 전부
// - sword: 3종(01/02/03) x 5색 전부 — 날 모양이 다 달라서 전부 사용
// - staff: 02 시리즈만
// - shield: 02 시리즈만
// - ring: 3종 x 5색 전부 — 종류/스탯만 데이터로 구분, 비주얼은 다 사용
public static class OrganizeKyriseIcons
{
    private const string SourceFolder =
        "Assets/Art/ItemIcons_Kyrise/ItemIcons/Kyrise_16x16_RPG_Icon_Pack_V1.3/icons/16x16";
    private const string DestBaseFolder = "Assets/Art/ItemIcons_Kyrise/Selected";

    private static readonly Dictionary<string, string[]> Selection = new Dictionary<string, string[]>
    {
        ["Armor"] = new[] { "armor_01a", "armor_01b", "armor_01c", "armor_01d", "armor_01e" },
        ["Sword"] = new[]
        {
            "sword_01a", "sword_01b", "sword_01c", "sword_01d", "sword_01e",
            "sword_02a", "sword_02b", "sword_02c", "sword_02d", "sword_02e",
            "sword_03a", "sword_03b", "sword_03c", "sword_03d", "sword_03e",
        },
        ["Staff"] = new[] { "staff_02ab", "staff_02b", "staff_02c", "staff_02d", "staff_02e" },
        ["Shield"] = new[] { "shield_02a", "shield_02b", "shield_02c", "shield_02d", "shield_02e" },
        ["Ring"] = new[]
        {
            "ring_01a", "ring_01b", "ring_01c", "ring_01d", "ring_01e",
            "ring_02a", "ring_02b", "ring_02c", "ring_02d", "ring_02e",
            "ring_03a", "ring_03b", "ring_03c", "ring_03d", "ring_03e",
        },
    };

    [MenuItem("Dungeon Tools/3a) Organize Selected Kyrise Icons")]
    public static void Organize()
    {
        EnsureFolder("Assets/Art/ItemIcons_Kyrise", "Selected");

        int moved = 0, missing = 0, alreadyThere = 0;

        foreach (var category in Selection)
        {
            string destFolder = $"{DestBaseFolder}/{category.Key}";
            EnsureFolder(DestBaseFolder, category.Key);

            foreach (var fileName in category.Value)
            {
                string sourcePath = $"{SourceFolder}/{fileName}.png";
                string destPath = $"{destFolder}/{fileName}.png";

                if (AssetDatabase.LoadAssetAtPath<Sprite>(destPath) != null)
                {
                    alreadyThere++;
                    continue;
                }

                if (AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath) == null)
                {
                    Debug.LogWarning($"[OrganizeKyriseIcons] 원본을 못 찾음, 건너뜀: {sourcePath}");
                    missing++;
                    continue;
                }

                string error = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"[OrganizeKyriseIcons] 이동 실패({error}): {sourcePath}");
                    missing++;
                }
                else
                {
                    moved++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[OrganizeKyriseIcons] 완료 — {moved}개 이동, 이미 정리돼 있던 {alreadyThere}개, " +
                  $"실패/누락 {missing}개. potion/bow/arrow/spellbook은 아직 결정 안 돼서 그대로 뒀습니다.");
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
