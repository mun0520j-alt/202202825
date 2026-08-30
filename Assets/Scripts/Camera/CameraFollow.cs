using UnityEngine;

// Dungeon Tools 9) 카메라가 타겟(플레이어)을 즉시 따라간다 — 턴제 타일 게임이라 카메라가
// 지연 없이 딱 붙어서 움직이는 쪽이 낫다는 판단(2026-08-27 확정, SmoothDamp 같은 지연 추적은 안 씀).
//
// LateUpdate에서 처리하는 이유: PlayerTurnActor의 이동 연출(HopTo 코루틴)이 Update 타이밍에
// transform.position을 매 프레임 갱신하는데, 카메라가 그거보다 먼저 움직여버리면 한 프레임
// 밀려서 따라가는 것처럼 보일 수 있다 — 항상 "이번 프레임에 확정된 플레이어 위치"를 보고
// 움직이도록 모든 Update가 끝난 뒤(LateUpdate)에 카메라를 옮긴다.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 대상(Player). Inspector에서 직접 드래그해서 연결한다 — Find 안 씀.")]
    [SerializeField] private Transform target;

    [Tooltip("2D 스프라이트(Z=0)와 카메라가 너무 가까우면 Near Clip에 걸려서 아무것도 안 보이니 " +
              "카메라 Z는 이 값으로 고정한다(FloorTilemapPreviewWindow.CenterMainCameraOn의 -10 관례와 동일).")]
    [SerializeField] private float cameraZ = -10f;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] target이 Inspector에 연결 안 돼있습니다 — Player 오브젝트를 드래그해서 연결해주세요.");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        transform.position = new Vector3(target.position.x, target.position.y, cameraZ);
    }
}
