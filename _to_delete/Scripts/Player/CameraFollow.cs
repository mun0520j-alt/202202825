using UnityEngine;

// Dungeon Tools 7) 카메라가 target(플레이어)의 XY를 매 프레임 따라간다. Z는 안 건드려서
// 원근/클리핑 설정에 영향 안 줌. 지금은 스냅(즉시 추적)이고, 나중에 필요하면 감쇠를 추가하면 됨.
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    private void LateUpdate()
    {
        if (target == null) return;

        var pos = transform.position;
        transform.position = new Vector3(target.position.x, target.position.y, pos.z);
    }
}
