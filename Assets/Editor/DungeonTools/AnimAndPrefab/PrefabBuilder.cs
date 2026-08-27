using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Dungeon Tools 2c) SpriteFrameSet + AnimatorController를 받아 SpriteRenderer + Animator를
// 가진 프리팹 하나를 만든다. 프리팹을 저장하려면 유니티 API 특성상 씬에 임시
// GameObject를 하나 만들었다가 저장 직후 바로 지워야 한다(같은 실행 흐름 안에서
// 렌더링 전에 파괴되므로 화면에는 보이지 않고, 씬 파일에도 저장되지 않는다) —
// 이 방식 말고는 코드로 프리팹 에셋을 만들 방법이 없어서 불가피하게 이렇게 했다.
public static class PrefabBuilder
{
    public static GameObject BuildPrefab(SpriteFrameSet set, AnimatorController controller, Sprite previewSprite, string prefabFolder)
    {
        var go = new GameObject(set.objectName);
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = previewSprite;

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            string prefabPath = $"{prefabFolder}/{set.objectName}.prefab";
            return PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
