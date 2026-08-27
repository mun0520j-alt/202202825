using UnityEngine;

// 디버그 프리뷰 씬 전용 런타임 컴포넌트. Animator의 기본 상태(idle)가 아니라
// 지정된 다른 상태(예: "run")를 Play 모드 진입 시 강제로 재생시킨다.
// Editor 폴더가 아닌 일반 런타임 스크립트로 둬야 Play 모드에서 정상 동작한다.
public class DebugAnimatorStatePlayer : MonoBehaviour
{
    public string stateName;

    private void Start()
    {
        var animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(stateName))
        {
            animator.Play(stateName);
        }
    }
}
