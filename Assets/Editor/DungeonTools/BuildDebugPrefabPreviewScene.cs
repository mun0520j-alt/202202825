using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dungeon Tools 3) Assets/Generated/Prefabs 밑의 프리팹을 전부 불러와서 새 디버그 씬에
// x축 2 / y축 2 간격의 그리드로 배치하고 저장한다(한 줄로 늘어놓으면 142개 기준
// 옆으로 너무 길어져서 격자로 나눠 배치). idle 그리드를 전부 배치한 다음, run 상태가
// 있는 오브젝트들은 idle 그리드의 마지막 줄 y에서 -5 낮춘 지점을 기준으로 별도의
// 그리드로 한 번 더 배치한다(같은 y줄에 겹치지 않게 idle 블록 전체 아래에 따로 둠).
// Play 모드 진입 시 run 인스턴스들은 DebugAnimatorStatePlayer로 run을 강제 재생한다.
//
// 주의:
// - 새 씬을 Single 모드로 여는 방식이라, 지금 에디터에 열려있는 씬에 저장 안 한
//   변경사항이 있으면 유니티가 저장 여부를 물어봄(그대로 응답하면 됨) — 기존 씬
//   파일 자체를 덮어쓰거나 지우지는 않는다.
// - Animator는 Edit 모드에서는 재생되지 않는다. 애니메이션까지 눈으로 확인하려면
//   이 씬을 연 채로 Play 모드에 들어가야 한다.
public static class BuildDebugPrefabPreviewScene
{
    private const string PrefabFolder = "Assets/Generated/Prefabs";
    private const string ScenePath = "Assets/Scenes/DungeonTools_DebugPrefabPreview.unity";
    private const float SpacingX = 2f;
    private const float SpacingY = 2f;
    private const float RunBlockGap = 5f; // run 그리드 시작 y = idle 마지막 줄 y - 5
    private const int Columns = 12; // 142개 기준 세로로 너무 안 늘어지게 12열 그리드로 배치

    [MenuItem("Dungeon Tools/3) Build Debug Prefab Preview Scene")]
    public static void Build()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        if (guids.Length == 0)
        {
            Debug.LogError($"[BuildDebugPrefabPreviewScene] {PrefabFolder} 안에 프리팹이 없습니다. " +
                            "먼저 \"Dungeon Tools/2) Build Anim States + Prefabs\"를 돌려주세요.");
            return;
        }

        System.Array.Sort(guids, (a, b) =>
            string.Compare(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b), System.StringComparison.Ordinal));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1) idle 그리드 — 전부 배치
        var runCandidates = new List<GameObject>();
        int idleCount = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int col = idleCount % Columns;
            int row = idleCount / Columns;

            var idleInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            idleInstance.transform.position = new Vector3(col * SpacingX, -row * SpacingY, 0f);
            idleCount++;

            var animator = prefab.GetComponent<Animator>();
            bool hasRun = animator != null && animator.runtimeAnimatorController != null &&
                          animator.runtimeAnimatorController.animationClips.Any(c => c.name.EndsWith("_run"));
            if (hasRun) runCandidates.Add(prefab);
        }

        // 2) run 그리드 — idle 마지막 줄 y에서 -5 낮춘 지점부터 별도 그리드로 배치
        int idleRowCount = idleCount == 0 ? 0 : Mathf.CeilToInt(idleCount / (float)Columns);
        float idleLastRowY = -(idleRowCount - 1) * SpacingY;
        float runBaseY = idleLastRowY - RunBlockGap;

        int runCount = 0;
        foreach (var prefab in runCandidates)
        {
            int col = runCount % Columns;
            int row = runCount / Columns;

            var runInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            runInstance.name = prefab.name + "_run";
            runInstance.transform.position = new Vector3(col * SpacingX, runBaseY - row * SpacingY, 0f);
            var player = runInstance.AddComponent<DebugAnimatorStatePlayer>();
            player.stateName = "run";
            runCount++;
        }

        string sceneDir = Path.GetDirectoryName(ScenePath);
        if (!string.IsNullOrEmpty(sceneDir) && !Directory.Exists(sceneDir))
        {
            Directory.CreateDirectory(sceneDir);
        }
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log($"[BuildDebugPrefabPreviewScene] idle {idleCount}개를 {Columns}열 그리드로 배치, " +
                  $"run이 있는 {runCount}개는 idle 마지막 줄(y={idleLastRowY})에서 {RunBlockGap} 낮춘 y={runBaseY}부터 " +
                  $"별도 그리드로 배치해 {ScenePath}에 저장했습니다. 애니메이션까지 보려면 이 씬을 연 채로 Play 모드에 들어가세요.");
    }
}
