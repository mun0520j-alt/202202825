using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Dungeon Tools 1b) 0x72 DungeonTileset II의 frames/ 폴더(이미 이름별로 낱장 분리된
// png 370개 — floor/wall/chest/weapon/캐릭터 전부 포함)를 순회해서 오브젝트별
// (Goblin/Rat/Sentinel/…) · 상태별(idle/run/hit/open/static)로 묶어 SpriteFrameSet
// 에셋을 생성한다. 메인 시트를 다시 자르지 않고 이미 분리돼 있는 개별 파일을 그대로
// 참조하기 때문에, 이전 버전에서 겪었던 "슬라이스 직후 같은 호출에서 재조회" 캐시
// 문제가 구조적으로 발생하지 않는다.
public static class TilesetFrameSlicer
{
    private const string FramesFolder = "Assets/Art/DungeonTiles_0x72/DungeonTiles/0x72_DungeonTilesetII_v1.7/frames";
    private const string OutputFolder = "Assets/Generated/SpriteSets";

    // {base}_{state}_anim_f{n} / {base}_anim_f{n} / {base}_f{n} 형태에서 base/state를 추출.
    // state가 idle/run/hit/open 중 하나가 아니면(coin_anim_f0, bomb_f0 등) state는 "anim"으로 묶는다.
    private static readonly Regex FrameNamePattern = new Regex(
        @"^(?<base>.+?)(?:_(?<state>idle|run|hit|open))?(?:_anim)?_f(?<frame>\d+)$",
        RegexOptions.Compiled);

    [MenuItem("Dungeon Tools/1b) Organize 0x72 Frames Into Sprite Sets")]
    public static void OrganizeFrames()
    {
        if (!AssetDatabase.IsValidFolder(FramesFolder))
        {
            Debug.LogError($"[TilesetFrameSlicer] frames 폴더를 찾을 수 없음: {FramesFolder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { FramesFolder });
        if (guids.Length == 0)
        {
            Debug.LogError($"[TilesetFrameSlicer] {FramesFolder} 안에서 Sprite를 하나도 못 찾았습니다. " +
                            "각 png의 Texture Type이 Sprite(2D and UI)인지 확인해보세요(1a를 먼저 돌렸으면 정상일 겁니다).");
            return;
        }

        // objectName -> stateName -> frame index -> Sprite
        var groups = new Dictionary<string, Dictionary<string, SortedDictionary<int, Sprite>>>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            string name = Path.GetFileNameWithoutExtension(path);
            var match = FrameNamePattern.Match(name);
            string objectName;
            string stateName;
            int frameIndex;

            if (match.Success)
            {
                objectName = match.Groups["base"].Value;
                stateName = match.Groups["state"].Success ? match.Groups["state"].Value : "anim";
                frameIndex = int.Parse(match.Groups["frame"].Value);
            }
            else
            {
                // 프레임 접미사가 없는 단일 스프라이트(floor_1, wall_mid 등)는
                // 그 자체가 프레임 1장짜리 오브젝트다.
                objectName = name;
                stateName = "static";
                frameIndex = 0;
            }

            if (!groups.TryGetValue(objectName, out var states))
            {
                states = new Dictionary<string, SortedDictionary<int, Sprite>>();
                groups[objectName] = states;
            }
            if (!states.TryGetValue(stateName, out var frames))
            {
                frames = new SortedDictionary<int, Sprite>();
                states[stateName] = frames;
            }
            frames[frameIndex] = sprite;
        }

        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "SpriteSets");

        int createdCount = 0;
        foreach (var objKvp in groups)
        {
            string objectName = objKvp.Key;
            string assetPath = $"{OutputFolder}/{objectName}.asset";

            var set = AssetDatabase.LoadAssetAtPath<SpriteFrameSet>(assetPath);
            bool isNew = set == null;
            if (isNew) set = ScriptableObject.CreateInstance<SpriteFrameSet>();

            set.objectName = objectName;
            set.states = objKvp.Value.Select(stateKvp => new SpriteFrameSet.StateFrames
            {
                stateName = stateKvp.Key,
                frames = stateKvp.Value.Values.ToList(),
            }).ToList();

            if (isNew)
            {
                AssetDatabase.CreateAsset(set, assetPath);
                createdCount++;
            }
            else
            {
                EditorUtility.SetDirty(set);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TilesetFrameSlicer] frames/ {guids.Length}개 파일 → 오브젝트 {groups.Count}개로 정리 완료 " +
                  $"(신규 {createdCount}개) → {OutputFolder}/ 확인해보세요.");
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
