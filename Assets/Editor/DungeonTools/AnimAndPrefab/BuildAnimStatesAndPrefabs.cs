using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Dungeon Tools 2) 1단계에서 만든 SpriteFrameSet(Assets/Generated/SpriteSets/*.asset)들을
// 순회하며 애니메이션 + 프리팹을 만든다. 상태가 "static" 하나뿐인 단일 스프라이트
// 오브젝트(floor_1, wall_mid, weapon_regular_sword 등)는 애니메이션이 필요 없어서
// 건너뛴다 — 그대로 Sprite 에셋으로만 쓰면 됨.
// 실제 클립/컨트롤러/프리팹 생성 로직은 각각 별도 스크립트(AnimationClipBuilder /
// AnimatorControllerBuilder / PrefabBuilder)에 있고, 이 스크립트는 순회 + 호출만 한다.
public static class BuildAnimStatesAndPrefabs
{
    private const string SourceFolder = "Assets/Generated/SpriteSets";
    private const string AnimFolder = "Assets/Generated/Animations";
    private const string PrefabFolder = "Assets/Generated/Prefabs";

    [MenuItem("Dungeon Tools/2) Build Anim States + Prefabs")]
    public static void Build()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "Animations");
        EnsureFolder("Assets/Generated", "Prefabs");

        var guids = AssetDatabase.FindAssets("t:SpriteFrameSet", new[] { SourceFolder });
        int skipped = 0, built = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var set = AssetDatabase.LoadAssetAtPath<SpriteFrameSet>(path);
            if (set == null || set.states == null || set.states.Count == 0) continue;

            bool onlyStatic = set.states.Count == 1 && set.states[0].stateName == "static";
            if (onlyStatic) { skipped++; continue; }

            AnimatorController controller = AnimatorControllerBuilder.BuildController(set, AnimFolder, AnimFolder);

            var previewState = set.states.FirstOrDefault(s => s.stateName == "idle")
                                ?? set.states.FirstOrDefault(s => s.frames != null && s.frames.Count > 0);
            Sprite previewSprite = (previewState != null && previewState.frames.Count > 0) ? previewState.frames[0] : null;

            PrefabBuilder.BuildPrefab(set, controller, previewSprite, PrefabFolder);
            built++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[BuildAnimStatesAndPrefabs] 완료 — 프리팹 {built}개 생성, 단일 스프라이트라 건너뛴 오브젝트 {skipped}개 " +
                  $"(→ {PrefabFolder}/, 애니메이션은 {AnimFolder}/ 확인해보세요).");
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
