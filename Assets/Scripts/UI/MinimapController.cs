using UnityEngine;
using UnityEngine.UI;

// ================================================================================================
// [Summary] MinimapController
// 화면 오른쪽 위에 미니맵을 띄운다(2026-09-04 신규, 사용자 요청: "미니맵은 있으면 좋겠는데").
// 별도로 타일 텍스처를 굽거나 셀 단위로 다시 그리는 로직을 짜는 대신, 실제 맵을 위에서 내려다
// 보는 두 번째 Camera를 하나 더 만들어서 그 결과를 RenderTexture에 그린 뒤 화면 UI(RawImage)로
// 보여주는 표준적인 방식을 쓴다 — 실제 맵 비주얼(벽/바닥/이미 밝혀진 Fog of War 상태 포함)이
// 그대로 축소되어 보인다는 장점이 있고, 맵 데이터 구조(FloorLayout 등)에 전혀 손 댈 필요가 없다.
//
// Player를 따라다니며 항상 Player가 화면 중앙에 오도록 매 프레임 위치만 갱신한다. 회전은 고정 —
// 항상 월드 기준 정북 방향(캐릭터가 도는 대로 미니맵이 같이 도는 방식은 채택 안 함, 로그라이크
// 미니맵의 흔한 관례를 따름).
//
// 카메라 좌표계는 CameraFollow.cs와 동일한 관례를 따른다 — 2D 스프라이트는 Z=0, 카메라는 그보다
// -Z 방향으로 떨어진 곳에서 기본 회전(항등 회전)으로 바라본다.
//
// [2026-09-04 하이라키 배치 지원] Unity MCP 에디터 연결이 아직 안 잡혀서, 에디터 메뉴 스크립트
// (BuildHudInScene.cs)로 Edit 모드에서 직접 만들어 씬에 저장해두는 방식으로 대신한다.
// [ExecuteAlways]를 붙여서 Edit 모드에서도 Awake가 돌게 하고, Build()는 "이미 씬에 만들어져
// 있으면 새로 만들지 않고 기존 카메라/프레임에서 참조만 다시 찾아 연결"하도록 멱등하게 짰다
// (BindExisting). RenderTexture는 GPU 전용 런타임 리소스라 씬 저장/재로드 과정에서 유실될 수
// 있어서, 카메라에 연결된 텍스처가 없으면 새로 만들어서 다시 꽂아준다. LateUpdate의 플레이어
// 추적 로직은 Application.isPlaying으로 막아서 에디터에서는 실행되지 않는다.
// ================================================================================================
[ExecuteAlways]
public class MinimapController : MonoBehaviour
{
    [SerializeField] private Vector2 displaySize = new Vector2(220f, 220f);
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-24f, -24f);
    [Tooltip("미니맵 카메라의 orthographic size — 작을수록 확대(가까이), 클수록 축소(넓게 보임).")]
    [SerializeField] private float orthographicSize = 8f;
    [Tooltip("Player 기준 카메라가 -Z 방향으로 떨어진 거리 — CameraFollow의 cameraZ(-10) 관례와 동일한 목적(Near Clip 회피).")]
    [SerializeField] private float cameraDistance = 10f;

    private Camera minimapCamera;
    private Transform followTarget;
    private RenderTexture renderTexture;

    private void Awake()
    {
        Build();
    }

    // [2026-09-04 신규] 에디터 메뉴(BuildHudInScene)가 "컴포넌트는 이미 붙어있는데(Awake가 다시
    // 안 불림) 만들어뒀던 하이라키 자식만 지워진" 경우를 복구할 수 있게 Build()를 외부에서
    // 강제로 다시 부를 수 있게 노출한다. Build() 자체가 멱등하므로(있으면 재연결, 없으면 새로
    // 생성) 몇 번을 불러도 안전하다.
    public void RebuildForEditor()
    {
        Build();
    }

    // [2026-09-04 버그 수정] 코드 리뷰에서 발견 — 미니맵 카메라 GameObject가 this.transform의
    // 자식이 아니라 씬 루트에 독립적으로 떠 있었고, RenderTexture도 해제하는 코드가 없었다.
    // 그래서 MinimapController가 파괴돼도(씬 전환 등) 카메라와 RenderTexture(GPU 메모리)가
    // 그대로 남아 계속 렌더링을 수행하는 누수가 있었다 — 플레이 세션을 반복할수록 고아 카메라가
    // 쌓인다. 카메라를 자식으로 부모 지정해서 GameObject 생명주기를 같이 묶고, RenderTexture는
    // GameObject가 아니라 별도 에셋이라 Release() + Destroy()를 직접 호출해야 한다.
    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            if (minimapCamera != null) minimapCamera.targetTexture = null;
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return; // 에디터에서는 플레이어 추적 안 함([ExecuteAlways] 대비).

        if (followTarget == null)
        {
            // Player가 아직 씬에 안 만들어졌을 수도 있는 초기화 순서 대비 — 매 프레임 가볍게
            // 재시도(Instance 참조 확인뿐이라 비용 거의 없음).
            if (PlayerTurnActor.Instance != null)
            {
                followTarget = PlayerTurnActor.Instance.transform;
            }
            return;
        }

        var pos = followTarget.position;
        minimapCamera.transform.position = new Vector3(pos.x, pos.y, pos.z - cameraDistance);
    }

    private void Build()
    {
        var hudRootForCheck = ScreenSpaceCanvasProvider.GetOrCreateHudRoot();
        var existingCamera = transform.Find("MinimapCamera");
        var existingFrame = hudRootForCheck.Find("MinimapFrame");
        if (existingCamera != null && existingFrame != null)
        {
            BindExisting(existingCamera, existingFrame);
            return;
        }

        var cameraGo = new GameObject("MinimapCamera");
        cameraGo.transform.SetParent(transform, false); // [2026-09-04 버그 수정] 생명주기를 이 컴포넌트에 묶는다.
        minimapCamera = cameraGo.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = orthographicSize;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = Color.black;
        minimapCamera.depth = -10f; // 메인 카메라보다 먼저 계산되지만 화면에 직접 그려지진 않고 RT로만 나간다.

        // UI 레이어가 있다면 미니맵 카메라가 다시 그리지 않도록 제외한다 — 미니맵 안에 또 다른
        // 화면 UI가 겹쳐서 찍히는 재귀적인 상황을 막기 위함. 이 프로젝트에 "UI" 레이어가 아직
        // 없으면 LayerMask.NameToLayer가 -1을 반환해서 아래 비트 연산은 그냥 아무 영향이 없다.
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            minimapCamera.cullingMask &= ~(1 << uiLayer);
        }

        renderTexture = new RenderTexture(512, 512, 16);
        minimapCamera.targetTexture = renderTexture;

        var hudRoot = hudRootForCheck;

        var frameGo = new GameObject("MinimapFrame");
        frameGo.transform.SetParent(hudRoot, false);
        var frameRect = frameGo.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(1f, 1f);
        frameRect.anchorMax = new Vector2(1f, 1f);
        frameRect.pivot = new Vector2(1f, 1f);
        frameRect.anchoredPosition = anchoredPosition;
        frameRect.sizeDelta = displaySize;

        // 테두리 역할의 배경 — RawImage보다 살짝 크게 깔아서 액자처럼 보이게 한다.
        var borderImage = frameGo.AddComponent<Image>();
        borderImage.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        borderImage.color = new Color(1f, 1f, 1f, 0.6f);

        var rawGo = new GameObject("MinimapView");
        rawGo.transform.SetParent(frameGo.transform, false);
        var rawRect = rawGo.AddComponent<RectTransform>();
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = new Vector2(3f, 3f); // 테두리 두께만큼 안쪽으로.
        rawRect.offsetMax = new Vector2(-3f, -3f);

        var rawImage = rawGo.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
    }

    // [2026-09-04 신규] 이미 씬에 만들어져 있는 미니맵 카메라/프레임에서 참조만 다시 찾아
    // 연결한다(Build() 상단의 멱등성 체크 참고). RenderTexture는 GPU 전용 런타임 리소스라 씬을
    // 저장했다가 다시 열면 유실될 수 있어서, 카메라에 연결된 텍스처가 없으면 새로 만들어서
    // 카메라와 RawImage 양쪽에 다시 꽂아준다.
    private void BindExisting(Transform cameraTransform, Transform frameTransform)
    {
        minimapCamera = cameraTransform.GetComponent<Camera>();
        renderTexture = minimapCamera != null ? minimapCamera.targetTexture as RenderTexture : null;

        if (minimapCamera != null && renderTexture == null)
        {
            renderTexture = new RenderTexture(512, 512, 16);
            minimapCamera.targetTexture = renderTexture;
        }

        var rawImageTransform = frameTransform.Find("MinimapView");
        var rawImage = rawImageTransform != null ? rawImageTransform.GetComponent<RawImage>() : null;
        if (rawImage != null) rawImage.texture = renderTexture;

        if (minimapCamera == null || rawImage == null)
        {
            Debug.LogWarning("[MinimapController] 기존 하이라키에서 일부 참조를 못 찾았습니다 — " +
                              "씬의 MinimapCamera/MinimapFrame을 지우고 다시 배치해보세요.");
        }
    }
}
