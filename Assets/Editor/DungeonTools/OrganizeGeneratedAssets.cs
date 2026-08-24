using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Dungeon Tools 4) Assets/Generated 아래 SpriteSets / Animations / Prefabs 3개 폴더를
// 이름(base name) 기준으로 카테고리별 하위 폴더로 정리한다.
// AssetDatabase.MoveAsset을 사용해서 .meta/GUID가 안 깨지게 이동한다(직접 파일시스템
// 이동 금지 — Unity가 참조를 잃어버림).
//
// 카테고리 (2026-08-24 기준 인벤토리 분석 결과, GENERATED_ASSETS_INVENTORY.md 참고):
// - Creatures        : idle+run(+hit) 풀 애니메이션 있는 몬스터/캐릭터 (SpriteSet+Anim+Prefab)
// - CreaturesSimple   : 단일 애니메이션(run 없음)만 있는 몬스터 (SpriteSet+Anim+Prefab)
// - Interactables     : 상호작용 오브젝트 — 상자/폭탄/코인/함정 (SpriteSet+Anim+Prefab)
// - FountainProps      : 분수 장식 오브젝트 (SpriteSet+Anim+Prefab)
// - EnvironmentTiles   : 맵 배치용 정적 타일 — 바닥/벽/문 등 (SpriteSet만)
// - Weapons            : 무기 아이콘 (SpriteSet만)
// - Potions            : 포션/플라스크 아이콘 (SpriteSet만)
// - UiSprites          : UI용 스프라이트 — 버튼/하트 (SpriteSet만)
//
// Animations 폴더는 base name에 _idle/_run/_hit/_open/_anim 접미사가 붙은 클립과
// 접미사 없는 {base}.controller가 섞여 있어서, 파일명에서 알려진 접미사를 떼어낸 뒤
// base name으로 카테고리를 찾는다.
public static class OrganizeGeneratedAssets
{
    private const string SpriteSetsFolder = "Assets/Generated/SpriteSets";
    private const string AnimationsFolder = "Assets/Generated/Animations";
    private const string PrefabsFolder = "Assets/Generated/Prefabs";

    private static readonly string[] AnimSuffixes = { "_idle", "_run", "_hit", "_open", "_anim" };

    // 애니메이션+프리팹까지 있는 카테고리 (SpriteSets/Animations/Prefabs 전부 적용)
    private static readonly string[] Creatures = { "angel", "big_demon", "big_zombie", "chort", "doc", "dwarf_f", "dwarf_m", "elf_f", "elf_m", "goblin", "imp", "knight_f", "knight_m", "lizard_f", "lizard_m", "masked_orc", "ogre", "orc_shaman", "orc_warrior", "pumpkin_dude", "skelet", "tiny_zombie", "wizzard_f", "wizzard_m", "wogol" };
    private static readonly string[] CreaturesSimple = { "ice_zombie", "muddy", "necromancer", "slug", "swampy", "tiny_slug", "zombie" };
    private static readonly string[] Interactables = { "bomb", "chest_empty", "chest_full", "chest_mimic", "coin", "floor_spikes" };
    private static readonly string[] FountainProps = { "wall_fountain_basin_blue", "wall_fountain_basin_red", "wall_fountain_mid_blue", "wall_fountain_mid_red" };

    // 스프라이트만 있는 카테고리 (SpriteSets에만 적용)
    private static readonly string[] EnvironmentTiles = { "column", "column_wall", "crate", "doors_frame_left", "doors_frame_right", "doors_frame_top", "doors_leaf_closed", "doors_leaf_open", "edge_down", "floor_1", "floor_2", "floor_3", "floor_4", "floor_5", "floor_6", "floor_7", "floor_8", "floor_ladder", "floor_stairs", "hole", "lever_left", "lever_right", "skull", "wall_banner_blue", "wall_banner_green", "wall_banner_red", "wall_banner_yellow", "wall_edge_bottom_left", "wall_edge_bottom_right", "wall_edge_left", "wall_edge_mid_left", "wall_edge_mid_right", "wall_edge_right", "wall_edge_top_left", "wall_edge_top_right", "wall_edge_tshape_bottom_left", "wall_edge_tshape_bottom_right", "wall_edge_tshape_left", "wall_edge_tshape_right", "wall_fountain_top_1", "wall_fountain_top_2", "wall_fountain_top_3", "wall_goo", "wall_goo_base", "wall_hole_1", "wall_hole_2", "wall_left", "wall_mid", "wall_outer_front_left", "wall_outer_front_right", "wall_outer_mid_left", "wall_outer_mid_right", "wall_outer_top_left", "wall_outer_top_right", "wall_right", "wall_top_left", "wall_top_mid", "wall_top_right" };
    private static readonly string[] Weapons = { "weapon_anime_sword", "weapon_arrow", "weapon_axe", "weapon_baton_with_spikes", "weapon_big_hammer", "weapon_bow", "weapon_bow_2", "weapon_cleaver", "weapon_double_axe", "weapon_duel_sword", "weapon_golden_sword", "weapon_green_magic_staff", "weapon_hammer", "weapon_katana", "weapon_knife", "weapon_knight_sword", "weapon_lavish_sword", "weapon_mace", "weapon_machete", "weapon_red_gem_sword", "weapon_red_magic_staff", "weapon_regular_sword", "weapon_rusty_sword", "weapon_saw_sword", "weapon_spear", "weapon_throwing_axe", "weapon_waraxe" };
    private static readonly string[] Potions = { "flask_big_blue", "flask_big_green", "flask_big_red", "flask_big_yellow", "flask_blue", "flask_green", "flask_red", "flask_yellow" };
    private static readonly string[] UiSprites = { "button_blue_down", "button_blue_up", "button_red_down", "button_red_up", "ui_heart_empty", "ui_heart_full", "ui_heart_half" };

    // 카테고리별 폴더가 적용될 base name → 폴더 이름 매핑 (SpriteSets/Animations/Prefabs 공통)
    private static readonly Dictionary<string, string> AnimatedCategoryByBase = BuildLookup(
        ("Creatures", Creatures),
        ("CreaturesSimple", CreaturesSimple),
        ("Interactables", Interactables),
        ("FountainProps", FountainProps));

    // SpriteSets에만 추가로 적용되는 스프라이트 전용 카테고리
    private static readonly Dictionary<string, string> SpriteOnlyCategoryByBase = BuildLookup(
        ("EnvironmentTiles", EnvironmentTiles),
        ("Weapons", Weapons),
        ("Potions", Potions),
        ("UiSprites", UiSprites));

    private static Dictionary<string, string> BuildLookup(params (string category, string[] names)[] groups)
    {
        var lookup = new Dictionary<string, string>();
        foreach (var (category, names) in groups)
        {
            foreach (var name in names)
            {
                lookup[name] = category;
            }
        }
        return lookup;
    }

    [MenuItem("Dungeon Tools/4) Organize Generated Assets By Category")]
    public static void Organize()
    {
        int moved = 0, alreadyThere = 0, unmatched = 0, failed = 0;

        MoveFolder(SpriteSetsFolder, ".asset", baseName =>
            {
                if (AnimatedCategoryByBase.TryGetValue(baseName, out var c1)) return c1;
                if (SpriteOnlyCategoryByBase.TryGetValue(baseName, out var c2)) return c2;
                return null;
            },
            ref moved, ref alreadyThere, ref unmatched, ref failed);

        MoveFolder(PrefabsFolder, ".prefab", baseName =>
                AnimatedCategoryByBase.TryGetValue(baseName, out var c) ? c : null,
            ref moved, ref alreadyThere, ref unmatched, ref failed);

        MoveFolder(AnimationsFolder, null, fileNameNoExt =>
            {
                string baseName = StripAnimSuffix(fileNameNoExt);
                return AnimatedCategoryByBase.TryGetValue(baseName, out var c) ? c : null;
            },
            ref moved, ref alreadyThere, ref unmatched, ref failed);

        AssetDatabase.SaveAssets();
        Debug.Log($"[OrganizeGeneratedAssets] 완료 — 이동 {moved}개, 이미 정리됨 {alreadyThere}개, " +
                  $"카테고리 미매칭(원래 자리 유지) {unmatched}개, 실패 {failed}개.");
    }

    // extensionFilter가 null이면 폴더 안의 모든 파일(.meta 제외) 대상, 아니면 해당 확장자만.
    private static void MoveFolder(string folder, string extensionFilter, System.Func<string, string> categoryResolver,
        ref int moved, ref int alreadyThere, ref int unmatched, ref int failed)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[OrganizeGeneratedAssets] 폴더 없음, 건너뜀: {folder}");
            return;
        }

        string absFolder = Path.Combine(Directory.GetCurrentDirectory(), folder);
        var files = Directory.GetFiles(absFolder, "*", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".meta"))
            .Where(f => extensionFilter == null || f.EndsWith(extensionFilter))
            .OrderBy(f => f)
            .ToList();

        foreach (var absPath in files)
        {
            string fileName = Path.GetFileName(absPath);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(absPath);
            string category = categoryResolver(fileNameNoExt);

            if (category == null)
            {
                unmatched++;
                continue;
            }

            string sourcePath = ToAssetPath(folder, fileName);
            string destFolder = $"{folder}/{category}";
            string destPath = $"{destFolder}/{fileName}";

            if (sourcePath == destPath)
            {
                alreadyThere++;
                continue;
            }

            EnsureFolder(folder, category);

            if (AssetDatabase.LoadMainAssetAtPath(destPath) != null)
            {
                alreadyThere++;
                continue;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[OrganizeGeneratedAssets] 이동 실패({error}): {sourcePath}");
                failed++;
            }
            else
            {
                moved++;
            }
        }
    }

    private static string StripAnimSuffix(string fileNameNoExt)
    {
        foreach (var suffix in AnimSuffixes)
        {
            if (fileNameNoExt.EndsWith(suffix))
            {
                return fileNameNoExt.Substring(0, fileNameNoExt.Length - suffix.Length);
            }
        }
        return fileNameNoExt; // .controller 등 접미사 없는 파일
    }

    private static string ToAssetPath(string folder, string fileName) => $"{folder}/{fileName}";

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
