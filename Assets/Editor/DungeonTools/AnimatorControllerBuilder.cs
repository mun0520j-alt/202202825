using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Dungeon Tools 2b) SpriteFrameSet 하나를 받아 상태별 AnimationClip을 만들고(1a에서 만든
// AnimationClipBuilder 재사용) AnimatorController에 상태로 등록한다. 상태 간 자동 전환
// (트랜지션)은 만들지 않는다 — 코드에서 Animator.Play(stateName)으로 직접 재생하는
// 방식을 전제로 함. 트랜지션 그래프/파라미터까지 지금 확정하는 건 과설계라 판단해 뺐다.
public static class AnimatorControllerBuilder
{
    // 여러 상태가 있을 때 기본으로 재생될 상태의 우선순위.
    private static readonly string[] DefaultStatePriority = { "idle", "run", "open", "anim", "hit" };

    public static AnimatorController BuildController(SpriteFrameSet set, string clipFolder, string controllerFolder)
    {
        string controllerPath = $"{controllerFolder}/{set.objectName}.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null) AssetDatabase.DeleteAsset(controllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var stateMachine = controller.layers[0].stateMachine;

        foreach (var state in set.states)
        {
            var clip = AnimationClipBuilder.BuildClip(set.objectName, state, clipFolder);
            if (clip == null) continue;

            var animState = stateMachine.AddState(state.stateName);
            animState.motion = clip;
        }

        foreach (var preferred in DefaultStatePriority)
        {
            var match = stateMachine.states.FirstOrDefault(s => s.state.name == preferred);
            if (match.state != null)
            {
                stateMachine.defaultState = match.state;
                break;
            }
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }
}
