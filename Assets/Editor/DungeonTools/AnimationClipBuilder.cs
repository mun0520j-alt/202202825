using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Dungeon Tools 2a) SpriteFrameSet.StateFrames 하나(예: goblin의 idle 4프레임)를 받아
// 스프라이트 애니메이션 AnimationClip 하나로 만든다. 이 역할만 담당 — 다른 스크립트가
// 재사용할 수 있게 분리해뒀다.
public static class AnimationClipBuilder
{
    private const float FramesPerSecond = 8f;

    // "hit"(피격 스냅샷), "open"(상자 개방)처럼 한 번 재생하고 멈춰야 하는 상태.
    // 나머지(idle/run/anim 등 ambient 애니메이션)는 기본적으로 루프시킨다.
    private static readonly HashSet<string> OneShotStates = new HashSet<string> { "hit", "open" };

    public static AnimationClip BuildClip(string objectName, SpriteFrameSet.StateFrames state, string outputFolder)
    {
        if (state?.frames == null || state.frames.Count == 0) return null;

        var clip = new AnimationClip { frameRate = FramesPerSecond };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = !OneShotStates.Contains(state.stateName);
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var keyframes = new ObjectReferenceKeyframe[state.frames.Count];
        for (int i = 0; i < state.frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / FramesPerSecond,
                value = state.frames[i],
            };
        }

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        string clipPath = $"{outputFolder}/{objectName}_{state.stateName}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null) AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }
}
