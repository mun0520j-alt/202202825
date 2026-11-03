using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ================================================================================================
// [Summary] ScreenSpaceCanvasProvider
// HUD류(시계, 인벤토리, 미니맵 등) 여러 컴포넌트가 각자 Canvas를 새로 만들면 화면에 Canvas가
// 여러 개 겹쳐서 정렬 순서 관리가 지저분해진다. 그래서 씬에 이미 있으면 그걸 재사용하고, 없으면
// 딱 하나만 새로 만들어서 공유하는 진입점을 이 클래스 하나로 통일한다(2026-09-04 신규, "UI 배치"
// 작업 — 시계/인벤토리/미니맵을 한 번에 추가하면서 공용 Canvas가 필요해져서 분리).
//
// AlertIndicator/HP바처럼 이미 확립된 "완전 자체 생성" 패턴을 그대로 따른다 — Inspector 세팅이나
// 프리팹 준비 없이, 아무 씬에나 이 HUD 컴포넌트들을 붙이기만 하면 알아서 Canvas까지 만들어 붙는다.
// ================================================================================================
public static class ScreenSpaceCanvasProvider
{
    private static Canvas cachedCanvas;

    public static Transform GetOrCreateHudRoot()
    {
        if (cachedCanvas != null) return cachedCanvas.transform;

        // 씬 리로드 등으로 캐시가 끊긴 참조를 들고 있을 수도 있어서, 실제로 씬에 있는지도 확인한다.
        cachedCanvas = Object.FindObjectOfType<Canvas>();
        if (cachedCanvas == null)
        {
            var canvasGo = new GameObject("HudCanvas(AutoCreated)");
            cachedCanvas = canvasGo.AddComponent<Canvas>();
            cachedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // [2026-09-04 버그 수정] 코드 리뷰에서 발견 — 이 EventSystem 생성 체크가 원래 "새 Canvas를
        // 만드는" 위 if 블록 안에만 있었다. 그래서 씬에 이미 Canvas는 있는데 EventSystem은 없는
        // 경우(예: 다른 시스템이 Canvas만 미리 만들어둔 씬)는 이 함수를 여러 번 호출해도 여기까지
        // 도달을 못 해서 EventSystem이 영영 안 생겼다. Canvas 신규/재사용 여부와 무관하게 항상
        // 확인하도록 블록 밖으로 뺐다 — 인벤토리 패널처럼 나중에 클릭 상호작용이 필요한 UI를
        // 대비해서 미리 준비해두는 용도(3단계 아이템 슬롯 클릭에서 실제로 쓰일 예정).
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemGo = new GameObject("EventSystem(AutoCreated)");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        return cachedCanvas.transform;
    }
}
